using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Domain.Contracts;

JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);
LocalBridgeOptions options = LocalBridgeOptions.Parse(args);

if (options.ShowHelp)
{
    LocalBridgeOptions.WriteUsage();
    return 0;
}

using HttpClient httpClient = new()
{
    BaseAddress = new Uri(options.ServerBaseUrl)
};

var runner = new LocalDashboardBridgeRunner(httpClient, jsonOptions);
LocalBridgeSummary summary = await runner.RunAsync(options);

Console.WriteLine(JsonSerializer.Serialize(summary, jsonOptions));
return 0;

public sealed class LocalDashboardBridgeRunner
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync;

    public LocalDashboardBridgeRunner(
        HttpClient httpClient,
        JsonSerializerOptions? jsonOptions = null,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null)
    {
        _httpClient = httpClient;
        _jsonOptions = jsonOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _delayAsync = delayAsync ?? Task.Delay;
    }

    public async Task<LocalBridgeSummary> RunAsync(
        LocalBridgeOptions options,
        CancellationToken cancellationToken = default)
    {
        var total = new LocalBridgeSummary();
        int iterations = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LocalBridgeSummary iteration = await RunOnceAsync(options, cancellationToken);
            total.Add(iteration);
            iterations++;

            if (options.RunOnce)
            {
                break;
            }

            if (options.MaxIterations.HasValue && iterations >= options.MaxIterations.Value)
            {
                break;
            }

            await _delayAsync(options.Interval!.Value, cancellationToken);
        }

        return total;
    }

    private async Task<LocalBridgeSummary> RunOnceAsync(
        LocalBridgeOptions options,
        CancellationToken cancellationToken)
    {
        LocalBridgeCheckpointStore? checkpoints = LocalBridgeCheckpointStore.Load(
            options.CheckpointPath,
            _jsonOptions);
        LocalBridgeSummary summary = new()
        {
            Iterations = 1
        };

        if (!string.IsNullOrWhiteSpace(options.WindowsDatabasePath))
        {
            DeviceRegistrationResponse windowsDevice = await RegisterDeviceAsync(
                options.UserId,
                Platform.Windows,
                $"{options.UserId}-windows-local",
                "Windows WPF Local",
                options.TimezoneId,
                cancellationToken);

            LocalUploadBatch windowsBatch = WindowsSqliteReader.Read(
                options.WindowsDatabasePath,
                windowsDevice.DeviceId,
                options.TimezoneId,
                checkpoints);

            summary.WindowsFocus = await UploadFocusSessionsAsync(
                windowsDevice,
                windowsBatch.FocusSessions,
                cancellationToken);
            checkpoints?.AdvanceAfterSuccessfulUpload(
                LocalBridgeCheckpointKeys.WindowsFocusSession,
                LocalBridgeCheckpointCursor.FromFocusSessions(windowsBatch.FocusSessions),
                summary.WindowsFocus);
            checkpoints?.Save();
            summary.WindowsWeb = await UploadWebSessionsAsync(
                windowsDevice,
                windowsBatch.WebSessions,
                cancellationToken);
            checkpoints?.AdvanceAfterSuccessfulUpload(
                LocalBridgeCheckpointKeys.WindowsWebSession,
                LocalBridgeCheckpointCursor.FromWebSessions(windowsBatch.WebSessions),
                summary.WindowsWeb);
            checkpoints?.Save();
            summary.WindowsCurrentApp = await UploadCurrentAppStatesAsync(
                windowsDevice,
                windowsBatch.CurrentAppStates,
                cancellationToken);
            checkpoints?.AdvanceAfterSuccessfulUpload(
                LocalBridgeCheckpointKeys.WindowsCurrentAppState,
                LocalBridgeCheckpointCursor.FromCurrentAppStates(windowsBatch.CurrentAppStates),
                summary.WindowsCurrentApp);
            checkpoints?.Save();
        }

        if (!string.IsNullOrWhiteSpace(options.AndroidDatabasePath))
        {
            DeviceRegistrationResponse androidDevice = await RegisterDeviceAsync(
                options.UserId,
                Platform.Android,
                $"{options.UserId}-android-emulator",
                "Android Emulator Local",
                options.TimezoneId,
                cancellationToken);

            LocalUploadBatch androidBatch = AndroidRoomReader.Read(
                options.AndroidDatabasePath,
                androidDevice.DeviceId,
                options.TimezoneId,
                checkpoints);

            summary.AndroidFocus = await UploadFocusSessionsAsync(
                androidDevice,
                androidBatch.FocusSessions,
                cancellationToken);
            checkpoints?.AdvanceAfterSuccessfulUpload(
                LocalBridgeCheckpointKeys.AndroidFocusSession,
                LocalBridgeCheckpointCursor.FromFocusSessions(androidBatch.FocusSessions),
                summary.AndroidFocus);
            checkpoints?.Save();
            summary.AndroidLocation = await UploadLocationContextsAsync(
                androidDevice,
                androidBatch.LocationContexts,
                cancellationToken);
            checkpoints?.AdvanceAfterSuccessfulUpload(
                LocalBridgeCheckpointKeys.AndroidLocationContext,
                LocalBridgeCheckpointCursor.FromLocationContexts(androidBatch.LocationContexts),
                summary.AndroidLocation);
            checkpoints?.Save();
            summary.AndroidCurrentApp = await UploadCurrentAppStatesAsync(
                androidDevice,
                androidBatch.CurrentAppStates,
                cancellationToken);
            checkpoints?.AdvanceAfterSuccessfulUpload(
                LocalBridgeCheckpointKeys.AndroidCurrentAppState,
                LocalBridgeCheckpointCursor.FromCurrentAppStates(androidBatch.CurrentAppStates),
                summary.AndroidCurrentApp);
            checkpoints?.Save();
        }

        return summary;
    }

    private async Task<DeviceRegistrationResponse> RegisterDeviceAsync(
        string userId,
        Platform platform,
        string deviceKey,
        string deviceName,
        string timezoneId,
        CancellationToken cancellationToken)
    {
        var request = new RegisterDeviceRequest(userId, platform, deviceKey, deviceName, timezoneId);
        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            "/api/devices/register",
            request,
            _jsonOptions,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<DeviceRegistrationResponse>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Device registration returned an empty response.");
    }

    private async Task<UploadStatusSummary> UploadFocusSessionsAsync(
        DeviceRegistrationResponse device,
        IReadOnlyList<FocusSessionUploadItem> sessions,
        CancellationToken cancellationToken)
    {
        if (sessions.Count == 0)
        {
            return UploadStatusSummary.Empty;
        }

        using HttpRequestMessage request = CreateJsonPost(
            "/api/focus-sessions/upload",
            new UploadFocusSessionsRequest(device.DeviceId, sessions),
            device.DeviceToken);
        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await ReadUploadSummaryAsync(response, sessions.Count, cancellationToken);
    }

    private async Task<UploadStatusSummary> UploadWebSessionsAsync(
        DeviceRegistrationResponse device,
        IReadOnlyList<WebSessionUploadItem> sessions,
        CancellationToken cancellationToken)
    {
        if (sessions.Count == 0)
        {
            return UploadStatusSummary.Empty;
        }

        using HttpRequestMessage request = CreateJsonPost(
            "/api/web-sessions/upload",
            new UploadWebSessionsRequest(device.DeviceId, sessions),
            device.DeviceToken);
        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await ReadUploadSummaryAsync(response, sessions.Count, cancellationToken);
    }

    private async Task<UploadStatusSummary> UploadLocationContextsAsync(
        DeviceRegistrationResponse device,
        IReadOnlyList<LocationContextUploadItem> contexts,
        CancellationToken cancellationToken)
    {
        if (contexts.Count == 0)
        {
            return UploadStatusSummary.Empty;
        }

        using HttpRequestMessage request = CreateJsonPost(
            "/api/location-contexts/upload",
            new UploadLocationContextsRequest(device.DeviceId, contexts),
            device.DeviceToken);
        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await ReadUploadSummaryAsync(response, contexts.Count, cancellationToken);
    }

    private async Task<UploadStatusSummary> UploadCurrentAppStatesAsync(
        DeviceRegistrationResponse device,
        IReadOnlyList<CurrentAppStateUploadItem> states,
        CancellationToken cancellationToken)
    {
        if (states.Count == 0)
        {
            return UploadStatusSummary.Empty;
        }

        using HttpRequestMessage request = CreateJsonPost(
            "/api/current-app-states/upload",
            new UploadCurrentAppStatesRequest(device.DeviceId, states),
            device.DeviceToken);
        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await ReadUploadSummaryAsync(response, states.Count, cancellationToken);
    }

    private async Task<UploadStatusSummary> ReadUploadSummaryAsync(
        HttpResponseMessage response,
        int attempted,
        CancellationToken cancellationToken)
    {
        UploadBatchResult result = await response.Content.ReadFromJsonAsync<UploadBatchResult>(
            _jsonOptions,
            cancellationToken)
            ?? throw new InvalidOperationException("Upload endpoint returned an empty response.");

        return UploadStatusSummary.FromResult(attempted, result);
    }

    private HttpRequestMessage CreateJsonPost<T>(
        string path,
        T body,
        string deviceToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body, options: _jsonOptions)
        };
        request.Headers.TryAddWithoutValidation("X-Device-Token", deviceToken);
        return request;
    }
}

