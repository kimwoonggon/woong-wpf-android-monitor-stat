using System.Net;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Woong.MonitorStack.LocalDashboardBridge.Tests;

public sealed class LocalDashboardBridgePollingTests : IDisposable
{
    private readonly string _tempDirectory;

    public LocalDashboardBridgePollingTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            $"woong-local-dashboard-bridge-polling-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task RunAsync_WhenIntervalModeHasMaxIterations_StopsAfterBoundedPolls()
    {
        string databasePath = CreateWindowsDatabaseWithFocusSession("win-session-1");
        using var handler = new RecordingBridgeHandler();
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://127.0.0.1:5087")
        };
        List<TimeSpan> delays = [];
        var runner = new LocalDashboardBridgeRunner(
            httpClient,
            delayAsync: (delay, _) =>
            {
                delays.Add(delay);
                return Task.CompletedTask;
            });
        LocalBridgeOptions options = LocalBridgeOptions.Parse(
        [
            "--server",
            "http://127.0.0.1:5087",
            "--userId",
            "local-user",
            "--timezoneId",
            "UTC",
            "--windowsDb",
            databasePath,
            "--intervalSeconds",
            "7",
            "--maxIterations",
            "3"
        ]);

        LocalBridgeSummary summary = await runner.RunAsync(options);

