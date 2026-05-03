using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.Presentation.Tests.Dashboard;

public sealed class DashboardRowMapperTests
{
    [Fact]
    public void BuildRecentRows_FormatsDurationsAndKeepsPrivateTitlesHidden()
    {
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul");
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var mapper = new DashboardRowMapper(timeZone);
        FocusSession focusSession = FocusSession.FromUtc(
            "focus-1",
            "windows-device-1",
            "Code.exe",
            now.AddSeconds(-75),
            now,
            "Asia/Seoul",
            isIdle: false,
            "foreground_window",
            processName: "Code.exe",
            windowTitle: "Private project",
            processPath: @"C:\Tools\Code.exe");
        WebSession webSession = WebSession.FromUtc(
            "focus-1",
            "Chrome",
            "https://github.com/org/private",
            "Private issue",
            now.AddSeconds(-45),
            now);

        DashboardSessionRow sessionRow = Assert.Single(mapper.BuildRecentSessionRows([focusSession], isWindowTitleVisible: false));
        DashboardWebSessionRow webRow = Assert.Single(mapper.BuildRecentWebSessionRows([webSession], isWindowTitleVisible: false));

        Assert.Equal("1m 15s", sessionRow.Duration);
        Assert.Equal("Hidden by privacy setting", sessionRow.WindowTitle);
        Assert.Equal(@"C:\Tools\Code.exe", sessionRow.ProcessPath);
        Assert.Equal("45s", webRow.Duration);
        Assert.Equal("Page title hidden by privacy settings", webRow.PageTitle);
        Assert.Equal("Full URL disabled", webRow.UrlMode);
    }

    [Fact]
    public void BuildLiveEventRows_CombinesFocusAndWebRowsNewestFirst()
    {
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul");
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var mapper = new DashboardRowMapper(timeZone);
        FocusSession focusSession = FocusSession.FromUtc(
            "focus-1",
            "windows-device-1",
            "Code.exe",
            now.AddMinutes(-20),
            now.AddMinutes(-10),
            "Asia/Seoul",
            isIdle: false,
            "foreground_window");
        WebSession webSession = WebSession.FromUtc(
            "focus-1",
            "Chrome",
            "https://example.com/",
            "Example",
            now.AddMinutes(-5),
            now);

        IReadOnlyList<DashboardEventLogRow> rows = mapper.BuildLiveEventRows([focusSession], [webSession]);

        Assert.Collection(
            rows,
            row => Assert.Equal(("Web", "example.com", "11:55"), (row.EventType, row.Domain, row.OccurredAtLocal)),
            row => Assert.Equal(("Focus", "Code.exe", "11:40"), (row.EventType, row.AppName, row.OccurredAtLocal)));
    }
}
