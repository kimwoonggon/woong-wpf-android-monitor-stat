namespace Woong.MonitorStack.Server.Tests.Devices;

public sealed class DeviceRegistrationPolicyTests
{
    private const string MissingServerUserAuthStack =
        "Public-release registration policy is not implemented yet: add server user/session auth first, then require registration to derive user identity from auth context instead of trusting payload userId.";

    [Fact(Skip = MissingServerUserAuthStack)]
    public Task RegisterDevice_WhenUserSessionAuthIsMissing_ReturnsUnauthorized()
        => Task.CompletedTask;

    [Fact(Skip = MissingServerUserAuthStack)]
    public Task RegisterDevice_UsesAuthenticatedUserIdInsteadOfPayloadUserId()
        => Task.CompletedTask;

    [Fact(Skip = MissingServerUserAuthStack)]
    public Task RegisterDevice_WhenSameDeviceKeyIsRegisteredByDifferentUsers_CreatesSeparateDevices()
        => Task.CompletedTask;

    [Fact(Skip = MissingServerUserAuthStack)]
    public Task RegisterDevice_WhenPayloadUserIdTargetsAnotherUser_DoesNotReturnExistingDeviceToken()
        => Task.CompletedTask;
}