public sealed record DeviceRegistrationResponse(string DeviceId, string DeviceToken);

public sealed class LocalBridgeSummary
{
    public int Iterations { get; set; }
    public UploadStatusSummary WindowsFocus { get; set; } = new();
    public UploadStatusSummary WindowsWeb { get; set; } = new();
    public UploadStatusSummary WindowsCurrentApp { get; set; } = new();
    public UploadStatusSummary AndroidFocus { get; set; } = new();
    public UploadStatusSummary AndroidLocation { get; set; } = new();
    public UploadStatusSummary AndroidCurrentApp { get; set; } = new();

    public int WindowsFocusUploaded
    {
        get => WindowsFocus.Accepted;
        set => WindowsFocus.Accepted = value;
    }

    public int WindowsWebUploaded
    {
        get => WindowsWeb.Accepted;
        set => WindowsWeb.Accepted = value;
    }

    public int WindowsCurrentAppUploaded
    {
        get => WindowsCurrentApp.Accepted;
        set => WindowsCurrentApp.Accepted = value;
    }

    public int AndroidFocusUploaded
    {
        get => AndroidFocus.Accepted;
        set => AndroidFocus.Accepted = value;
    }

    public int AndroidLocationUploaded
    {
        get => AndroidLocation.Accepted;
        set => AndroidLocation.Accepted = value;
    }

