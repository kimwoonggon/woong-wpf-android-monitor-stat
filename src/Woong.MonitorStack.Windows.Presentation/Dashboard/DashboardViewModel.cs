using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore.SkiaSharpView;
using System.Globalization;
using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public sealed partial class DashboardViewModel : ObservableObject
{
    private readonly IDashboardDataSource _dataSource;
    private readonly IDashboardClock _clock;
    private readonly TimeZoneInfo _timeZone;
    private TimeRange? _customRange;

    [ObservableProperty]
    private DashboardPeriod _selectedPeriod = DashboardPeriod.Today;

    [ObservableProperty]
    private long _totalActiveMs;

    [ObservableProperty]
    private long _totalIdleMs;

    [ObservableProperty]
    private long _totalWebMs;

    [ObservableProperty]
    private string _topAppName = "";

    [ObservableProperty]
    private string _topDomainName = "";

    [ObservableProperty]
    private IReadOnlyList<DashboardSummaryCard> _summaryCards =
    [
        new("Active", "0m"),
        new("Idle", "0m"),
        new("Web", "0m")
    ];

    [ObservableProperty]
    private IReadOnlyList<DashboardChartPoint> _hourlyActivityPoints = [];

    [ObservableProperty]
    private IReadOnlyList<DashboardChartPoint> _appUsagePoints = [];

    [ObservableProperty]
    private IReadOnlyList<DashboardChartPoint> _domainUsagePoints = [];

    [ObservableProperty]
    private DashboardLiveChartsData _hourlyActivityChart = new([], []);

    [ObservableProperty]
    private DashboardLiveChartsData _appUsageChart = new([], []);

    [ObservableProperty]
    private IReadOnlyList<PieSeries<long>> _domainUsageSeries = [];

    [ObservableProperty]
    private IReadOnlyList<DashboardSessionRow> _recentSessions = [];

    [ObservableProperty]
    private IReadOnlyList<DashboardWebSessionRow> _recentWebSessions = [];

    [ObservableProperty]
    private IReadOnlyList<DashboardEventLogRow> _liveEvents = [];

    public DashboardViewModel(
        IDashboardDataSource dataSource,
        IDashboardClock clock,
        DashboardOptions options)
    {
        _dataSource = dataSource;
        _clock = clock;
        ArgumentNullException.ThrowIfNull(options);
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.TimeZoneId);
    }

    public DashboardSettingsViewModel Settings { get; } = new();

    public void SelectPeriod(DashboardPeriod period)
    {
        SelectedPeriod = period;
        RefreshSummary(ResolveRange(period));
    }

    public void SelectCustomRange(DateTimeOffset startedAtUtc, DateTimeOffset endedAtUtc)
    {
        _customRange = TimeRange.FromUtc(startedAtUtc, endedAtUtc);
        SelectPeriod(DashboardPeriod.Custom);
    }

    [RelayCommand]
    private void SelectDashboardPeriod(DashboardPeriod period)
        => SelectPeriod(period);

    [RelayCommand]
    private void RefreshDashboard()
        => RefreshSummary(ResolveRange(SelectedPeriod));

    private void RefreshSummary(TimeRange range)
    {
        IReadOnlyList<FocusSession> focusSessions = _dataSource.QueryFocusSessions(range.StartedAtUtc, range.EndedAtUtc);
        IReadOnlyList<WebSession> webSessions = _dataSource.QueryWebSessions(range.StartedAtUtc, range.EndedAtUtc);
        DateOnly summaryDate = LocalDateCalculator.GetLocalDate(_clock.UtcNow, _timeZone.Id);
        DailySummary summary = DailySummaryCalculator.Calculate(focusSessions, webSessions, summaryDate, _timeZone.Id);

        TotalActiveMs = summary.TotalActiveMs;
        TotalIdleMs = summary.TotalIdleMs;
        TotalWebMs = summary.TotalWebMs;
        TopAppName = summary.TopApps.FirstOrDefault()?.Key ?? "";
        TopDomainName = summary.TopDomains.FirstOrDefault()?.Key ?? "";
        SummaryCards =
        [
            new("Active", FormatDuration(summary.TotalActiveMs)),
            new("Idle", FormatDuration(summary.TotalIdleMs)),
            new("Web", FormatDuration(summary.TotalWebMs))
        ];
        HourlyActivityPoints = DashboardChartMapper.BuildHourlyActivityPoints(focusSessions, _timeZone.Id);
        AppUsagePoints = DashboardChartMapper.BuildAppUsagePoints(summary);
        DomainUsagePoints = DashboardChartMapper.BuildDomainUsagePoints(summary);
        HourlyActivityChart = DashboardLiveChartsMapper.BuildColumnChart("Activity", HourlyActivityPoints);
        AppUsageChart = DashboardLiveChartsMapper.BuildColumnChart("Apps", AppUsagePoints);
        DomainUsageSeries = DashboardLiveChartsMapper.BuildPieSeries(DomainUsagePoints);
        RecentSessions = BuildRecentSessionRows(focusSessions);
        RecentWebSessions = BuildRecentWebSessionRows(webSessions);
        LiveEvents = BuildLiveEventRows(focusSessions, webSessions);
    }

    private TimeRange ResolveRange(DashboardPeriod period)
    {
        DateTimeOffset utcNow = _clock.UtcNow.ToUniversalTime();

        return period switch
        {
            DashboardPeriod.Today => ResolveTodayRange(utcNow),
            DashboardPeriod.LastHour => TimeRange.FromUtc(utcNow.AddHours(-1), utcNow),
            DashboardPeriod.Last6Hours => TimeRange.FromUtc(utcNow.AddHours(-6), utcNow),
            DashboardPeriod.Last24Hours => TimeRange.FromUtc(utcNow.AddHours(-24), utcNow),
            DashboardPeriod.Custom => _customRange ?? TimeRange.FromUtc(utcNow.AddHours(-1), utcNow),
            _ => throw new ArgumentOutOfRangeException(nameof(period), period, "Unsupported dashboard period.")
        };
    }

    private TimeRange ResolveTodayRange(DateTimeOffset utcNow)
    {
        DateTimeOffset localNow = TimeZoneInfo.ConvertTime(utcNow, _timeZone);
        var localStart = new DateTimeOffset(localNow.Date, localNow.Offset);

        return TimeRange.FromUtc(localStart.ToUniversalTime(), utcNow);
    }

    private IReadOnlyList<DashboardSessionRow> BuildRecentSessionRows(IEnumerable<FocusSession> focusSessions)
    {
        return focusSessions
            .OrderByDescending(session => session.StartedAtUtc)
            .Select(session => new DashboardSessionRow(
                session.PlatformAppKey,
                TimeZoneInfo.ConvertTime(session.StartedAtUtc, _timeZone).ToString("HH:mm", CultureInfo.InvariantCulture),
                FormatDuration(session.DurationMs),
                session.IsIdle))
            .ToList();
    }

    private IReadOnlyList<DashboardWebSessionRow> BuildRecentWebSessionRows(IEnumerable<WebSession> webSessions)
    {
        return webSessions
            .OrderByDescending(session => session.StartedAtUtc)
            .Select(session => new DashboardWebSessionRow(
                session.Domain,
                session.PageTitle,
                TimeZoneInfo.ConvertTime(session.StartedAtUtc, _timeZone).ToString("HH:mm", CultureInfo.InvariantCulture),
                FormatDuration(session.DurationMs)))
            .ToList();
    }

    private IReadOnlyList<DashboardEventLogRow> BuildLiveEventRows(
        IEnumerable<FocusSession> focusSessions,
        IEnumerable<WebSession> webSessions)
    {
        IEnumerable<(DateTimeOffset OccurredAtUtc, DashboardEventLogRow Row)> focusRows = focusSessions
            .Select(session => (
                session.StartedAtUtc,
                new DashboardEventLogRow(
                    "Focus",
                    FormatLocalTime(session.StartedAtUtc, _timeZone),
                    session.PlatformAppKey)));
        IEnumerable<(DateTimeOffset OccurredAtUtc, DashboardEventLogRow Row)> webRows = webSessions
            .Select(session => (
                session.StartedAtUtc,
                new DashboardEventLogRow(
                    "Web",
                    FormatLocalTime(session.StartedAtUtc, _timeZone),
                    session.Domain)));

        return focusRows
            .Concat(webRows)
            .OrderByDescending(row => row.OccurredAtUtc)
            .Select(row => row.Row)
            .ToList();
    }

    private static string FormatLocalTime(DateTimeOffset utcValue, TimeZoneInfo timeZone)
        => TimeZoneInfo.ConvertTime(utcValue, timeZone).ToString("HH:mm", CultureInfo.InvariantCulture);

    private static string FormatDuration(long durationMs)
    {
        if (durationMs <= 0)
        {
            return "0m";
        }

        long totalMinutes = durationMs / 60_000;
        long hours = totalMinutes / 60;
        long minutes = totalMinutes % 60;

        return hours > 0
            ? $"{hours}h {minutes:D2}m"
            : $"{Math.Max(1, minutes)}m";
    }
}
