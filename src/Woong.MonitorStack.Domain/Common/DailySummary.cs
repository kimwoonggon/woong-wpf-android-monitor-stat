namespace Woong.MonitorStack.Domain.Common;

public sealed record DailySummary(
    DateOnly SummaryDate,
    long TotalActiveMs,
    long TotalIdleMs,
    long TotalWebMs,
    IReadOnlyList<UsageTotal> TopApps,
    IReadOnlyList<UsageTotal> TopDomains);
