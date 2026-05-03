using System.Net;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Domain.Contracts;

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

    [Fact]
    public async Task RunAsync_WhenLateWebRowHasSameEndAndFocusSession_DoesNotSkipItAfterCheckpoint()
    {
        string databasePath = CreateWindowsDatabaseWithFocusAndWebSession(
            "win-session-1",
            "2026-05-02T00:01:00Z",
            "2026-05-02T00:10:00Z");
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
        InsertWindowsWebSession(
            databasePath,
            "win-session-1",
            "2026-05-02T00:02:00Z",
            "2026-05-02T00:10:00Z",
            480000);

        LocalBridgeSummary second = await runner.RunAsync(options);

        Assert.Equal(1, first.WindowsWeb.Attempted);
        Assert.Equal(1, second.WindowsWeb.Attempted);
        Assert.Equal(2, handler.WebUploadCount);

        string checkpointJson = await File.ReadAllTextAsync(checkpointPath);
        Assert.Contains("windows.web_session", checkpointJson, StringComparison.Ordinal);
        Assert.Contains("win-session-1", checkpointJson, StringComparison.Ordinal);
        Assert.Contains("2026-05-02T00:02:00", checkpointJson, StringComparison.Ordinal);
        Assert.DoesNotContain("example.com", checkpointJson, StringComparison.Ordinal);
        Assert.DoesNotContain("Chrome", checkpointJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_UploadsCurrentAppStatesThroughApiDto()
    {
        string databasePath = CreateAndroidDatabaseWithCurrentAppState("android-current-1");
        using var handler = new RecordingBridgeHandler();
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://127.0.0.1:5087")
        };
        var runner = new LocalDashboardBridgeRunner(httpClient);
        LocalBridgeOptions options = LocalBridgeOptions.Parse(
        [
            "--server",
            "http://127.0.0.1:5087",
            "--userId",
            "local-user",
            "--timezoneId",
            "UTC",
            "--androidDb",
            databasePath
        ]);

        LocalBridgeSummary summary = await runner.RunAsync(options);

        Assert.Equal(1, handler.CurrentAppUploadCount);
        Assert.Equal("/api/current-app-states/upload", handler.CurrentAppUploadPaths.Single());
        Assert.Contains("\"states\"", handler.CurrentAppUploadBodies.Single(), StringComparison.Ordinal);
        Assert.Contains("\"clientStateId\":\"android-current-1\"", handler.CurrentAppUploadBodies.Single(), StringComparison.Ordinal);
        Assert.Contains("\"platform\":2", handler.CurrentAppUploadBodies.Single(), StringComparison.Ordinal);
        Assert.Contains("\"platformAppKey\":\"com.android.chrome\"", handler.CurrentAppUploadBodies.Single(), StringComparison.Ordinal);
        Assert.Equal(1, summary.AndroidCurrentApp.Attempted);
        Assert.Equal(1, summary.AndroidCurrentApp.Accepted);
    }

    [Fact]
    public async Task RunAsync_WithAcceptedCurrentAppState_UploadsServerContractAndAdvancesCheckpoint()
    {
        string databasePath = CreateAndroidDatabaseWithCurrentAppState("android-current-1");
        string checkpointPath = Path.Combine(_tempDirectory, "bridge-current-contract-checkpoints.json");
        using var handler = new RecordingBridgeHandler();
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://127.0.0.1:5087")
        };
        var runner = new LocalDashboardBridgeRunner(httpClient);
        LocalBridgeOptions options = LocalBridgeOptions.Parse(
        [
            "--server",
            "http://127.0.0.1:5087",
            "--userId",
            "local-user",
            "--timezoneId",
            "UTC",
            "--androidDb",
            databasePath,
            "--checkpointPath",
            checkpointPath
        ]);

        LocalBridgeSummary summary = await runner.RunAsync(options);

        Assert.Equal(1, handler.CurrentAppUploadCount);
        Assert.Equal("/api/current-app-states/upload", handler.CurrentAppUploadPaths.Single());
        Assert.Equal("token-1", handler.CurrentAppUploadDeviceTokens.Single());
        UploadCurrentAppStatesRequest request = Assert.Single(handler.CurrentAppUploadRequests);
        Assert.Equal("android-device-1", request.DeviceId);
        CurrentAppStateUploadItem state = Assert.Single(request.States);
        Assert.Equal("android-current-1", state.ClientStateId);
        Assert.Equal(Platform.Android, state.Platform);
        Assert.Equal("com.android.chrome", state.PlatformAppKey);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1777792500000), state.ObservedAtUtc);
        Assert.Equal(new DateOnly(2026, 5, 3), state.LocalDate);
        Assert.Equal("UTC", state.TimezoneId);
        Assert.Equal("Active", state.Status);
        Assert.Equal("android_current_app_state", state.Source);
        Assert.Null(state.ProcessId);
        Assert.Null(state.ProcessName);
        Assert.Null(state.ProcessPath);
        Assert.Null(state.WindowHandle);
        Assert.Null(state.WindowTitle);
        Assert.Equal(1, summary.AndroidCurrentApp.Attempted);
        Assert.Equal(1, summary.AndroidCurrentApp.Accepted);

        string checkpointJson = await File.ReadAllTextAsync(checkpointPath);
        Assert.Contains("android.current_app_states", checkpointJson, StringComparison.Ordinal);
        Assert.Contains("android-current-1", checkpointJson, StringComparison.Ordinal);
        Assert.Contains("2026-05-03T07:15:00", checkpointJson, StringComparison.Ordinal);
        Assert.DoesNotContain("com.android.chrome", checkpointJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_WithCheckpoint_DoesNotReattemptUnchangedCurrentAppStates()
    {
        string databasePath = CreateAndroidDatabaseWithCurrentAppState("android-current-1");
        string checkpointPath = Path.Combine(_tempDirectory, "bridge-current-checkpoints.json");
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
            "--androidDb",
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
        Assert.Equal(1, handler.CurrentAppUploadCount);
        Assert.Equal(1, summary.AndroidCurrentApp.Attempted);
        Assert.Equal(1, summary.AndroidCurrentApp.Accepted);

        string checkpointJson = await File.ReadAllTextAsync(checkpointPath);
        Assert.Contains("android.current_app_states", checkpointJson, StringComparison.Ordinal);
        Assert.Contains("android-current-1", checkpointJson, StringComparison.Ordinal);
        Assert.Contains("2026-05-03T07:15:00", checkpointJson, StringComparison.Ordinal);
        Assert.DoesNotContain("com.android.chrome", checkpointJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_WhenCurrentAppUploadErrors_DoesNotAdvanceAndroidCurrentAppCheckpoint()
    {
        string databasePath = CreateAndroidDatabaseWithCurrentAppState("android-current-1");
        string checkpointPath = Path.Combine(_tempDirectory, "bridge-current-error-checkpoints.json");
        using var handler = new RecordingBridgeHandler(currentAppStatus: 3);
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
            "--androidDb",
            databasePath,
            "--intervalSeconds",
            "0",
            "--maxIterations",
            "2",
            "--checkpointPath",
            checkpointPath
        ]);

        LocalBridgeSummary summary = await runner.RunAsync(options);

        Assert.Equal(2, handler.CurrentAppUploadCount);
        Assert.Equal(2, summary.AndroidCurrentApp.Attempted);
        Assert.Equal(0, summary.AndroidCurrentApp.Accepted);
        Assert.Equal(2, summary.AndroidCurrentApp.Error);
        if (File.Exists(checkpointPath))
        {
            string checkpointJson = await File.ReadAllTextAsync(checkpointPath);
            Assert.DoesNotContain("android.current_app_states", checkpointJson, StringComparison.Ordinal);
            Assert.DoesNotContain("android-current-1", checkpointJson, StringComparison.Ordinal);
        }
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

    private string CreateWindowsDatabaseWithFocusAndWebSession(
        string focusSessionId,
        string webStartedAtUtc,
        string webEndedAtUtc)
    {
        string databasePath = CreateWindowsDatabaseWithFocusSession(focusSessionId);
        using var connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();
        Execute(connection, """
            CREATE TABLE web_session (
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
            """);
        InsertWindowsWebSession(connection, focusSessionId, webStartedAtUtc, webEndedAtUtc, 540000);

        return databasePath;
    }

    private string CreateAndroidDatabaseWithCurrentAppState(string clientStateId)
    {
        string databasePath = Path.Combine(_tempDirectory, $"{Guid.NewGuid():N}.db");
        using var connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();
        Execute(connection, """
            CREATE TABLE current_app_states (
                clientStateId TEXT NOT NULL,
                packageName TEXT NOT NULL,
                observedAtUtcMillis INTEGER NOT NULL,
                localDate TEXT NOT NULL,
                timezoneId TEXT NULL,
                status TEXT NULL,
                source TEXT NULL
            );
            """);
        Execute(connection, $"""
            INSERT INTO current_app_states VALUES (
                '{clientStateId}',
                'com.android.chrome',
                1777792500000,
                '2026-05-03',
                'UTC',
                'Active',
                'android_current_app_state'
            );
            """);

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

    private static void InsertWindowsWebSession(
        string databasePath,
        string focusSessionId,
        string startedAtUtc,
        string endedAtUtc,
        long durationMs)
    {
        using var connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();
        InsertWindowsWebSession(connection, focusSessionId, startedAtUtc, endedAtUtc, durationMs);
    }

    private static void InsertWindowsWebSession(
        SqliteConnection connection,
        string focusSessionId,
        string startedAtUtc,
        string endedAtUtc,
        long durationMs)
    {
        Execute(connection, $"""
            INSERT INTO web_session VALUES (
                '{focusSessionId}',
                'Chrome',
                NULL,
                'example.com',
                NULL,
                '{startedAtUtc}',
                '{endedAtUtc}',
                {durationMs},
                'native_messaging',
                'domain_only',
                0
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
        private readonly int _currentAppStatus;
        private readonly HashSet<string> _storedFocusSessionIds = new(StringComparer.Ordinal);

        public RecordingBridgeHandler(bool idempotentFocusUploads = false, int currentAppStatus = 1)
        {
            _idempotentFocusUploads = idempotentFocusUploads;
            _currentAppStatus = currentAppStatus;
        }

        public int FocusUploadCount { get; private set; }

        public int WebUploadCount { get; private set; }

        public int CurrentAppUploadCount { get; private set; }

        public List<string> CurrentAppUploadPaths { get; } = [];

        public List<string> CurrentAppUploadBodies { get; } = [];

        public List<string?> CurrentAppUploadDeviceTokens { get; } = [];

        public List<UploadCurrentAppStatesRequest> CurrentAppUploadRequests { get; } = [];

        public int StoredFocusSessionCount => _storedFocusSessionIds.Count;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri?.AbsolutePath == "/api/devices/register")
            {
                string registerBody = await request.Content!.ReadAsStringAsync(cancellationToken);
                RegisterDeviceRequest? registerRequest = JsonSerializer.Deserialize<RegisterDeviceRequest>(
                    registerBody,
                    JsonOptions);
                string deviceId = registerRequest?.Platform == Platform.Android
                    ? "android-device-1"
                    : "windows-device-1";

                return JsonResponse(new
                {
                    deviceId,
                    deviceToken = "token-1"
                });
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

                return JsonResponse(new
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
                });
            }

            if (request.RequestUri?.AbsolutePath == "/api/web-sessions/upload")
            {
                WebUploadCount++;
                return JsonResponse(new
                {
                    items = new[]
                    {
                        new
                        {
                            clientId = $"web-session-{WebUploadCount}",
                            status = 1,
                            errorMessage = (string?)null
                        }
                    }
                });
            }

            if (request.RequestUri?.AbsolutePath == "/api/current-app-states/upload")
            {
                CurrentAppUploadCount++;
                CurrentAppUploadPaths.Add(request.RequestUri.AbsolutePath);
                CurrentAppUploadDeviceTokens.Add(
                    request.Headers.TryGetValues("X-Device-Token", out IEnumerable<string>? values)
                        ? values.SingleOrDefault()
                        : null);
                string body = await request.Content!.ReadAsStringAsync(cancellationToken);
                CurrentAppUploadBodies.Add(body);
                UploadCurrentAppStatesRequest? uploadRequest = JsonSerializer.Deserialize<UploadCurrentAppStatesRequest>(
                    body,
                    JsonOptions);
                if (uploadRequest is not null)
                {
                    CurrentAppUploadRequests.Add(uploadRequest);
                }

                string clientId = uploadRequest?.States.FirstOrDefault()?.ClientStateId ?? "android-current-1";

                return JsonResponse(new
                {
                    items = new[]
                    {
                        new
                        {
                            clientId,
                            status = _currentAppStatus,
                            errorMessage = _currentAppStatus == 3 ? "current app upload failed" : null
                        }
                    }
                });
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private static HttpResponseMessage JsonResponse<T>(T value)
            => new(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(value, JsonOptions))
            };

    }
}
