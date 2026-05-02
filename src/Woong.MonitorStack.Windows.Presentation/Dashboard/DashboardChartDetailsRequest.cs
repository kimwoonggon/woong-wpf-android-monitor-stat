using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public sealed class DashboardChartDetailsRequest
{
    public DashboardChartDetailsRequest(
        string Title,
        string SeriesName,
        IReadOnlyList<DashboardChartPoint> Points,
        DashboardPeriod SelectedPeriod = DashboardPeriod.Today,
        IReadOnlyDictionary<DashboardPeriod, IReadOnlyList<DashboardChartPoint>>? PeriodPoints = null,
        Func<TimeRange, IReadOnlyList<DashboardChartPoint>>? CustomRangePointsProvider = null,
        string TimeZoneId = "Asia/Seoul")
    {
        this.Title = Title;
        this.SeriesName = SeriesName;
        this.Points = Points;
        this.SelectedPeriod = SelectedPeriod;
        this.PeriodPoints = PeriodPoints ?? new Dictionary<DashboardPeriod, IReadOnlyList<DashboardChartPoint>>
        {
            [SelectedPeriod] = Points
        };
        this.CustomRangePointsProvider = CustomRangePointsProvider;
        this.TimeZoneId = TimeZoneId;
    }

    public string Title { get; }

    public string SeriesName { get; }

    public IReadOnlyList<DashboardChartPoint> Points { get; }

    public DashboardPeriod SelectedPeriod { get; }

    public IReadOnlyDictionary<DashboardPeriod, IReadOnlyList<DashboardChartPoint>> PeriodPoints { get; }

    public Func<TimeRange, IReadOnlyList<DashboardChartPoint>>? CustomRangePointsProvider { get; }

    public string TimeZoneId { get; }
}
