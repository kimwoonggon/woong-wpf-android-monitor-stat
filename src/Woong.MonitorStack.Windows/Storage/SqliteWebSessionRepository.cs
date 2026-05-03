using System.Globalization;
using Microsoft.Data.Sqlite;
using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Windows.Storage;

public sealed class SqliteWebSessionRepository
{
    private readonly Func<string> _connectionStringFactory;

    public SqliteWebSessionRepository(string connectionString)
        : this(() => connectionString)
    {
    }

    public SqliteWebSessionRepository(Func<string> connectionStringFactory)
    {
        _connectionStringFactory = connectionStringFactory ?? throw new ArgumentNullException(nameof(connectionStringFactory));
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
                url TEXT NULL,
                domain TEXT NOT NULL,
                page_title TEXT NULL,
                started_at_utc TEXT NOT NULL,
                ended_at_utc TEXT NOT NULL,
                duration_ms INTEGER NOT NULL,
                capture_method TEXT NULL,
                capture_confidence TEXT NULL,
                is_private_or_unknown INTEGER NULL
            );

            CREATE INDEX IF NOT EXISTS ix_web_session_focus_session_id
                ON web_session(focus_session_id);
            """;
        _ = command.ExecuteNonQuery();
        EnsureNullableColumn(connection, "capture_method", "TEXT NULL");
        EnsureNullableColumn(connection, "capture_confidence", "TEXT NULL");
        EnsureNullableColumn(connection, "is_private_or_unknown", "INTEGER NULL");
        EnsureUrlAllowsNull(connection);
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
                duration_ms,
                capture_method,
                capture_confidence,
                is_private_or_unknown
            )
            SELECT
                $focusSessionId,
                $browserFamily,
                $url,
                $domain,
                $pageTitle,
                $startedAtUtc,
                $endedAtUtc,
                $durationMs,
                $captureMethod,
                $captureConfidence,
                $isPrivateOrUnknown
            WHERE NOT EXISTS (
                SELECT 1
                FROM web_session
                WHERE focus_session_id = $focusSessionId
                  AND started_at_utc = $startedAtUtc
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
                ended_at_utc,
                capture_method,
                capture_confidence,
                is_private_or_unknown
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

    public IReadOnlyList<WebSession> QueryByRange(DateTimeOffset startedAtUtc, DateTimeOffset endedAtUtc)
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
                ended_at_utc,
                capture_method,
                capture_confidence,
                is_private_or_unknown
            FROM web_session
            WHERE started_at_utc < $endedAtUtc
              AND ended_at_utc > $startedAtUtc
            ORDER BY started_at_utc;
            """;
        _ = command.Parameters.AddWithValue("$startedAtUtc", FormatUtc(startedAtUtc));
        _ = command.Parameters.AddWithValue("$endedAtUtc", FormatUtc(endedAtUtc));

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
        string connectionString = _connectionStringFactory();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("SQLite connection string must not be empty.");
        }

        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return connection;
    }

    private static void EnsureNullableColumn(
        SqliteConnection connection,
        string columnName,
        string columnDefinition)
    {
        if (HasColumn(connection, columnName))
        {
            return;
        }

        using SqliteCommand alterCommand = connection.CreateCommand();
        alterCommand.CommandText = $"ALTER TABLE web_session ADD COLUMN {columnName} {columnDefinition};";
        _ = alterCommand.ExecuteNonQuery();
    }

    private static bool HasColumn(SqliteConnection connection, string columnName)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(web_session);";

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

    private static void EnsureUrlAllowsNull(SqliteConnection connection)
    {
        if (!IsColumnNotNull(connection, "url"))
        {
            return;
        }

        RebuildTableWithNullableUrl(connection);
    }

    private static bool IsColumnNotNull(SqliteConnection connection, string columnName)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(web_session);";

        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return reader.GetInt64(3) == 1;
            }
        }

        return false;
    }

    private static void RebuildTableWithNullableUrl(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA foreign_keys=OFF;

            ALTER TABLE web_session RENAME TO web_session_legacy_rebuild;

            CREATE TABLE web_session (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                focus_session_id TEXT NOT NULL,
                browser_family TEXT NOT NULL,
                url TEXT NULL,
                domain TEXT NOT NULL,
                page_title TEXT NULL,
                started_at_utc TEXT NOT NULL,
                ended_at_utc TEXT NOT NULL,
                duration_ms INTEGER NOT NULL,
                capture_method TEXT NULL,
                capture_confidence TEXT NULL,
                is_private_or_unknown INTEGER NULL
            );

            INSERT INTO web_session (
                id,
                focus_session_id,
                browser_family,
                url,
                domain,
                page_title,
                started_at_utc,
                ended_at_utc,
                duration_ms,
                capture_method,
                capture_confidence,
                is_private_or_unknown
            )
            SELECT
                id,
                focus_session_id,
                browser_family,
                url,
                domain,
                page_title,
                started_at_utc,
                ended_at_utc,
                duration_ms,
                capture_method,
                capture_confidence,
                is_private_or_unknown
            FROM web_session_legacy_rebuild;

            DROP TABLE web_session_legacy_rebuild;

            CREATE INDEX IF NOT EXISTS ix_web_session_focus_session_id
                ON web_session(focus_session_id);

            PRAGMA foreign_keys=ON;
            """;
        _ = command.ExecuteNonQuery();
    }

    private static void AddParameters(SqliteCommand command, WebSession session)
    {
        _ = command.Parameters.AddWithValue("$focusSessionId", session.FocusSessionId);
        _ = command.Parameters.AddWithValue("$browserFamily", session.BrowserFamily);
        _ = command.Parameters.AddWithValue("$url", ToDbValue(session.Url));
        _ = command.Parameters.AddWithValue("$domain", session.Domain);
        _ = command.Parameters.AddWithValue("$pageTitle", ToDbValue(session.PageTitle));
        _ = command.Parameters.AddWithValue("$startedAtUtc", FormatUtc(session.StartedAtUtc));
        _ = command.Parameters.AddWithValue("$endedAtUtc", FormatUtc(session.EndedAtUtc));
        _ = command.Parameters.AddWithValue("$durationMs", session.DurationMs);
        _ = command.Parameters.AddWithValue("$captureMethod", ToDbValue(session.CaptureMethod));
        _ = command.Parameters.AddWithValue("$captureConfidence", ToDbValue(session.CaptureConfidence));
        _ = command.Parameters.AddWithValue("$isPrivateOrUnknown", ToDbValue(session.IsPrivateOrUnknown));
    }

    private static WebSession ReadSession(SqliteDataReader reader)
        => new(
            reader.GetString(0),
            reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            TimeRange.FromUtc(
                DateTimeOffset.Parse(reader.GetString(5), CultureInfo.InvariantCulture),
                DateTimeOffset.Parse(reader.GetString(6), CultureInfo.InvariantCulture)),
            reader.IsDBNull(7) ? null : reader.GetString(7),
            reader.IsDBNull(8) ? null : reader.GetString(8),
            reader.IsDBNull(9) ? null : reader.GetInt64(9) == 1);

    private static string FormatUtc(DateTimeOffset value)
        => value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private static object ToDbValue(string? value)
        => value is null ? DBNull.Value : value;

    private static object ToDbValue(bool? value)
        => value is null ? DBNull.Value : value.Value ? 1 : 0;
}
