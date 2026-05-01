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
    private readonly IDashboardDatabaseController _databaseController;
    private readonly IDashboardRuntimeLogSink _runtimeLogSink;
    private readonly IDashboardApplicationLifetime _applicationLifetime;
    private readonly IDashboardChartDetailsPresenter _chartDetailsPresenter;
    private readonly TimeZoneInfo _timeZone;
    private readonly List<DashboardEventLogRow> _runtimeLiveEvents = [];
    private IReadOnlyList<DashboardEventLogRow> _persistedLiveEvents = [];
    private string? _currentWindowTitle;
    private bool _isTrackingRunning;
    private TimeRange? _customRange;

    [ObservableProperty]
    private DashboardPeriod _selectedPeriod = DashboardPeriod.Today;

    [ObservableProperty]
    private bool _isCustomRangeEditorVisible;

    [ObservableProperty]
    private DateTime? _customStartDate;

    [ObservableProperty]
    private string _customStartTimeText = "09:00";

    [ObservableProperty]
    private DateTime? _customEndDate;

    [ObservableProperty]
    private string _customEndTimeText = "18:00";

    [ObservableProperty]
    private string _customRangeStatusText = "Choose a date and time range.";

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
    private IReadOnlyList<DashboardChartPoint> _appUsageDetailPoints = [];

    [ObservableProperty]
    private IReadOnlyList<DashboardChartPoint> _domainUsagePoints = [];

    [ObservableProperty]
    private IReadOnlyList<DashboardChartPoint> _domainUsageDetailPoints = [];

    [ObservableProperty]
    private DashboardLiveChartsData _hourlyActivityChart = DashboardLiveChartsMapper.BuildColumnChart("Activity", []);

    [ObservableProperty]
    private DashboardLiveChartsData _appUsageChart = DashboardLiveChartsMapper.BuildHorizontalBarChart("Apps", []);

    [ObservableProperty]
    private DashboardLiveChartsData _domainUsageChart = DashboardLiveChartsMapper.BuildHorizontalBarChart("Domains", []);

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
        IDashboardTrackingCoordinator? trackingCoordinator = null,
        IDashboardDatabaseController? databaseController = null,
        IDashboardRuntimeLogSink? runtimeLogSink = null,
        IDashboardApplicationLifetime? applicationLifetime = null,
        IDashboardChartDetailsPresenter? chartDetailsPresenter = null)
    {
        _dataSource = dataSource;
        _clock = clock;
        _trackingCoordinator = trackingCoordinator ?? new NoopDashboardTrackingCoordinator();
        _databaseController = databaseController ?? new NullDashboardDatabaseController();
        _runtimeLogSink = runtimeLogSink ?? new NullDashboardRuntimeLogSink();
        _applicationLifetime = applicationLifetime ?? new NullDashboardApplicationLifetime();
        _chartDetailsPresenter = chartDetailsPresenter ?? new NullDashboardChartDetailsPresenter();
        ArgumentNullException.ThrowIfNull(options);
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.TimeZoneId);
        InitializeCustomRangeDefaults();
        Settings.CurrentDatabasePathText = _databaseController.CurrentDatabasePath;
        Settings.RuntimeLogPathText = _runtimeLogSink.LogPath;
        Settings.CanClearLocalData = _databaseController.CanDeleteCurrentDatabase;
        Settings.PropertyChanged += OnSettingsPropertyChanged;
    }

    public DashboardSettingsViewModel Settings { get; } = new();

    public void UpdateCurrentActivity(DashboardTrackingSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        CurrentAppNameText = TextOrDefault(snapshot.AppName, "No current app");
        CurrentProcessNameText = TextOrDefault(snapshot.ProcessName, "No process");
        CurrentBrowserDomainText = FormatBrowserDomain(snapshot.CurrentBrowserDomain);
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

        DateTimeOffset? lastDbWriteAtUtc = snapshot.LastDbWriteAtUtc
            ?? snapshot.LastPersistedSession?.EndedAtUtc
            ?? (snapshot.HasPersistedWebSession ? snapshot.LastPollAtUtc : null);
        if (lastDbWriteAtUtc is not null)
        {
            LastDbWriteTimeText = FormatLocalTime(lastDbWriteAtUtc.Value, _timeZone);
        }
    }

    public void SelectPeriod(DashboardPeriod period)
    {
        SelectedPeriod = period;
        IsCustomRangeEditorVisible = period == DashboardPeriod.Custom;
        RefreshSummary(ResolveRange(period));
    }

    public void SelectCustomRange(DateTimeOffset startedAtUtc, DateTimeOffset endedAtUtc)
    {
        _customRange = TimeRange.FromUtc(startedAtUtc, endedAtUtc);
        CustomRangeStatusText = FormatCustomRangeStatus(_customRange);
        SelectPeriod(DashboardPeriod.Custom);
    }

    [RelayCommand]
    private void SelectDashboardPeriod(DashboardPeriod period)
        => SelectPeriod(period);

    [RelayCommand]
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

        DateTimeOffset startedAtUtc = ConvertLocalDashboardDateTimeToUtc(CustomStartDate.Value, startTime);
        DateTimeOffset endedAtUtc = ConvertLocalDashboardDateTimeToUtc(CustomEndDate.Value, endTime);
        if (endedAtUtc <= startedAtUtc)
        {
            CustomRangeStatusText = "Custom end must be after custom start.";
            return;
        }

        SelectCustomRange(startedAtUtc, endedAtUtc);
    }

    [RelayCommand]
    private void ShowAppFocusDetails()
    {
        ShowDetailsTab(DetailsTab.AppSessions);
        _chartDetailsPresenter.ShowChartDetails(new DashboardChartDetailsRequest(
            "App focus details",
            "Apps",
            AppUsageDetailPoints));
    }

    [RelayCommand]
    private void ShowDomainFocusDetails()
    {
        ShowDetailsTab(DetailsTab.WebSessions);
        _chartDetailsPresenter.ShowChartDetails(new DashboardChartDetailsRequest(
            "Domain focus details",
            "Domains",
            DomainUsageDetailPoints));
    }

    [RelayCommand]
    private void RefreshDashboard()
        => RefreshSummary(ResolveRange(SelectedPeriod));

    [RelayCommand]
    private void CreateLocalDatabase()
        => ApplyDatabaseActionResult(_databaseController.CreateNewDatabase());

    [RelayCommand]
    private void LoadExistingLocalDatabase()
        => ApplyDatabaseActionResult(_databaseController.LoadExistingDatabase());

    [RelayCommand]
    private void DeleteLocalDatabase()
        => ApplyDatabaseActionResult(_databaseController.DeleteCurrentDatabase());

    [RelayCommand]
    private void OpenRuntimeLogFolder()
        => ApplyRuntimeLogFolderOpenResult(_runtimeLogSink.OpenLogFolder());

    [RelayCommand]
    private void ExitApplication()
        => _applicationLifetime.RequestExit();

    [RelayCommand(CanExecute = nameof(CanStartTracking))]
    private void StartTracking()
        => RunDashboardOperation(nameof(StartTracking), StartTrackingCore);

    private void StartTrackingCore()
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
        => RunDashboardOperation(nameof(StopTracking), StopTrackingCore);

    private void StopTrackingCore()
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
        => RunDashboardOperation(nameof(PollTracking), PollTrackingCore);

    private void PollTrackingCore()
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
        => RunDashboardOperation(nameof(SyncNow), SyncNowCore);

    private void SyncNowCore()
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

    private void RunDashboardOperation(string operation, Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            ReportRuntimeError(operation, exception);
        }
    }

    private void ReportRuntimeError(string operation, Exception exception)
    {
        try
        {
            _runtimeLogSink.WriteException(operation, exception);
        }
        catch (Exception)
        {
        }

        BrowserCaptureStatusText = "Runtime error logged";
        AddRuntimeEvent(
            "Runtime error",
            CurrentAppNameText,
            CurrentBrowserDomainText,
            $"{operation} failed: {exception.Message}. See log: {_runtimeLogSink.LogPath}");
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

    private void ApplyDatabaseActionResult(DashboardDatabaseActionResult result)
    {
        Settings.CurrentDatabasePathText = result.DatabasePath;
        Settings.DatabaseStatusLabel = result.StatusMessage;
        Settings.CanClearLocalData = _databaseController.CanDeleteCurrentDatabase;
        if (result.Succeeded)
        {
            RefreshSummary(ResolveRange(SelectedPeriod));
        }
    }

    private void ApplyRuntimeLogFolderOpenResult(DashboardRuntimeLogFolderOpenResult result)
    {
        Settings.RuntimeLogStatusLabel = result.StatusMessage;
    }

    private void RefreshSummary(TimeRange range)
    {
        IReadOnlyList<FocusSession> focusSessions = _dataSource.QueryFocusSessions(range.StartedAtUtc, range.EndedAtUtc);
        IReadOnlyList<WebSession> webSessions = _dataSource.QueryWebSessions(range.StartedAtUtc, range.EndedAtUtc);
        DailySummary summary = BuildRangeSummary(focusSessions, webSessions, range);
        long totalForegroundMs = focusSessions.Sum(session => session.DurationMs);
        string periodDescriptor = FormatPeriodDescriptor(SelectedPeriod);

        TotalActiveMs = summary.TotalActiveMs;
        TotalForegroundMs = totalForegroundMs;
        TotalIdleMs = summary.TotalIdleMs;
        TotalWebMs = summary.TotalWebMs;
        TopAppName = summary.TopApps.FirstOrDefault()?.Key ?? "";
        TopDomainName = summary.TopDomains.FirstOrDefault()?.Key ?? "";
        SummaryCards =
        [
            new("Active Focus", FormatDuration(summary.TotalActiveMs), $"{periodDescriptor} focused foreground time"),
            new("Foreground", FormatDuration(totalForegroundMs), $"{periodDescriptor} foreground time"),
            new("Idle", FormatDuration(summary.TotalIdleMs), $"{periodDescriptor} idle foreground time"),
            new("Web Focus", FormatDuration(summary.TotalWebMs), $"{periodDescriptor} browser domain time")
        ];
        HourlyActivityPoints = DashboardChartMapper.BuildHourlyActivityPoints(focusSessions, _timeZone.Id);
        IReadOnlyList<DashboardChartPoint> allAppUsagePoints = DashboardChartMapper.BuildAppUsagePoints(summary);
        IReadOnlyList<DashboardChartPoint> allDomainUsagePoints = DashboardChartMapper.BuildDomainUsagePoints(summary);
        AppUsageDetailPoints = allAppUsagePoints.Take(10).ToList();
        DomainUsageDetailPoints = allDomainUsagePoints.Take(10).ToList();
        AppUsagePoints = allAppUsagePoints.Take(3).ToList();
        DomainUsagePoints = allDomainUsagePoints.Take(3).ToList();
        HourlyActivityChart = DashboardLiveChartsMapper.BuildColumnChart("Activity", HourlyActivityPoints);
        AppUsageChart = DashboardLiveChartsMapper.BuildHorizontalBarChart("Apps", AppUsagePoints);
        DomainUsageChart = DashboardLiveChartsMapper.BuildHorizontalBarChart("Domains", DomainUsagePoints);
        RecentSessions = BuildRecentSessionRows(focusSessions);
        RecentWebSessions = BuildRecentWebSessionRows(webSessions);
        _persistedLiveEvents = BuildLiveEventRows(focusSessions, webSessions);
        PublishLiveEvents();
        CurrentDetailsPage = 1;
        UpdateVisibleDetailsRows();
    }

    private void AddRuntimeEvent(string eventType, string? appName, string? domain, string message)
    {
        DateTimeOffset occurredAtUtc = _clock.UtcNow;
        string resolvedAppName = TextOrDefault(appName, "");
        string resolvedDomain = TextOrDefault(domain, "");
        try
        {
            _runtimeLogSink.WriteEvent(new DashboardRuntimeLogEvent(
                occurredAtUtc,
                eventType,
                resolvedAppName,
                resolvedDomain,
                message));
        }
        catch (Exception)
        {
        }

        _runtimeLiveEvents.Insert(
            0,
            new DashboardEventLogRow(
                eventType,
                FormatLocalTime(occurredAtUtc, _timeZone),
                resolvedAppName,
                resolvedDomain,
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

    private DailySummary BuildRangeSummary(
        IEnumerable<FocusSession> focusSessions,
        IEnumerable<WebSession> webSessions,
        TimeRange range)
    {
        List<FocusSession> focusSessionList = focusSessions.ToList();
        List<WebSession> webSessionList = webSessions.ToList();
        DateOnly summaryDate = LocalDateCalculator.GetLocalDate(range.StartedAtUtc, _timeZone.Id);
        long totalActiveMs = focusSessionList
            .Where(session => !session.IsIdle)
            .Sum(session => session.DurationMs);
        long totalIdleMs = focusSessionList
            .Where(session => session.IsIdle)
            .Sum(session => session.DurationMs);
        List<UsageTotal> topApps = focusSessionList
            .Where(session => !session.IsIdle)
            .GroupBy(session => session.PlatformAppKey)
            .Select(group => new UsageTotal(group.Key, group.Sum(session => session.DurationMs)))
            .OrderByDescending(total => total.DurationMs)
            .ThenBy(total => total.Key, StringComparer.Ordinal)
            .ToList();
        long totalWebMs = webSessionList.Sum(session => session.DurationMs);
        List<UsageTotal> topDomains = webSessionList
            .Where(session => !string.IsNullOrWhiteSpace(session.Domain))
            .GroupBy(session => session.Domain)
            .Select(group => new UsageTotal(group.Key, group.Sum(session => session.DurationMs)))
            .OrderByDescending(total => total.DurationMs)
            .ThenBy(total => total.Key, StringComparer.Ordinal)
            .ToList();

        return new DailySummary(summaryDate, totalActiveMs, totalIdleMs, totalWebMs, topApps, topDomains);
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

    private void InitializeCustomRangeDefaults()
    {
        DateTimeOffset localNow = TimeZoneInfo.ConvertTime(_clock.UtcNow, _timeZone);
        CustomStartDate = localNow.Date;
        CustomEndDate = localNow.Date;
        CustomStartTimeText = localNow.AddHours(-1).ToString("HH:mm", CultureInfo.InvariantCulture);
        CustomEndTimeText = localNow.ToString("HH:mm", CultureInfo.InvariantCulture);
    }

    private DateTimeOffset ConvertLocalDashboardDateTimeToUtc(DateTime date, TimeSpan time)
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

    private static string FormatPeriodDescriptor(DashboardPeriod period)
        => period switch
        {
            DashboardPeriod.Today => "Today's",
            DashboardPeriod.LastHour => "Last 1h",
            DashboardPeriod.Last6Hours => "Last 6h",
            DashboardPeriod.Last24Hours => "Last 24h",
            DashboardPeriod.Custom => "Custom range",
            _ => "Selected range"
        };

    private IReadOnlyList<DashboardSessionRow> BuildRecentSessionRows(IEnumerable<FocusSession> focusSessions)
    {
        return focusSessions
            .OrderByDescending(session => session.StartedAtUtc)
            .Select(session => new DashboardSessionRow(
                session.PlatformAppKey,
                TextOrDefault(session.ProcessName, session.PlatformAppKey),
                TimeZoneInfo.ConvertTime(session.StartedAtUtc, _timeZone).ToString("HH:mm", CultureInfo.InvariantCulture),
                TimeZoneInfo.ConvertTime(session.EndedAtUtc, _timeZone).ToString("HH:mm", CultureInfo.InvariantCulture),
                FormatSessionDuration(session.DurationMs),
                session.IsIdle ? "Idle" : "Active",
                Settings.IsWindowTitleVisible
                    ? TextOrDefault(session.WindowTitle, "No window title")
                    : "Hidden by privacy setting",
                session.Source,
                session.IsIdle,
                session.ProcessPath))
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
                FormatUrlMode(session),
                TimeZoneInfo.ConvertTime(session.StartedAtUtc, _timeZone).ToString("HH:mm", CultureInfo.InvariantCulture),
                TimeZoneInfo.ConvertTime(session.EndedAtUtc, _timeZone).ToString("HH:mm", CultureInfo.InvariantCulture),
                FormatSessionDuration(session.DurationMs),
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

    private static string FormatBrowserDomain(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return BrowserDomainUnavailableText;
        }

        string trimmed = value.Trim();
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out Uri? absoluteUri) && !string.IsNullOrWhiteSpace(absoluteUri.Host))
        {
            return absoluteUri.Host.ToLowerInvariant();
        }

        string candidate = trimmed;
        int pathStart = candidate.IndexOfAny(['/', '?', '#']);
        if (pathStart >= 0)
        {
            candidate = candidate[..pathStart];
        }

        int portStart = candidate.LastIndexOf(':');
        if (portStart > 0)
        {
            candidate = candidate[..portStart];
        }

        return candidate.Trim().ToLowerInvariant();
    }

    private static string FormatUrlMode(WebSession session)
    {
        if (string.IsNullOrWhiteSpace(session.Url))
        {
            return "Domain only";
        }

        return Uri.TryCreate(session.Url, UriKind.Absolute, out Uri? uri) &&
               string.Equals($"{uri.GetLeftPart(UriPartial.Authority)}/", session.Url, StringComparison.OrdinalIgnoreCase)
            ? "Domain only"
            : "Full URL disabled";
    }

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

    private static string FormatSessionDuration(long durationMs)
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

    private static string FormatBrowserCaptureStatus(DashboardBrowserCaptureStatus status)
        => status switch
        {
            DashboardBrowserCaptureStatus.ExtensionConnected => "Browser extension connected",
            DashboardBrowserCaptureStatus.UiAutomationFallbackActive => "Domain from address bar fallback",
            DashboardBrowserCaptureStatus.Error => "Browser capture error",
            _ => "Browser capture unavailable"
        };
}
