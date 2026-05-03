using System.IO;
using Woong.MonitorStack.Windows.Presentation.Dashboard;
using Woong.MonitorStack.Windows.Sync;

namespace Woong.MonitorStack.Windows.App;

public sealed class WindowsAppOptions
{
    public const string LocalDbEnvironmentVariable = "WOONG_MONITOR_LOCAL_DB";

    public const string DeviceIdEnvironmentVariable = "WOONG_MONITOR_DEVICE_ID";

    public const string AcceptanceModeEnvironmentVariable = "WOONG_MONITOR_ACCEPTANCE_MODE";

    public const string AutoStartTrackingEnvironmentVariable = "WOONG_MONITOR_AUTO_START_TRACKING";

    public const string SyncBaseUrlEnvironmentVariable = "WOONG_MONITOR_SYNC_BASE_URL";

    public const string DeviceTokenEnvironmentVariable = "WOONG_MONITOR_DEVICE_TOKEN";

    public WindowsAppOptions(
        DashboardOptions dashboardOptions,
        string deviceId,
        string localDatabaseConnectionString,
        TimeSpan idleThreshold,
        WindowsAppAcceptanceMode acceptanceMode = WindowsAppAcceptanceMode.None,
        bool autoStartTracking = true,
        WindowsAppSyncOptions? syncOptions = null)
        : this(
            dashboardOptions,
            deviceId,
            ExtractDataSource(localDatabaseConnectionString),
            localDatabaseConnectionString,
            idleThreshold,
            acceptanceMode,
            autoStartTracking,
            syncOptions: syncOptions)
    {
    }

    public WindowsAppOptions(
        DashboardOptions dashboardOptions,
        string deviceId,
        string localDatabasePath,
        string localDatabaseConnectionString,
        TimeSpan idleThreshold,
        WindowsAppAcceptanceMode acceptanceMode = WindowsAppAcceptanceMode.None,
        bool autoStartTracking = true,
        string? runtimeLogPath = null,
        WindowsAppSyncOptions? syncOptions = null)
    {
        DashboardOptions = dashboardOptions ?? throw new ArgumentNullException(nameof(dashboardOptions));
        DeviceId = string.IsNullOrWhiteSpace(deviceId)
            ? throw new ArgumentException("Device id must not be empty.", nameof(deviceId))
            : deviceId;
        LocalDatabasePath = string.IsNullOrWhiteSpace(localDatabasePath)
            ? throw new ArgumentException("Local database path must not be empty.", nameof(localDatabasePath))
            : localDatabasePath;
        LocalDatabaseConnectionString = string.IsNullOrWhiteSpace(localDatabaseConnectionString)
            ? throw new ArgumentException("Connection string must not be empty.", nameof(localDatabaseConnectionString))
            : localDatabaseConnectionString;
        IdleThreshold = idleThreshold > TimeSpan.Zero
            ? idleThreshold
            : throw new ArgumentOutOfRangeException(nameof(idleThreshold));
        AcceptanceMode = acceptanceMode;
        AutoStartTracking = autoStartTracking;
        RuntimeLogPath = string.IsNullOrWhiteSpace(runtimeLogPath)
            ? BuildDefaultRuntimeLogPath(LocalDatabasePath)
            : runtimeLogPath;
        SyncOptions = syncOptions ?? WindowsAppSyncOptions.LocalOnly;
    }

    public DashboardOptions DashboardOptions { get; }

    public string DeviceId { get; }

    public string LocalDatabasePath { get; }

    public string LocalDatabaseConnectionString { get; }

    public TimeSpan IdleThreshold { get; }

    public WindowsAppAcceptanceMode AcceptanceMode { get; }

    public bool AutoStartTracking { get; }

    public string RuntimeLogPath { get; }

    public WindowsAppSyncOptions SyncOptions { get; }

    public static WindowsAppOptions CreateDefault(DashboardOptions dashboardOptions)
    {
        ArgumentNullException.ThrowIfNull(dashboardOptions);

        string? deviceId = Environment.GetEnvironmentVariable(DeviceIdEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            deviceId = $"windows-{Environment.MachineName}";
        }

        WindowsAppSyncOptions syncOptions = ParseSyncOptions();
        string? localDbOverride = Environment.GetEnvironmentVariable(LocalDbEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(localDbOverride))
        {
            string? overrideDirectory = Path.GetDirectoryName(localDbOverride);
            if (!string.IsNullOrWhiteSpace(overrideDirectory))
            {
                Directory.CreateDirectory(overrideDirectory);
            }

            return new WindowsAppOptions(
                dashboardOptions,
                deviceId,
                localDatabasePath: localDbOverride,
                localDatabaseConnectionString: BuildConnectionString(localDbOverride),
                idleThreshold: TimeSpan.FromMinutes(5),
                acceptanceMode: ParseAcceptanceMode(),
                autoStartTracking: ParseAutoStartTracking(),
                syncOptions: syncOptions);
        }

        string dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WoongMonitorStack");
        Directory.CreateDirectory(dataDirectory);

        string localDatabasePath = Path.Combine(dataDirectory, "windows-local.db");
        return new WindowsAppOptions(
            dashboardOptions,
            deviceId,
            localDatabasePath: localDatabasePath,
            localDatabaseConnectionString: BuildConnectionString(localDatabasePath),
            idleThreshold: TimeSpan.FromMinutes(5),
            acceptanceMode: ParseAcceptanceMode(),
            autoStartTracking: ParseAutoStartTracking(),
            syncOptions: syncOptions);
    }

