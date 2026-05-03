using System.Globalization;
using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public sealed class DashboardPeriodRangeResolver
{
    private readonly TimeZoneInfo _timeZone;

    public DashboardPeriodRangeResolver(TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        _timeZone = timeZone;
    }

    public TimeRange ResolveRange(
        DashboardPeriod period,
        DateTimeOffset utcNow,
        TimeRange? customRange = null)
    {
        DateTimeOffset safeUtcNow = utcNow.ToUniversalTime();

        return period switch
        {
            DashboardPeriod.Today => ResolveTodayRange(safeUtcNow),
            DashboardPeriod.LastHour => TimeRange.FromUtc(safeUtcNow.AddHours(-1), safeUtcNow),
            DashboardPeriod.Last6Hours => TimeRange.FromUtc(safeUtcNow.AddHours(-6), safeUtcNow),
            DashboardPeriod.Last24Hours => TimeRange.FromUtc(safeUtcNow.AddHours(-24), safeUtcNow),
            DashboardPeriod.Custom => customRange ?? TimeRange.FromUtc(safeUtcNow.AddHours(-1), safeUtcNow),
            _ => throw new ArgumentOutOfRangeException(nameof(period), period, "Unsupported dashboard period.")
        };
    }

    public TimeRange ResolveTodayRange(DateTimeOffset utcNow)
    {
        DateTimeOffset localNow = TimeZoneInfo.ConvertTime(utcNow.ToUniversalTime(), _timeZone);
        var localStart = new DateTimeOffset(localNow.Date, localNow.Offset);

        return TimeRange.FromUtc(localStart.ToUniversalTime(), utcNow.ToUniversalTime());
    }

    public DashboardCustomRangeDefaults CreateCustomRangeDefaults(DateTimeOffset utcNow)
    {
        DateTimeOffset localNow = TimeZoneInfo.ConvertTime(utcNow.ToUniversalTime(), _timeZone);

        return new DashboardCustomRangeDefaults(
            localNow.Date,
            localNow.AddHours(-1).ToString("HH:mm", CultureInfo.InvariantCulture),
            localNow.Date,
            localNow.ToString("HH:mm", CultureInfo.InvariantCulture));
    }

    public DateTimeOffset ConvertLocalDashboardDateTimeToUtc(DateTime date, TimeSpan time)
    {
        DateTime localDateTime = DateTime.SpecifyKind(date.Date.Add(time), DateTimeKind.Unspecified);
        TimeSpan offset = _timeZone.GetUtcOffset(localDateTime);

        return new DateTimeOffset(localDateTime, offset).ToUniversalTime();
    }

    public string FormatCustomRangeStatus(TimeRange range)
    {
        DateTimeOffset localStart = TimeZoneInfo.ConvertTime(range.StartedAtUtc, _timeZone);
        DateTimeOffset localEnd = TimeZoneInfo.ConvertTime(range.EndedAtUtc, _timeZone);

        return $"{localStart:yyyy-MM-dd HH:mm} - {localEnd:yyyy-MM-dd HH:mm}";
    }

    public bool TryParseClockTime(string value, out TimeSpan time)
        => TimeSpan.TryParseExact(value, "hh\\:mm", CultureInfo.InvariantCulture, out time)
           || TimeSpan.TryParseExact(value, "h\\:mm", CultureInfo.InvariantCulture, out time);
}

public sealed record DashboardCustomRangeDefaults(
    DateTime StartDate,
    string StartTimeText,
    DateTime EndDate,
    string EndTimeText);
