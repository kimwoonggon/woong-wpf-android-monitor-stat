using System.IO;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class WindowsAppOptionsTests
{
    [Fact]
    public void CreateDefault_WhenEnvironmentOverridesLocalDb_UsesOverrideAsSqlitePath()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        string? previousDb = Environment.GetEnvironmentVariable(WindowsAppOptions.LocalDbEnvironmentVariable);
        string? previousDevice = Environment.GetEnvironmentVariable(WindowsAppOptions.DeviceIdEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(WindowsAppOptions.LocalDbEnvironmentVariable, dbPath);
            Environment.SetEnvironmentVariable(WindowsAppOptions.DeviceIdEnvironmentVariable, "real-start-device");

            WindowsAppOptions options = WindowsAppOptions.CreateDefault(new DashboardOptions("Asia/Seoul"));

            Assert.Equal("real-start-device", options.DeviceId);
            Assert.Equal($"Data Source={dbPath};Pooling=False", options.LocalDatabaseConnectionString);
        }
        finally
        {
            Environment.SetEnvironmentVariable(WindowsAppOptions.LocalDbEnvironmentVariable, previousDb);
            Environment.SetEnvironmentVariable(WindowsAppOptions.DeviceIdEnvironmentVariable, previousDevice);
        }
    }
}
