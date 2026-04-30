using System.Globalization;
using Microsoft.Data.Sqlite;
using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Windows.Storage;

public sealed class SqliteFocusSessionRepository
{
    private readonly Func<string> _connectionStringFactory;

    public SqliteFocusSessionRepository(string connectionString)
        : this(() => connectionString)
    {
    }

    public SqliteFocusSessionRepository(Func<string> connectionStringFactory)
    {
        _connectionStringFactory = connectionStringFactory ?? throw new ArgumentNullException(nameof(connectionStringFactory));
    }

    public void Initialize()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS focus_session (
                client_session_id TEXT NOT NULL PRIMARY KEY,
                device_id TEXT NOT NULL,
                platform_app_key TEXT NOT NULL,
                started_at_utc TEXT NOT NULL,
                ended_at_utc TEXT NOT NULL,
                duration_ms INTEGER NOT NULL,
                local_date TEXT NOT NULL,
                timezone_id TEXT NOT NULL,
                is_idle INTEGER NOT NULL,
                source TEXT NOT NULL,
                process_id INTEGER NULL,
                process_name TEXT NULL,
                process_path TEXT NULL,
                window_handle INTEGER NULL,
                window_title TEXT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_focus_session_started_at_utc
                ON focus_session(started_at_utc);
            """;
        _ = command.ExecuteNonQuery();
        EnsureColumn(connection, "process_id", "INTEGER NULL");
        EnsureColumn(connection, "process_name", "TEXT NULL");
        EnsureColumn(connection, "process_path", "TEXT NULL");
        EnsureColumn(connection, "window_handle", "INTEGER NULL");
        EnsureColumn(connection, "window_title", "TEXT NULL");
    }

    public void Save(FocusSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT OR IGNORE INTO focus_session (
                client_session_id,
                device_id,
                platform_app_key,
                started_at_utc,
                ended_at_utc,
                duration_ms,
                local_date,
                timezone_id,
                is_idle,
                source,
                process_id,
                process_name,
                process_path,
                window_handle,
                window_title
            ) VALUES (
                $clientSessionId,
                $deviceId,
                $platformAppKey,
                $startedAtUtc,
                $endedAtUtc,
                $durationMs,
                $localDate,
                $timezoneId,
                $isIdle,
                $source,
                $processId,
                $processName,
                $processPath,
                $windowHandle,
                $windowTitle
            );
            """;
        AddCommonParameters(command, session);
        _ = command.ExecuteNonQuery();
    }

    public IReadOnlyList<FocusSession> QueryByRange(DateTimeOffset startedAtUtc, DateTimeOffset endedAtUtc)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                client_session_id,
                device_id,
                platform_app_key,
                started_at_utc,
                ended_at_utc,
                local_date,
                timezone_id,
                is_idle,
                source,
                process_id,
                process_name,
                process_path,
                window_handle,
                window_title
            FROM focus_session
            WHERE started_at_utc < $endedAtUtc
              AND ended_at_utc > $startedAtUtc
            ORDER BY started_at_utc;
            """;
        _ = command.Parameters.AddWithValue("$startedAtUtc", FormatUtc(startedAtUtc));
        _ = command.Parameters.AddWithValue("$endedAtUtc", FormatUtc(endedAtUtc));

        using var reader = command.ExecuteReader();
        var sessions = new List<FocusSession>();
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

    private static void AddCommonParameters(SqliteCommand command, FocusSession session)
    {
        _ = command.Parameters.AddWithValue("$clientSessionId", session.ClientSessionId);
        _ = command.Parameters.AddWithValue("$deviceId", session.DeviceId);
        _ = command.Parameters.AddWithValue("$platformAppKey", session.PlatformAppKey);
        _ = command.Parameters.AddWithValue("$startedAtUtc", FormatUtc(session.StartedAtUtc));
        _ = command.Parameters.AddWithValue("$endedAtUtc", FormatUtc(session.EndedAtUtc));
        _ = command.Parameters.AddWithValue("$durationMs", session.DurationMs);
        _ = command.Parameters.AddWithValue("$localDate", session.LocalDate.ToString("yyyy-MM-dd"));
        _ = command.Parameters.AddWithValue("$timezoneId", session.TimezoneId);
        _ = command.Parameters.AddWithValue("$isIdle", session.IsIdle ? 1 : 0);
        _ = command.Parameters.AddWithValue("$source", session.Source);
        _ = command.Parameters.AddWithValue("$processId", (object?)session.ProcessId ?? DBNull.Value);
        _ = command.Parameters.AddWithValue("$processName", (object?)session.ProcessName ?? DBNull.Value);
        _ = command.Parameters.AddWithValue("$processPath", (object?)session.ProcessPath ?? DBNull.Value);
        _ = command.Parameters.AddWithValue("$windowHandle", (object?)session.WindowHandle ?? DBNull.Value);
        _ = command.Parameters.AddWithValue("$windowTitle", (object?)session.WindowTitle ?? DBNull.Value);
    }

    private static FocusSession ReadSession(SqliteDataReader reader)
        => new(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            TimeRange.FromUtc(
                DateTimeOffset.Parse(reader.GetString(3), CultureInfo.InvariantCulture),
                DateTimeOffset.Parse(reader.GetString(4), CultureInfo.InvariantCulture)),
            DateOnly.ParseExact(reader.GetString(5), "yyyy-MM-dd", CultureInfo.InvariantCulture),
            reader.GetString(6),
            reader.GetInt32(7) == 1,
            reader.GetString(8),
            reader.IsDBNull(9) ? null : reader.GetInt32(9),
            reader.IsDBNull(10) ? null : reader.GetString(10),
            reader.IsDBNull(11) ? null : reader.GetString(11),
            reader.IsDBNull(12) ? null : reader.GetInt64(12),
            reader.IsDBNull(13) ? null : reader.GetString(13));

    private static string FormatUtc(DateTimeOffset value)
        => value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private static void EnsureColumn(SqliteConnection connection, string columnName, string columnDefinition)
    {
        using SqliteCommand readColumns = connection.CreateCommand();
        readColumns.CommandText = "PRAGMA table_info(focus_session);";

        using SqliteDataReader reader = readColumns.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        using SqliteCommand addColumn = connection.CreateCommand();
        addColumn.CommandText = $"ALTER TABLE focus_session ADD COLUMN {columnName} {columnDefinition};";
        _ = addColumn.ExecuteNonQuery();
    }
}
