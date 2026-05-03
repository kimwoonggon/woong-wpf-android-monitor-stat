using System.IO;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class WindowsAppOptionsTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("1", true)]
    [InlineData("0", false)]
    public void CreateDefault_WhenAutoStartTrackingEnvironmentIsSet_ParsesStartupOption(
        string? configuredValue,
        bool expectedAutoStart)
    {
        string? previousValue = Environment.GetEnvironmentVariable(WindowsAppOptions.AutoStartTrackingEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(WindowsAppOptions.AutoStartTrackingEnvironmentVariable, configuredValue);

            WindowsAppOptions options = WindowsAppOptions.CreateDefault(new DashboardOptions("Asia/Seoul"));

            Assert.Equal(expectedAutoStart, options.AutoStartTracking);
        }
        finally
        {
            Environment.SetEnvironmentVariable(WindowsAppOptions.AutoStartTrackingEnvironmentVariable, previousValue);
        }
    }

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
            Assert.Equal(Path.Combine(Path.GetDirectoryName(dbPath)!, "logs", "windows-runtime.log"), options.RuntimeLogPath);
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

    [Fact]
    public void CreateDefault_WhenSyncEnvironmentIsMissing_KeepsSyncLocalOnly()
    {
        PreserveSyncEnvironment(() =>
        {
            Environment.SetEnvironmentVariable(WindowsAppOptions.SyncBaseUrlEnvironmentVariable, null);
            Environment.SetEnvironmentVariable(WindowsAppOptions.DeviceTokenEnvironmentVariable, null);

            WindowsAppOptions options = WindowsAppOptions.CreateDefault(new DashboardOptions("Asia/Seoul"));

            Assert.False(options.SyncOptions.IsUploadConfigured);
            Assert.False(options.SyncOptions.HasServerEndpoint);
            Assert.False(options.SyncOptions.HasDeviceToken);
            Assert.Equal("No sync endpoint configured", options.SyncOptions.EndpointDisplayText);
        });
    }

    [Fact]
    public void CreateDefault_WhenSyncEnvironmentIsConfigured_ParsesUploadConfiguration()
    {
        PreserveSyncEnvironment(() =>
        {
            Environment.SetEnvironmentVariable(WindowsAppOptions.SyncBaseUrlEnvironmentVariable, "https://monitor.example");
            Environment.SetEnvironmentVariable(WindowsAppOptions.DeviceTokenEnvironmentVariable, "device-token-1");

            WindowsAppOptions options = WindowsAppOptions.CreateDefault(new DashboardOptions("Asia/Seoul"));

            Assert.True(options.SyncOptions.IsUploadConfigured);
            Assert.True(options.SyncOptions.HasServerEndpoint);
            Assert.True(options.SyncOptions.HasDeviceToken);
            Assert.Equal("https://monitor.example/", options.SyncOptions.EndpointDisplayText);
            Assert.Equal(new Uri("https://monitor.example/"), options.SyncOptions.CreateClientOptions().ServerBaseUri);
            Assert.Equal("device-token-1", options.SyncOptions.CreateClientOptions().DeviceToken);
        });
    }

    [Fact]
    public void CreateDefault_WhenSyncEndpointIsUnsafe_KeepsSyncLocalOnlyWithoutLeakingToken()
    {
        PreserveSyncEnvironment(() =>
        {
            Environment.SetEnvironmentVariable(WindowsAppOptions.SyncBaseUrlEnvironmentVariable, "http://monitor.example");
            Environment.SetEnvironmentVariable(WindowsAppOptions.DeviceTokenEnvironmentVariable, "secret-device-token");

            WindowsAppOptions options = WindowsAppOptions.CreateDefault(new DashboardOptions("Asia/Seoul"));

            Assert.False(options.SyncOptions.IsUploadConfigured);
            Assert.False(options.SyncOptions.HasServerEndpoint);
            Assert.False(options.SyncOptions.HasDeviceToken);
            Assert.Equal("Sync endpoint rejected", options.SyncOptions.EndpointDisplayText);
            Assert.DoesNotContain("secret-device-token", options.SyncOptions.ConfigurationStatusText, StringComparison.Ordinal);
            Assert.DoesNotContain("http://monitor.example", options.SyncOptions.ConfigurationStatusText, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void CreateDefault_WhenOnlyDeviceTokenIsConfigured_KeepsSyncLocalOnlyWithoutLeakingToken()
    {
        PreserveSyncEnvironment(() =>
        {
            Environment.SetEnvironmentVariable(WindowsAppOptions.SyncBaseUrlEnvironmentVariable, null);
            Environment.SetEnvironmentVariable(WindowsAppOptions.DeviceTokenEnvironmentVariable, "secret-device-token");

            WindowsAppOptions options = WindowsAppOptions.CreateDefault(new DashboardOptions("Asia/Seoul"));

            Assert.False(options.SyncOptions.IsUploadConfigured);
            Assert.False(options.SyncOptions.HasServerEndpoint);
            Assert.False(options.SyncOptions.HasDeviceToken);
            Assert.Equal("No sync endpoint configured", options.SyncOptions.EndpointDisplayText);
            Assert.DoesNotContain("secret-device-token", options.SyncOptions.ConfigurationStatusText, StringComparison.Ordinal);
        });
    }

    private static void PreserveSyncEnvironment(Action action)
    {
        string? previousBaseUrl = Environment.GetEnvironmentVariable(WindowsAppOptions.SyncBaseUrlEnvironmentVariable);
        string? previousDeviceToken = Environment.GetEnvironmentVariable(WindowsAppOptions.DeviceTokenEnvironmentVariable);
        try
        {
            action();
        }
        finally
        {
            Environment.SetEnvironmentVariable(WindowsAppOptions.SyncBaseUrlEnvironmentVariable, previousBaseUrl);
            Environment.SetEnvironmentVariable(WindowsAppOptions.DeviceTokenEnvironmentVariable, previousDeviceToken);
        }
    }
}
