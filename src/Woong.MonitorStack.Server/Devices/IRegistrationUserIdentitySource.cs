using Microsoft.Extensions.Options;

namespace Woong.MonitorStack.Server.Devices;

public interface IRegistrationUserIdentitySource
{
    string? GetAuthenticatedUserId(HttpRequest request);
}

public sealed class ConfiguredRegistrationUserIdentitySource : IRegistrationUserIdentitySource
{
    private readonly IOptions<DeviceRegistrationAuthOptions> _options;
    private readonly HeaderRegistrationUserIdentitySource _headerSource;
    private readonly ClaimsPrincipalRegistrationUserIdentitySource _claimsSource;

    public ConfiguredRegistrationUserIdentitySource(
        IOptions<DeviceRegistrationAuthOptions> options,
        HeaderRegistrationUserIdentitySource headerSource,
        ClaimsPrincipalRegistrationUserIdentitySource claimsSource)
    {
        _options = options;
        _headerSource = headerSource;
        _claimsSource = claimsSource;
    }

    public string? GetAuthenticatedUserId(HttpRequest request)
    {
        string providerMode = _options.Value.UserIdentityProviderMode.Trim();
        if (string.Equals(
                providerMode,
                DeviceRegistrationAuthOptions.ClaimsPrincipalProviderMode,
                StringComparison.OrdinalIgnoreCase))
        {
            return _claimsSource.GetAuthenticatedUserId(request);
        }

        return _headerSource.GetAuthenticatedUserId(request);
    }
}

public sealed class HeaderRegistrationUserIdentitySource : IRegistrationUserIdentitySource
{
    private readonly IOptions<DeviceRegistrationAuthOptions> _options;

    public HeaderRegistrationUserIdentitySource(IOptions<DeviceRegistrationAuthOptions> options)
    {
        _options = options;
    }

    public string? GetAuthenticatedUserId(HttpRequest request)
    {
        string headerName = _options.Value.AuthenticatedUserHeaderName;
        if (string.IsNullOrWhiteSpace(headerName) ||
            !request.Headers.TryGetValue(headerName, out var headerValues))
        {
            return null;
        }

        string? userId = headerValues.FirstOrDefault();

        return string.IsNullOrWhiteSpace(userId)
            ? null
            : userId.Trim();
    }
}

public sealed class ClaimsPrincipalRegistrationUserIdentitySource : IRegistrationUserIdentitySource
{
    private readonly IOptions<DeviceRegistrationAuthOptions> _options;

    public ClaimsPrincipalRegistrationUserIdentitySource(IOptions<DeviceRegistrationAuthOptions> options)
    {
        _options = options;
    }

    public string? GetAuthenticatedUserId(HttpRequest request)
    {
        string claimType = _options.Value.AuthenticatedUserClaimType;
        if (string.IsNullOrWhiteSpace(claimType) ||
            request.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        string? userId = request.HttpContext.User.FindFirst(claimType)?.Value;

        return string.IsNullOrWhiteSpace(userId)
            ? null
            : userId.Trim();
    }
}
