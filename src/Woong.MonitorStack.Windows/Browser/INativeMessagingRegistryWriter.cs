namespace Woong.MonitorStack.Windows.Browser;

public interface INativeMessagingRegistryWriter
{
    void SetCurrentUserStringValue(string keyPath, string valueName, string value);
}
