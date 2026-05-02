namespace Woong.MonitorStack.Server.Devices;

public sealed class DeviceRegistrationAuthOptions
{
    public const string SectionName = "DeviceRegistrationAuth";

    public bool RequireAuthenticatedUser { get; set; }

    public string AuthenticatedUserHeaderName { get; set; } = "X-Woong-User-Id";
}
