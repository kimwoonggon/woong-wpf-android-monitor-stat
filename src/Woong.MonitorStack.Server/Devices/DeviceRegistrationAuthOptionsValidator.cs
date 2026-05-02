using Microsoft.Extensions.Options;

namespace Woong.MonitorStack.Server.Devices;

public sealed class DeviceRegistrationAuthOptionsValidator : IValidateOptions<DeviceRegistrationAuthOptions>
{
    private readonly IHostEnvironment _environment;

    public DeviceRegistrationAuthOptionsValidator(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public ValidateOptionsResult Validate(string? name, DeviceRegistrationAuthOptions options)
    {
        if (_environment.IsProduction() &&
            options.RequireAuthenticatedUser &&
            string.Equals(
                options.UserIdentityProviderMode,
                DeviceRegistrationAuthOptions.HeaderStubProviderMode,
                StringComparison.OrdinalIgnoreCase))
        {
            return ValidateOptionsResult.Fail(
                "Production strict-auth mode requires a real user/session provider. " +
                "DeviceRegistrationAuth:UserIdentityProviderMode=HeaderStub is for dev/MVP compatibility only.");
        }

        return ValidateOptionsResult.Success;
    }
}
