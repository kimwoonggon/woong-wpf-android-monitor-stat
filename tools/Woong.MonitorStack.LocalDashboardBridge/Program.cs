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

LocalBridgeSummary summary = new();

if (!string.IsNullOrWhiteSpace(options.WindowsDatabasePath))
{
    DeviceRegistrationResponse windowsDevice = await RegisterDeviceAsync(
        httpClient,
        options.UserId,
        Platform.Windows,
        $"{options.UserId}-windows-local",
        "Windows WPF Local",
        options.TimezoneId,
        jsonOptions);

    LocalUploadBatch windowsBatch = WindowsSqliteReader.Read(
        options.WindowsDatabasePath,
        windowsDevice.DeviceId,
        options.TimezoneId);

    summary.WindowsFocusUploaded = await UploadFocusSessionsAsync(
        httpClient,
        windowsDevice,
        windowsBatch.FocusSessions,
        jsonOptions);
    summary.WindowsWebUploaded = await UploadWebSessionsAsync(
        httpClient,
        windowsDevice,
        windowsBatch.WebSessions,
        jsonOptions);
}

if (!string.IsNullOrWhiteSpace(options.AndroidDatabasePath))
{
    DeviceRegistrationResponse androidDevice = await RegisterDeviceAsync(
        httpClient,
        options.UserId,
        Platform.Android,
        $"{options.UserId}-android-emulator",
        "Android Emulator Local",
        options.TimezoneId,
        jsonOptions);

    LocalUploadBatch androidBatch = AndroidRoomReader.Read(
        options.AndroidDatabasePath,
        androidDevice.DeviceId,
        options.TimezoneId);

    summary.AndroidFocusUploaded = await UploadFocusSessionsAsync(
        httpClient,
        androidDevice,
        androidBatch.FocusSessions,
        jsonOptions);
    summary.AndroidLocationUploaded = await UploadLocationContextsAsync(
        httpClient,
        androidDevice,
        androidBatch.LocationContexts,
        jsonOptions);
}

Console.WriteLine(JsonSerializer.Serialize(summary, jsonOptions));
return 0;

static async Task<DeviceRegistrationResponse> RegisterDeviceAsync(
    HttpClient httpClient,
    string userId,
    Platform platform,
    string deviceKey,
    string deviceName,
    string timezoneId,
    JsonSerializerOptions jsonOptions)
{
    var request = new RegisterDeviceRequest(userId, platform, deviceKey, deviceName, timezoneId);
    using HttpResponseMessage response = await httpClient.PostAsJsonAsync("/api/devices/register", request, jsonOptions);
    response.EnsureSuccessStatusCode();

    return await response.Content.ReadFromJsonAsync<DeviceRegistrationResponse>(jsonOptions)
        ?? throw new InvalidOperationException("Device registration returned an empty response.");
}

static async Task<int> UploadFocusSessionsAsync(
    HttpClient httpClient,
    DeviceRegistrationResponse device,
    IReadOnlyList<FocusSessionUploadItem> sessions,
    JsonSerializerOptions jsonOptions)
{
    if (sessions.Count == 0)
    {
        return 0;
    }

    using HttpRequestMessage request = CreateJsonPost(
        "/api/focus-sessions/upload",
        new UploadFocusSessionsRequest(device.DeviceId, sessions),
        device.DeviceToken,
        jsonOptions);
    using HttpResponseMessage response = await httpClient.SendAsync(request);
    response.EnsureSuccessStatusCode();

    return sessions.Count;
}

static async Task<int> UploadWebSessionsAsync(
    HttpClient httpClient,
    DeviceRegistrationResponse device,
    IReadOnlyList<WebSessionUploadItem> sessions,
    JsonSerializerOptions jsonOptions)
{
    if (sessions.Count == 0)
    {
        return 0;
    }

    using HttpRequestMessage request = CreateJsonPost(
        "/api/web-sessions/upload",
        new UploadWebSessionsRequest(device.DeviceId, sessions),
        device.DeviceToken,
        jsonOptions);
    using HttpResponseMessage response = await httpClient.SendAsync(request);
    response.EnsureSuccessStatusCode();

    return sessions.Count;
}

static async Task<int> UploadLocationContextsAsync(
    HttpClient httpClient,
    DeviceRegistrationResponse device,
    IReadOnlyList<LocationContextUploadItem> contexts,
    JsonSerializerOptions jsonOptions)
{
    if (contexts.Count == 0)
    {
        return 0;
    }

    using HttpRequestMessage request = CreateJsonPost(
        "/api/location-contexts/upload",
        new UploadLocationContextsRequest(device.DeviceId, contexts),
        device.DeviceToken,
        jsonOptions);
    using HttpResponseMessage response = await httpClient.SendAsync(request);
    response.EnsureSuccessStatusCode();

    return contexts.Count;
}

static HttpRequestMessage CreateJsonPost<T>(
    string path,
    T body,
    string deviceToken,
    JsonSerializerOptions jsonOptions)
{
    var request = new HttpRequestMessage(HttpMethod.Post, path)
    {
        Content = JsonContent.Create(body, options: jsonOptions)
    };
    request.Headers.TryAddWithoutValidation("X-Device-Token", deviceToken);
    return request;
}

public sealed record DeviceRegistrationResponse(string DeviceId, string DeviceToken);

public sealed class LocalBridgeSummary
{
    public int WindowsFocusUploaded { get; set; }
    public int WindowsWebUploaded { get; set; }
    public int AndroidFocusUploaded { get; set; }
    public int AndroidLocationUploaded { get; set; }
}

