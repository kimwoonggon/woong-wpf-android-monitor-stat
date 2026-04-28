using Woong.MonitorStack.Windows.Browser;

namespace Woong.MonitorStack.Windows.Tests.Browser;

public sealed class NativeMessagingHostRegistrationTests
{
    [Fact]
    public void RegisterForCurrentUser_WritesChromeNativeMessagingHostRegistryKey()
    {
        var registry = new FakeRegistryWriter();
        var registration = new NativeMessagingHostRegistration(
            hostName: "com.woong.monitorstack.chrome",
            manifestPath: @"C:\Users\gerard\AppData\Local\WoongMonitor\chrome-host.json",
            registry);

        registration.RegisterForCurrentUser();

        var write = Assert.Single(registry.Writes);
        Assert.Equal(
            @"Software\Google\Chrome\NativeMessagingHosts\com.woong.monitorstack.chrome",
            write.KeyPath);
        Assert.Equal("", write.ValueName);
        Assert.Equal(@"C:\Users\gerard\AppData\Local\WoongMonitor\chrome-host.json", write.Value);
    }

    private sealed class FakeRegistryWriter : INativeMessagingRegistryWriter
    {
        public List<RegistryWrite> Writes { get; } = [];

        public void SetCurrentUserStringValue(string keyPath, string valueName, string value)
            => Writes.Add(new RegistryWrite(keyPath, valueName, value));
    }

    private sealed record RegistryWrite(string KeyPath, string ValueName, string Value);
}