    public int AndroidCurrentAppUploaded
    {
        get => AndroidCurrentApp.Accepted;
        set => AndroidCurrentApp.Accepted = value;
    }

    public void Add(LocalBridgeSummary iteration)
    {
        Iterations += iteration.Iterations;
        WindowsFocus.Add(iteration.WindowsFocus);
        WindowsWeb.Add(iteration.WindowsWeb);
        WindowsCurrentApp.Add(iteration.WindowsCurrentApp);
        AndroidFocus.Add(iteration.AndroidFocus);
        AndroidLocation.Add(iteration.AndroidLocation);
        AndroidCurrentApp.Add(iteration.AndroidCurrentApp);
    }
}

public sealed class UploadStatusSummary
{
    public int Attempted { get; set; }
    public int Accepted { get; set; }
    public int Duplicate { get; set; }
    public int Error { get; set; }

    public static UploadStatusSummary Empty => new();

    public static UploadStatusSummary FromResult(int attempted, UploadBatchResult result)
    {
        var summary = new UploadStatusSummary
        {
            Attempted = attempted
        };

        foreach (UploadItemResult item in result.Items)
        {
            switch (item.Status)
            {
                case UploadItemStatus.Accepted:
                    summary.Accepted++;
                    break;
                case UploadItemStatus.Duplicate:
                    summary.Duplicate++;
                    break;
                case UploadItemStatus.Error:
                    summary.Error++;
                    break;
            }
        }

        return summary;
    }

    public void Add(UploadStatusSummary summary)
    {
        Attempted += summary.Attempted;
        Accepted += summary.Accepted;
        Duplicate += summary.Duplicate;
        Error += summary.Error;
    }
}

public sealed record LocalUploadBatch(
    IReadOnlyList<FocusSessionUploadItem> FocusSessions,
    IReadOnlyList<WebSessionUploadItem> WebSessions,
    IReadOnlyList<LocationContextUploadItem> LocationContexts,
    IReadOnlyList<CurrentAppStateUploadItem> CurrentAppStates)
{
    public static LocalUploadBatch Empty { get; } = new([], [], [], []);
}

public sealed class LocalBridgeOptions
{
    private LocalBridgeOptions(
        bool showHelp,
        string serverBaseUrl,
        string userId,
        string timezoneId,
        string? windowsDatabasePath,
        string? androidDatabasePath,
        bool runOnce,
        int? intervalSeconds,
        int? maxIterations,
        string? checkpointPath)
    {
        ShowHelp = showHelp;
        ServerBaseUrl = serverBaseUrl;
        UserId = userId;
        TimezoneId = timezoneId;
        WindowsDatabasePath = windowsDatabasePath;
        AndroidDatabasePath = androidDatabasePath;
        RunOnce = runOnce;
        IntervalSeconds = intervalSeconds;
        MaxIterations = maxIterations;
        CheckpointPath = checkpointPath;
    }

    public bool ShowHelp { get; }

    public string ServerBaseUrl { get; }

    public string UserId { get; }

    public string TimezoneId { get; }

    public string? WindowsDatabasePath { get; }

    public string? AndroidDatabasePath { get; }

    public bool RunOnce { get; }

    public int? IntervalSeconds { get; }

    public int? MaxIterations { get; }

    public string? CheckpointPath { get; }

    public TimeSpan? Interval => IntervalSeconds.HasValue
        ? TimeSpan.FromSeconds(IntervalSeconds.Value)
        : null;

    public static LocalBridgeOptions Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool showHelp = args.Any(arg => arg is "--help" or "-h" or "/?");

