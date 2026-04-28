namespace Woong.MonitorStack.Domain.Common;

public sealed record TimeBucket(DateTimeOffset BucketStartUtc, TimeSpan Duration)
{
    public DateTimeOffset BucketEndUtc => BucketStartUtc.AddHours(1);
}
