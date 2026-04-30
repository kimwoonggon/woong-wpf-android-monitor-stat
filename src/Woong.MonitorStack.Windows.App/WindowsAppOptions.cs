using System.IO;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App;

public sealed class WindowsAppOptions
{
    public const string LocalDbEnvironmentVariable = "WOONG_MONITOR_LOCAL_DB";

    public const string DeviceIdEnvironmentVariable = "WOONG_MONITOR_DEVICE_ID";

    public const string AcceptanceModeEnvironmentVariable = "WOONG_MONITOR_ACCEPTANCE_MODE";

    public const string AutoStartTrackingEnvironmentVariable = "WOONG_MONITOR_AUTO_START_TRACKING";

    public WindowsAppOptions(
        DashboardOptions dashboardOptions,
        string deviceId,
        string localDatabaseConnectionString,
        TimeSpan idleThreshold,
        WindowsAppAcceptanceMode acceptanceMode = WindowsAppAcceptanceMode.None,
        bool autoStartTracking = true)
        : this(
            dashboardOptions,
            deviceId,
            ExtractDataSource(localDatabaseConnectionString),
            localDatabaseConnectionString,
            idleThreshold,
            acceptanceMode,
            autoStartTracking)
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
        string? runtimeLogPath = null)
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
    }

    public DashboardOptions DashboardOptions { get; }

    public string DeviceId { get; }

    public string LocalDatabasePath { get; }

    public string LocalDatabaseConnectionString { get; }

    public TimeSpan IdleThreshold { get; }

    public WindowsAppAcceptanceMode AcceptanceMode { get; }

    public bool AutoStartTracking { get; }

    public string RuntimeLogPath { get; }

    public static WindowsAppOptions CreateDefault(DashboardOptions dashboardOptions)
    {
        ArgumentNullException.ThrowIfNull(dashboardOptions);

        string? deviceId = Environment.GetEnvironmentVariable(DeviceIdEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            deviceId = $"windows-{Environment.MachineName}";
        }

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
                autoStartTracking: ParseAutoStartTracking());
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
            autoStartTracking: ParseAutoStartTracking());
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
}

public enum WindowsAppAcceptanceMode
{
    None = 0,
    TrackingPipeline = 1,
    SampleDashboard = 2
}
