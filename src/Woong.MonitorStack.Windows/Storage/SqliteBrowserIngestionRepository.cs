using System.Globalization;
using Microsoft.Data.Sqlite;
using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Windows.Storage;

public sealed class SqliteBrowserIngestionRepository
{
    private readonly Func<string> _connectionStringFactory;

    public SqliteBrowserIngestionRepository(string connectionString)
        : this(() => connectionString)
    {
    }

    public SqliteBrowserIngestionRepository(Func<string> connectionStringFactory)
    {
        _connectionStringFactory = connectionStringFactory ?? throw new ArgumentNullException(nameof(connectionStringFactory));
    }

    public bool IngestInsertedRawEvent(
        BrowserRawEventRecord rawEvent,
        DateTimeOffset? retentionCutoffUtc,
        Func<IReadOnlyList<WebSession>> createCompletedSessions,
        Func<WebSession, SyncOutboxItem?> createOutboxItem)
    {
        ArgumentNullException.ThrowIfNull(rawEvent);
        ArgumentNullException.ThrowIfNull(createCompletedSessions);
        ArgumentNullException.ThrowIfNull(createOutboxItem);

        using var connection = OpenConnection();
        using SqliteTransaction transaction = connection.BeginTransaction();

        if (InsertRawEvent(connection, transaction, rawEvent) == 0)
        {
            transaction.Commit();
            return false;
        }

        if (retentionCutoffUtc is not null)
        {
            DeleteRawEventsOlderThan(connection, transaction, retentionCutoffUtc.Value);
        }

        foreach (WebSession session in createCompletedSessions())
        {
            int webRowsInserted = InsertWebSession(connection, transaction, session);
            if (webRowsInserted == 0)
            {
                continue;
            }

            SyncOutboxItem? outboxItem = createOutboxItem(session);
            if (outboxItem is null)
            {
                continue;
            }

            int outboxRowsInserted = SqliteSyncOutboxCommands.Add(connection, transaction, outboxItem);
            if (outboxRowsInserted == 0
                && !SqliteSyncOutboxCommands.Exists(connection, transaction, outboxItem.Id))
            {
                throw new InvalidOperationException("Browser ingestion outbox enqueue failed; local browser persistence was rolled back.");
            }
        }

        transaction.Commit();
        return true;
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

    private static int InsertRawEvent(
        SqliteConnection connection,
        SqliteTransaction transaction,
        BrowserRawEventRecord record)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO browser_raw_event (
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
            )
            ON CONFLICT(client_event_id) DO NOTHING;
            """;
        _ = command.Parameters.AddWithValue("$clientEventId", RequiredStorageText.Ensure(record.ClientEventId, nameof(record.ClientEventId)));
        _ = command.Parameters.AddWithValue("$browserFamily", record.BrowserFamily);
        _ = command.Parameters.AddWithValue("$windowId", record.WindowId);
        _ = command.Parameters.AddWithValue("$tabId", record.TabId);
        _ = command.Parameters.AddWithValue("$url", ToDbValue(record.Url));
        _ = command.Parameters.AddWithValue("$title", ToDbValue(record.Title));
        _ = command.Parameters.AddWithValue("$domain", ToDbValue(record.Domain));
        _ = command.Parameters.AddWithValue("$observedAtUtc", FormatUtc(record.ObservedAtUtc));
        return command.ExecuteNonQuery();
    }

    private static int InsertWebSession(
        SqliteConnection connection,
        SqliteTransaction transaction,
        WebSession session)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
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
            ON CONFLICT(focus_session_id, started_at_utc) DO NOTHING;
            """;
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
        return command.ExecuteNonQuery();
    }

    private static int DeleteRawEventsOlderThan(
        SqliteConnection connection,
        SqliteTransaction transaction,
        DateTimeOffset cutoffUtc)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            DELETE FROM browser_raw_event
            WHERE observed_at_utc < $cutoffUtc;
            """;
        _ = command.Parameters.AddWithValue("$cutoffUtc", FormatUtc(cutoffUtc));
        return command.ExecuteNonQuery();
    }

    private static string FormatUtc(DateTimeOffset value)
        => value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private static object ToDbValue(string? value)
        => value is null ? DBNull.Value : value;

    private static object ToDbValue(bool? value)
        => value is null ? DBNull.Value : value.Value ? 1 : 0;
}
