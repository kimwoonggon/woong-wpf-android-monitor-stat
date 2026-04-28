using Woong.MonitorStack.Windows.Tracking;

namespace Woong.MonitorStack.Windows.Tests.Tracking;

public sealed class FocusSessionizerTests
{
    [Fact]
    public void Process_WhenAppChanges_ClosesPreviousAndStartsNewSession()
    {
        var sessionizer = new FocusSessionizer("windows-device-1", "Asia/Seoul");
        var first = Snapshot(
            hwnd: 100,
            processId: 10,
            processName: "chrome.exe",
            title: "Docs - Chrome",
            timestampUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero));
        var second = Snapshot(
            hwnd: 200,
            processId: 20,
            processName: "Code.exe",
            title: "Woong Monitor - Visual Studio Code",
            timestampUtc: new DateTimeOffset(2026, 4, 28, 0, 5, 0, TimeSpan.Zero));

        var firstResult = sessionizer.Process(first, isIdle: false);
        var secondResult = sessionizer.Process(second, isIdle: false);

        Assert.Null(firstResult.ClosedSession);
        Assert.NotNull(firstResult.CurrentSession);
        Assert.Equal("chrome.exe", firstResult.CurrentSession.PlatformAppKey);
        Assert.NotNull(secondResult.ClosedSession);
        Assert.Equal("chrome.exe", secondResult.ClosedSession.PlatformAppKey);
        Assert.Equal(300_000, secondResult.ClosedSession.DurationMs);
        Assert.Equal("Code.exe", secondResult.CurrentSession.PlatformAppKey);
    }

    [Fact]
    public void Process_WhenSameWindowContinues_ExtendsCurrentSession()
    {
        var sessionizer = new FocusSessionizer("windows-device-1", "Asia/Seoul");
        var first = Snapshot(
            hwnd: 100,
            processId: 10,
            processName: "chrome.exe",
            title: "Docs - Chrome",
            timestampUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero));
        var second = Snapshot(
            hwnd: 100,
            processId: 10,
            processName: "chrome.exe",
            title: "Docs - Chrome",
            timestampUtc: new DateTimeOffset(2026, 4, 28, 0, 3, 0, TimeSpan.Zero));

        sessionizer.Process(first, isIdle: false);
        var result = sessionizer.Process(second, isIdle: false);

        Assert.Null(result.ClosedSession);
        Assert.Equal("chrome.exe", result.CurrentSession.PlatformAppKey);
        Assert.Equal(180_000, result.CurrentSession.DurationMs);
    }

    [Fact]
    public void Process_WhenIdleStateChanges_ClosesPreviousAndStartsIdleSession()
    {
        var sessionizer = new FocusSessionizer("windows-device-1", "Asia/Seoul");
        var activeSnapshot = Snapshot(
            hwnd: 100,
            processId: 10,
            processName: "chrome.exe",
            title: "Docs - Chrome",
            timestampUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero));
        var idleSnapshot = Snapshot(
            hwnd: 100,
            processId: 10,
            processName: "chrome.exe",
            title: "Docs - Chrome",
            timestampUtc: new DateTimeOffset(2026, 4, 28, 0, 6, 0, TimeSpan.Zero));

        sessionizer.Process(activeSnapshot, isIdle: false);
        var result = sessionizer.Process(idleSnapshot, isIdle: true);

        Assert.NotNull(result.ClosedSession);
        Assert.False(result.ClosedSession.IsIdle);
        Assert.True(result.CurrentSession.IsIdle);
        Assert.Equal(360_000, result.ClosedSession.DurationMs);
    }

    private static ForegroundWindowSnapshot Snapshot(
        nint hwnd,
        int processId,
        string processName,
        string title,
        DateTimeOffset timestampUtc)
        => new(
            hwnd,
            processId,
            processName,
            executablePath: $"C:\\Apps\\{processName}",
            title,
            timestampUtc);
}
