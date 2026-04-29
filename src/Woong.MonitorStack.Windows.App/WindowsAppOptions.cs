using System.IO;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App;

public sealed class WindowsAppOptions
{
    public const string LocalDbEnvironmentVariable = "WOONG_MONITOR_LOCAL_DB";

    public const string DeviceIdEnvironmentVariable = "WOONG_MONITOR_DEVICE_ID";

    public const string AcceptanceModeEnvironmentVariable = "WOONG_MONITOR_ACCEPTANCE_MODE";

    public WindowsAppOptions(
        DashboardOptions dashboardOptions,
        string deviceId,
        string localDatabaseConnectionString,
        TimeSpan idleThreshold,
        WindowsAppAcceptanceMode acceptanceMode = WindowsAppAcceptanceMode.None)
    {
        DashboardOptions = dashboardOptions ?? throw new ArgumentNullException(nameof(dashboardOptions));
        DeviceId = string.IsNullOrWhiteSpace(deviceId)
            ? throw new ArgumentException("Device id must not be empty.", nameof(deviceId))
            : deviceId;
        LocalDatabaseConnectionString = string.IsNullOrWhiteSpace(localDatabaseConnectionString)
            ? throw new ArgumentException("Connection string must not be empty.", nameof(localDatabaseConnectionString))
            : localDatabaseConnectionString;
        IdleThreshold = idleThreshold > TimeSpan.Zero
            ? idleThreshold
            : throw new ArgumentOutOfRangeException(nameof(idleThreshold));
        AcceptanceMode = acceptanceMode;
    }

    public DashboardOptions DashboardOptions { get; }

    public string DeviceId { get; }

    public string LocalDatabaseConnectionString { get; }

    public TimeSpan IdleThreshold { get; }

    public WindowsAppAcceptanceMode AcceptanceMode { get; }

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
                localDatabaseConnectionString: $"Data Source={localDbOverride};Pooling=False",
                idleThreshold: TimeSpan.FromMinutes(5),
                acceptanceMode: ParseAcceptanceMode());
        }

        string dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WoongMonitorStack");
        Directory.CreateDirectory(dataDirectory);

        return new WindowsAppOptions(
            dashboardOptions,
            deviceId,
            localDatabaseConnectionString: $"Data Source={Path.Combine(dataDirectory, "windows-local.db")};Pooling=False",
            idleThreshold: TimeSpan.FromMinutes(5),
            acceptanceMode: ParseAcceptanceMode());
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
    TrackingPipeline = 1
}
