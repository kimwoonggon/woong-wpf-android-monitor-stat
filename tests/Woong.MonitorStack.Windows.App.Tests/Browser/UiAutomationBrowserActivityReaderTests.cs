using Woong.MonitorStack.Windows.App.Browser;
using Woong.MonitorStack.Windows.Browser;
using Woong.MonitorStack.Windows.Tracking;

namespace Woong.MonitorStack.Windows.App.Tests.Browser;

public sealed class UiAutomationBrowserActivityReaderTests
{
    [Fact]
    public void TryRead_WhenSupportedBrowserAddressBarHasUrl_ReturnsDomainSnapshotImmediately()
    {
        var reader = new UiAutomationBrowserActivityReader(
            new BrowserProcessClassifier(),
            new FakeAddressBarReader("https://github.com/kimwoonggon/woong-wpf-android-monitor-stat?tab=readme"));

        BrowserActivitySnapshot? snapshot = reader.TryRead(CreateForegroundWindow("chrome.exe"));

        Assert.NotNull(snapshot);
        Assert.Equal("Chrome", snapshot.BrowserName);
        Assert.Equal("chrome.exe", snapshot.ProcessName);
        Assert.Equal("github.com", snapshot.Domain);
        Assert.Equal("https://github.com/kimwoonggon/woong-wpf-android-monitor-stat?tab=readme", snapshot.Url);
        Assert.Equal(CaptureMethod.UIAutomationAddressBar, snapshot.CaptureMethod);
        Assert.Equal(CaptureConfidence.Medium, snapshot.CaptureConfidence);
        Assert.False(snapshot.IsPrivateOrUnknown);
    }

    [Fact]
    public void TryRead_WhenForegroundIsNotBrowser_ReturnsNull()
    {
        var reader = new UiAutomationBrowserActivityReader(
            new BrowserProcessClassifier(),
            new FakeAddressBarReader("https://github.com/org/repo"));

        BrowserActivitySnapshot? snapshot = reader.TryRead(CreateForegroundWindow("Code.exe"));

        Assert.Null(snapshot);
    }

    [Fact]
    public void TryRead_WhenAddressBarUnavailable_ReturnsWindowTitleOnlyWithoutInventingDomain()
    {
        var reader = new UiAutomationBrowserActivityReader(
            new BrowserProcessClassifier(),
            new FakeAddressBarReader(url: null));

        BrowserActivitySnapshot? snapshot = reader.TryRead(CreateForegroundWindow("msedge.exe"));

        Assert.NotNull(snapshot);
        Assert.Equal("Microsoft Edge", snapshot.BrowserName);
        Assert.Null(snapshot.Url);
        Assert.Null(snapshot.Domain);
        Assert.Equal(CaptureMethod.WindowTitleOnly, snapshot.CaptureMethod);
        Assert.Equal(CaptureConfidence.Low, snapshot.CaptureConfidence);
        Assert.Null(snapshot.IsPrivateOrUnknown);
    }

    [Fact]
    public void TryRead_WhenAddressBarHasNonWebUrl_DoesNotCreateDomain()
    {
        var reader = new UiAutomationBrowserActivityReader(
            new BrowserProcessClassifier(),
            new FakeAddressBarReader("chrome://settings/privacy"));

        BrowserActivitySnapshot? snapshot = reader.TryRead(CreateForegroundWindow("brave.exe"));

        Assert.NotNull(snapshot);
        Assert.Null(snapshot.Url);
        Assert.Null(snapshot.Domain);
        Assert.Equal(CaptureMethod.WindowTitleOnly, snapshot.CaptureMethod);
    }

    private static ForegroundWindowSnapshot CreateForegroundWindow(string processName)
        => new(
            hwnd: 200,
            processId: 20,
            processName,
            executablePath: $@"C:\Apps\{processName}",
            windowTitle: "Browser window",
            timestampUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero));

    private sealed class FakeAddressBarReader(string? url) : IBrowserAddressBarReader
    {
        public string? TryReadAddress(ForegroundWindowSnapshot foregroundWindow)
            => url;
    }
}
