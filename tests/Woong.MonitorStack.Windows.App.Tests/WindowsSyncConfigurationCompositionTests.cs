using System.IO;
using Microsoft.Data.Sqlite;
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

    [Theory]
    [MemberData(nameof(SyncEndpointDisplayCases))]
    public void AddWindowsApp_BindsSafeSyncEndpointDisplayTextToSettings(
        WindowsAppSyncOptions syncOptions,
        string expectedEndpointText)
    {
        var services = new ServiceCollection();
        services.AddWindowsApp(CreateOptions(syncOptions));

        using ServiceProvider provider = services.BuildServiceProvider();

        DashboardViewModel viewModel = provider.GetRequiredService<DashboardViewModel>();
        Assert.Equal(expectedEndpointText, viewModel.Settings.SyncEndpointText);
        Assert.DoesNotContain("device-token", viewModel.Settings.SyncEndpointText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", viewModel.Settings.SyncEndpointText, StringComparison.OrdinalIgnoreCase);
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

    [Fact]
    public void AddWindowsApp_WhenSyncTokenIsConfigured_DoesNotExposeTokenInSettingsStoreOrSqlite()
    {
        var services = new ServiceCollection();
        services.AddWindowsApp(CreateOptions(WindowsAppSyncOptions.FromValidatedEndpoint(
            new Uri("https://monitor.example"),
            "secret-device-token")));

        using ServiceProvider provider = services.BuildServiceProvider();
        DashboardViewModel viewModel = provider.GetRequiredService<DashboardViewModel>();
        FileWindowsSyncTokenStore tokenStore = Assert.IsType<FileWindowsSyncTokenStore>(
            provider.GetRequiredService<IWindowsSyncTokenStore>());
        _ = provider.GetRequiredService<SqliteSyncOutboxRepository>();

        Assert.DoesNotContain("secret-device-token", viewModel.Settings.SyncEndpointText, StringComparison.Ordinal);
        Assert.Null(tokenStore.GetDeviceToken());
        Assert.False(File.Exists(tokenStore.TokenFilePath));
        Assert.DoesNotContain("token", ReadSqliteSchema(), StringComparison.OrdinalIgnoreCase);
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

    private string ReadSqliteSchema()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath};Pooling=False");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT group_concat(sql, char(10)) FROM sqlite_master WHERE sql IS NOT NULL;";

        return command.ExecuteScalar() as string ?? "";
    }

    public static TheoryData<WindowsAppSyncOptions, string> SyncEndpointDisplayCases()
        => new()
        {
            { WindowsAppSyncOptions.LocalOnly, "No sync endpoint configured" },
            { WindowsAppSyncOptions.RejectedEndpoint, "Sync endpoint rejected" },
            {
                WindowsAppSyncOptions.FromValidatedEndpoint(
                    new Uri("https://monitor.example"),
                    "secret-device-token"),
                "https://monitor.example/"
            }
        };
}
