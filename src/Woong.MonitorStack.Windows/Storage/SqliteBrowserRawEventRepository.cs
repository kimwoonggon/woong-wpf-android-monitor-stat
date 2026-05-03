using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using Woong.MonitorStack.Windows.Browser;

namespace Woong.MonitorStack.Windows.Storage;

public sealed class SqliteBrowserRawEventRepository
{
    private readonly Func<string> _connectionStringFactory;

    public SqliteBrowserRawEventRepository(string connectionString)
        : this(() => connectionString)
    {
    }

    public SqliteBrowserRawEventRepository(Func<string> connectionStringFactory)
    {
        _connectionStringFactory = connectionStringFactory ?? throw new ArgumentNullException(nameof(connectionStringFactory));
    }

    public void Initialize()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS browser_raw_event (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                client_event_id TEXT NOT NULL,
                browser_family TEXT NOT NULL,
                window_id INTEGER NOT NULL,
                tab_id INTEGER NOT NULL,
                url TEXT NULL,
                title TEXT NULL,
                domain TEXT NULL,
                observed_at_utc TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_browser_raw_event_tab_time
                ON browser_raw_event(tab_id, observed_at_utc);
            """;
        _ = command.ExecuteNonQuery();
        EnsureClientEventIdColumn(connection);
        BackfillLegacyClientEventIds(connection);
        EnsureClientEventIdRequiredSchema(connection);
        EnsureTabTimeIndex(connection);
        EnsureClientEventIdIndex(connection);
    }

    public void Save(ChromeTabChangedMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        Save(new BrowserRawEventRecord(
            message.BrowserFamily,
            message.WindowId,
            message.TabId,
            message.Url,
            message.Title,
            message.Domain,
            message.ObservedAtUtc,
            message.ClientEventId));
    }

    public void Save(BrowserRawEventRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT OR IGNORE INTO browser_raw_event (
                client_event_id,
                browser_family,
                window_id,
                tab_id,
                url,
                title,
                domain,
                observed_at_utc
            ) VALUES (
                $clientEventId,
                $browserFamily,
                $windowId,
                $tabId,
                $url,
                $title,
                $domain,
                $observedAtUtc
            );
            """;
        AddParameters(command, record);
        _ = command.ExecuteNonQuery();
    }

    public IReadOnlyList<ChromeTabChangedMessage> QueryByTabId(int tabId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                client_event_id,
                browser_family,
                window_id,
                tab_id,
                url,
                title,
                observed_at_utc
            FROM browser_raw_event
            WHERE tab_id = $tabId
            ORDER BY observed_at_utc;
            """;
        _ = command.Parameters.AddWithValue("$tabId", tabId);

        using var reader = command.ExecuteReader();
        var messages = new List<ChromeTabChangedMessage>();
        while (reader.Read())
        {
            messages.Add(ReadMessage(reader));
        }

        return messages;
    }

    public IReadOnlyList<BrowserRawEventRecord> QueryRecordsByTabId(int tabId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                client_event_id,
                browser_family,
                window_id,
                tab_id,
                url,
                title,
                domain,
                observed_at_utc
            FROM browser_raw_event
            WHERE tab_id = $tabId
            ORDER BY observed_at_utc;
            """;
        _ = command.Parameters.AddWithValue("$tabId", tabId);

        using var reader = command.ExecuteReader();
        var records = new List<BrowserRawEventRecord>();
        while (reader.Read())
        {
            records.Add(ReadRecord(reader));
        }

        return records;
    }

