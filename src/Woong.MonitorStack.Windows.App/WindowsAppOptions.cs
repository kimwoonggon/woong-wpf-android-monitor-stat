using System.IO;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App;

public sealed class WindowsAppOptions
{
    public WindowsAppOptions(
        DashboardOptions dashboardOptions,
        string deviceId,
        string localDatabaseConnectionString,
        TimeSpan idleThreshold)
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
    }

    public DashboardOptions DashboardOptions { get; }

    public string DeviceId { get; }

    public string LocalDatabaseConnectionString { get; }

    public TimeSpan IdleThreshold { get; }

    public static WindowsAppOptions CreateDefault(DashboardOptions dashboardOptions)
    {
        ArgumentNullException.ThrowIfNull(dashboardOptions);

        string dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WoongMonitorStack");
        Directory.CreateDirectory(dataDirectory);

        return new WindowsAppOptions(
            dashboardOptions,
            deviceId: $"windows-{Environment.MachineName}",
            localDatabaseConnectionString: $"Data Source={Path.Combine(dataDirectory, "windows-local.db")};Pooling=False",
            idleThreshold: TimeSpan.FromMinutes(5));
    }
}
