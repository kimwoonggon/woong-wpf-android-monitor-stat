using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Domain.Tests.Common;

public sealed class DomainModelConstructionTests
{
    [Fact]
    public void Device_RequiresStableDeviceKey()
    {
        Assert.Throws<ArgumentException>(() => new Device(
            id: "device-1",
            userId: "local-user",
            platform: Platform.Windows,
            deviceKey: "",
            deviceName: "Dev PC",
            timezoneId: "Asia/Seoul",
            createdAtUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            lastSeenAtUtc: null));
    }

    [Fact]
    public void WebSession_NormalizesDomainFromUrl()
    {
        var webSession = WebSession.FromUtc(
            focusSessionId: "focus-1",
            browserFamily: "Chrome",
            url: "https://www.youtube.com/watch?v=abc",
            pageTitle: "Video",
            startedAtUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            endedAtUtc: new DateTimeOffset(2026, 4, 28, 0, 5, 0, TimeSpan.Zero));

        Assert.Equal("youtube.com", webSession.Domain);
        Assert.Equal(300_000, webSession.DurationMs);
    }
}
