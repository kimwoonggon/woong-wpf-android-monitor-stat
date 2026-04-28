namespace Woong.MonitorStack.Server.Data;

public sealed class DailySummaryEntity
{
    public long Id { get; set; }

    public string UserId { get; set; } = "";

    public DateOnly SummaryDate { get; set; }

    public string TimezoneId { get; set; } = "";

    public long TotalActiveMs { get; set; }

    public long TotalIdleMs { get; set; }

    public long TotalWebMs { get; set; }

    public string TopAppsJson { get; set; } = "[]";

    public string TopDomainsJson { get; set; } = "[]";

    public DateTimeOffset GeneratedAtUtc { get; set; }
}