        for (int index = 0; index < args.Length; index++)
        {
            string arg = args[index];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            string key = arg[2..];
            if (index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                values[key] = args[++index];
            }
            else
            {
                flags.Add(key);
            }
        }

        string server = ValueOrDefault(values, "server", "http://127.0.0.1:5087");
        string userId = ValueOrDefault(values, "userId", "local-user");
        string timezoneId = ValueOrDefault(values, "timezoneId", TimeZoneInfo.Local.Id);
        int? intervalSeconds = OptionalInt(values, "intervalSeconds");
        int? maxIterations = OptionalInt(values, "maxIterations");

        if (intervalSeconds is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(args), "--intervalSeconds must be zero or greater.");
        }

        if (maxIterations is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(args), "--maxIterations must be greater than zero.");
        }

        bool runOnce = flags.Contains("once") || !intervalSeconds.HasValue;

        return new LocalBridgeOptions(
            showHelp,
            server.TrimEnd('/'),
            userId,
            timezoneId,
            OptionalPath(values, "windowsDb"),
            OptionalPath(values, "androidDb"),
            runOnce,
            intervalSeconds,
            maxIterations,
            OptionalPath(values, "checkpointPath"));
    }

    public static void WriteUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project tools/Woong.MonitorStack.LocalDashboardBridge -- --server http://127.0.0.1:5087 --userId local-user --windowsDb <windows-local.db> --androidDb <woong-monitor.db> [--once|--intervalSeconds 5 --maxIterations 12] [--checkpointPath <bridge-checkpoints.json>]");
        Console.WriteLine();
        Console.WriteLine("Uploads local WPF SQLite and Android emulator Room metadata to the local ASP.NET Core server through API DTO contracts.");
    }

    private static string ValueOrDefault(Dictionary<string, string> values, string key, string fallback)
        => values.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;

    private static string? OptionalPath(Dictionary<string, string> values, string key)
        => values.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;

    private static int? OptionalInt(Dictionary<string, string> values, string key)
        => values.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? int.Parse(value, CultureInfo.InvariantCulture)
            : null;
}

public static class LocalBridgeCheckpointKeys
{
    public const string WindowsFocusSession = "windows.focus_session";
    public const string WindowsWebSession = "windows.web_session";
    public const string WindowsCurrentAppState = "windows.current_app_state";
    public const string AndroidFocusSession = "android.focus_sessions";
    public const string AndroidLocationContext = "android.location_context_snapshots";
    public const string AndroidCurrentAppState = "android.current_app_states";
}

public sealed class LocalBridgeCheckpointStore
{
    private readonly string? _path;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly Dictionary<string, LocalBridgeCheckpointCursor> _sources;
    private bool _changed;

    private LocalBridgeCheckpointStore(
        string? path,
        JsonSerializerOptions jsonOptions,
        Dictionary<string, LocalBridgeCheckpointCursor> sources)
    {
        _path = path;
        _jsonOptions = jsonOptions;
        _sources = sources;
    }

    public static LocalBridgeCheckpointStore? Load(
        string? path,
        JsonSerializerOptions jsonOptions)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        Dictionary<string, LocalBridgeCheckpointCursor> sources = [];
        if (File.Exists(path))
        {
            LocalBridgeCheckpointDocument? document = JsonSerializer.Deserialize<LocalBridgeCheckpointDocument>(
                File.ReadAllText(path),
                jsonOptions);
            sources = document?.Sources is null
                ? []
                : new Dictionary<string, LocalBridgeCheckpointCursor>(document.Sources, StringComparer.Ordinal);
        }

        return new LocalBridgeCheckpointStore(path, jsonOptions, sources);
    }

    public LocalBridgeCheckpointCursor? Get(string key)
        => _sources.TryGetValue(key, out LocalBridgeCheckpointCursor? cursor)
            ? cursor
            : null;

    public void AdvanceAfterSuccessfulUpload(
        string key,
        LocalBridgeCheckpointCursor? cursor,
        UploadStatusSummary uploadSummary)
    {
        if (cursor is null || uploadSummary.Error > 0)
        {
            return;
        }

        if (!_sources.TryGetValue(key, out LocalBridgeCheckpointCursor? current) || cursor.CompareTo(current) > 0)
        {
            _sources[key] = cursor;
            _changed = true;
        }
    }

    public void Save()
    {
        if (!_changed || string.IsNullOrWhiteSpace(_path))
        {
            return;
        }

        string? directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var document = new LocalBridgeCheckpointDocument(1, _sources);
        File.WriteAllText(_path, JsonSerializer.Serialize(document, _jsonOptions));
        _changed = false;
    }
}

public sealed record LocalBridgeCheckpointDocument(
    int Version,
    Dictionary<string, LocalBridgeCheckpointCursor> Sources);

