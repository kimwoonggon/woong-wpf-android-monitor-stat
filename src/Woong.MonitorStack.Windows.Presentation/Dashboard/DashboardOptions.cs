namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public sealed class DashboardOptions
{
    public DashboardOptions(string timeZoneId)
    {
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(
            string.IsNullOrWhiteSpace(timeZoneId)
                ? throw new ArgumentException("Value must not be empty.", nameof(timeZoneId))
                : timeZoneId);
        TimeZoneId = timeZone.Id;
    }

    public string TimeZoneId { get; }
}
