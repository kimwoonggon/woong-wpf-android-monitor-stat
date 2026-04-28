using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Woong.MonitorStack.Windows.Storage;

public sealed class SqliteSyncOutboxRepository
{
    private readonly string _connectionString;

    public SqliteSyncOutboxRepository(string connectionString)
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
            CREATE TABLE IF NOT EXISTS sync_outbox (
                id TEXT NOT NULL PRIMARY KEY,
                aggregate_type TEXT NOT NULL,
                aggregate_id TEXT NOT NULL,
                payload_json TEXT NOT NULL,
                status INTEGER NOT NULL,
                retry_count INTEGER NOT NULL,
                created_at_utc TEXT NOT NULL,
                synced_at_utc TEXT NULL,
                last_error TEXT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_sync_outbox_status
                ON sync_outbox(status, created_at_utc);
            """;
        _ = command.ExecuteNonQuery();
    }

    public void Add(SyncOutboxItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
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
        _ = command.ExecuteNonQuery();
    }

    public IReadOnlyList<SyncOutboxItem> QueryAll()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                id,
                aggregate_type,
                aggregate_id,
                payload_json,
                status,
                retry_count,
                created_at_utc,
                synced_at_utc,
                last_error
            FROM sync_outbox
            ORDER BY created_at_utc;
            """;

        using var reader = command.ExecuteReader();
        var items = new List<SyncOutboxItem>();
        while (reader.Read())
        {
            items.Add(ReadItem(reader));
        }

        return items;
    }

    public void MarkSynced(string id, DateTimeOffset syncedAtUtc)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE sync_outbox
            SET status = $status,
                synced_at_utc = $syncedAtUtc,
                last_error = NULL
            WHERE id = $id;
            """;
        _ = command.Parameters.AddWithValue("$status", (int)SyncOutboxStatus.Synced);
        _ = command.Parameters.AddWithValue("$syncedAtUtc", FormatUtc(syncedAtUtc));
        _ = command.Parameters.AddWithValue("$id", id);
        _ = command.ExecuteNonQuery();
    }

    public void MarkFailed(string id, string error)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE sync_outbox
            SET status = $status,
                retry_count = retry_count + 1,
                last_error = $lastError
            WHERE id = $id;
            """;
        _ = command.Parameters.AddWithValue("$status", (int)SyncOutboxStatus.Failed);
        _ = command.Parameters.AddWithValue("$lastError", RequiredStorageText.Ensure(error, nameof(error)));
        _ = command.Parameters.AddWithValue("$id", id);
        _ = command.ExecuteNonQuery();
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
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

    private static SyncOutboxItem ReadItem(SqliteDataReader reader)
        => new(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            (SyncOutboxStatus)reader.GetInt32(4),
            reader.GetInt32(5),
            DateTimeOffset.Parse(reader.GetString(6), CultureInfo.InvariantCulture),
            reader.IsDBNull(7) ? null : DateTimeOffset.Parse(reader.GetString(7), CultureInfo.InvariantCulture),
            reader.IsDBNull(8) ? null : reader.GetString(8));

    private static string FormatUtc(DateTimeOffset value)
        => value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
}