public sealed record LocalBridgeCheckpointCursor(
    DateTimeOffset TimestampUtc,
    string Id,
    DateTimeOffset? SecondaryTimestampUtc = null)
{
    public int CompareTo(LocalBridgeCheckpointCursor other)
    {
        int timestampComparison = TimestampUtc.CompareTo(other.TimestampUtc);
        if (timestampComparison != 0)
        {
            return timestampComparison;
        }

        int idComparison = string.Compare(Id, other.Id, StringComparison.Ordinal);
        if (idComparison != 0)
        {
            return idComparison;
        }

        DateTimeOffset secondary = SecondaryTimestampUtc ?? DateTimeOffset.MinValue;
        DateTimeOffset otherSecondary = other.SecondaryTimestampUtc ?? DateTimeOffset.MinValue;
        return secondary.CompareTo(otherSecondary);
    }

    public static LocalBridgeCheckpointCursor? FromFocusSessions(
        IReadOnlyList<FocusSessionUploadItem> sessions)
        => FromItems(sessions, session => session.EndedAtUtc, session => session.ClientSessionId);

    public static LocalBridgeCheckpointCursor? FromWebSessions(
        IReadOnlyList<WebSessionUploadItem> sessions)
        => FromItems(
            sessions,
            session => session.EndedAtUtc,
            session => session.FocusSessionId,
            session => session.StartedAtUtc);

    public static LocalBridgeCheckpointCursor? FromLocationContexts(
        IReadOnlyList<LocationContextUploadItem> contexts)
        => FromItems(contexts, context => context.CapturedAtUtc, context => context.ClientContextId);

    public static LocalBridgeCheckpointCursor? FromCurrentAppStates(
        IReadOnlyList<CurrentAppStateUploadItem> states)
        => FromItems(states, state => state.ObservedAtUtc, state => state.ClientStateId);

    private static LocalBridgeCheckpointCursor? FromItems<T>(
        IReadOnlyList<T> items,
        Func<T, DateTimeOffset> timestamp,
        Func<T, string> id,
        Func<T, DateTimeOffset>? secondaryTimestamp = null)
    {
        LocalBridgeCheckpointCursor? cursor = null;
        foreach (T item in items)
        {
            var candidate = new LocalBridgeCheckpointCursor(
                timestamp(item).ToUniversalTime(),
                id(item),
                secondaryTimestamp?.Invoke(item).ToUniversalTime());
            if (cursor is null || candidate.CompareTo(cursor) > 0)
            {
                cursor = candidate;
            }
        }

        return cursor;
    }
}

public static class WindowsSqliteReader
{
    public static LocalUploadBatch Read(
        string databasePath,
        string serverDeviceId,
        string fallbackTimezoneId,
        LocalBridgeCheckpointStore? checkpoints = null)
    {
        if (!File.Exists(databasePath))
        {
            return LocalUploadBatch.Empty;
        }

        using var connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();

        IReadOnlyList<FocusSessionUploadItem> focusSessions = TableExists(connection, "focus_session")
            ? ReadFocusSessions(
                connection,
                serverDeviceId,
                fallbackTimezoneId,
                checkpoints?.Get(LocalBridgeCheckpointKeys.WindowsFocusSession))
            : [];
        IReadOnlyList<WebSessionUploadItem> webSessions = TableExists(connection, "web_session")
            ? ReadWebSessions(
                connection,
                checkpoints?.Get(LocalBridgeCheckpointKeys.WindowsWebSession))
            : [];
        IReadOnlyList<CurrentAppStateUploadItem> currentAppStates = TableExists(connection, "current_app_state")
            ? ReadCurrentAppStates(
                connection,
                fallbackTimezoneId,
                checkpoints?.Get(LocalBridgeCheckpointKeys.WindowsCurrentAppState))
            : [];

        return new LocalUploadBatch(focusSessions, webSessions, [], currentAppStates);
    }

