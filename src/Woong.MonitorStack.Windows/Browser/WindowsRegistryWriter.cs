using System.Runtime.Versioning;
using Microsoft.Win32;

namespace Woong.MonitorStack.Windows.Browser;

[SupportedOSPlatform("windows")]
public sealed class WindowsRegistryWriter : INativeMessagingRegistryWriter
{
    public void SetCurrentUserStringValue(string keyPath, string valueName, string value)
    {
        using var key = Registry.CurrentUser.CreateSubKey(keyPath);
        key.SetValue(valueName, value, RegistryValueKind.String);
    }
}
