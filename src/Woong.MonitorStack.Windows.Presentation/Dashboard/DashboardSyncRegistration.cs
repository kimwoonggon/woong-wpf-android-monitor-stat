namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public interface IDashboardSyncRegistrationService
{
    Task<DashboardSyncRegistrationResult> RegisterOrRepairAsync(CancellationToken cancellationToken = default);

    Task<DashboardSyncRegistrationResult> DisconnectAsync(CancellationToken cancellationToken = default);
}

public sealed record DashboardSyncRegistrationResult(bool Succeeded, string StatusText)
{
    public static DashboardSyncRegistrationResult Success(string statusText)
        => new(true, RequiredText(statusText));

    public static DashboardSyncRegistrationResult Failed(string statusText)
        => new(false, RequiredText(statusText));

    private static string RequiredText(string value)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Status text must not be empty.", nameof(value))
            : value;
}

internal sealed class NullDashboardSyncRegistrationService : IDashboardSyncRegistrationService
{
    public static NullDashboardSyncRegistrationService Instance { get; } = new();

    private NullDashboardSyncRegistrationService()
    {
    }

    public Task<DashboardSyncRegistrationResult> RegisterOrRepairAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(DashboardSyncRegistrationResult.Failed(
            "Configure a safe sync endpoint before registering this device."));

    public Task<DashboardSyncRegistrationResult> DisconnectAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(DashboardSyncRegistrationResult.Success(
            "Device disconnected. Sync is off and data stays local."));
}