    private static IReadOnlyList<FocusSessionUploadItem> ReadFocusSessions(
        SqliteConnection connection,
        string serverDeviceId,
        string fallbackTimezoneId,
        LocalBridgeCheckpointCursor? checkpoint)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT client_session_id, platform_app_key, started_at_utc, ended_at_utc,
                   duration_ms, local_date, timezone_id, is_idle, source,
                   process_id, process_name, process_path, window_handle, window_title
            FROM focus_session
            WHERE duration_ms > 0
              AND (
                  $cursorTimestamp IS NULL
                  OR julianday(ended_at_utc) > julianday($cursorTimestamp)
                  OR (
                      julianday(ended_at_utc) = julianday($cursorTimestamp)
                      AND client_session_id > $cursorId
                  )
              )
            ORDER BY ended_at_utc, client_session_id
            """;
        AddCursorParameters(command, checkpoint);

        using SqliteDataReader reader = command.ExecuteReader();
        var sessions = new List<FocusSessionUploadItem>();
        while (reader.Read())
        {
            sessions.Add(new FocusSessionUploadItem(
                clientSessionId: reader.GetString(0),
                platformAppKey: reader.GetString(1),
                startedAtUtc: DateTimeOffset.Parse(reader.GetString(2), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                endedAtUtc: DateTimeOffset.Parse(reader.GetString(3), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                durationMs: reader.GetInt64(4),
                localDate: ParseDateOnly(reader.GetString(5)),
                timezoneId: ReadOptionalString(reader, 6) ?? fallbackTimezoneId,
                isIdle: ReadBoolean(reader, 7),
                source: ReadOptionalString(reader, 8) ?? "windows_local_sqlite",
                processId: ReadOptionalInt(reader, 9),
                processName: ReadOptionalString(reader, 10),
                processPath: ReadOptionalString(reader, 11),
                windowHandle: ReadOptionalLong(reader, 12),
                windowTitle: ReadOptionalString(reader, 13)));
        }

        return sessions;
    }

    private static IReadOnlyList<WebSessionUploadItem> ReadWebSessions(
        SqliteConnection connection,
        LocalBridgeCheckpointCursor? checkpoint)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT focus_session_id, browser_family, url, domain, page_title,
                   started_at_utc, ended_at_utc, duration_ms,
                   capture_method, capture_confidence, is_private_or_unknown
            FROM web_session
            WHERE duration_ms > 0
              AND (
                  $cursorTimestamp IS NULL
                  OR julianday(ended_at_utc) > julianday($cursorTimestamp)
                  OR (
                      julianday(ended_at_utc) = julianday($cursorTimestamp)
                      AND (
                          focus_session_id > $cursorId
                          OR (
                              focus_session_id = $cursorId
                              AND (
                                  $cursorSecondaryTimestamp IS NULL
                                  OR julianday(started_at_utc) > julianday($cursorSecondaryTimestamp)
                              )
                          )
                      )
                  )
              )
            ORDER BY ended_at_utc, focus_session_id, started_at_utc
            """;
        AddCursorParameters(command, checkpoint);

