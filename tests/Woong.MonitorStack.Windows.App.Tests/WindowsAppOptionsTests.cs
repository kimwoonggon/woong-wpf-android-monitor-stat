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
        string? previousMode = Environment.GetEnvironmentVariable(WindowsAppOptions.AcceptanceModeEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(WindowsAppOptions.LocalDbEnvironmentVariable, dbPath);
            Environment.SetEnvironmentVariable(WindowsAppOptions.DeviceIdEnvironmentVariable, "real-start-device");
            Environment.SetEnvironmentVariable(WindowsAppOptions.AcceptanceModeEnvironmentVariable, null);

            WindowsAppOptions options = WindowsAppOptions.CreateDefault(new DashboardOptions("Asia/Seoul"));

            Assert.Equal("real-start-device", options.DeviceId);
            Assert.Equal($"Data Source={dbPath};Pooling=False", options.LocalDatabaseConnectionString);
            Assert.Equal(WindowsAppAcceptanceMode.None, options.AcceptanceMode);
        }
        finally
        {
            Environment.SetEnvironmentVariable(WindowsAppOptions.LocalDbEnvironmentVariable, previousDb);
            Environment.SetEnvironmentVariable(WindowsAppOptions.DeviceIdEnvironmentVariable, previousDevice);
            Environment.SetEnvironmentVariable(WindowsAppOptions.AcceptanceModeEnvironmentVariable, previousMode);
        }
    }

    [Fact]
    public void CreateDefault_WhenAcceptanceModeIsTrackingPipeline_EnablesTrackingPipelineAcceptanceMode()
    {
        string? previousMode = Environment.GetEnvironmentVariable(WindowsAppOptions.AcceptanceModeEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(WindowsAppOptions.AcceptanceModeEnvironmentVariable, "TrackingPipeline");

            WindowsAppOptions options = WindowsAppOptions.CreateDefault(new DashboardOptions("Asia/Seoul"));

            Assert.Equal(WindowsAppAcceptanceMode.TrackingPipeline, options.AcceptanceMode);
        }
        finally
        {
            Environment.SetEnvironmentVariable(WindowsAppOptions.AcceptanceModeEnvironmentVariable, previousMode);
        }
    }

    [Fact]
    public void CreateDefault_WhenAcceptanceModeIsSampleDashboard_EnablesSampleDashboardAcceptanceMode()
    {
        string? previousMode = Environment.GetEnvironmentVariable(WindowsAppOptions.AcceptanceModeEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(WindowsAppOptions.AcceptanceModeEnvironmentVariable, "SampleDashboard");

            WindowsAppOptions options = WindowsAppOptions.CreateDefault(new DashboardOptions("Asia/Seoul"));

            Assert.Equal(WindowsAppAcceptanceMode.SampleDashboard, options.AcceptanceMode);
        }
        finally
        {
            Environment.SetEnvironmentVariable(WindowsAppOptions.AcceptanceModeEnvironmentVariable, previousMode);
        }
    }
}
