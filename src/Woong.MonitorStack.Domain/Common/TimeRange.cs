namespace Woong.MonitorStack.Domain.Common;

public sealed record TimeRange
{
    private TimeRange(DateTimeOffset startedAtUtc, DateTimeOffset endedAtUtc)
    {
        if (endedAtUtc <= startedAtUtc)
        {
            throw new ArgumentException("End must be after start.", nameof(endedAtUtc));
        }

        StartedAtUtc = startedAtUtc;
        EndedAtUtc = endedAtUtc;
    }

    public DateTimeOffset StartedAtUtc { get; }

    public DateTimeOffset EndedAtUtc { get; }

    public TimeSpan Duration => EndedAtUtc - StartedAtUtc;

    public static TimeRange FromUtc(DateTimeOffset startedAtUtc, DateTimeOffset endedAtUtc)
        => new(startedAtUtc.ToUniversalTime(), endedAtUtc.ToUniversalTime());
}
