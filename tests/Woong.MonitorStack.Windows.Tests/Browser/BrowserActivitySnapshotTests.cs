using Woong.MonitorStack.Windows.Browser;

namespace Woong.MonitorStack.Windows.Tests.Browser;

public sealed class BrowserActivitySnapshotTests
{
    [Fact]
    public void Create_WhenCapturedWithOffset_StoresUtcTimestampAndMetadata()
    {
        var capturedAt = new DateTimeOffset(2026, 4, 29, 9, 15, 0, TimeSpan.FromHours(9));

        var snapshot = new BrowserActivitySnapshot(
            capturedAt,
            browserName: "Chrome",
            processName: "chrome.exe",
            processId: 1234,
            windowHandle: 999,
            windowTitle: "GitHub - Chrome",
            tabTitle: "GitHub",
            url: "https://github.com/kimwoonggon/woong-wpf-android-monitor-stat",
            domain: "github.com",
            CaptureMethod.BrowserExtensionFuture,
            CaptureConfidence.High,
            isPrivateOrUnknown: false);

        Assert.Equal(DateTimeOffset.Parse("2026-04-29T00:15:00Z"), snapshot.CapturedAtUtc);
        Assert.Equal("Chrome", snapshot.BrowserName);
        Assert.Equal("chrome.exe", snapshot.ProcessName);
        Assert.Equal("github.com", snapshot.Domain);
        Assert.Equal(CaptureMethod.BrowserExtensionFuture, snapshot.CaptureMethod);
        Assert.Equal(CaptureConfidence.High, snapshot.CaptureConfidence);
        Assert.False(snapshot.IsPrivateOrUnknown);
    }
}
