namespace Woong.MonitorStack.Domain.Common;

public sealed record DeviceStateSession
{
    public DeviceStateSession(
        string clientSessionId,
        string deviceId,
        DeviceState state,
        TimeRange range,
        DateOnly localDate,
        string timezoneId)
    {
        ClientSessionId = RequiredText.Ensure(clientSessionId, nameof(clientSessionId));
        DeviceId = RequiredText.Ensure(deviceId, nameof(deviceId));
        State = state;
        Range = range;
        LocalDate = localDate;
        TimezoneId = RequiredText.Ensure(timezoneId, nameof(timezoneId));
    }

    public string ClientSessionId { get; }

    public string DeviceId { get; }

    public DeviceState State { get; }

    public TimeRange Range { get; }

    public long DurationMs => Convert.ToInt64(Range.Duration.TotalMilliseconds);

    public DateOnly LocalDate { get; }

    public string TimezoneId { get; }
}
