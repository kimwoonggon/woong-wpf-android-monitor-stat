using System.Globalization;
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
            message.ObservedAtUtc));
    }

    public void Save(BrowserRawEventRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO browser_raw_event (
                browser_family,
                window_id,
                tab_id,
                url,
                title,
                domain,
                observed_at_utc
            ) VALUES (
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
            windowId: reader.GetInt32(1),
            tabId: reader.GetInt32(2),
            url: reader.GetString(3),
            title: reader.GetString(4),
            observedAtUtc: DateTimeOffset.Parse(reader.GetString(5), CultureInfo.InvariantCulture),
            browserFamily: reader.GetString(0));

    private static BrowserRawEventRecord ReadRecord(SqliteDataReader reader)
        => new(
            reader.GetString(0),
            reader.GetInt32(1),
            reader.GetInt32(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            reader.IsDBNull(5) ? null : reader.GetString(5),
            DateTimeOffset.Parse(reader.GetString(6), CultureInfo.InvariantCulture));

    private static string FormatUtc(DateTimeOffset value)
        => value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private static object ToDbValue(string? value)
        => value is null ? DBNull.Value : value;
}
