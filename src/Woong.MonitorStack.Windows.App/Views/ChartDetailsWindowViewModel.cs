using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App.Views;

public sealed class ChartDetailsWindowViewModel : INotifyPropertyChanged
{
    private readonly string _seriesName;
    private readonly IReadOnlyDictionary<DashboardPeriod, IReadOnlyList<DashboardChartPoint>> _periodPoints;
    private readonly Func<TimeRange, IReadOnlyList<DashboardChartPoint>>? _customRangePointsProvider;
    private readonly TimeZoneInfo _timeZone;
    private DashboardPeriod _selectedPeriod;
    private DashboardLiveChartsData _chart;
    private IReadOnlyList<ChartDetailsRow> _detailRows;
    private bool _isCustomRangeEditorVisible;
    private DateTime? _customStartDate;
    private string _customStartTimeText;
    private DateTime? _customEndDate;
    private string _customEndTimeText;
    private string _customRangeStatusText = "Choose a custom date and time range.";

    private ChartDetailsWindowViewModel(
        string title,
        string seriesName,
        DashboardPeriod selectedPeriod,
        IReadOnlyDictionary<DashboardPeriod, IReadOnlyList<DashboardChartPoint>> periodPoints,
        Func<TimeRange, IReadOnlyList<DashboardChartPoint>>? customRangePointsProvider,
        string timeZoneId)
    {
        Title = title;
        _seriesName = seriesName;
        _selectedPeriod = selectedPeriod;
        _periodPoints = periodPoints;
        _customRangePointsProvider = customRangePointsProvider;
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        PeriodOptions =
        [
            new(DashboardPeriod.Today, "Today"),
            new(DashboardPeriod.LastHour, "1h"),
            new(DashboardPeriod.Last6Hours, "6h"),
            new(DashboardPeriod.Last24Hours, "24h"),
            new(DashboardPeriod.Custom, "Custom")
        ];
        DateTimeOffset localNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, _timeZone);
        _customStartDate = localNow.Date;
        _customEndDate = localNow.Date;
        _customStartTimeText = localNow.AddHours(-1).ToString("HH:mm", CultureInfo.InvariantCulture);
        _customEndTimeText = localNow.ToString("HH:mm", CultureInfo.InvariantCulture);
        _isCustomRangeEditorVisible = selectedPeriod == DashboardPeriod.Custom;
        ApplyCustomRangeCommand = new ChartDetailsRelayCommand(ApplyCustomRange);
        (_chart, _detailRows) = BuildDetails(_seriesName, ResolvePoints(_selectedPeriod));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Title { get; }

    public IReadOnlyList<ChartDetailsPeriodOption> PeriodOptions { get; }

    public ICommand ApplyCustomRangeCommand { get; }

    public DashboardPeriod SelectedPeriod
    {
        get => _selectedPeriod;
        set => SelectPeriod(value);
    }

    public bool IsCustomRangeEditorVisible
    {
        get => _isCustomRangeEditorVisible;
        private set
        {
            if (_isCustomRangeEditorVisible != value)
            {
                _isCustomRangeEditorVisible = value;
                OnPropertyChanged();
            }
        }
    }

    public DateTime? CustomStartDate
    {
        get => _customStartDate;
        set
        {
            if (_customStartDate != value)
            {
                _customStartDate = value;
                OnPropertyChanged();
            }
        }
    }

    public string CustomStartTimeText
    {
        get => _customStartTimeText;
        set
        {
            if (!string.Equals(_customStartTimeText, value, StringComparison.Ordinal))
            {
                _customStartTimeText = value;
                OnPropertyChanged();
            }
        }
    }

    public DateTime? CustomEndDate
    {
        get => _customEndDate;
        set
        {
            if (_customEndDate != value)
            {
                _customEndDate = value;
                OnPropertyChanged();
            }
        }
    }

