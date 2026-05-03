using Microsoft.Data.Sqlite;
using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.LocalDashboardBridge.Tests;

public sealed class LocalDashboardBridgeReaderTests : IDisposable
{
    private readonly string _tempDirectory;

    public LocalDashboardBridgeReaderTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            $"woong-local-dashboard-bridge-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void WindowsSqliteReader_ReadsFocusAndDomainMetadataFromTempDatabase()
    {
        string databasePath = CreateDatabasePath();
        using (SqliteConnection connection = OpenDatabase(databasePath))
        {
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
            Execute(connection, """
                INSERT INTO focus_session VALUES (
                    'win-session-1',
                    'devenv.exe',
                    '2026-05-02T00:00:00Z',
                    '2026-05-02T00:05:00Z',
                    300000,
                    '2026-05-02',
                    'UTC',
                    0,
                    'windows_collector',
                    42,
                    'devenv.exe',
                    'C:\Program Files\Microsoft Visual Studio\devenv.exe',
                    12345,
                    'Solution Explorer'
                );
                """);
            Execute(connection, """
                INSERT INTO web_session VALUES (
                    'win-session-1',
                    'Chrome',
                    NULL,
                    'example.com',
                    NULL,
                    '2026-05-02T00:01:00Z',
                    '2026-05-02T00:03:00Z',
                    120000,
                    'native_messaging',
                    'domain_only',
                    0
                );
                """);
        }

        LocalUploadBatch batch = global::WindowsSqliteReader.Read(databasePath, "server-device", "UTC");

        Assert.Collection(
            batch.FocusSessions,
            session =>
            {
                Assert.Equal("win-session-1", session.ClientSessionId);
                Assert.Equal("devenv.exe", session.PlatformAppKey);
                Assert.Equal(300000, session.DurationMs);
                Assert.Equal(DateOnly.FromDateTime(new DateTime(2026, 5, 2)), session.LocalDate);
                Assert.False(session.IsIdle);
                Assert.Equal("windows_collector", session.Source);
                Assert.Equal("devenv.exe", session.ProcessName);
                Assert.Equal("Solution Explorer", session.WindowTitle);
            });
        Assert.Collection(
            batch.WebSessions,
            session =>
            {
                Assert.Equal("win-session-1", session.FocusSessionId);
                Assert.Equal("Chrome", session.BrowserFamily);
                Assert.Null(session.Url);
                Assert.Equal("example.com", session.Domain);
                Assert.Null(session.PageTitle);
                Assert.Equal("native_messaging", session.CaptureMethod);
                Assert.Equal("domain_only", session.CaptureConfidence);
            });
        Assert.Empty(batch.LocationContexts);
        Assert.Empty(batch.CurrentAppStates);
    }

    [Fact]
    public void WindowsSqliteReader_WhenCurrentAppStateTableMissing_ReturnsNoCurrentAppStates()
    {
        string databasePath = CreateDatabasePath();
        using (SqliteConnection connection = OpenDatabase(databasePath))
        {
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
        }

        LocalUploadBatch batch = global::WindowsSqliteReader.Read(databasePath, "server-device", "UTC");

        Assert.Empty(batch.CurrentAppStates);
    }

