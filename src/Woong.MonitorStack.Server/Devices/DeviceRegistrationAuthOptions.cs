namespace Woong.MonitorStack.Server.Devices;

public sealed class DeviceRegistrationAuthOptions
{
    public const string SectionName = "DeviceRegistrationAuth";
    public const string HeaderStubProviderMode = "HeaderStub";

    public bool RequireAuthenticatedUser { get; set; }

    public string UserIdentityProviderMode { get; set; } = HeaderStubProviderMode;

    public string AuthenticatedUserHeaderName { get; set; } = "X-Woong-User-Id";
}
