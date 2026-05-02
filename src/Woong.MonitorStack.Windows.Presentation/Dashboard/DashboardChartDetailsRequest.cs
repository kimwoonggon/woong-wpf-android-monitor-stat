namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public sealed class DashboardChartDetailsRequest
{
    public DashboardChartDetailsRequest(
        string Title,
        string SeriesName,
        IReadOnlyList<DashboardChartPoint> Points,
        DashboardPeriod SelectedPeriod = DashboardPeriod.Today,
        IReadOnlyDictionary<DashboardPeriod, IReadOnlyList<DashboardChartPoint>>? PeriodPoints = null)
    {
        this.Title = Title;
        this.SeriesName = SeriesName;
        this.Points = Points;
        this.SelectedPeriod = SelectedPeriod;
        this.PeriodPoints = PeriodPoints ?? new Dictionary<DashboardPeriod, IReadOnlyList<DashboardChartPoint>>
        {
            [SelectedPeriod] = Points
        };
    }

    public string Title { get; }

    public string SeriesName { get; }

    public IReadOnlyList<DashboardChartPoint> Points { get; }

    public DashboardPeriod SelectedPeriod { get; }

    public IReadOnlyDictionary<DashboardPeriod, IReadOnlyList<DashboardChartPoint>> PeriodPoints { get; }
}
