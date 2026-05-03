using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Windows.Presentation.Dashboard;
using Woong.MonitorStack.Windows.Storage;
using Woong.MonitorStack.Windows.Sync;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class WindowsSyncConfigurationCompositionTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");

    [Fact]
    public void AddWindowsApp_WhenSyncOptionsAreConfigured_RegistersHttpSyncClient()
    {
        var services = new ServiceCollection();
        services.AddWindowsApp(CreateOptions(WindowsAppSyncOptions.FromValidatedEndpoint(
            new Uri("https://monitor.example"),
            "device-token-1")));

        using ServiceProvider provider = services.BuildServiceProvider();

        WindowsSyncClientOptions clientOptions = provider.GetRequiredService<WindowsSyncClientOptions>();
        Assert.Equal(new Uri("https://monitor.example/"), clientOptions.ServerBaseUri);
        Assert.Equal("device-token-1", clientOptions.DeviceToken);
        Assert.IsType<HttpWindowsSyncApiClient>(provider.GetRequiredService<IWindowsSyncApiClient>());
    }

    [Fact]
    public void AddWindowsApp_WhenSyncEndpointWasRejected_DoesNotRegisterUploadClient()
    {
        var services = new ServiceCollection();
        services.AddWindowsApp(CreateOptions(WindowsAppSyncOptions.RejectedEndpoint));

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Null(provider.GetService<IWindowsSyncApiClient>());
    }

    [Fact]
    public void SyncNow_WhenSyncEnabledButUploadIsNotConfigured_LeavesOutboxPendingWithoutLeakingDetails()
    {
        var services = new ServiceCollection();
        services.AddWindowsApp(CreateOptions(WindowsAppSyncOptions.LocalOnly));

        using ServiceProvider provider = services.BuildServiceProvider();
        SqliteSyncOutboxRepository outboxRepository = provider.GetRequiredService<SqliteSyncOutboxRepository>();
        outboxRepository.Add(SyncOutboxItem.Pending(
            id: "outbox-focus-1",
            aggregateType: "focus_session",
            aggregateId: "focus-session-1",
            payloadJson: """{"clientSessionId":"focus-session-1","windowTitle":"Private draft title","token":"secret-device-token"}""",
            createdAtUtc: new DateTimeOffset(2026, 5, 3, 0, 0, 0, TimeSpan.Zero)));
        DashboardViewModel viewModel = provider.GetRequiredService<DashboardViewModel>();
        viewModel.Settings.IsSyncEnabled = true;

        viewModel.SyncNowCommand.Execute(null);

        SyncOutboxItem saved = Assert.Single(outboxRepository.QueryAll());
        Assert.Equal(SyncOutboxStatus.Pending, saved.Status);
        Assert.Equal(0, saved.RetryCount);
        Assert.Null(saved.LastError);
        Assert.DoesNotContain("Private draft title", viewModel.LastSyncStatusText, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-device-token", viewModel.LastSyncStatusText, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    private WindowsAppOptions CreateOptions(WindowsAppSyncOptions syncOptions)
        => new(
            new DashboardOptions("Asia/Seoul"),
            deviceId: "windows-device-1",
            localDatabaseConnectionString: $"Data Source={_dbPath};Pooling=False",
            idleThreshold: TimeSpan.FromMinutes(5),
            syncOptions: syncOptions);
}
