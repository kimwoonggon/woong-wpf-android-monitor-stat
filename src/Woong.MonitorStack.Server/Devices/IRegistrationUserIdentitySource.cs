using Microsoft.Extensions.Options;

namespace Woong.MonitorStack.Server.Devices;

public interface IRegistrationUserIdentitySource
{
    string? GetAuthenticatedUserId(HttpRequest request);
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