    public string CustomEndTimeText
    {
        get => _customEndTimeText;
        set
        {
            if (!string.Equals(_customEndTimeText, value, StringComparison.Ordinal))
            {
                _customEndTimeText = value;
                OnPropertyChanged();
            }
        }
    }

    public string CustomRangeStatusText
    {
        get => _customRangeStatusText;
        private set
        {
            if (!string.Equals(_customRangeStatusText, value, StringComparison.Ordinal))
            {
                _customRangeStatusText = value;
                OnPropertyChanged();
            }
        }
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
            request.PeriodPoints,
            request.CustomRangePointsProvider,
            request.TimeZoneId);
    }

    public void SelectPeriod(DashboardPeriod period)
    {
        if (_selectedPeriod == period)
        {
            return;
        }

        _selectedPeriod = period;
        OnPropertyChanged(nameof(SelectedPeriod));
        IsCustomRangeEditorVisible = period == DashboardPeriod.Custom;
        (Chart, DetailRows) = BuildDetails(_seriesName, ResolvePoints(period));
    }

    private void ApplyCustomRange()
    {
        if (CustomStartDate is null || CustomEndDate is null)
        {
            CustomRangeStatusText = "Choose both start and end dates.";
            return;
        }

        if (!TryParseClockTime(CustomStartTimeText, out TimeSpan startTime)
            || !TryParseClockTime(CustomEndTimeText, out TimeSpan endTime))
        {
            CustomRangeStatusText = "Use HH:mm for custom start and end times.";
            return;
        }

        DateTimeOffset startedAtUtc = ConvertLocalDateTimeToUtc(CustomStartDate.Value, startTime);
        DateTimeOffset endedAtUtc = ConvertLocalDateTimeToUtc(CustomEndDate.Value, endTime);
        if (endedAtUtc <= startedAtUtc)
        {
            CustomRangeStatusText = "Custom end must be after custom start.";
            return;
        }

        _selectedPeriod = DashboardPeriod.Custom;
        OnPropertyChanged(nameof(SelectedPeriod));
        IsCustomRangeEditorVisible = true;
        TimeRange range = TimeRange.FromUtc(startedAtUtc, endedAtUtc);
        IReadOnlyList<DashboardChartPoint> points = _customRangePointsProvider?.Invoke(range)
            ?? ResolvePoints(DashboardPeriod.Custom);
        (Chart, DetailRows) = BuildDetails(_seriesName, points);
        CustomRangeStatusText = FormatCustomRangeStatus(range);
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

    private DateTimeOffset ConvertLocalDateTimeToUtc(DateTime date, TimeSpan time)
    {
        DateTime localDateTime = DateTime.SpecifyKind(date.Date.Add(time), DateTimeKind.Unspecified);
        TimeSpan offset = _timeZone.GetUtcOffset(localDateTime);

        return new DateTimeOffset(localDateTime, offset).ToUniversalTime();
    }

    private string FormatCustomRangeStatus(TimeRange range)
    {
        DateTimeOffset localStart = TimeZoneInfo.ConvertTime(range.StartedAtUtc, _timeZone);
        DateTimeOffset localEnd = TimeZoneInfo.ConvertTime(range.EndedAtUtc, _timeZone);

        return $"{localStart:yyyy-MM-dd HH:mm} - {localEnd:yyyy-MM-dd HH:mm}";
    }

    private static bool TryParseClockTime(string value, out TimeSpan time)
        => TimeSpan.TryParseExact(value, "hh\\:mm", CultureInfo.InvariantCulture, out time)
           || TimeSpan.TryParseExact(value, "h\\:mm", CultureInfo.InvariantCulture, out time);

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

internal sealed class ChartDetailsRelayCommand(Action execute) : ICommand
{
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
        => true;

    public void Execute(object? parameter)
    {
        execute();
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

public sealed record ChartDetailsPeriodOption(
    DashboardPeriod Period,
    string Label);

public sealed record ChartDetailsRow(
    string Label,
    long ValueMs,
    string DurationText);
