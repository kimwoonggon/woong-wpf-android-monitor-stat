using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Domain.Contracts;

public sealed record DateRangeStatisticsResponse(
    DateOnly FromDate,
    DateOnly ToDate,
    long TotalActiveMs,
    long TotalIdleMs,
    long TotalWebMs,
    IReadOnlyList<UsageTotal> TopApps,
    IReadOnlyList<UsageTotal> TopDomains);