        Assert.Equal(3, summary.Iterations);
        Assert.Equal(3, handler.FocusUploadCount);
        Assert.Equal([TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(7)], delays);
    }

    [Fact]
    public async Task RunAsync_WhenSameRowsArePolledTwice_ReportsDuplicatesWithoutInflatingAcceptedUploads()
    {
        string databasePath = CreateWindowsDatabaseWithFocusSession("win-session-1");
        using var handler = new RecordingBridgeHandler(idempotentFocusUploads: true);
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://127.0.0.1:5087")
        };
        var runner = new LocalDashboardBridgeRunner(
            httpClient,
            delayAsync: (_, _) => Task.CompletedTask);
        LocalBridgeOptions options = LocalBridgeOptions.Parse(
        [
            "--server",
            "http://127.0.0.1:5087",
            "--userId",
            "local-user",
            "--timezoneId",
            "UTC",
            "--windowsDb",
            databasePath,
            "--intervalSeconds",
            "0",
            "--maxIterations",
            "2"
        ]);

        LocalBridgeSummary summary = await runner.RunAsync(options);

        Assert.Equal(2, summary.Iterations);
        Assert.Equal(1, handler.StoredFocusSessionCount);
        Assert.Equal(2, summary.WindowsFocus.Attempted);
        Assert.Equal(1, summary.WindowsFocus.Accepted);
        Assert.Equal(1, summary.WindowsFocus.Duplicate);
        Assert.Equal(0, summary.WindowsFocus.Error);
        Assert.Equal(1, summary.WindowsFocusUploaded);
    }

    [Fact]
    public async Task RunAsync_WhenCheckpointPathIsSet_DoesNotReattemptUnchangedRowsOnNextPoll()
    {
        string databasePath = CreateWindowsDatabaseWithFocusSession("win-session-1");
        string checkpointPath = Path.Combine(_tempDirectory, "bridge-checkpoints.json");
        using var handler = new RecordingBridgeHandler();
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://127.0.0.1:5087")
        };
        var runner = new LocalDashboardBridgeRunner(
            httpClient,
            delayAsync: (_, _) => Task.CompletedTask);
        LocalBridgeOptions options = LocalBridgeOptions.Parse(
        [
            "--server",
            "http://127.0.0.1:5087",
            "--userId",
            "local-user",
            "--timezoneId",
            "UTC",
            "--windowsDb",
            databasePath,
            "--intervalSeconds",
            "0",
            "--maxIterations",
            "2",
            "--checkpointPath",
            checkpointPath
        ]);

        LocalBridgeSummary summary = await runner.RunAsync(options);

        Assert.Equal(2, summary.Iterations);
        Assert.Equal(1, handler.FocusUploadCount);
        Assert.Equal(1, summary.WindowsFocus.Attempted);
        Assert.Equal(1, summary.WindowsFocus.Accepted);
        Assert.True(File.Exists(checkpointPath));

        string checkpointJson = await File.ReadAllTextAsync(checkpointPath);
        Assert.Contains("windows.focus_session", checkpointJson, StringComparison.Ordinal);
        Assert.Contains("2026-05-02T00:05:00", checkpointJson, StringComparison.Ordinal);
        Assert.Contains("win-session-1", checkpointJson, StringComparison.Ordinal);
        Assert.DoesNotContain("Solution Explorer", checkpointJson, StringComparison.Ordinal);
        Assert.DoesNotContain("devenv.exe", checkpointJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_WhenCheckpointedDatabaseGetsNewRow_UploadsOnlyTheNewRow()
    {
        string databasePath = CreateWindowsDatabaseWithFocusSession("win-session-1");
        string checkpointPath = Path.Combine(_tempDirectory, "bridge-checkpoints.json");
        using var handler = new RecordingBridgeHandler();
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://127.0.0.1:5087")
        };
        var runner = new LocalDashboardBridgeRunner(
            httpClient,
            delayAsync: (_, _) => Task.CompletedTask);
        LocalBridgeOptions options = LocalBridgeOptions.Parse(
        [
            "--server",
            "http://127.0.0.1:5087",
            "--userId",
            "local-user",
            "--timezoneId",
            "UTC",
            "--windowsDb",
            databasePath,
            "--intervalSeconds",
            "0",
            "--maxIterations",
            "1",
            "--checkpointPath",
            checkpointPath
        ]);

        LocalBridgeSummary first = await runner.RunAsync(options);
        InsertWindowsFocusSession(
            databasePath,
            "win-session-2",
            "2026-05-02T00:06:00Z",
            "2026-05-02T00:07:00Z",
            60000);

        LocalBridgeSummary second = await runner.RunAsync(options);

        Assert.Equal(1, first.WindowsFocus.Attempted);
        Assert.Equal(1, second.WindowsFocus.Attempted);
        Assert.Equal(2, handler.FocusUploadCount);

        string checkpointJson = await File.ReadAllTextAsync(checkpointPath);
        Assert.Contains("win-session-2", checkpointJson, StringComparison.Ordinal);
        Assert.DoesNotContain("Solution Explorer", checkpointJson, StringComparison.Ordinal);
        Assert.DoesNotContain("devenv.exe", checkpointJson, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private string CreateWindowsDatabaseWithFocusSession(string clientSessionId)
    {
        string databasePath = Path.Combine(_tempDirectory, $"{Guid.NewGuid():N}.db");
        using var connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();
        Execute(connection, """
            CREATE TABLE focus_session (
                client_session_id TEXT NOT NULL,
                platform_app_key TEXT NOT NULL,
                started_at_utc TEXT NOT NULL,
                ended_at_utc TEXT NOT NULL,
                duration_ms INTEGER NOT NULL,
                local_date TEXT NOT NULL,
                timezone_id TEXT NULL,
                is_idle INTEGER NOT NULL,
                source TEXT NULL,
                process_id INTEGER NULL,
                process_name TEXT NULL,
                process_path TEXT NULL,
                window_handle INTEGER NULL,
                window_title TEXT NULL
            );
            """);
        InsertWindowsFocusSession(
            connection,
            clientSessionId,
            "2026-05-02T00:00:00Z",
            "2026-05-02T00:05:00Z",
            300000);

        return databasePath;
    }

    private static void InsertWindowsFocusSession(
        string databasePath,
        string clientSessionId,
        string startedAtUtc,
        string endedAtUtc,
        long durationMs)
    {
        using var connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();
        InsertWindowsFocusSession(connection, clientSessionId, startedAtUtc, endedAtUtc, durationMs);
    }

    private static void InsertWindowsFocusSession(
        SqliteConnection connection,
        string clientSessionId,
        string startedAtUtc,
        string endedAtUtc,
        long durationMs)
    {
        Execute(connection, $"""
            INSERT INTO focus_session VALUES (
                '{clientSessionId}',
                'devenv.exe',
                '{startedAtUtc}',
                '{endedAtUtc}',
                {durationMs},
                '2026-05-02',
                'UTC',
                0,
                'windows_collector',
                42,
                'devenv.exe',
                NULL,
                12345,
                'Solution Explorer'
            );
            """);
    }

    private static void Execute(SqliteConnection connection, string commandText)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = commandText;
        command.ExecuteNonQuery();
    }

    private sealed class RecordingBridgeHandler : HttpMessageHandler
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly bool _idempotentFocusUploads;
        private readonly HashSet<string> _storedFocusSessionIds = new(StringComparer.Ordinal);

        public RecordingBridgeHandler(bool idempotentFocusUploads = false)
        {
            _idempotentFocusUploads = idempotentFocusUploads;
        }

        public int FocusUploadCount { get; private set; }

        public int StoredFocusSessionCount => _storedFocusSessionIds.Count;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri?.AbsolutePath == "/api/devices/register")
            {
                return Task.FromResult(JsonResponse(new
                {
                    deviceId = "windows-device-1",
                    deviceToken = "token-1"
                }));
            }

            if (request.RequestUri?.AbsolutePath == "/api/focus-sessions/upload")
            {
                FocusUploadCount++;
                int status = 1;
                if (_idempotentFocusUploads && !_storedFocusSessionIds.Add("win-session-1"))
                {
                    status = 2;
                }
                else if (!_idempotentFocusUploads)
                {
                    _storedFocusSessionIds.Add($"win-session-{FocusUploadCount}");
                }

                return Task.FromResult(JsonResponse(new
                {
                    items = new[]
                    {
                        new
                        {
                            clientId = "win-session-1",
                            status,
                            errorMessage = (string?)null
                        }
                    }
                }));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private static HttpResponseMessage JsonResponse<T>(T value)
            => new(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(value, JsonOptions))
            };

    }
}
