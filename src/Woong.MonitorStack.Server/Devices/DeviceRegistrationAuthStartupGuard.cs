using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Woong.MonitorStack.Server.Devices;

public sealed class DeviceRegistrationAuthStartupGuard : IHostedService
{
    private readonly IHostEnvironment _environment;
    private readonly IOptions<DeviceRegistrationAuthOptions> _options;
    private readonly IServiceProvider _serviceProvider;

    public DeviceRegistrationAuthStartupGuard(
        IHostEnvironment environment,
        IOptions<DeviceRegistrationAuthOptions> options,
        IServiceProvider serviceProvider)
    {
        _environment = environment;
        _options = options;
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        DeviceRegistrationAuthOptions options = _options.Value;
        if (!_environment.IsProduction() ||
            !options.RequireAuthenticatedUser ||
            !string.Equals(
                options.UserIdentityProviderMode,
                DeviceRegistrationAuthOptions.ClaimsPrincipalProviderMode,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string requiredScheme = options.RequiredAuthenticationScheme.Trim();
        IAuthenticationSchemeProvider? schemes =
            _serviceProvider.GetService<IAuthenticationSchemeProvider>();
        AuthenticationScheme? registeredScheme = schemes is null
            ? null
            : await schemes.GetSchemeAsync(requiredScheme);
        if (registeredScheme is null)
        {
            throw new InvalidOperationException(
                $"DeviceRegistrationAuth:RequiredAuthenticationScheme={requiredScheme} does not have a registered authentication handler. " +
                "Configure upstream authentication middleware/provider before enabling production strict ClaimsPrincipal registration.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
