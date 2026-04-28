using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Domain.Tests.Common;

public sealed class DailySummaryCalculatorTests
{
    [Fact]
    public void Calculate_ExcludesIdleSessionsFromActiveTotal()
    {
        var active = Session(
            "active-session",
            new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 27, 15, 30, 0, TimeSpan.Zero),
            isIdle: false);
        var idle = Session(
            "idle-session",
            new DateTimeOffset(2026, 4, 27, 15, 30, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 27, 16, 0, 0, TimeSpan.Zero),
            isIdle: true);

        var summary = DailySummaryCalculator.Calculate([active, idle], new DateOnly(2026, 4, 28));

        Assert.Equal(1_800_000, summary.TotalActiveMs);
        Assert.Equal(1_800_000, summary.TotalIdleMs);
    }

    [Fact]
    public void Calculate_GroupsByLocalDate()
    {
        var previousLocalDate = Session(
            "previous-local-date",
            new DateTimeOffset(2026, 4, 27, 14, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 27, 14, 30, 0, TimeSpan.Zero),
            isIdle: false);
        var targetLocalDate = Session(
            "target-local-date",
            new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 27, 15, 20, 0, TimeSpan.Zero),
            isIdle: false);

        var summary = DailySummaryCalculator.Calculate([previousLocalDate, targetLocalDate], new DateOnly(2026, 4, 28));

        Assert.Equal(1_200_000, summary.TotalActiveMs);
    }

    [Fact]
    public void Calculate_IncludesWebTotalsAndTopDomains()
    {
        var focus = Session(
            "focus-session",
            new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 27, 15, 10, 0, TimeSpan.Zero),
            isIdle: false);
        var web = WebSession.FromUtc(
            focusSessionId: "focus-session",
            browserFamily: "Chrome",
            url: "https://www.youtube.com/watch?v=abc",
            pageTitle: "Video",
            startedAtUtc: new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero),
            endedAtUtc: new DateTimeOffset(2026, 4, 27, 15, 5, 0, TimeSpan.Zero));

        var summary = DailySummaryCalculator.Calculate([focus], [web], new DateOnly(2026, 4, 28), "Asia/Seoul");

        var topDomain = Assert.Single(summary.TopDomains);
        Assert.Equal(300_000, summary.TotalWebMs);
        Assert.Equal("youtube.com", topDomain.Key);
        Assert.Equal(300_000, topDomain.DurationMs);
    }

    [Fact]
    public void Calculate_GroupsTopAppsByAppFamily()
    {
        var windowsChrome = FocusSession.FromUtc(
            clientSessionId: "windows-chrome",
            deviceId: "windows-device-1",
            platformAppKey: "chrome.exe",
            startedAtUtc: new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero),
            endedAtUtc: new DateTimeOffset(2026, 4, 27, 15, 10, 0, TimeSpan.Zero),
            timezoneId: "Asia/Seoul",
            isIdle: false,
            source: "foreground_window");
        var androidChrome = FocusSession.FromUtc(
            clientSessionId: "android-chrome",
            deviceId: "android-device-1",
            platformAppKey: "com.android.chrome",
            startedAtUtc: new DateTimeOffset(2026, 4, 27, 15, 10, 0, TimeSpan.Zero),
            endedAtUtc: new DateTimeOffset(2026, 4, 27, 15, 25, 0, TimeSpan.Zero),
            timezoneId: "Asia/Seoul",
            isIdle: false,
            source: "usage_stats");
        var platformApps = new[]
        {
            new PlatformApp(Platform.Windows, "chrome.exe", "Chrome", appFamilyKey: "chrome"),
            new PlatformApp(Platform.Android, "com.android.chrome", "Chrome", appFamilyKey: "chrome")
        };
        var appFamilies = new[]
        {
            new AppFamily("chrome", "Chrome")
        };

        var summary = DailySummaryCalculator.Calculate(
            [windowsChrome, androidChrome],
            webSessions: [],
            new DateOnly(2026, 4, 28),
            "Asia/Seoul",
            platformApps,
            appFamilies);

        var topApp = Assert.Single(summary.TopApps);
        Assert.Equal("Chrome", topApp.Key);
        Assert.Equal(1_500_000, topApp.DurationMs);
    }

    private static FocusSession Session(
        string clientSessionId,
        DateTimeOffset startedAtUtc,
        DateTimeOffset endedAtUtc,
        bool isIdle)
        => FocusSession.FromUtc(
            clientSessionId,
            deviceId: "windows-device-1",
            platformAppKey: "chrome.exe",
            startedAtUtc,
            endedAtUtc,
            timezoneId: "Asia/Seoul",
            isIdle,
            source: "foreground_window");
}
