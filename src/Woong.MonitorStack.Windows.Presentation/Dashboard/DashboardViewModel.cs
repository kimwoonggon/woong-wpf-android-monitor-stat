using CommunityToolkit.Mvvm.ComponentModel;
using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public sealed partial class DashboardViewModel : ObservableObject
{
    private readonly IDashboardDataSource _dataSource;
    private readonly IDashboardClock _clock;
    private readonly string _timezoneId;
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

    public DashboardViewModel(
        IDashboardDataSource dataSource,
        IDashboardClock clock,
        string timezoneId)
    {
        _dataSource = dataSource;
        _clock = clock;
        _timezoneId = string.IsNullOrWhiteSpace(timezoneId)
            ? throw new ArgumentException("Value must not be empty.", nameof(timezoneId))
            : timezoneId;
    }

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

    private void RefreshSummary(TimeRange range)
    {
        IReadOnlyList<FocusSession> focusSessions = _dataSource.QueryFocusSessions(range.StartedAtUtc, range.EndedAtUtc);
        IReadOnlyList<WebSession> webSessions = _dataSource.QueryWebSessions(range.StartedAtUtc, range.EndedAtUtc);
        DateOnly summaryDate = LocalDateCalculator.GetLocalDate(_clock.UtcNow, _timezoneId);
        DailySummary summary = DailySummaryCalculator.Calculate(focusSessions, webSessions, summaryDate, _timezoneId);

        TotalActiveMs = summary.TotalActiveMs;
        TotalIdleMs = summary.TotalIdleMs;
        TotalWebMs = summary.TotalWebMs;
        TopAppName = summary.TopApps.FirstOrDefault()?.Key ?? "";
        TopDomainName = summary.TopDomains.FirstOrDefault()?.Key ?? "";
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
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(_timezoneId);
        DateTimeOffset localNow = TimeZoneInfo.ConvertTime(utcNow, timeZone);
        var localStart = new DateTimeOffset(localNow.Date, localNow.Offset);

        return TimeRange.FromUtc(localStart.ToUniversalTime(), utcNow);
    }
}