    [Fact]
    public void WindowsSqliteReader_ReadsCurrentAppStateMetadataFromTempDatabase()
    {
        string databasePath = CreateDatabasePath();
        DateTimeOffset observedAtUtc = new(2026, 5, 3, 7, 15, 0, TimeSpan.Zero);

        using (SqliteConnection connection = OpenDatabase(databasePath))
        {
            Execute(connection, """
                CREATE TABLE current_app_state (
                    id INTEGER NOT NULL PRIMARY KEY CHECK (id = 1),
                    client_state_id TEXT NOT NULL,
                    device_id TEXT NOT NULL,
                    platform_app_key TEXT NOT NULL,
                    process_id INTEGER NULL,
                    process_name TEXT NULL,
                    process_path TEXT NULL,
                    window_handle INTEGER NULL,
                    observed_at_utc TEXT NOT NULL,
                    local_date TEXT NOT NULL,
                    timezone_id TEXT NOT NULL,
                    status TEXT NOT NULL,
                    source TEXT NOT NULL
                );
                """);
            Execute(connection, $"""
                INSERT INTO current_app_state VALUES (
                    1,
                    'windows-current-1',
                    'windows-device-1',
                    'chrome.exe',
                    20,
                    'chrome.exe',
                    'C:\Apps\chrome.exe',
                    200,
                    '{observedAtUtc:O}',
                    '2026-05-03',
                    'UTC',
                    'Active',
                    'windows_foreground_current_app'
                );
                """);
        }

        LocalUploadBatch batch = global::WindowsSqliteReader.Read(databasePath, "server-device", "UTC");

        Assert.Collection(
            batch.CurrentAppStates,
            state =>
            {
                Assert.Equal("windows-current-1", state.ClientStateId);
                Assert.Equal(Platform.Windows, state.Platform);
                Assert.Equal("chrome.exe", state.PlatformAppKey);
                Assert.Equal(observedAtUtc, state.ObservedAtUtc);
                Assert.Equal(new DateOnly(2026, 5, 3), state.LocalDate);
                Assert.Equal("UTC", state.TimezoneId);
                Assert.Equal("Active", state.Status);
                Assert.Equal("windows_foreground_current_app", state.Source);
                Assert.Equal(20, state.ProcessId);
                Assert.Equal("chrome.exe", state.ProcessName);
                Assert.Equal(@"C:\Apps\chrome.exe", state.ProcessPath);
                Assert.Equal(200, state.WindowHandle);
                Assert.Null(state.WindowTitle);
            });
    }

    [Fact]
    public void AndroidRoomReader_ReadsUsageAndOptedInLocationMetadataFromTempDatabase()
    {
        string databasePath = CreateDatabasePath();
        string timezoneId = FindPacificTimeZoneId();
        DateTimeOffset capturedAtUtc = new(2026, 5, 3, 6, 30, 0, TimeSpan.Zero);
        DateOnly expectedLocalDate = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(capturedAtUtc, TimeZoneInfo.FindSystemTimeZoneById(timezoneId)).DateTime);

        using (SqliteConnection connection = OpenDatabase(databasePath))
        {
            Execute(connection, """
                CREATE TABLE focus_sessions (
                    clientSessionId TEXT NOT NULL,
                    packageName TEXT NOT NULL,
                    startedAtUtcMillis INTEGER NOT NULL,
                    endedAtUtcMillis INTEGER NOT NULL,
                    durationMs INTEGER NOT NULL,
                    localDate TEXT NOT NULL,
                    timezoneId TEXT NULL,
                    isIdle INTEGER NOT NULL,
                    source TEXT NULL
                );
                """);
            Execute(connection, """
                CREATE TABLE location_context_snapshots (
                    id TEXT NOT NULL,
                    capturedAtUtcMillis INTEGER NOT NULL,
                    latitude REAL NULL,
                    longitude REAL NULL,
                    accuracyMeters REAL NULL,
                    permissionState TEXT NULL,
                    captureMode TEXT NULL
                );
                """);
            Execute(connection, $"""
                INSERT INTO focus_sessions VALUES (
                    'android-session-1',
                    'com.android.chrome',
                    1777708800000,
                    1777708860000,
                    60000,
                    '2026-05-02',
                    '{timezoneId}',
                    0,
                    'usage_stats'
                );
                """);
            Execute(connection, $"""
                INSERT INTO location_context_snapshots VALUES (
                    'location-1',
                    {capturedAtUtc.ToUnixTimeMilliseconds()},
                    37.7749,
                    -122.4194,
                    25.5,
                    'GrantedApproximate',
                    'AppUsageContext'
                );
                """);
        }

        LocalUploadBatch batch = global::AndroidRoomReader.Read(databasePath, "server-device", timezoneId);

