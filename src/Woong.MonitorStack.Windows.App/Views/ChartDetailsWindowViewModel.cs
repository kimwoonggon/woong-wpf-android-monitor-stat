using System.ComponentModel;
using System.Runtime.CompilerServices;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App.Views;

public sealed class ChartDetailsWindowViewModel : INotifyPropertyChanged
{
    private readonly string _seriesName;
    private readonly IReadOnlyDictionary<DashboardPeriod, IReadOnlyList<DashboardChartPoint>> _periodPoints;
    private DashboardPeriod _selectedPeriod;
    private DashboardLiveChartsData _chart;
    private IReadOnlyList<ChartDetailsRow> _detailRows;

    private ChartDetailsWindowViewModel(
        string title,
        string seriesName,
        DashboardPeriod selectedPeriod,
        IReadOnlyDictionary<DashboardPeriod, IReadOnlyList<DashboardChartPoint>> periodPoints)
    {
        Title = title;
        _seriesName = seriesName;
        _selectedPeriod = selectedPeriod;
        _periodPoints = periodPoints;
        PeriodOptions =
        [
            new(DashboardPeriod.Today, "Today"),
            new(DashboardPeriod.LastHour, "1h"),
            new(DashboardPeriod.Last6Hours, "6h"),
            new(DashboardPeriod.Last24Hours, "24h"),
            new(DashboardPeriod.Custom, "Custom")
        ];
        (_chart, _detailRows) = BuildDetails(_seriesName, ResolvePoints(_selectedPeriod));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Title { get; }

    public IReadOnlyList<ChartDetailsPeriodOption> PeriodOptions { get; }

    public DashboardPeriod SelectedPeriod
    {
        get => _selectedPeriod;
        set => SelectPeriod(value);
    }

    public DashboardLiveChartsData Chart
    {
        get => _chart;
        private set
        {
            if (!ReferenceEquals(_chart, value))
            {
                _chart = value;
                OnPropertyChanged();
            }
        }
    }

    public IReadOnlyList<ChartDetailsRow> DetailRows
    {
        get => _detailRows;
        private set
        {
            if (!ReferenceEquals(_detailRows, value))
            {
                _detailRows = value;
                OnPropertyChanged();
            }
        }
    }

    public static ChartDetailsWindowViewModel FromRequest(DashboardChartDetailsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new ChartDetailsWindowViewModel(
            request.Title,
            request.SeriesName,
            request.SelectedPeriod,
            request.PeriodPoints);
    }

    public void SelectPeriod(DashboardPeriod period)
    {
        if (_selectedPeriod == period)
        {
            return;
        }

        _selectedPeriod = period;
        OnPropertyChanged(nameof(SelectedPeriod));
        (Chart, DetailRows) = BuildDetails(_seriesName, ResolvePoints(period));
    }

    private IReadOnlyList<DashboardChartPoint> ResolvePoints(DashboardPeriod period)
        => _periodPoints.TryGetValue(period, out IReadOnlyList<DashboardChartPoint>? points)
            ? points
            : [];

    private static (DashboardLiveChartsData Chart, IReadOnlyList<ChartDetailsRow> DetailRows) BuildDetails(
        string seriesName,
        IReadOnlyList<DashboardChartPoint> sourcePoints)
    {
        IReadOnlyList<DashboardChartPoint> points = sourcePoints
            .GroupBy(point => point.Label, StringComparer.OrdinalIgnoreCase)
            .Select(group => new DashboardChartPoint(group.First().Label, group.Sum(point => point.ValueMs)))
            .OrderByDescending(point => point.ValueMs)
            .ThenBy(point => point.Label, StringComparer.Ordinal)
            .Take(10)
            .ToList();

        DashboardLiveChartsData chart = DashboardLiveChartsMapper.BuildHorizontalBarChart(
            seriesName,
            points,
            maxCategoryLabelLength: null);
        IReadOnlyList<ChartDetailsRow> detailRows = points
            .Select(point => new ChartDetailsRow(
                point.Label,
                point.ValueMs,
                FormatDuration(point.ValueMs)))
            .ToList();

        return (chart, detailRows);
    }

    private static string FormatDuration(long durationMs)
    {
        if (durationMs <= 0)
        {
            return "0s";
        }

        long totalSeconds = Math.Max(1, (durationMs + 999) / 1_000);
        long hours = totalSeconds / 3_600;
        long minutes = totalSeconds % 3_600 / 60;
        long seconds = totalSeconds % 60;

        if (hours > 0)
        {
            return seconds > 0
                ? $"{hours}h {minutes:D2}m {seconds:D2}s"
                : $"{hours}h {minutes:D2}m";
        }

        if (minutes > 0)
        {
            return seconds > 0
                ? $"{minutes}m {seconds:D2}s"
                : $"{minutes}m";
        }

        return $"{seconds}s";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed record ChartDetailsPeriodOption(
    DashboardPeriod Period,
    string Label);

public sealed record ChartDetailsRow(
    string Label,
    long ValueMs,
    string DurationText);
