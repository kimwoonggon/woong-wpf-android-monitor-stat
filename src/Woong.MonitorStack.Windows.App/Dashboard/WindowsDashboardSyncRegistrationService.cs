using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.Presentation.Dashboard;
using Woong.MonitorStack.Windows.Sync;

namespace Woong.MonitorStack.Windows.App.Dashboard;

public sealed class WindowsDashboardSyncRegistrationService : IDashboardSyncRegistrationService
{
    private const string LocalWindowsUserId = "local-windows-user";

    private readonly WindowsAppOptions _options;
    private readonly IWindowsDeviceRegistrationClient _registrationClient;
    private readonly IWindowsSyncTokenStore _tokenStore;
    private readonly IWindowsSyncRegistrationStore _registrationStore;

    public WindowsDashboardSyncRegistrationService(
        WindowsAppOptions options,
        IWindowsDeviceRegistrationClient registrationClient,
        IWindowsSyncTokenStore tokenStore,
        IWindowsSyncRegistrationStore registrationStore)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _registrationClient = registrationClient ?? throw new ArgumentNullException(nameof(registrationClient));
        _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
        _registrationStore = registrationStore ?? throw new ArgumentNullException(nameof(registrationStore));
    }

    public async Task<DashboardSyncRegistrationResult> RegisterOrRepairAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_options.SyncOptions.HasServerEndpoint)
        {
            return DashboardSyncRegistrationResult.Failed(
                "Configure a safe sync endpoint before registering this device.");
        }

        WindowsDeviceRegistrationResponse registration = await _registrationClient.RegisterAsync(
            new WindowsDeviceRegistrationRequest(
                LocalWindowsUserId,
                Platform.Windows,
                _options.DeviceId,
                ResolveDeviceName(),
                _options.DashboardOptions.TimeZoneId),
            cancellationToken);

        _tokenStore.SaveDeviceToken(registration.DeviceToken);
        _registrationStore.SaveRegistration(new WindowsSyncRegistration(registration.ServerDeviceId));

        return DashboardSyncRegistrationResult.Success(
            "Device registered. Sync will use this device registration.");
    }

    public Task<DashboardSyncRegistrationResult> DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _tokenStore.DeleteDeviceToken();
        _registrationStore.ClearRegistration();

        return Task.FromResult(DashboardSyncRegistrationResult.Success(
            "Device disconnected. Sync is off and data stays local."));
    }

    private static string ResolveDeviceName()
        => string.IsNullOrWhiteSpace(Environment.MachineName)
            ? "Windows device"
            : Environment.MachineName;
}
