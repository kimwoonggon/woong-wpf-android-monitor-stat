using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Woong.MonitorStack.Windows.Storage;

internal static class SqliteSyncOutboxCommands
{
    public static int Add(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        SyncOutboxItem item)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT OR IGNORE INTO sync_outbox (
                id,
                aggregate_type,
                aggregate_id,
                payload_json,
                status,
                retry_count,
                created_at_utc,
                synced_at_utc,
                last_error
            ) VALUES (
                $id,
                $aggregateType,
                $aggregateId,
                $payloadJson,
                $status,
                $retryCount,
                $createdAtUtc,
                $syncedAtUtc,
                $lastError
            );
            """;
        AddParameters(command, item);
        int rowsInserted = command.ExecuteNonQuery();
        if (rowsInserted > 0)
        {
            return rowsInserted;
        }

        return ExistsAggregateIdentity(connection, transaction, item.AggregateType, item.AggregateId)
            ? 1
            : 0;
    }

    public static bool Exists(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        string id)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT 1
            FROM sync_outbox
            WHERE id = $id
            LIMIT 1;
            """;
        _ = command.Parameters.AddWithValue("$id", id);
        return command.ExecuteScalar() is not null;
    }

    private static void AddParameters(SqliteCommand command, SyncOutboxItem item)
    {
        _ = command.Parameters.AddWithValue("$id", item.Id);
        _ = command.Parameters.AddWithValue("$aggregateType", item.AggregateType);
        _ = command.Parameters.AddWithValue("$aggregateId", item.AggregateId);
        _ = command.Parameters.AddWithValue("$payloadJson", item.PayloadJson);
        _ = command.Parameters.AddWithValue("$status", (int)item.Status);
        _ = command.Parameters.AddWithValue("$retryCount", item.RetryCount);
        _ = command.Parameters.AddWithValue("$createdAtUtc", FormatUtc(item.CreatedAtUtc));
        _ = command.Parameters.AddWithValue("$syncedAtUtc", item.SyncedAtUtc is null ? DBNull.Value : FormatUtc(item.SyncedAtUtc.Value));
        _ = command.Parameters.AddWithValue("$lastError", item.LastError is null ? DBNull.Value : item.LastError);
    }

    private static bool ExistsAggregateIdentity(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        string aggregateType,
        string aggregateId)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT 1
            FROM sync_outbox
            WHERE aggregate_type = $aggregateType
              AND aggregate_id = $aggregateId
            LIMIT 1;
            """;
        _ = command.Parameters.AddWithValue("$aggregateType", aggregateType);
        _ = command.Parameters.AddWithValue("$aggregateId", aggregateId);
        return command.ExecuteScalar() is not null;
    }

    private static string FormatUtc(DateTimeOffset value)
        => value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
}