        Assert.Collection(
            batch.FocusSessions,
            session =>
            {
                Assert.Equal("android-session-1", session.ClientSessionId);
                Assert.Equal("com.android.chrome", session.PlatformAppKey);
                Assert.Equal(60000, session.DurationMs);
                Assert.Equal("usage_stats", session.Source);
            });
        Assert.Collection(
            batch.LocationContexts,
            context =>
            {
                Assert.Equal("location-1", context.ClientContextId);
                Assert.Equal(capturedAtUtc, context.CapturedAtUtc);
                Assert.Equal(expectedLocalDate, context.LocalDate);
                Assert.Equal(timezoneId, context.TimezoneId);
                Assert.Equal(37.7749, context.Latitude);
                Assert.Equal(-122.4194, context.Longitude);
                Assert.Equal("GrantedApproximate", context.PermissionState);
            });
        Assert.Empty(batch.WebSessions);
        Assert.Empty(batch.CurrentAppStates);
    }

    [Fact]
    public void AndroidRoomReader_WhenCurrentAppStateTableMissing_ReturnsNoCurrentAppStates()
    {
        string databasePath = CreateDatabasePath();
        using (SqliteConnection connection = OpenDatabase(databasePath))
        {
            Execute(connection, """
                CREATE TABLE focus_sessions (
                    clientSessionId TEXT NOT NULL,
                    packageName TEXT NOT NULL,
                    startedAtUtcMillis INTEGER NOT NULL,
                    endedAtUtcMillis INTEGER NOT NULL,
                    durationMs INTEGER NOT NULL,
                    localDate TEXT NOT NULL,
                    timezoneId TEXT NULL,
                    isIdle INTEGER NOT NULL,
                    source TEXT NULL
                );
                """);
        }

        LocalUploadBatch batch = global::AndroidRoomReader.Read(databasePath, "server-device", "UTC");

        Assert.Empty(batch.CurrentAppStates);
    }

    [Fact]
    public void AndroidRoomReader_ReadsCurrentAppStateMetadataFromTempDatabase()
    {
        string databasePath = CreateDatabasePath();
        string timezoneId = FindPacificTimeZoneId();
        DateTimeOffset observedAtUtc = new(2026, 5, 3, 7, 15, 0, TimeSpan.Zero);

        using (SqliteConnection connection = OpenDatabase(databasePath))
        {
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
                    'android-current-1',
                    'com.android.chrome',
                    {observedAtUtc.ToUnixTimeMilliseconds()},
                    '2026-05-03',
                    '{timezoneId}',
                    'Active',
                    'android_current_app_state'
                );
                """);
        }

        LocalUploadBatch batch = global::AndroidRoomReader.Read(databasePath, "server-device", timezoneId);

        Assert.Collection(
            batch.CurrentAppStates,
            state =>
            {
                Assert.Equal("android-current-1", state.ClientStateId);
                Assert.Equal(Platform.Android, state.Platform);
                Assert.Equal("com.android.chrome", state.PlatformAppKey);
                Assert.Equal(observedAtUtc, state.ObservedAtUtc);
                Assert.Equal(new DateOnly(2026, 5, 3), state.LocalDate);
                Assert.Equal(timezoneId, state.TimezoneId);
                Assert.Equal("Active", state.Status);
                Assert.Equal("android_current_app_state", state.Source);
                Assert.Null(state.ProcessId);
                Assert.Null(state.ProcessName);
                Assert.Null(state.ProcessPath);
                Assert.Null(state.WindowHandle);
                Assert.Null(state.WindowTitle);
            });
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

    private string CreateDatabasePath()
        => Path.Combine(_tempDirectory, $"{Guid.NewGuid():N}.db");

    private static SqliteConnection OpenDatabase(string databasePath)
    {
        var connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();
        return connection;
    }

    private static void Execute(SqliteConnection connection, string commandText)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = commandText;
        command.ExecuteNonQuery();
    }

    private static string FindPacificTimeZoneId()
    {
        foreach (string timezoneId in new[] { "Pacific Standard Time", "America/Los_Angeles" })
        {
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
                return timezoneId;
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        throw new InvalidOperationException("Pacific timezone was not available on this machine.");
    }
}
