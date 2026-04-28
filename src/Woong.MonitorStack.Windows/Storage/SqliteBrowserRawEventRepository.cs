using System.Globalization;
using Microsoft.Data.Sqlite;
using Woong.MonitorStack.Windows.Browser;

namespace Woong.MonitorStack.Windows.Storage;

public sealed class SqliteBrowserRawEventRepository
{
    private readonly string _connectionString;

    public SqliteBrowserRawEventRepository(string connectionString)
    {
        _connectionString = string.IsNullOrWhiteSpace(connectionString)
            ? throw new ArgumentException("Value must not be empty.", nameof(connectionString))
            : connectionString;
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
                url TEXT NOT NULL,
                title TEXT NOT NULL,
                domain TEXT NOT NULL,
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
        AddParameters(command, message);
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

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static void AddParameters(SqliteCommand command, ChromeTabChangedMessage message)
    {
        _ = command.Parameters.AddWithValue("$browserFamily", message.BrowserFamily);
        _ = command.Parameters.AddWithValue("$windowId", message.WindowId);
        _ = command.Parameters.AddWithValue("$tabId", message.TabId);
        _ = command.Parameters.AddWithValue("$url", message.Url);
        _ = command.Parameters.AddWithValue("$title", message.Title);
        _ = command.Parameters.AddWithValue("$domain", message.Domain);
        _ = command.Parameters.AddWithValue("$observedAtUtc", FormatUtc(message.ObservedAtUtc));
    }

    private static ChromeTabChangedMessage ReadMessage(SqliteDataReader reader)
        => ChromeTabChangedMessage.FromExtensionPayload(
            windowId: reader.GetInt32(1),
            tabId: reader.GetInt32(2),
            url: reader.GetString(3),
            title: reader.GetString(4),
            observedAtUtc: DateTimeOffset.Parse(reader.GetString(5), CultureInfo.InvariantCulture),
            browserFamily: reader.GetString(0));

    private static string FormatUtc(DateTimeOffset value)
        => value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
}
