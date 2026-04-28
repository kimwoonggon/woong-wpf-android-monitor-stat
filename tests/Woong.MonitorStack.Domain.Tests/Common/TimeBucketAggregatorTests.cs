using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Domain.Tests.Common;

public sealed class TimeBucketAggregatorTests
{
    [Fact]
    public void AggregateByHour_SplitsSessionAcrossHours()
    {
        var session = ActiveSession(
            "session-1",
            new DateTimeOffset(2026, 4, 28, 0, 45, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 28, 2, 15, 0, TimeSpan.Zero));

        var buckets = TimeBucketAggregator.AggregateByHour([session]).ToList();

        Assert.Collection(
            buckets,
            bucket =>
            {
                Assert.Equal(new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero), bucket.BucketStartUtc);
                Assert.Equal(TimeSpan.FromMinutes(15), bucket.Duration);
            },
            bucket =>
            {
                Assert.Equal(new DateTimeOffset(2026, 4, 28, 1, 0, 0, TimeSpan.Zero), bucket.BucketStartUtc);
                Assert.Equal(TimeSpan.FromMinutes(60), bucket.Duration);
            },
            bucket =>
            {
                Assert.Equal(new DateTimeOffset(2026, 4, 28, 2, 0, 0, TimeSpan.Zero), bucket.BucketStartUtc);
                Assert.Equal(TimeSpan.FromMinutes(15), bucket.Duration);
            });
    }

    [Fact]
    public void AggregateByHour_ExcludesIdleSessionsByDefault()
    {
        var active = ActiveSession(
            "active-session",
            new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 28, 0, 10, 0, TimeSpan.Zero));
        var idle = IdleSession(
            "idle-session",
            new DateTimeOffset(2026, 4, 28, 0, 10, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 28, 0, 30, 0, TimeSpan.Zero));

        var bucket = Assert.Single(TimeBucketAggregator.AggregateByHour([active, idle]));

        Assert.Equal(TimeSpan.FromMinutes(10), bucket.Duration);
    }

    private static FocusSession ActiveSession(
        string clientSessionId,
        DateTimeOffset startedAtUtc,
        DateTimeOffset endedAtUtc)
        => Session(clientSessionId, startedAtUtc, endedAtUtc, isIdle: false);

    private static FocusSession IdleSession(
        string clientSessionId,
        DateTimeOffset startedAtUtc,
        DateTimeOffset endedAtUtc)
        => Session(clientSessionId, startedAtUtc, endedAtUtc, isIdle: true);

    private static FocusSession Session(
        string clientSessionId,
        DateTimeOffset startedAtUtc,
        DateTimeOffset endedAtUtc,
        bool isIdle)
        => new(
            clientSessionId: clientSessionId,
            deviceId: "windows-device-1",
            platformAppKey: "chrome.exe",
            range: TimeRange.FromUtc(startedAtUtc, endedAtUtc),
            localDate: LocalDateCalculator.GetLocalDate(startedAtUtc, "Asia/Seoul"),
            timezoneId: "Asia/Seoul",
            isIdle: isIdle,
            source: "foreground_window");
}
