using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Globalization;
using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public sealed partial class DashboardViewModel : ObservableObject
{
    private readonly IDashboardDataSource _dataSource;
    private readonly IDashboardClock _clock;
    private readonly IDashboardTrackingCoordinator _trackingCoordinator;
    private readonly TimeZoneInfo _timeZone;
    private readonly List<DashboardEventLogRow> _runtimeLiveEvents = [];
    private IReadOnlyList<DashboardEventLogRow> _persistedLiveEvents = [];
    private string? _currentWindowTitle;
    private bool _isTrackingRunning;
    private TimeRange? _customRange;

    [ObservableProperty]
    private DashboardPeriod _selectedPeriod = DashboardPeriod.Today;

    [ObservableProperty]
    private DetailsTab _selectedDetailsTab = DetailsTab.AppSessions;

    [ObservableProperty]
    private string _trackingStatusText = "Stopped";

    [ObservableProperty]
    private string _trackingBadgeText = "Tracking Stopped";

    [ObservableProperty]
    private string _syncBadgeText = "Sync Off";

    [ObservableProperty]
    private string _privacyBadgeText = "Privacy Safe";

    [ObservableProperty]
    private string _currentAppNameText = "No current app";

    [ObservableProperty]
    private string _currentProcessNameText = "No process";

    [ObservableProperty]
    private string _currentWindowTitleText = "Window title hidden by privacy settings";

    [ObservableProperty]
    private string _currentBrowserDomainText = BrowserDomainUnavailableText;

    [ObservableProperty]
    private string _browserCaptureStatusText = "Browser capture unavailable";

    [ObservableProperty]
    private string _currentSessionDurationText = "00:00:00";

    [ObservableProperty]
    private string _lastPersistedSessionText = "No session persisted";

    [ObservableProperty]
    private string _lastPollTimeText = "No poll yet";

    [ObservableProperty]
    private string _lastDbWriteTimeText = "No DB write yet";

    [ObservableProperty]
    private string _lastSyncStatusText = "Sync is off. Data stays on this Windows device.";

    [ObservableProperty]
    private long _totalActiveMs;

    [ObservableProperty]
    private long _totalForegroundMs;

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
        new("Active Focus", "0m", "Today's focused foreground time"),
        new("Foreground", "0m", "Today's foreground time"),
        new("Idle", "0m", "Today's idle foreground time"),
        new("Web Focus", "0m", "Today's browser domain time")
    ];

    [ObservableProperty]
    private IReadOnlyList<DashboardChartPoint> _hourlyActivityPoints = [];

    [ObservableProperty]
    private IReadOnlyList<DashboardChartPoint> _appUsagePoints = [];

    [ObservableProperty]
    private IReadOnlyList<DashboardChartPoint> _domainUsagePoints = [];

    [ObservableProperty]
    private DashboardLiveChartsData _hourlyActivityChart = DashboardLiveChartsMapper.BuildColumnChart("Activity", []);

    [ObservableProperty]
    private DashboardLiveChartsData _appUsageChart = DashboardLiveChartsMapper.BuildColumnChart("Apps", []);

    [ObservableProperty]
    private DashboardLiveChartsData _domainUsageChart = DashboardLiveChartsMapper.BuildColumnChart("Domains", []);

    [ObservableProperty]
    private IReadOnlyList<DashboardSessionRow> _recentSessions = [];

    [ObservableProperty]
    private IReadOnlyList<DashboardWebSessionRow> _recentWebSessions = [];

    [ObservableProperty]
    private IReadOnlyList<DashboardEventLogRow> _liveEvents = [];

    [ObservableProperty]
    private IReadOnlyList<DashboardSessionRow> _visibleAppSessionRows = [];

    [ObservableProperty]
    private IReadOnlyList<DashboardWebSessionRow> _visibleWebSessionRows = [];

    [ObservableProperty]
    private IReadOnlyList<DashboardEventLogRow> _visibleLiveEventRows = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DetailsPageText))]
    [NotifyPropertyChangedFor(nameof(TotalDetailsPages))]
    private int _currentDetailsPage = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DetailsPageText))]
    [NotifyPropertyChangedFor(nameof(TotalDetailsPages))]
    private int _rowsPerPage = 10;

    public IReadOnlyList<int> RowsPerPageOptions { get; } = [10, 25, 50];

    public int TotalDetailsPages => Math.Max(1, CalculateTotalDetailsPages());

    public string DetailsPageText => $"{CurrentDetailsPage} / {TotalDetailsPages}";

    private const string BrowserDomainUnavailableText = "No browser domain yet. Connect browser capture; app focus is tracked.";

    public DashboardViewModel(
        IDashboardDataSource dataSource,
        IDashboardClock clock,
        DashboardOptions options,
        IDashboardTrackingCoordinator? trackingCoordinator = null)
    {
        _dataSource = dataSource;
        _clock = clock;
        _trackingCoordinator = trackingCoordinator ?? new NoopDashboardTrackingCoordinator();
        ArgumentNullException.ThrowIfNull(options);
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.TimeZoneId);
        Settings.PropertyChanged += OnSettingsPropertyChanged;
    }

    public DashboardSettingsViewModel Settings { get; } = new();

    public void UpdateCurrentActivity(DashboardTrackingSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        CurrentAppNameText = TextOrDefault(snapshot.AppName, "No current app");
        CurrentProcessNameText = TextOrDefault(snapshot.ProcessName, "No process");
        CurrentBrowserDomainText = TextOrDefault(snapshot.CurrentBrowserDomain, BrowserDomainUnavailableText);
        BrowserCaptureStatusText = FormatBrowserCaptureStatus(snapshot.BrowserCaptureStatus);
        _currentWindowTitle = Settings.IsWindowTitleVisible ? snapshot.WindowTitle : null;
        UpdateCurrentWindowTitleText();
        CurrentSessionDurationText = FormatClockDuration(snapshot.CurrentSessionDuration);
        if (snapshot.LastPersistedSession is not null)
        {
            LastPersistedSessionText = FormatPersistedSession(snapshot.LastPersistedSession);
        }

        if (snapshot.LastPollAtUtc is not null)
        {
            LastPollTimeText = FormatLocalTime(snapshot.LastPollAtUtc.Value, _timeZone);
        }

        DateTimeOffset? lastDbWriteAtUtc = snapshot.LastDbWriteAtUtc ?? snapshot.LastPersistedSession?.EndedAtUtc;
        if (lastDbWriteAtUtc is not null)
        {
            LastDbWriteTimeText = FormatLocalTime(lastDbWriteAtUtc.Value, _timeZone);
        }
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

    [RelayCommand]
    private void SelectDashboardPeriod(DashboardPeriod period)
        => SelectPeriod(period);

    [RelayCommand]
    private void ShowAppFocusDetails()
        => ShowDetailsTab(DetailsTab.AppSessions);

    [RelayCommand]
    private void ShowDomainFocusDetails()
        => ShowDetailsTab(DetailsTab.WebSessions);

    [RelayCommand]
    private void RefreshDashboard()
        => RefreshSummary(ResolveRange(SelectedPeriod));

    [RelayCommand(CanExecute = nameof(CanStartTracking))]
    private void StartTracking()
    {
        _isTrackingRunning = true;
        TrackingStatusText = "Running";
        TrackingBadgeText = "Tracking Running";
        DashboardTrackingSnapshot snapshot = _trackingCoordinator.StartTracking();
        UpdateCurrentActivity(snapshot);
        AddRuntimeEvent("Tracking started", snapshot.AppName, snapshot.CurrentBrowserDomain, "Tracking started.");
        AddSessionStartedEvents(snapshot);
        SyncNow();
        StartTrackingCommand.NotifyCanExecuteChanged();
        StopTrackingCommand.NotifyCanExecuteChanged();
        PollTrackingCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanStopTracking))]
    private void StopTracking()
    {
        _isTrackingRunning = false;
        TrackingStatusText = "Stopped";
        TrackingBadgeText = "Tracking Stopped";
        DashboardTrackingSnapshot snapshot = _trackingCoordinator.StopTracking();
        UpdateCurrentActivity(snapshot);
        AddPersistenceEvents(snapshot);
        AddRuntimeEvent("Tracking stopped", snapshot.AppName, snapshot.CurrentBrowserDomain, "Tracking stopped.");
        RefreshSummary(ResolveRange(SelectedPeriod));
        StartTrackingCommand.NotifyCanExecuteChanged();
        StopTrackingCommand.NotifyCanExecuteChanged();
        PollTrackingCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanStopTracking))]
    private void PollTracking()
    {
        DashboardTrackingSnapshot snapshot = _trackingCoordinator.PollOnce();
        UpdateCurrentActivity(snapshot);
        if (snapshot.LastPersistedSession is not null || snapshot.HasPersistedWebSession)
        {
            AddPersistenceEvents(snapshot);
            AddSessionStartedEvents(snapshot);
            RefreshSummary(ResolveRange(SelectedPeriod));
        }
    }

    [RelayCommand]
    private void SyncNow()
    {
        DashboardSyncResult syncResult = _trackingCoordinator.SyncNow(Settings.IsSyncEnabled);
        LastSyncStatusText = syncResult.StatusText;
        Settings.SyncStatusLabel = LastSyncStatusText;
        UpdateSyncBadge();
        if (!Settings.IsSyncEnabled)
        {
            AddRuntimeEvent("Sync skipped", CurrentAppNameText, CurrentBrowserDomainText, syncResult.StatusText);
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoToPreviousDetailsPage))]
    private void PreviousDetailsPage()
    {
        if (CurrentDetailsPage <= 1)
        {
            return;
        }

        CurrentDetailsPage--;
        UpdateVisibleDetailsRows();
    }

    [RelayCommand(CanExecute = nameof(CanGoToNextDetailsPage))]
    private void NextDetailsPage()
    {
        if (CurrentDetailsPage >= TotalDetailsPages)
        {
            return;
        }

        CurrentDetailsPage++;
        UpdateVisibleDetailsRows();
    }

    private bool CanStartTracking()
        => !_isTrackingRunning;

    private bool CanStopTracking()
        => _isTrackingRunning;

    private bool CanGoToPreviousDetailsPage()
        => CurrentDetailsPage > 1;

    private bool CanGoToNextDetailsPage()
        => CurrentDetailsPage < TotalDetailsPages;

    private void ShowDetailsTab(DetailsTab detailsTab)
    {
        if (SelectedDetailsTab == detailsTab)
        {
            CurrentDetailsPage = 1;
            UpdateVisibleDetailsRows();
            return;
        }

        SelectedDetailsTab = detailsTab;
    }

    partial void OnRowsPerPageChanged(int value)
    {
        if (!RowsPerPageOptions.Contains(value))
        {
            RowsPerPage = 10;
            return;
        }

        UpdateVisibleDetailsRows();
    }

    partial void OnSelectedDetailsTabChanged(DetailsTab value)
    {
        CurrentDetailsPage = 1;
        UpdateVisibleDetailsRows();
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DashboardSettingsViewModel.IsWindowTitleVisible))
        {
            if (!Settings.IsWindowTitleVisible)
            {
                _currentWindowTitle = null;
            }

            UpdateCurrentWindowTitleText();
            UpdatePrivacyBadge();
        }

        if (e.PropertyName is nameof(DashboardSettingsViewModel.IsSyncEnabled)
            or nameof(DashboardSettingsViewModel.HasSyncFailure)
            or nameof(DashboardSettingsViewModel.SyncStatusLabel))
        {
            if (e.PropertyName == nameof(DashboardSettingsViewModel.SyncStatusLabel))
            {
                LastSyncStatusText = Settings.SyncStatusLabel;
            }

            UpdateSyncBadge();
        }
    }

    private void UpdateCurrentWindowTitleText()
    {
        CurrentWindowTitleText = Settings.IsWindowTitleVisible
            ? TextOrDefault(_currentWindowTitle, "No window title")
            : "Window title hidden by privacy settings";
    }

    private void RefreshSummary(TimeRange range)
    {
        IReadOnlyList<FocusSession> focusSessions = _dataSource.QueryFocusSessions(range.StartedAtUtc, range.EndedAtUtc);
        IReadOnlyList<WebSession> webSessions = _dataSource.QueryWebSessions(range.StartedAtUtc, range.EndedAtUtc);
        DateOnly summaryDate = LocalDateCalculator.GetLocalDate(_clock.UtcNow, _timeZone.Id);
        DailySummary summary = DailySummaryCalculator.Calculate(focusSessions, webSessions, summaryDate, _timeZone.Id);
        long totalForegroundMs = focusSessions.Sum(session => session.DurationMs);

        TotalActiveMs = summary.TotalActiveMs;
        TotalForegroundMs = totalForegroundMs;
        TotalIdleMs = summary.TotalIdleMs;
        TotalWebMs = summary.TotalWebMs;
        TopAppName = summary.TopApps.FirstOrDefault()?.Key ?? "";
        TopDomainName = summary.TopDomains.FirstOrDefault()?.Key ?? "";
        SummaryCards =
        [
            new("Active Focus", FormatDuration(summary.TotalActiveMs), "Today's focused foreground time"),
            new("Foreground", FormatDuration(totalForegroundMs), "Today's foreground time"),
            new("Idle", FormatDuration(summary.TotalIdleMs), "Today's idle foreground time"),
            new("Web Focus", FormatDuration(summary.TotalWebMs), "Today's browser domain time")
        ];
        HourlyActivityPoints = DashboardChartMapper.BuildHourlyActivityPoints(focusSessions, _timeZone.Id);
        AppUsagePoints = DashboardChartMapper.BuildAppUsagePoints(summary);
        DomainUsagePoints = DashboardChartMapper.BuildDomainUsagePoints(summary);
        HourlyActivityChart = DashboardLiveChartsMapper.BuildColumnChart("Activity", HourlyActivityPoints);
        AppUsageChart = DashboardLiveChartsMapper.BuildColumnChart("Apps", AppUsagePoints);
        DomainUsageChart = DashboardLiveChartsMapper.BuildColumnChart("Domains", DomainUsagePoints);
        RecentSessions = BuildRecentSessionRows(focusSessions);
        RecentWebSessions = BuildRecentWebSessionRows(webSessions);
        _persistedLiveEvents = BuildLiveEventRows(focusSessions, webSessions);
        PublishLiveEvents();
        CurrentDetailsPage = 1;
        UpdateVisibleDetailsRows();
    }

    private void AddRuntimeEvent(string eventType, string? appName, string? domain, string message)
    {
        _runtimeLiveEvents.Insert(
            0,
            new DashboardEventLogRow(
                eventType,
                FormatLocalTime(_clock.UtcNow, _timeZone),
                TextOrDefault(appName, ""),
                TextOrDefault(domain, ""),
                message));
        PublishLiveEvents();
        UpdateVisibleDetailsRows();
    }

    private void AddSessionStartedEvents(DashboardTrackingSnapshot snapshot)
    {
        if (!string.IsNullOrWhiteSpace(snapshot.AppName))
        {
            AddRuntimeEvent(
                "FocusSession started",
                snapshot.AppName,
                snapshot.CurrentBrowserDomain,
                $"FocusSession started for {snapshot.AppName}.");
        }

        if (!string.IsNullOrWhiteSpace(snapshot.CurrentBrowserDomain))
        {
            AddRuntimeEvent(
                "WebSession started",
                snapshot.AppName,
                snapshot.CurrentBrowserDomain,
                $"WebSession started for {snapshot.CurrentBrowserDomain}.");
        }
    }

    private void AddPersistenceEvents(DashboardTrackingSnapshot snapshot)
    {
        if (snapshot.LastPersistedSession is not null)
        {
            string appName = TextOrDefault(
                snapshot.LastPersistedSession.AppName,
                TextOrDefault(snapshot.LastPersistedSession.ProcessName, "Unknown app"));
            AddRuntimeEvent("FocusSession closed", appName, snapshot.CurrentBrowserDomain, $"FocusSession closed for {appName}.");
            AddRuntimeEvent("FocusSession persisted", appName, snapshot.CurrentBrowserDomain, $"FocusSession persisted for {appName}.");
            AddRuntimeEvent("Outbox row created", appName, snapshot.CurrentBrowserDomain, $"Outbox row created for {appName}.");
        }

        if (snapshot.HasPersistedWebSession)
        {
            AddRuntimeEvent("WebSession closed", snapshot.AppName, snapshot.CurrentBrowserDomain, "WebSession closed.");
            AddRuntimeEvent("WebSession persisted", snapshot.AppName, snapshot.CurrentBrowserDomain, "WebSession persisted.");
            AddRuntimeEvent("Outbox row created", snapshot.AppName, snapshot.CurrentBrowserDomain, "Outbox row created for WebSession.");
        }
    }

    private void PublishLiveEvents()
        => LiveEvents = _runtimeLiveEvents
            .Concat(_persistedLiveEvents)
            .ToList();

    private int CalculateTotalDetailsPages()
    {
        int selectedRowCount = SelectedDetailsTab switch
        {
            DetailsTab.AppSessions => RecentSessions.Count,
            DetailsTab.WebSessions => RecentWebSessions.Count,
            DetailsTab.LiveEventLog => LiveEvents.Count,
            DetailsTab.Settings => 0,
            _ => 0
        };

        return (int)Math.Ceiling(selectedRowCount / (double)Math.Max(1, RowsPerPage));
    }

    private void UpdateVisibleDetailsRows()
    {
        int totalPages = TotalDetailsPages;
        if (CurrentDetailsPage > totalPages)
        {
            CurrentDetailsPage = totalPages;
        }

        int skip = (CurrentDetailsPage - 1) * RowsPerPage;
        VisibleAppSessionRows = RecentSessions.Skip(skip).Take(RowsPerPage).ToList();
        VisibleWebSessionRows = RecentWebSessions.Skip(skip).Take(RowsPerPage).ToList();
        VisibleLiveEventRows = LiveEvents.Skip(skip).Take(RowsPerPage).ToList();
        OnPropertyChanged(nameof(TotalDetailsPages));
        OnPropertyChanged(nameof(DetailsPageText));
        PreviousDetailsPageCommand.NotifyCanExecuteChanged();
        NextDetailsPageCommand.NotifyCanExecuteChanged();
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
                TextOrDefault(session.ProcessName, session.PlatformAppKey),
                TimeZoneInfo.ConvertTime(session.StartedAtUtc, _timeZone).ToString("HH:mm", CultureInfo.InvariantCulture),
                TimeZoneInfo.ConvertTime(session.EndedAtUtc, _timeZone).ToString("HH:mm", CultureInfo.InvariantCulture),
                FormatDuration(session.DurationMs),
                session.IsIdle ? "Idle" : "Active",
                Settings.IsWindowTitleVisible
                    ? TextOrDefault(session.WindowTitle, "No window title")
                    : "Hidden by privacy setting",
                session.Source,
                session.IsIdle))
            .ToList();
    }

    private IReadOnlyList<DashboardWebSessionRow> BuildRecentWebSessionRows(IEnumerable<WebSession> webSessions)
    {
        return webSessions
            .OrderByDescending(session => session.StartedAtUtc)
            .Select(session => new DashboardWebSessionRow(
                session.Domain,
                Settings.IsWindowTitleVisible
                    ? TextOrDefault(session.PageTitle, "No page title")
                    : "Page title hidden by privacy settings",
                session.Url is null ? "Domain only" : "Full URL disabled",
                TimeZoneInfo.ConvertTime(session.StartedAtUtc, _timeZone).ToString("HH:mm", CultureInfo.InvariantCulture),
                TimeZoneInfo.ConvertTime(session.EndedAtUtc, _timeZone).ToString("HH:mm", CultureInfo.InvariantCulture),
                FormatDuration(session.DurationMs),
                session.BrowserFamily,
                TextOrDefault(session.CaptureConfidence, "Unknown")))
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
                    session.PlatformAppKey,
                    "",
                    session.PlatformAppKey)));
        IEnumerable<(DateTimeOffset OccurredAtUtc, DashboardEventLogRow Row)> webRows = webSessions
            .Select(session => (
                session.StartedAtUtc,
                new DashboardEventLogRow(
                    "Web",
                    FormatLocalTime(session.StartedAtUtc, _timeZone),
                    "",
                    session.Domain,
                    session.Domain)));

        return focusRows
            .Concat(webRows)
            .OrderByDescending(row => row.OccurredAtUtc)
            .Select(row => row.Row)
            .ToList();
    }

    private static string FormatLocalTime(DateTimeOffset utcValue, TimeZoneInfo timeZone)
        => TimeZoneInfo.ConvertTime(utcValue, timeZone).ToString("HH:mm", CultureInfo.InvariantCulture);

    private void UpdateSyncBadge()
    {
        SyncBadgeText = Settings.HasSyncFailure
            ? "Sync Error"
            : Settings.IsSyncEnabled ? "Sync On" : "Sync Off";
    }

    private void UpdatePrivacyBadge()
    {
        PrivacyBadgeText = Settings.IsWindowTitleVisible
            ? "Privacy Custom"
            : "Privacy Safe";
    }

    private static string TextOrDefault(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value;

    private static string FormatClockDuration(TimeSpan duration)
    {
        TimeSpan safeDuration = duration < TimeSpan.Zero ? TimeSpan.Zero : duration;

        return $"{(int)safeDuration.TotalHours:D2}:{safeDuration.Minutes:D2}:{safeDuration.Seconds:D2}";
    }

    private string FormatPersistedSession(DashboardPersistedSessionSnapshot? session)
    {
        if (session is null)
        {
            return "No session persisted";
        }

        return session.ToDisplayText(_timeZone.Id);
    }

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

    private static string FormatBrowserCaptureStatus(DashboardBrowserCaptureStatus status)
        => status switch
        {
            DashboardBrowserCaptureStatus.ExtensionConnected => "Browser extension connected",
            DashboardBrowserCaptureStatus.UiAutomationFallbackActive => "Domain from address bar fallback",
            DashboardBrowserCaptureStatus.Error => "Browser capture error",
            _ => "Browser capture unavailable"
        };
}
