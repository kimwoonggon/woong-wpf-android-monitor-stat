using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.App.Dashboard;
using Woong.MonitorStack.Windows.Presentation.Dashboard;
using Woong.MonitorStack.Windows.Sync;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class WindowsSyncRegistrationServiceTests
{
    [Fact]
    public async Task RegisterOrRepairAsync_WhenRegistrationSucceeds_StoresTokenAndServerDeviceIdWithoutLeakingToken()
    {
        var options = new WindowsAppOptions(
            new DashboardOptions("Asia/Seoul"),
            deviceId: "windows-device-key",
            localDatabaseConnectionString: "Data Source=:memory:",
            idleThreshold: TimeSpan.FromMinutes(5),
            syncOptions: WindowsAppSyncOptions.FromValidatedEndpoint(new Uri("https://monitor.example"), null));
        var registrationClient = new RecordingDeviceRegistrationClient
        {
            Response = new WindowsDeviceRegistrationResponse(
                ServerDeviceId: "server-device-1",
                DeviceToken: "secret-device-token")
        };
        var tokenStore = new RecordingSyncTokenStore();
        var registrationStore = new RecordingSyncRegistrationStore();
        var service = new WindowsDashboardSyncRegistrationService(
            options,
            registrationClient,
            tokenStore,
            registrationStore);

        DashboardSyncRegistrationResult result = await service.RegisterOrRepairAsync();

        Assert.True(result.Succeeded);
        Assert.Equal("secret-device-token", tokenStore.SavedToken);
        Assert.Equal("server-device-1", registrationStore.SavedRegistration?.ServerDeviceId);
        Assert.Equal("local-windows-user", registrationClient.Request?.UserId);
        Assert.Equal(Platform.Windows, registrationClient.Request?.Platform);
        Assert.Equal("windows-device-key", registrationClient.Request?.DeviceKey);
        Assert.Equal("Asia/Seoul", registrationClient.Request?.TimezoneId);
        Assert.DoesNotContain("secret-device-token", result.StatusText, StringComparison.Ordinal);
        Assert.DoesNotContain("token", result.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class RecordingDeviceRegistrationClient : IWindowsDeviceRegistrationClient
    {
        public WindowsDeviceRegistrationRequest? Request { get; private set; }

        public WindowsDeviceRegistrationResponse Response { get; init; } =
            new("server-device-1", "device-token-1");

        public Task<WindowsDeviceRegistrationResponse> RegisterAsync(
            WindowsDeviceRegistrationRequest request,
            CancellationToken cancellationToken = default)
        {
            Request = request;

            return Task.FromResult(Response);
        }
    }

    private sealed class RecordingSyncTokenStore : IWindowsSyncTokenStore
    {
        public string? SavedToken { get; private set; }

        public string? GetDeviceToken()
            => SavedToken;

        public void SaveDeviceToken(string deviceToken)
            => SavedToken = deviceToken;

        public void DeleteDeviceToken()
            => SavedToken = null;
    }

    private sealed class RecordingSyncRegistrationStore : IWindowsSyncRegistrationStore
    {
        public WindowsSyncRegistration? SavedRegistration { get; private set; }

        public WindowsSyncRegistration? GetRegistration()
            => SavedRegistration;

        public void SaveRegistration(WindowsSyncRegistration registration)
            => SavedRegistration = registration;

        public void ClearRegistration()
            => SavedRegistration = null;
    }
}
