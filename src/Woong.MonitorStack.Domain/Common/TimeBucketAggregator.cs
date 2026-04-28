namespace Woong.MonitorStack.Domain.Common;

public static class TimeBucketAggregator
{
    public static IReadOnlyList<TimeBucket> AggregateByHour(IEnumerable<FocusSession> sessions)
    {
        ArgumentNullException.ThrowIfNull(sessions);

        var durationsByBucket = new SortedDictionary<DateTimeOffset, TimeSpan>();

        foreach (var session in sessions.Where(session => !session.IsIdle))
        {
            foreach (var (bucketStartUtc, duration) in SplitAcrossHours(session.Range))
            {
                durationsByBucket[bucketStartUtc] = durationsByBucket.TryGetValue(bucketStartUtc, out var existing)
                    ? existing + duration
                    : duration;
            }
        }

        return durationsByBucket
            .Select(pair => new TimeBucket(pair.Key, pair.Value))
            .ToList();
    }

    private static IEnumerable<(DateTimeOffset BucketStartUtc, TimeSpan Duration)> SplitAcrossHours(TimeRange range)
    {
        var cursor = range.StartedAtUtc;

        while (cursor < range.EndedAtUtc)
        {
            var bucketStart = TruncateToHour(cursor);
            var bucketEnd = bucketStart.AddHours(1);
            var segmentEnd = bucketEnd < range.EndedAtUtc ? bucketEnd : range.EndedAtUtc;

            yield return (bucketStart, segmentEnd - cursor);

            cursor = segmentEnd;
        }
    }

    private static DateTimeOffset TruncateToHour(DateTimeOffset value)
        => new(value.Year, value.Month, value.Day, value.Hour, 0, 0, TimeSpan.Zero);
}