        using SqliteDataReader reader = command.ExecuteReader();
        var sessions = new List<WebSessionUploadItem>();
        while (reader.Read())
        {
            string focusSessionId = reader.GetString(0);
            DateTimeOffset startedAtUtc = DateTimeOffset.Parse(reader.GetString(5), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            sessions.Add(new WebSessionUploadItem(
                clientSessionId: $"windows-web:{focusSessionId}:{startedAtUtc:yyyyMMddHHmmssfffffff}",
                focusSessionId: focusSessionId,
                browserFamily: reader.GetString(1),
                url: ReadOptionalString(reader, 2),
                domain: reader.GetString(3),
                pageTitle: ReadOptionalString(reader, 4),
                startedAtUtc: startedAtUtc,
                endedAtUtc: DateTimeOffset.Parse(reader.GetString(6), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                durationMs: reader.GetInt64(7),
                captureMethod: ReadOptionalString(reader, 8),
                captureConfidence: ReadOptionalString(reader, 9),
                isPrivateOrUnknown: reader.IsDBNull(10) ? null : ReadBoolean(reader, 10)));
        }

        return sessions;
    }

    private static IReadOnlyList<CurrentAppStateUploadItem> ReadCurrentAppStates(
        SqliteConnection connection,
        string fallbackTimezoneId,
        LocalBridgeCheckpointCursor? checkpoint)
    {
        string windowTitleProjection = ColumnExists(connection, "current_app_state", "window_title")
            ? "window_title"
            : "NULL AS window_title";
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT client_state_id, platform_app_key, observed_at_utc, local_date,
                   timezone_id, status, source, process_id, process_name,
                   process_path, window_handle, {windowTitleProjection}
            FROM current_app_state
            WHERE (
                $cursorTimestamp IS NULL
                OR julianday(observed_at_utc) > julianday($cursorTimestamp)
                OR (
                    julianday(observed_at_utc) = julianday($cursorTimestamp)
                    AND client_state_id > $cursorId
                )
            )
            ORDER BY observed_at_utc, client_state_id
            """;
        AddCursorParameters(command, checkpoint);

        using SqliteDataReader reader = command.ExecuteReader();
        var states = new List<CurrentAppStateUploadItem>();
        while (reader.Read())
        {
            states.Add(new CurrentAppStateUploadItem(
                clientStateId: reader.GetString(0),
                platform: Platform.Windows,
                platformAppKey: reader.GetString(1),
                observedAtUtc: DateTimeOffset.Parse(reader.GetString(2), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                localDate: ParseDateOnly(reader.GetString(3)),
                timezoneId: ReadOptionalString(reader, 4) ?? fallbackTimezoneId,
                status: ReadOptionalString(reader, 5) ?? "Active",
                source: ReadOptionalString(reader, 6) ?? "windows_current_app_state",
                processId: ReadOptionalInt(reader, 7),
                processName: ReadOptionalString(reader, 8),
                processPath: ReadOptionalString(reader, 9),
                windowHandle: ReadOptionalLong(reader, 10),
                windowTitle: ReadOptionalString(reader, 11)));
        }

        return states;
    }

    private static bool TableExists(SqliteConnection connection, string tableName)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name";
        command.Parameters.AddWithValue("$name", tableName);
        return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
    }

    private static bool ColumnExists(SqliteConnection connection, string tableName, string columnName)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{tableName}') WHERE name = $columnName";
        command.Parameters.AddWithValue("$columnName", columnName);
        return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
    }

    private static void AddCursorParameters(
        SqliteCommand command,
        LocalBridgeCheckpointCursor? checkpoint)
    {
        command.Parameters.AddWithValue(
            "$cursorTimestamp",
            checkpoint is null
                ? DBNull.Value
                : checkpoint.TimestampUtc.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$cursorId", checkpoint?.Id ?? string.Empty);
        command.Parameters.AddWithValue(
            "$cursorSecondaryTimestamp",
            checkpoint?.SecondaryTimestampUtc is null
                ? DBNull.Value
                : checkpoint.SecondaryTimestampUtc.Value.ToString("O", CultureInfo.InvariantCulture));
    }

    private static DateOnly ParseDateOnly(string value)
        => DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string? ReadOptionalString(SqliteDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static int? ReadOptionalInt(SqliteDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);

    private static long? ReadOptionalLong(SqliteDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);

    private static bool ReadBoolean(SqliteDataReader reader, int ordinal)
        => reader.GetFieldValue<object>(ordinal) switch
        {
            long value => value != 0,
            int value => value != 0,
            bool value => value,
            string value => bool.Parse(value),
            _ => false
        };
}

public static class AndroidRoomReader
{
    public static LocalUploadBatch Read(
        string databasePath,
        string serverDeviceId,
        string fallbackTimezoneId,
        LocalBridgeCheckpointStore? checkpoints = null)
    {
        if (!File.Exists(databasePath))
        {
            return LocalUploadBatch.Empty;
        }

        using var connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();

        IReadOnlyList<FocusSessionUploadItem> focusSessions = TableExists(connection, "focus_sessions")
            ? ReadFocusSessions(
                connection,
                fallbackTimezoneId,
                checkpoints?.Get(LocalBridgeCheckpointKeys.AndroidFocusSession))
            : [];
        IReadOnlyList<LocationContextUploadItem> locationContexts = TableExists(connection, "location_context_snapshots")
            ? ReadLocationContexts(
                connection,
                fallbackTimezoneId,
                checkpoints?.Get(LocalBridgeCheckpointKeys.AndroidLocationContext))
            : [];
        IReadOnlyList<CurrentAppStateUploadItem> currentAppStates = TableExists(connection, "current_app_states")
            ? ReadCurrentAppStates(
                connection,
                fallbackTimezoneId,
                checkpoints?.Get(LocalBridgeCheckpointKeys.AndroidCurrentAppState))
            : [];

        return new LocalUploadBatch(focusSessions, [], locationContexts, currentAppStates);
    }

    private static IReadOnlyList<FocusSessionUploadItem> ReadFocusSessions(
        SqliteConnection connection,
        string fallbackTimezoneId,
        LocalBridgeCheckpointCursor? checkpoint)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT clientSessionId, packageName, startedAtUtcMillis, endedAtUtcMillis,
                   durationMs, localDate, timezoneId, isIdle, source
            FROM focus_sessions
            WHERE durationMs > 0
              AND (
                  $cursorMillis IS NULL
                  OR endedAtUtcMillis > $cursorMillis
                  OR (endedAtUtcMillis = $cursorMillis AND clientSessionId > $cursorId)
              )
            ORDER BY endedAtUtcMillis, clientSessionId
            """;
        AddCursorParameters(command, checkpoint);

        using SqliteDataReader reader = command.ExecuteReader();
        var sessions = new List<FocusSessionUploadItem>();
        while (reader.Read())
        {
            sessions.Add(new FocusSessionUploadItem(
                clientSessionId: reader.GetString(0),
                platformAppKey: reader.GetString(1),
                startedAtUtc: DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(2)),
                endedAtUtc: DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(3)),
                durationMs: reader.GetInt64(4),
                localDate: DateOnly.ParseExact(reader.GetString(5), "yyyy-MM-dd", CultureInfo.InvariantCulture),
                timezoneId: ReadOptionalString(reader, 6) ?? fallbackTimezoneId,
                isIdle: ReadBoolean(reader, 7),
                source: ReadOptionalString(reader, 8) ?? "android_room"));
        }