    public int DeleteOlderThan(DateTimeOffset cutoffUtc)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM browser_raw_event
            WHERE observed_at_utc < $cutoffUtc;
            """;
        _ = command.Parameters.AddWithValue("$cutoffUtc", FormatUtc(cutoffUtc));

        return command.ExecuteNonQuery();
    }

    private SqliteConnection OpenConnection()
    {
        string connectionString = _connectionStringFactory();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("SQLite connection string must not be empty.");
        }

        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return connection;
    }

    private static void AddParameters(SqliteCommand command, BrowserRawEventRecord record)
    {
        _ = command.Parameters.AddWithValue("$clientEventId", ResolveClientEventId(record));
        _ = command.Parameters.AddWithValue("$browserFamily", record.BrowserFamily);
        _ = command.Parameters.AddWithValue("$windowId", record.WindowId);
        _ = command.Parameters.AddWithValue("$tabId", record.TabId);
        _ = command.Parameters.AddWithValue("$url", ToDbValue(record.Url));
        _ = command.Parameters.AddWithValue("$title", ToDbValue(record.Title));
        _ = command.Parameters.AddWithValue("$domain", ToDbValue(record.Domain));
        _ = command.Parameters.AddWithValue("$observedAtUtc", FormatUtc(record.ObservedAtUtc));
    }

    private static ChromeTabChangedMessage ReadMessage(SqliteDataReader reader)
        => ChromeTabChangedMessage.FromExtensionPayload(
            windowId: reader.GetInt32(2),
            tabId: reader.GetInt32(3),
            url: reader.GetString(4),
            title: reader.GetString(5),
            observedAtUtc: DateTimeOffset.Parse(reader.GetString(6), CultureInfo.InvariantCulture),
            browserFamily: reader.GetString(1),
            clientEventId: reader.GetString(0));

    private static BrowserRawEventRecord ReadRecord(SqliteDataReader reader)
        => new(
            reader.GetString(1),
            reader.GetInt32(2),
            reader.GetInt32(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            reader.IsDBNull(5) ? null : reader.GetString(5),
            reader.IsDBNull(6) ? null : reader.GetString(6),
            DateTimeOffset.Parse(reader.GetString(7), CultureInfo.InvariantCulture),
            reader.GetString(0));

    private static string FormatUtc(DateTimeOffset value)
        => value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private static object ToDbValue(string? value)
        => value is null ? DBNull.Value : value;

    private static void EnsureClientEventIdColumn(SqliteConnection connection)
    {
        if (ColumnExists(connection, "client_event_id"))
        {
            return;
        }

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "ALTER TABLE browser_raw_event ADD COLUMN client_event_id TEXT NULL;";
        _ = command.ExecuteNonQuery();
    }

    private static bool ColumnExists(SqliteConnection connection, string columnName)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(browser_raw_event);";

        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void BackfillLegacyClientEventIds(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            UPDATE browser_raw_event
            SET client_event_id = 'legacy-browser-raw-event:' || id
            WHERE client_event_id IS NULL
               OR TRIM(client_event_id) = '';
            """;
        _ = command.ExecuteNonQuery();
    }

    private static void EnsureClientEventIdIndex(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            CREATE UNIQUE INDEX IF NOT EXISTS ux_browser_raw_event_client_event_id
                ON browser_raw_event(client_event_id);
        """;
        _ = command.ExecuteNonQuery();
    }

    private static void EnsureTabTimeIndex(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            CREATE INDEX IF NOT EXISTS ix_browser_raw_event_tab_time
                ON browser_raw_event(tab_id, observed_at_utc);
            """;
        _ = command.ExecuteNonQuery();
    }

    private static void EnsureClientEventIdRequiredSchema(SqliteConnection connection)
    {
        if (ColumnIsRequired(connection, "client_event_id"))
        {
            return;
        }

        using SqliteTransaction transaction = connection.BeginTransaction();
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            CREATE TABLE browser_raw_event_rebuild (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                client_event_id TEXT NOT NULL,
                browser_family TEXT NOT NULL,
                window_id INTEGER NOT NULL,
                tab_id INTEGER NOT NULL,
                url TEXT NULL,
                title TEXT NULL,
                domain TEXT NULL,
                observed_at_utc TEXT NOT NULL
            );

            INSERT INTO browser_raw_event_rebuild (
                id,
                client_event_id,
                browser_family,
                window_id,
                tab_id,
                url,
                title,
                domain,
                observed_at_utc
            )
            SELECT
                id,
                client_event_id,
                browser_family,
                window_id,
                tab_id,
                url,
                title,
                domain,
                observed_at_utc
            FROM browser_raw_event;

            DROP TABLE browser_raw_event;
            ALTER TABLE browser_raw_event_rebuild RENAME TO browser_raw_event;
            """;
        _ = command.ExecuteNonQuery();
        transaction.Commit();
    }

    private static bool ColumnIsRequired(SqliteConnection connection, string columnName)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(browser_raw_event);";

        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return reader.GetInt32(3) == 1;
            }
        }

        return false;
    }

    private static string ResolveClientEventId(BrowserRawEventRecord record)
        => string.IsNullOrWhiteSpace(record.ClientEventId)
            ? DeriveMetadataOnlyClientEventId(record)
            : record.ClientEventId.Trim();

    private static string DeriveMetadataOnlyClientEventId(BrowserRawEventRecord record)
    {
        string stableMetadata = string.Join(
            "\n",
            record.BrowserFamily.Trim(),
            record.WindowId.ToString(CultureInfo.InvariantCulture),
            record.TabId.ToString(CultureInfo.InvariantCulture),
            (record.Domain ?? "").Trim().ToUpperInvariant(),
            record.ObservedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(stableMetadata));
        return $"browser-raw-event:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}
