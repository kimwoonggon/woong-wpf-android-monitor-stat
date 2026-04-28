using Woong.MonitorStack.Windows.Tracking;

namespace Woong.MonitorStack.Windows.Tests.Tracking;

public sealed class ForegroundWindowCollectorTests
{
    [Fact]
    public void Capture_ReturnsForegroundSnapshotWithClockTimestamp()
    {
        var timestamp = new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero);
        var collector = new ForegroundWindowCollector(
            new FakeForegroundWindowReader(new ForegroundWindowInfo(
                hwnd: 123,
                processId: 456,
                processName: "chrome.exe",
                executablePath: "C:\\Apps\\chrome.exe",
                windowTitle: "Docs - Chrome")),
            new FixedClock(timestamp));

        var snapshot = collector.Capture();

        Assert.Equal(123, snapshot.Hwnd);
        Assert.Equal(456, snapshot.ProcessId);
        Assert.Equal("chrome.exe", snapshot.ProcessName);
        Assert.Equal("Docs - Chrome", snapshot.WindowTitle);
        Assert.Equal(timestamp, snapshot.TimestampUtc);
    }

    private sealed class FakeForegroundWindowReader(ForegroundWindowInfo info) : IForegroundWindowReader
    {
        public ForegroundWindowInfo ReadForegroundWindow() => info;
    }

    private sealed class FixedClock(DateTimeOffset nowUtc) : ISystemClock
    {
        public DateTimeOffset UtcNow => nowUtc;
    }
}