public sealed record LocalUploadBatch(
    IReadOnlyList<FocusSessionUploadItem> FocusSessions,
    IReadOnlyList<WebSessionUploadItem> WebSessions,
    IReadOnlyList<LocationContextUploadItem> LocationContexts)
{
    public static LocalUploadBatch Empty { get; } = new([], [], []);
}

public sealed class LocalBridgeOptions
{
    private LocalBridgeOptions(
        bool showHelp,
        string serverBaseUrl,
        string userId,
        string timezoneId,
        string? windowsDatabasePath,
        string? androidDatabasePath)
    {
        ShowHelp = showHelp;
        ServerBaseUrl = serverBaseUrl;
        UserId = userId;
        TimezoneId = timezoneId;
        WindowsDatabasePath = windowsDatabasePath;
        AndroidDatabasePath = androidDatabasePath;
    }

    public bool ShowHelp { get; }

    public string ServerBaseUrl { get; }

    public string UserId { get; }

    public string TimezoneId { get; }

    public string? WindowsDatabasePath { get; }

    public string? AndroidDatabasePath { get; }

    public static LocalBridgeOptions Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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
        }

        string server = ValueOrDefault(values, "server", "http://127.0.0.1:5087");
        string userId = ValueOrDefault(values, "userId", "local-user");
        string timezoneId = ValueOrDefault(values, "timezoneId", TimeZoneInfo.Local.Id);

        return new LocalBridgeOptions(
            showHelp,
            server.TrimEnd('/'),
            userId,
            timezoneId,
            OptionalPath(values, "windowsDb"),
            OptionalPath(values, "androidDb"));
    }

    public static void WriteUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project tools/Woong.MonitorStack.LocalDashboardBridge -- --server http://127.0.0.1:5087 --userId local-user --windowsDb <windows-local.db> --androidDb <woong-monitor.db>");
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
}

public static class WindowsSqliteReader
{
    public static LocalUploadBatch Read(string databasePath, string serverDeviceId, string fallbackTimezoneId)
    {
        if (!File.Exists(databasePath))
        {
            return LocalUploadBatch.Empty;
        }

        using var connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();

        IReadOnlyList<FocusSessionUploadItem> focusSessions = TableExists(connection, "focus_session")
            ? ReadFocusSessions(connection, serverDeviceId, fallbackTimezoneId)
            : [];
        IReadOnlyList<WebSessionUploadItem> webSessions = TableExists(connection, "web_session")
            ? ReadWebSessions(connection)
            : [];

        return new LocalUploadBatch(focusSessions, webSessions, []);
    }

    private static IReadOnlyList<FocusSessionUploadItem> ReadFocusSessions(
        SqliteConnection connection,
        string serverDeviceId,
        string fallbackTimezoneId)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT client_session_id, platform_app_key, started_at_utc, ended_at_utc,
                   duration_ms, local_date, timezone_id, is_idle, source,
                   process_id, process_name, process_path, window_handle, window_title
            FROM focus_session
            WHERE duration_ms > 0
            ORDER BY started_at_utc
            """;

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

    private static IReadOnlyList<WebSessionUploadItem> ReadWebSessions(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT focus_session_id, browser_family, url, domain, page_title,
                   started_at_utc, ended_at_utc, duration_ms,
                   capture_method, capture_confidence, is_private_or_unknown
            FROM web_session
            WHERE duration_ms > 0
            ORDER BY started_at_utc
            """;

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

    private static bool TableExists(SqliteConnection connection, string tableName)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name";
        command.Parameters.AddWithValue("$name", tableName);
        return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
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
    public static LocalUploadBatch Read(string databasePath, string serverDeviceId, string fallbackTimezoneId)
    {
        if (!File.Exists(databasePath))
        {
            return LocalUploadBatch.Empty;
        }

        using var connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();

        IReadOnlyList<FocusSessionUploadItem> focusSessions = TableExists(connection, "focus_sessions")
            ? ReadFocusSessions(connection, fallbackTimezoneId)
            : [];
        IReadOnlyList<LocationContextUploadItem> locationContexts = TableExists(connection, "location_context_snapshots")
            ? ReadLocationContexts(connection, fallbackTimezoneId)
            : [];

        return new LocalUploadBatch(focusSessions, [], locationContexts);
    }

    private static IReadOnlyList<FocusSessionUploadItem> ReadFocusSessions(
        SqliteConnection connection,
        string fallbackTimezoneId)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT clientSessionId, packageName, startedAtUtcMillis, endedAtUtcMillis,
                   durationMs, localDate, timezoneId, isIdle, source
            FROM focus_sessions
            WHERE durationMs > 0
            ORDER BY startedAtUtcMillis
            """;

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
        string fallbackTimezoneId)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, capturedAtUtcMillis, latitude, longitude, accuracyMeters,
                   permissionState, captureMode
            FROM location_context_snapshots
            ORDER BY capturedAtUtcMillis
            """;

        using SqliteDataReader reader = command.ExecuteReader();
        var contexts = new List<LocationContextUploadItem>();
        while (reader.Read())
        {
            DateTimeOffset capturedAtUtc = DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(1));
            contexts.Add(new LocationContextUploadItem(
                clientContextId: reader.GetString(0),
                capturedAtUtc: capturedAtUtc,
                localDate: DateOnly.FromDateTime(capturedAtUtc.LocalDateTime),
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

    private static bool TableExists(SqliteConnection connection, string tableName)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name";
        command.Parameters.AddWithValue("$name", tableName);
        return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
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
