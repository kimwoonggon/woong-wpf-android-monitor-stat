using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
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
    private readonly IDashboardSyncRegistrationService _syncRegistrationService;
    private readonly TimeZoneInfo _timeZone;
    private readonly DashboardRowMapper _rowMapper;
    private readonly DashboardPeriodRangeResolver _periodRangeResolver;
    private readonly DashboardCurrentActivityMapper _currentActivityMapper;
    private readonly DashboardDetailsPager _detailsPager = new();
    private readonly List<DashboardEventLogRow> _runtimeLiveEvents = [];
    private IReadOnlyList<DashboardEventLogRow> _persistedLiveEvents = [];
    private string? _currentWindowTitle;
    private DashboardActiveWebSessionPreview? _activeWebSession;
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
    private string _currentBrowserDomainText = DashboardCurrentActivityMapper.BrowserDomainUnavailableText;

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

    public IReadOnlyList<int> RowsPerPageOptions { get; } = DashboardDetailsPager.RowsPerPageOptions;

    public int TotalDetailsPages => _detailsPager.CalculateTotalPages(
        SelectedDetailsTab,
        RecentSessions.Count,
        RecentWebSessions.Count,
        LiveEvents.Count,
        RowsPerPage);

    public string DetailsPageText => $"{CurrentDetailsPage} / {TotalDetailsPages}";

    public DashboardViewModel(
        IDashboardDataSource dataSource,
        IDashboardClock clock,
        DashboardOptions options,
        IDashboardTrackingCoordinator? trackingCoordinator = null,
        IDashboardDatabaseController? databaseController = null,
        IDashboardRuntimeLogSink? runtimeLogSink = null,
        IDashboardApplicationLifetime? applicationLifetime = null,
        IDashboardChartDetailsPresenter? chartDetailsPresenter = null,
        IDashboardSyncRegistrationService? syncRegistrationService = null)
    {
        _dataSource = dataSource;
        _clock = clock;
        _trackingCoordinator = trackingCoordinator ?? new NoopDashboardTrackingCoordinator();
        _databaseController = databaseController ?? new NullDashboardDatabaseController();
        _runtimeLogSink = runtimeLogSink ?? new NullDashboardRuntimeLogSink();
        _applicationLifetime = applicationLifetime ?? new NullDashboardApplicationLifetime();
        _chartDetailsPresenter = chartDetailsPresenter ?? new NullDashboardChartDetailsPresenter();
        _syncRegistrationService = syncRegistrationService ?? NullDashboardSyncRegistrationService.Instance;
        ArgumentNullException.ThrowIfNull(options);
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.TimeZoneId);
        _rowMapper = new DashboardRowMapper(_timeZone);
        _periodRangeResolver = new DashboardPeriodRangeResolver(_timeZone);
        _currentActivityMapper = new DashboardCurrentActivityMapper(_timeZone);
        InitializeCustomRangeDefaults();
        Settings.CurrentDatabasePathText = _databaseController.CurrentDatabasePath;
        Settings.RuntimeLogPathText = _runtimeLogSink.LogPath;
        Settings.CanClearLocalData = _databaseController.CanDeleteCurrentDatabase;
        Settings.PropertyChanged += OnSettingsPropertyChanged;
    }

    public DashboardSettingsViewModel Settings { get; } = new();

    public void UpdateCurrentActivity(DashboardTrackingSnapshot snapshot)
    {
        DashboardCurrentActivityPresentation presentation = _currentActivityMapper.Map(
            snapshot,
            Settings.IsWindowTitleVisible);

        CurrentAppNameText = presentation.AppNameText;
        CurrentProcessNameText = presentation.ProcessNameText;
        CurrentBrowserDomainText = presentation.BrowserDomainText;
        BrowserCaptureStatusText = presentation.BrowserCaptureStatusText;
        _activeWebSession = presentation.ActiveWebSessionPreview;
        _currentWindowTitle = presentation.CapturedWindowTitle;
        CurrentWindowTitleText = presentation.CurrentWindowTitleText;
        CurrentSessionDurationText = presentation.CurrentSessionDurationText;
        if (snapshot.LastPersistedSession is not null)
        {
            LastPersistedSessionText = FormatPersistedSession(snapshot.LastPersistedSession);
        }

        if (presentation.LastPollTimeText is not null)
        {
            LastPollTimeText = presentation.LastPollTimeText;
        }

        if (presentation.LastDbWriteTimeText is not null)
        {
            LastDbWriteTimeText = presentation.LastDbWriteTimeText;
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
        CustomRangeStatusText = _periodRangeResolver.FormatCustomRangeStatus(_customRange);
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

        if (!_periodRangeResolver.TryParseClockTime(CustomStartTimeText, out TimeSpan startTime)
            || !_periodRangeResolver.TryParseClockTime(CustomEndTimeText, out TimeSpan endTime))
        {
            CustomRangeStatusText = "Use HH:mm for custom start and end times.";
            return;
        }

        DateTimeOffset startedAtUtc = _periodRangeResolver.ConvertLocalDashboardDateTimeToUtc(CustomStartDate.Value, startTime);
        DateTimeOffset endedAtUtc = _periodRangeResolver.ConvertLocalDashboardDateTimeToUtc(CustomEndDate.Value, endTime);
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
            AppUsageDetailPoints,
            SelectedPeriod,
            BuildPeriodPointSets(BuildAppUsageDetailPoints),
            BuildAppUsageDetailPoints,
            _timeZone.Id));
    }

    [RelayCommand]
    private void ShowDomainFocusDetails()
    {
        ShowDetailsTab(DetailsTab.WebSessions);
        _chartDetailsPresenter.ShowChartDetails(new DashboardChartDetailsRequest(
            "Domain focus details",
            "Domains",
            DomainUsageDetailPoints,
            SelectedPeriod,
            BuildPeriodPointSets(BuildDomainUsageDetailPoints),
            BuildDomainUsageDetailPoints,
            _timeZone.Id));
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
        bool hasPersistedChanges = snapshot.LastPersistedSession is not null || snapshot.HasPersistedWebSession;
        if (hasPersistedChanges)
        {
            AddPersistenceEvents(snapshot);
            AddSessionStartedEvents(snapshot);
        }

        if (hasPersistedChanges || snapshot.CurrentWebSessionDuration > TimeSpan.Zero)
        {
            RefreshSummary(ResolveRange(SelectedPeriod));
        }
    }

    [RelayCommand]
    private void SyncNow()
        => RunDashboardOperation(nameof(SyncNow), SyncNowCore);

    [RelayCommand]
    private async Task RegisterRepairDeviceAsync()
    {
        if (!Settings.IsSyncEnabled)
        {
            const string skippedStatus = "Register / repair skipped. Enable sync to register this device.";
            Settings.SyncDeviceRegistrationStatusText =
                "Device not registered. Register / repair requires sync opt-in.";
            Settings.SyncStatusLabel = skippedStatus;
            LastSyncStatusText = skippedStatus;
            UpdateSyncBadge();

            return;
        }

        try
        {
            DashboardSyncRegistrationResult result = await _syncRegistrationService.RegisterOrRepairAsync();
            Settings.SyncDeviceRegistrationStatusText = result.StatusText;
            Settings.SyncStatusLabel = result.StatusText;
            Settings.HasSyncFailure = !result.Succeeded;
            LastSyncStatusText = result.StatusText;
            UpdateSyncBadge();
        }
        catch (Exception exception)
        {
            ReportRuntimeError(nameof(RegisterRepairDeviceAsync), exception);
            Settings.SyncDeviceRegistrationStatusText = "Register / repair failed. See runtime log.";
            Settings.ReportSyncFailure("Register / repair failed. See runtime log.");
            LastSyncStatusText = Settings.SyncStatusLabel;
            UpdateSyncBadge();
        }
    }

    [RelayCommand]
    private async Task DisconnectSyncDeviceAsync()
    {
        try
        {
            DashboardSyncRegistrationResult result = await _syncRegistrationService.DisconnectAsync();
            if (result.Succeeded)
            {
                Settings.IsSyncEnabled = false;
            }

            Settings.SyncDeviceRegistrationStatusText = result.StatusText;
            Settings.SyncStatusLabel = result.StatusText;
            Settings.HasSyncFailure = !result.Succeeded;
            LastSyncStatusText = result.StatusText;
            UpdateSyncBadge();
        }
        catch (Exception exception)
        {
            ReportRuntimeError(nameof(DisconnectSyncDeviceAsync), exception);
            Settings.SyncDeviceRegistrationStatusText = "Disconnect failed. See runtime log.";
            Settings.ReportSyncFailure("Disconnect failed. See runtime log.");
            LastSyncStatusText = Settings.SyncStatusLabel;
            UpdateSyncBadge();
        }
    }

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
        if (!_detailsPager.IsSupportedRowsPerPage(value))
        {
            RowsPerPage = DashboardDetailsPager.DefaultRowsPerPage;
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
        CurrentWindowTitleText = _currentActivityMapper.FormatCurrentWindowTitle(
            _currentWindowTitle,
            Settings.IsWindowTitleVisible);
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
        IReadOnlyList<WebSession> persistedWebSessions = _dataSource.QueryWebSessions(range.StartedAtUtc, range.EndedAtUtc);
        IReadOnlyList<WebSession> webSessions = IncludeActiveWebSession(persistedWebSessions, range);
        DashboardSummarySnapshot summarySnapshot = DashboardSummaryBuilder.Build(
            focusSessions,
            webSessions,
            range,
            SelectedPeriod,
            _timeZone.Id);
        DailySummary summary = summarySnapshot.Summary;

        TotalActiveMs = summarySnapshot.TotalActiveMs;
        TotalForegroundMs = summarySnapshot.TotalForegroundMs;
        TotalIdleMs = summarySnapshot.TotalIdleMs;
        TotalWebMs = summarySnapshot.TotalWebMs;
        TopAppName = summarySnapshot.TopAppName;
        TopDomainName = summarySnapshot.TopDomainName;
        SummaryCards = summarySnapshot.SummaryCards;
        HourlyActivityPoints = DashboardChartMapper.BuildHourlyActivityPoints(focusSessions, range, _timeZone.Id);
        IReadOnlyList<DashboardChartPoint> allAppUsagePoints = DashboardChartMapper.BuildAppUsagePoints(summary);
        IReadOnlyList<DashboardChartPoint> allDomainUsagePoints = DashboardChartMapper.BuildDomainUsagePoints(summary);
        AppUsageDetailPoints = allAppUsagePoints.Take(10).ToList();
        DomainUsageDetailPoints = allDomainUsagePoints.Take(10).ToList();
        AppUsagePoints = allAppUsagePoints.Take(3).ToList();
        DomainUsagePoints = allDomainUsagePoints.Take(3).ToList();
        HourlyActivityChart = DashboardLiveChartsMapper.BuildColumnChart("Activity", HourlyActivityPoints);
        AppUsageChart = DashboardLiveChartsMapper.BuildHorizontalBarChart("Apps", AppUsagePoints);
        DomainUsageChart = DashboardLiveChartsMapper.BuildHorizontalBarChart("Domains", DomainUsagePoints);
        RecentSessions = _rowMapper.BuildRecentSessionRows(focusSessions, Settings.IsWindowTitleVisible);
        RecentWebSessions = _rowMapper.BuildRecentWebSessionRows(webSessions, Settings.IsWindowTitleVisible);
        _persistedLiveEvents = _rowMapper.BuildLiveEventRows(focusSessions, webSessions);
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
            _rowMapper.BuildRuntimeEventRow(
                occurredAtUtc,
                eventType,
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

    private void UpdateVisibleDetailsRows()
    {
        DashboardDetailsPage page = _detailsPager.BuildPage(
            SelectedDetailsTab,
            CurrentDetailsPage,
            RowsPerPage,
            RecentSessions,
            RecentWebSessions,
            LiveEvents);
        if (CurrentDetailsPage != page.CurrentPage)
        {
            CurrentDetailsPage = page.CurrentPage;
        }

        VisibleAppSessionRows = page.VisibleAppSessionRows;
        VisibleWebSessionRows = page.VisibleWebSessionRows;
        VisibleLiveEventRows = page.VisibleLiveEventRows;
        OnPropertyChanged(nameof(TotalDetailsPages));
        OnPropertyChanged(nameof(DetailsPageText));
        PreviousDetailsPageCommand.NotifyCanExecuteChanged();
        NextDetailsPageCommand.NotifyCanExecuteChanged();
    }

    private IReadOnlyDictionary<DashboardPeriod, IReadOnlyList<DashboardChartPoint>> BuildPeriodPointSets(
        Func<TimeRange, IReadOnlyList<DashboardChartPoint>> buildPoints)
    {
        DashboardPeriod[] periods =
        [
            DashboardPeriod.Today,
            DashboardPeriod.LastHour,
            DashboardPeriod.Last6Hours,
            DashboardPeriod.Last24Hours,
            DashboardPeriod.Custom
        ];

        return periods.ToDictionary(period => period, period => buildPoints(ResolveRange(period)));
    }

    private IReadOnlyList<DashboardChartPoint> BuildAppUsageDetailPoints(TimeRange range)
    {
        IReadOnlyList<FocusSession> focusSessions = _dataSource.QueryFocusSessions(range.StartedAtUtc, range.EndedAtUtc);
        DailySummary summary = DashboardSummaryBuilder.BuildDailySummary(focusSessions, [], range, _timeZone.Id);

        return DashboardChartMapper.BuildAppUsagePoints(summary).Take(10).ToList();
    }

    private IReadOnlyList<DashboardChartPoint> BuildDomainUsageDetailPoints(TimeRange range)
    {
        IReadOnlyList<WebSession> persistedWebSessions = _dataSource.QueryWebSessions(range.StartedAtUtc, range.EndedAtUtc);
        IReadOnlyList<WebSession> webSessions = IncludeActiveWebSession(persistedWebSessions, range);
        DailySummary summary = DashboardSummaryBuilder.BuildDailySummary([], webSessions, range, _timeZone.Id);

        return DashboardChartMapper.BuildDomainUsagePoints(summary).Take(10).ToList();
    }

    private IReadOnlyList<WebSession> IncludeActiveWebSession(IReadOnlyList<WebSession> persistedWebSessions, TimeRange range)
    {
        WebSession? activeWebSession = CreateActiveWebSessionForRange(range);
        if (activeWebSession is null)
        {
            return persistedWebSessions;
        }

        var sessions = new List<WebSession>(persistedWebSessions.Count + 1) { activeWebSession };
        sessions.AddRange(persistedWebSessions);
        return sessions;
    }

    private WebSession? CreateActiveWebSessionForRange(TimeRange range)
        => _currentActivityMapper.CreateActiveWebSessionForRange(_activeWebSession, range);

    private TimeRange ResolveRange(DashboardPeriod period)
        => _periodRangeResolver.ResolveRange(period, _clock.UtcNow, _customRange);

    private void InitializeCustomRangeDefaults()
    {
        DashboardCustomRangeDefaults defaults = _periodRangeResolver.CreateCustomRangeDefaults(_clock.UtcNow);
        CustomStartDate = defaults.StartDate;
        CustomEndDate = defaults.EndDate;
        CustomStartTimeText = defaults.StartTimeText;
        CustomEndTimeText = defaults.EndTimeText;
    }

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

    private string FormatPersistedSession(DashboardPersistedSessionSnapshot? session)
    {
        if (session is null)
        {
            return "No session persisted";
        }

        return session.ToDisplayText(_timeZone.Id);
    }

}
