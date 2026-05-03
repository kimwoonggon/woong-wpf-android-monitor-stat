namespace Woong.MonitorStack.Windows.Sync;

public sealed record WindowsSyncClientOptions
{
    public WindowsSyncClientOptions(Uri serverBaseUri, string deviceToken)
    {
        ServerBaseUri = NormalizeServerBaseUri(serverBaseUri);
        DeviceToken = ValidateDeviceToken(deviceToken);
    }

    public Uri ServerBaseUri { get; }

    public string DeviceToken { get; }

    public static bool TryNormalizeServerBaseUri(string? configuredValue, out Uri? serverBaseUri)
    {
        serverBaseUri = null;
        if (string.IsNullOrWhiteSpace(configuredValue))
        {
            return false;
        }

        if (!Uri.TryCreate(configuredValue.Trim(), UriKind.Absolute, out Uri? parsed))
        {
            return false;
        }

        if (!IsSafeServerBaseUri(parsed))
        {
            return false;
        }

        serverBaseUri = NormalizeServerBaseUri(parsed);
        return true;
    }

    private static Uri NormalizeServerBaseUri(Uri serverBaseUri)
    {
        ArgumentNullException.ThrowIfNull(serverBaseUri);

        if (!serverBaseUri.IsAbsoluteUri || !IsSafeServerBaseUri(serverBaseUri))
        {
            throw new ArgumentException(
                "Sync endpoint must use HTTPS, or loopback HTTP for local development, without credentials, query, or fragment.",
                nameof(serverBaseUri));
        }

        var builder = new UriBuilder(
            serverBaseUri.Scheme,
            serverBaseUri.Host,
            serverBaseUri.IsDefaultPort ? -1 : serverBaseUri.Port);
        return builder.Uri;
    }

    private static bool IsSafeServerBaseUri(Uri serverBaseUri)
        => serverBaseUri.IsAbsoluteUri &&
            string.IsNullOrEmpty(serverBaseUri.UserInfo) &&
            string.IsNullOrEmpty(serverBaseUri.Query) &&
            string.IsNullOrEmpty(serverBaseUri.Fragment) &&
            (serverBaseUri.Scheme == Uri.UriSchemeHttps ||
                (serverBaseUri.Scheme == Uri.UriSchemeHttp && serverBaseUri.IsLoopback));

    private static string ValidateDeviceToken(string deviceToken)
        => string.IsNullOrWhiteSpace(deviceToken)
            ? throw new ArgumentException("Device token must not be empty.", nameof(deviceToken))
            : deviceToken.Trim();
}
