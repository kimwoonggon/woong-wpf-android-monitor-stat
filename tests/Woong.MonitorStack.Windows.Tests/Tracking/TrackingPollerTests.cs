using Woong.MonitorStack.Windows.Tracking;

namespace Woong.MonitorStack.Windows.Tests.Tracking;

public sealed class TrackingPollerTests
{
    [Fact]
    public void Poll_CombinesForegroundSnapshotAndIdleState()
    {
        var clock = new MutableClock(new DateTimeOffset(2026, 4, 28, 0, 10, 0, TimeSpan.Zero));
        var poller = new TrackingPoller(
            new ForegroundWindowCollector(
                new FakeForegroundWindowReader(new ForegroundWindowInfo(
                    hwnd: 100,
                    processId: 10,
                    processName: "chrome.exe",
                    executablePath: "C:\\Apps\\chrome.exe",
                    windowTitle: "Docs - Chrome")),
                clock),
            new FakeLastInputReader(new DateTimeOffset(2026, 4, 28, 0, 4, 0, TimeSpan.Zero)),
            new IdleDetector(TimeSpan.FromMinutes(5)),
            new FocusSessionizer("windows-device-1", "Asia/Seoul"));

        var result = poller.Poll();

        Assert.True(result.CurrentSession.IsIdle);
        Assert.Equal("chrome.exe", result.CurrentSession.PlatformAppKey);
    }

    private sealed class FakeForegroundWindowReader(ForegroundWindowInfo info) : IForegroundWindowReader
    {
        public ForegroundWindowInfo ReadForegroundWindow() => info;
    }

    private sealed class FakeLastInputReader(DateTimeOffset lastInputAtUtc) : ILastInputReader
    {
        public DateTimeOffset ReadLastInputAtUtc(DateTimeOffset nowUtc) => lastInputAtUtc;
    }

    private sealed class MutableClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
    }
}
