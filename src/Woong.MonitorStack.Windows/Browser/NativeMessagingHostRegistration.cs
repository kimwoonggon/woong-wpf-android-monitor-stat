namespace Woong.MonitorStack.Windows.Browser;

public sealed class NativeMessagingHostRegistration
{
    private const string ChromeNativeMessagingHostsKeyPath =
        @"Software\Google\Chrome\NativeMessagingHosts";

    private readonly string _hostName;
    private readonly string _manifestPath;
    private readonly INativeMessagingRegistryWriter _registryWriter;

    public NativeMessagingHostRegistration(
        string hostName,
        string manifestPath,
        INativeMessagingRegistryWriter registryWriter)
    {
        _hostName = EnsureText(hostName, nameof(hostName));
        _manifestPath = EnsureText(manifestPath, nameof(manifestPath));
        _registryWriter = registryWriter ?? throw new ArgumentNullException(nameof(registryWriter));
    }

    public void RegisterForCurrentUser()
        => _registryWriter.SetCurrentUserStringValue(
            $@"{ChromeNativeMessagingHostsKeyPath}\{_hostName}",
            "",
            _manifestPath);

    private static string EnsureText(string value, string parameterName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value must not be empty.", parameterName)
            : value;
}
