using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App.Dashboard;

public sealed class SampleDashboardDataSource : IDashboardDataSource
{
    public const string TimeZoneId = "Asia/Seoul";

    public static DateTimeOffset SampleNowUtc { get; } = new(2026, 4, 28, 4, 30, 0, TimeSpan.Zero);

    private static readonly IReadOnlyList<FocusSession> FocusSessions =
    [
        FocusSession.FromUtc(
            "sample-code-focus",
            "sample-windows-device",
            "Code.exe",
            new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 28, 1, 20, 0, TimeSpan.Zero),
            TimeZoneId,
            isIdle: false,
            source: "sample_dashboard",
            processId: 10,
            processName: "Code.exe",
            processPath: @"C:\Sample\Code.exe",
            windowHandle: 100,
            windowTitle: null),
        FocusSession.FromUtc(
            "sample-chrome-focus",
            "sample-windows-device",
            "chrome.exe",
            new DateTimeOffset(2026, 4, 28, 1, 20, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 28, 3, 38, 0, TimeSpan.Zero),
            TimeZoneId,
            isIdle: false,
            source: "sample_dashboard",
            processId: 20,
            processName: "chrome.exe",
            processPath: @"C:\Sample\chrome.exe",
            windowHandle: 200,
            windowTitle: null),
        FocusSession.FromUtc(
            "sample-idle-focus",
            "sample-windows-device",
            "chrome.exe",
            new DateTimeOffset(2026, 4, 28, 3, 38, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 28, 4, 30, 0, TimeSpan.Zero),
            TimeZoneId,
            isIdle: true,
            source: "sample_dashboard",
            processId: 20,
            processName: "chrome.exe",
            processPath: @"C:\Sample\chrome.exe",
            windowHandle: 200,
            windowTitle: null)
    ];

    private static readonly IReadOnlyList<WebSession> WebSessions =
    [
        new(
            "sample-chrome-focus",
            "Chrome",
            url: null,
            "github.com",
            pageTitle: null,
            TimeRange.FromUtc(
                new DateTimeOffset(2026, 4, 28, 1, 30, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 4, 28, 2, 22, 0, TimeSpan.Zero)),
            captureMethod: "BrowserExtensionFuture",
            captureConfidence: "High",
            isPrivateOrUnknown: false),
        new(
            "sample-chrome-focus",
            "Chrome",
            url: null,
            "chatgpt.com",
            pageTitle: null,
            TimeRange.FromUtc(
                new DateTimeOffset(2026, 4, 28, 2, 25, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 4, 28, 2, 53, 0, TimeSpan.Zero)),
            captureMethod: "BrowserExtensionFuture",
            captureConfidence: "High",
            isPrivateOrUnknown: false),
        new(
            "sample-chrome-focus",
            "Chrome",
            url: null,
            "docs.microsoft.com",
            pageTitle: null,
            TimeRange.FromUtc(
                new DateTimeOffset(2026, 4, 28, 2, 58, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 4, 28, 3, 10, 0, TimeSpan.Zero)),
            captureMethod: "BrowserExtensionFuture",
            captureConfidence: "High",
            isPrivateOrUnknown: false)
    ];

    public IReadOnlyList<FocusSession> QueryFocusSessions(DateTimeOffset startedAtUtc, DateTimeOffset endedAtUtc)
        => FocusSessions
            .Where(session => session.StartedAtUtc < endedAtUtc && session.EndedAtUtc > startedAtUtc)
            .OrderBy(session => session.StartedAtUtc)
            .ToList();

    public IReadOnlyList<WebSession> QueryWebSessions(DateTimeOffset startedAtUtc, DateTimeOffset endedAtUtc)
        => WebSessions
            .Where(session => session.StartedAtUtc < endedAtUtc && session.EndedAtUtc > startedAtUtc)
            .OrderBy(session => session.StartedAtUtc)
            .ToList();
}
