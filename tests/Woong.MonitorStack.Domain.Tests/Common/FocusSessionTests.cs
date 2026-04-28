using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Domain.Tests.Common;

public sealed class FocusSessionTests
{
    [Fact]
    public void FromUtc_ComputesLocalDateFromTimezone()
    {
        var session = FocusSession.FromUtc(
            clientSessionId: "session-1",
            deviceId: "windows-device-1",
            platformAppKey: "chrome.exe",
            startedAtUtc: new DateTimeOffset(2026, 4, 27, 15, 30, 0, TimeSpan.Zero),
            endedAtUtc: new DateTimeOffset(2026, 4, 27, 16, 0, 0, TimeSpan.Zero),
            timezoneId: "Asia/Seoul",
            isIdle: false,
            source: "foreground_window");

        Assert.Equal(new DateOnly(2026, 4, 28), session.LocalDate);
    }

    [Fact]
    public void DurationMs_CalculatesElapsedMilliseconds()
    {
        var session = new FocusSession(
            clientSessionId: "session-1",
            deviceId: "windows-device-1",
            platformAppKey: "chrome.exe",
            range: TimeRange.FromUtc(
                new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 4, 28, 0, 1, 30, TimeSpan.Zero)),
            localDate: new DateOnly(2026, 4, 28),
            timezoneId: "Asia/Seoul",
            isIdle: false,
            source: "foreground_window");

        Assert.Equal(90_000, session.DurationMs);
    }

    [Fact]
    public void Constructor_WhenEndedAtIsNotAfterStartedAt_Throws()
    {
        var instant = new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero);

        Assert.Throws<ArgumentException>(() => new FocusSession(
            clientSessionId: "session-1",
            deviceId: "windows-device-1",
            platformAppKey: "chrome.exe",
            range: TimeRange.FromUtc(instant, instant),
            localDate: new DateOnly(2026, 4, 28),
            timezoneId: "Asia/Seoul",
            isIdle: false,
            source: "foreground_window"));
    }
}
