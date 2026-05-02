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
        string providerMode = (options.UserIdentityProviderMode ?? string.Empty).Trim();
        if (!string.Equals(
                providerMode,
                DeviceRegistrationAuthOptions.HeaderStubProviderMode,
                StringComparison.OrdinalIgnoreCase))
        {
            return ValidateOptionsResult.Fail(
                $"DeviceRegistrationAuth:UserIdentityProviderMode={providerMode} is not wired to a registered " +
                "IRegistrationUserIdentitySource. Add a real user/session provider before enabling this mode.");
        }

        if (_environment.IsProduction() &&
            options.RequireAuthenticatedUser &&
            string.Equals(
                providerMode,
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
