using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.Presentation.Tests.Dashboard;

public sealed class DashboardCurrentActivityMapperTests
{
    [Fact]
    public void Map_SanitizesBrowserUrlToHostOnlyAndShowsCaptureStatus()
    {
        DashboardCurrentActivityMapper mapper = CreateMapper();

        DashboardCurrentActivityPresentation presentation = mapper.Map(
            new DashboardTrackingSnapshot(
                AppName: "Chrome",
                ProcessName: "chrome.exe",
                WindowTitle: null,
                CurrentSessionDuration: TimeSpan.FromSeconds(5),
                LastPersistedSession: null,
                CurrentBrowserDomain: "https://github.com/org/private-repo?token=secret",
                BrowserCaptureStatus: DashboardBrowserCaptureStatus.UiAutomationFallbackActive),
            isWindowTitleVisible: false);

        Assert.Equal("github.com", presentation.BrowserDomainText);
        Assert.Equal("Domain from address bar fallback", presentation.BrowserCaptureStatusText);
        Assert.DoesNotContain("private-repo", presentation.BrowserDomainText, StringComparison.Ordinal);
        Assert.DoesNotContain("token=secret", presentation.BrowserDomainText, StringComparison.Ordinal);
    }

    [Fact]
    public void Map_WhenWindowTitleHidden_DoesNotCaptureTitleForLaterReveal()
    {
        DashboardCurrentActivityMapper mapper = CreateMapper();

        DashboardCurrentActivityPresentation presentation = mapper.Map(
            new DashboardTrackingSnapshot(
                AppName: "Chrome",
                ProcessName: "chrome.exe",
                WindowTitle: "Secret Project - Chrome",
                CurrentSessionDuration: TimeSpan.Zero,
                LastPersistedSession: null),
            isWindowTitleVisible: false);

        Assert.Null(presentation.CapturedWindowTitle);
        Assert.Equal("Window title hidden by privacy settings", presentation.CurrentWindowTitleText);
        Assert.Equal("No window title", mapper.FormatCurrentWindowTitle(presentation.CapturedWindowTitle, isWindowTitleVisible: true));
    }

    [Fact]
    public void Map_FormatsDurationAndPollWriteTimesInDashboardTimezone()
    {
        DashboardCurrentActivityMapper mapper = CreateMapper();
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);

        DashboardCurrentActivityPresentation presentation = mapper.Map(
            new DashboardTrackingSnapshot(
                AppName: null,
                ProcessName: null,
                WindowTitle: null,
                CurrentSessionDuration: TimeSpan.FromSeconds(75),
                LastPersistedSession: null,
                CurrentBrowserDomain: null,
                LastPollAtUtc: now,
                HasPersistedWebSession: true),
            isWindowTitleVisible: false);

        Assert.Equal("No current app", presentation.AppNameText);
        Assert.Equal("No process", presentation.ProcessNameText);
        Assert.Equal(DashboardCurrentActivityMapper.BrowserDomainUnavailableText, presentation.BrowserDomainText);
        Assert.Equal("00:01:15", presentation.CurrentSessionDurationText);
        Assert.Equal("12:00", presentation.LastPollTimeText);
        Assert.Equal("12:00", presentation.LastDbWriteTimeText);
    }

    [Fact]
    public void CreateActiveWebSessionForRange_ClipsPreviewToRequestedRange()
    {
        DashboardCurrentActivityMapper mapper = CreateMapper();
        var startedAtUtc = new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero);
        DashboardCurrentActivityPresentation presentation = mapper.Map(
            new DashboardTrackingSnapshot(
                AppName: "Chrome",
                ProcessName: "chrome.exe",
                WindowTitle: null,
                CurrentSessionDuration: TimeSpan.FromMinutes(15),
                LastPersistedSession: null,
                CurrentBrowserDomain: "https://chatgpt.com/c/private",
                CurrentWebSessionStartedAtUtc: startedAtUtc,
                CurrentWebSessionDuration: TimeSpan.FromMinutes(15),
                LastPollAtUtc: startedAtUtc.AddMinutes(15)),
            isWindowTitleVisible: false);

        WebSession? session = mapper.CreateActiveWebSessionForRange(
            presentation.ActiveWebSessionPreview,
            TimeRange.FromUtc(startedAtUtc.AddMinutes(5), startedAtUtc.AddMinutes(20)));

        Assert.NotNull(session);
        Assert.Equal("chatgpt.com", session.Domain);
        Assert.Equal("Chrome", session.BrowserFamily);
        Assert.Equal(startedAtUtc.AddMinutes(5), session.StartedAtUtc);
        Assert.Equal(startedAtUtc.AddMinutes(15), session.EndedAtUtc);
        Assert.Equal(600_000, session.DurationMs);
    }

    private static DashboardCurrentActivityMapper CreateMapper()
        => new(TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul"));
}
