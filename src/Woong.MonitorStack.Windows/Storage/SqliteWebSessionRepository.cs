using System.Globalization;
using Microsoft.Data.Sqlite;
using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Windows.Storage;

public sealed class SqliteWebSessionRepository
{
    private readonly string _connectionString;

    public SqliteWebSessionRepository(string connectionString)
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
            CREATE TABLE IF NOT EXISTS web_session (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                focus_session_id TEXT NOT NULL,
                browser_family TEXT NOT NULL,
                url TEXT NOT NULL,
                domain TEXT NOT NULL,
                page_title TEXT NOT NULL,
                started_at_utc TEXT NOT NULL,
                ended_at_utc TEXT NOT NULL,
                duration_ms INTEGER NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_web_session_focus_session_id
                ON web_session(focus_session_id);
            """;
        _ = command.ExecuteNonQuery();
    }

    public void Save(WebSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO web_session (
                focus_session_id,
                browser_family,
                url,
                domain,
                page_title,
                started_at_utc,
                ended_at_utc,
                duration_ms
            ) VALUES (
                $focusSessionId,
                $browserFamily,
                $url,
                $domain,
                $pageTitle,
                $startedAtUtc,
                $endedAtUtc,
                $durationMs
            );
            """;
        AddParameters(command, session);
        _ = command.ExecuteNonQuery();
    }

    public IReadOnlyList<WebSession> QueryByFocusSessionId(string focusSessionId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                focus_session_id,
                browser_family,
                url,
                domain,
                page_title,
                started_at_utc,
                ended_at_utc
            FROM web_session
            WHERE focus_session_id = $focusSessionId
            ORDER BY started_at_utc;
            """;
        _ = command.Parameters.AddWithValue("$focusSessionId", focusSessionId);

        using var reader = command.ExecuteReader();
        var sessions = new List<WebSession>();
        while (reader.Read())
        {
            sessions.Add(ReadSession(reader));
        }

        return sessions;
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static void AddParameters(SqliteCommand command, WebSession session)
    {
        _ = command.Parameters.AddWithValue("$focusSessionId", session.FocusSessionId);
        _ = command.Parameters.AddWithValue("$browserFamily", session.BrowserFamily);
        _ = command.Parameters.AddWithValue("$url", session.Url);
        _ = command.Parameters.AddWithValue("$domain", session.Domain);
        _ = command.Parameters.AddWithValue("$pageTitle", session.PageTitle);
        _ = command.Parameters.AddWithValue("$startedAtUtc", FormatUtc(session.StartedAtUtc));
        _ = command.Parameters.AddWithValue("$endedAtUtc", FormatUtc(session.EndedAtUtc));
        _ = command.Parameters.AddWithValue("$durationMs", session.DurationMs);
    }

    private static WebSession ReadSession(SqliteDataReader reader)
        => new(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            TimeRange.FromUtc(
                DateTimeOffset.Parse(reader.GetString(5), CultureInfo.InvariantCulture),
                DateTimeOffset.Parse(reader.GetString(6), CultureInfo.InvariantCulture)));

    private static string FormatUtc(DateTimeOffset value)
        => value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
}