        return sessions;
    }

    private static IReadOnlyList<LocationContextUploadItem> ReadLocationContexts(
        SqliteConnection connection,
        string fallbackTimezoneId,
        LocalBridgeCheckpointCursor? checkpoint)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, capturedAtUtcMillis, latitude, longitude, accuracyMeters,
                   permissionState, captureMode
            FROM location_context_snapshots
            WHERE (
                $cursorMillis IS NULL
                OR capturedAtUtcMillis > $cursorMillis
                OR (capturedAtUtcMillis = $cursorMillis AND id > $cursorId)
            )
            ORDER BY capturedAtUtcMillis, id
            """;
        AddCursorParameters(command, checkpoint);

        using SqliteDataReader reader = command.ExecuteReader();
        var contexts = new List<LocationContextUploadItem>();
        while (reader.Read())
        {
            DateTimeOffset capturedAtUtc = DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(1));
            DateOnly localDate = DateOnly.FromDateTime(
                TimeZoneInfo.ConvertTime(capturedAtUtc, ResolveTimeZone(fallbackTimezoneId)).DateTime);
            contexts.Add(new LocationContextUploadItem(
                clientContextId: reader.GetString(0),
                capturedAtUtc: capturedAtUtc,
                localDate: localDate,
                timezoneId: fallbackTimezoneId,
                latitude: reader.IsDBNull(2) ? null : reader.GetDouble(2),
                longitude: reader.IsDBNull(3) ? null : reader.GetDouble(3),
                accuracyMeters: reader.IsDBNull(4) ? null : reader.GetDouble(4),
                captureMode: ReadEnumText(reader, 6, "AppUsageContext"),
                permissionState: ReadEnumText(reader, 5, "GrantedPrecise"),
                source: "android_room"));
        }

        return contexts;
    }

    private static IReadOnlyList<CurrentAppStateUploadItem> ReadCurrentAppStates(
        SqliteConnection connection,
        string fallbackTimezoneId,
        LocalBridgeCheckpointCursor? checkpoint)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT clientStateId, packageName, observedAtUtcMillis, localDate,
                   timezoneId, status, source
            FROM current_app_states
            WHERE (
                $cursorMillis IS NULL
                OR observedAtUtcMillis > $cursorMillis
                OR (observedAtUtcMillis = $cursorMillis AND clientStateId > $cursorId)
            )
            ORDER BY observedAtUtcMillis, clientStateId
            """;
        AddCursorParameters(command, checkpoint);

        using SqliteDataReader reader = command.ExecuteReader();
        var states = new List<CurrentAppStateUploadItem>();
        while (reader.Read())
        {
            states.Add(new CurrentAppStateUploadItem(
                clientStateId: reader.GetString(0),
                platform: Platform.Android,
                platformAppKey: reader.GetString(1),
                observedAtUtc: DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(2)),
                localDate: DateOnly.ParseExact(reader.GetString(3), "yyyy-MM-dd", CultureInfo.InvariantCulture),
                timezoneId: ReadOptionalString(reader, 4) ?? fallbackTimezoneId,
                status: ReadOptionalString(reader, 5) ?? "Active",
                source: ReadOptionalString(reader, 6) ?? "android_current_app_state"));
        }

        return states;
    }

    private static bool TableExists(SqliteConnection connection, string tableName)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name";
        command.Parameters.AddWithValue("$name", tableName);
        return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
    }

    private static void AddCursorParameters(
        SqliteCommand command,
        LocalBridgeCheckpointCursor? checkpoint)
    {
        command.Parameters.AddWithValue(
            "$cursorMillis",
            checkpoint is null
                ? DBNull.Value
                : checkpoint.TimestampUtc.ToUnixTimeMilliseconds());
        command.Parameters.AddWithValue("$cursorId", checkpoint?.Id ?? string.Empty);
    }

    private static TimeZoneInfo ResolveTimeZone(string timezoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }

    private static string? ReadOptionalString(SqliteDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static string ReadEnumText(SqliteDataReader reader, int ordinal, string fallback)
    {
        if (reader.IsDBNull(ordinal))
        {
            return fallback;
        }

        object value = reader.GetFieldValue<object>(ordinal);
        return value switch
        {
            string text when !string.IsNullOrWhiteSpace(text) => text,
            long number => number.ToString(CultureInfo.InvariantCulture),
            int number => number.ToString(CultureInfo.InvariantCulture),
            _ => fallback
        };
    }

    private static bool ReadBoolean(SqliteDataReader reader, int ordinal)
        => reader.GetFieldValue<object>(ordinal) switch
        {
            long value => value != 0,
            int value => value != 0,
            bool value => value,
            string value => bool.Parse(value),
            _ => false
        };
}
