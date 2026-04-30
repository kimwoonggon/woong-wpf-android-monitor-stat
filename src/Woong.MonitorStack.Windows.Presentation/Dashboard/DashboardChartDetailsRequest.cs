namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public sealed record DashboardChartDetailsRequest(
    string Title,
    string SeriesName,
    IReadOnlyList<DashboardChartPoint> Points);
