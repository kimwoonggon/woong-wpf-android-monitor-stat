using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

internal static class DashboardUsageLabelGrouper
{
    public static IReadOnlyList<UsageTotal> GroupTotals(IEnumerable<UsageTotal> totals)
    {
        ArgumentNullException.ThrowIfNull(totals);

        return totals
            .Where(total => !string.IsNullOrWhiteSpace(total.Key))
            .GroupBy(total => total.Key.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new UsageTotal(
                SelectDisplayLabel(group.Select(total => total.Key)),
                group.Sum(total => total.DurationMs)))
            .OrderByDescending(total => total.DurationMs)
            .ThenBy(total => total.Key, StringComparer.Ordinal)
            .ToList();
    }

    private static string SelectDisplayLabel(IEnumerable<string> labels)
        => labels
            .Select(label => label.Trim())
            .Where(label => label.Length > 0)
            .OrderByDescending(label => label.Length)
            .ThenBy(label => label, StringComparer.Ordinal)
            .First();
}
