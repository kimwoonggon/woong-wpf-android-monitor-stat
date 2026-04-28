namespace Woong.MonitorStack.Domain.Common;

public static class LocalDateCalculator
{
    public static DateOnly GetLocalDate(DateTimeOffset utcInstant, string timezoneId)
    {
        if (string.IsNullOrWhiteSpace(timezoneId))
        {
            throw new ArgumentException("Value must not be empty.", nameof(timezoneId));
        }

        var timeZone = ResolveTimeZone(timezoneId);
        var local = TimeZoneInfo.ConvertTime(utcInstant.ToUniversalTime(), timeZone);

        return DateOnly.FromDateTime(local.DateTime);
    }

    private static TimeZoneInfo ResolveTimeZone(string timezoneId)
    {
        if (string.Equals(timezoneId, "UTC", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(timezoneId, "Etc/UTC", StringComparison.OrdinalIgnoreCase))
        {
            return TimeZoneInfo.Utc;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        }
        catch (TimeZoneNotFoundException) when (WindowsFallbacks.TryGetValue(timezoneId, out var windowsId))
        {
            return TimeZoneInfo.FindSystemTimeZoneById(windowsId);
        }
        catch (InvalidTimeZoneException) when (WindowsFallbacks.TryGetValue(timezoneId, out var windowsId))
        {
            return TimeZoneInfo.FindSystemTimeZoneById(windowsId);
        }
    }

    private static readonly IReadOnlyDictionary<string, string> WindowsFallbacks =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Asia/Seoul"] = "Korea Standard Time"
        };
}