    public static string BuildConnectionString(string databasePath)
        => $"Data Source={databasePath};Pooling=False";

    public static string BuildDefaultRuntimeLogPath(string localDatabasePath)
    {
        string? databaseDirectory = Path.GetDirectoryName(localDatabasePath);
        string logDirectory = string.IsNullOrWhiteSpace(databaseDirectory)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WoongMonitorStack")
            : databaseDirectory;

        return Path.Combine(logDirectory, "logs", "windows-runtime.log");
    }

    private static string ExtractDataSource(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return "";
        }

        string? dataSourcePart = connectionString
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(part => part.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase));

        return dataSourcePart is null
            ? connectionString
            : dataSourcePart["Data Source=".Length..];
    }

    private static bool ParseAutoStartTracking()
    {
        string? value = Environment.GetEnvironmentVariable(AutoStartTrackingEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return value.Trim() switch
        {
            "1" => true,
            "0" => false,
            _ when bool.TryParse(value, out bool parsed) => parsed,
            _ => throw new InvalidOperationException($"Unsupported auto-start tracking value: {value}. Use true/false or 1/0.")
        };
    }

    private static WindowsAppAcceptanceMode ParseAcceptanceMode()
    {
        string? value = Environment.GetEnvironmentVariable(AcceptanceModeEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(value))
        {
            return WindowsAppAcceptanceMode.None;
        }

        return Enum.TryParse(value, ignoreCase: true, out WindowsAppAcceptanceMode mode)
            ? mode
            : throw new InvalidOperationException($"Unsupported WPF acceptance mode: {value}.");
    }

    private static WindowsAppSyncOptions ParseSyncOptions()
    {
        string? configuredBaseUrl = Environment.GetEnvironmentVariable(SyncBaseUrlEnvironmentVariable);
        string? configuredDeviceToken = Environment.GetEnvironmentVariable(DeviceTokenEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(configuredBaseUrl))
        {
            return WindowsAppSyncOptions.LocalOnly;
        }

        if (!WindowsSyncClientOptions.TryNormalizeServerBaseUri(configuredBaseUrl, out Uri? serverBaseUri))
        {
            return WindowsAppSyncOptions.RejectedEndpoint;
        }

        return WindowsAppSyncOptions.FromValidatedEndpoint(serverBaseUri!, configuredDeviceToken);
    }
}

public sealed class WindowsAppSyncOptions
{
    private const string MissingEndpointText = "No sync endpoint configured";

    private readonly string? _deviceToken;

    private WindowsAppSyncOptions(
        Uri? serverBaseUri,
        string? deviceToken,
        string endpointDisplayText,
        string configurationStatusText)
    {
        ServerBaseUri = serverBaseUri;
        _deviceToken = string.IsNullOrWhiteSpace(deviceToken) ? null : deviceToken.Trim();
        EndpointDisplayText = endpointDisplayText;
        ConfigurationStatusText = configurationStatusText;
    }

    public static WindowsAppSyncOptions LocalOnly { get; } = new(
        serverBaseUri: null,
        deviceToken: null,
        endpointDisplayText: MissingEndpointText,
        configurationStatusText: "Sync endpoint is not configured.");

    public static WindowsAppSyncOptions RejectedEndpoint { get; } = new(
        serverBaseUri: null,
        deviceToken: null,
        endpointDisplayText: "Sync endpoint rejected",
        configurationStatusText: "Sync endpoint is not safe. Use HTTPS, or loopback HTTP for local development.");

    public Uri? ServerBaseUri { get; }

    public bool HasServerEndpoint => ServerBaseUri is not null;

    public bool HasDeviceToken => !string.IsNullOrWhiteSpace(_deviceToken);

    public bool IsUploadConfigured => HasServerEndpoint && HasDeviceToken;

    public string EndpointDisplayText { get; }

    public string ConfigurationStatusText { get; }

    public WindowsSyncClientOptions CreateClientOptions()
    {
        if (!IsUploadConfigured)
        {
            throw new InvalidOperationException("Sync upload is not configured.");
        }

        return new WindowsSyncClientOptions(ServerBaseUri!, _deviceToken!);
    }

    public static WindowsAppSyncOptions FromValidatedEndpoint(Uri serverBaseUri, string? deviceToken)
    {
        Uri normalizedServerBaseUri = WindowsSyncClientOptions.TryNormalizeServerBaseUri(serverBaseUri.ToString(), out Uri? normalized)
            ? normalized!
            : throw new ArgumentException("Sync endpoint is not safe.", nameof(serverBaseUri));
        string endpointDisplayText = normalizedServerBaseUri.ToString();

        if (string.IsNullOrWhiteSpace(deviceToken))
        {
            return new WindowsAppSyncOptions(
                normalizedServerBaseUri,
                deviceToken: null,
                endpointDisplayText,
                configurationStatusText: "Sync device token is not configured.");
        }

        return new WindowsAppSyncOptions(
            normalizedServerBaseUri,
            deviceToken,
            endpointDisplayText,
            configurationStatusText: "Sync upload is configured.");
    }
}

public enum WindowsAppAcceptanceMode
{
    None = 0,
    TrackingPipeline = 1,
    SampleDashboard = 2
}
