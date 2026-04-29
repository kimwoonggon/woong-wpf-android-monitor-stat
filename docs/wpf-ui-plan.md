# WPF UI Plan

Updated: 2026-04-29

This document is the durable implementation plan for the Windows WPF dashboard
UI. It is based on the provided UI goal image and the product requirement that
the UI prove the real runtime tracking pipeline, not just render a polished
shell.

The reference image was provided in the Codex conversation on 2026-04-29. This
repository stores the textual implementation target here so future agents can
continue without relying on transient chat context.

## Product Metric

Primary metric: **Active Focus Time**.

Definitions:

- `FocusSession`: OS foreground app/window interval. It is not process running
  time and does not include background apps.
- Active Focus Time: non-idle `FocusSession` duration.
- Foreground Time: total foreground `FocusSession` duration including idle.
- Idle Time: foreground time where the user is idle.
- `WebSession`: browser tab/domain/url interval inside a browser
  `FocusSession`.

Dashboard values must aggregate persisted SQLite `FocusSession` and
`WebSession` rows. Production dashboard values must not come from fake
in-memory rows.

## UI Questions

The WPF dashboard must answer these questions at a glance:

1. Is tracking currently running?
2. What app/window/domain is currently focused?
3. How long has the current focus session lasted?
4. Was the last closed session persisted to SQLite?
5. How much Active Focus Time did I have today?
6. Which apps consumed the most focus time?
7. Which browser domains consumed the most web focus time?
8. Is sync off/local-only or enabled?
9. Are privacy settings safe?
10. Are App/Web sessions actually visible in the grids?

## Target Structure

```text
MainWindow
  DashboardView
    HeaderStatusBar
    ControlBar
    CurrentFocusPanel
    SummaryCardsPanel
    ChartsPanel
    DetailsTabsPanel
```

The target visual language is a modern desktop dashboard: spacious sections,
clear cards, readable command buttons, useful chart labels, and data grids that
remain usable instead of looking like clipped debug output.

## Component Architecture

The dashboard should not stay as one large `MainWindow.xaml`. The WPF shell
should be decomposed into reusable views, controls, and resource dictionaries.
`Windows.App` owns WPF UI composition and XAML. `Windows.Presentation` owns
MVVM state, display models, commands, and chart mapping that can be tested
without WPF UI automation.

Recommended WPF app structure:

```text
src/Woong.MonitorStack.Windows.App/
  MainWindow.xaml
  MainWindow.xaml.cs
  App.xaml
  App.xaml.cs
  Views/
    DashboardView.xaml
    HeaderStatusBar.xaml
    ControlBar.xaml
    CurrentFocusPanel.xaml
    SummaryCardsPanel.xaml
    ChartsPanel.xaml
    DetailsTabsPanel.xaml
    SettingsPanel.xaml
  Controls/
    StatusBadge.xaml
    MetricCard.xaml
    SectionCard.xaml
    DetailRow.xaml
    EmptyState.xaml
  Styles/
    Colors.xaml
    Typography.xaml
    Buttons.xaml
    Cards.xaml
    Badges.xaml
    DataGrid.xaml
    Tabs.xaml
```

Recommended presentation structure:

```text
src/Woong.MonitorStack.Windows.Presentation/
  Dashboard/
    DashboardViewModel.cs
    HeaderStatusViewModel.cs
    ControlBarViewModel.cs
    CurrentFocusViewModel.cs
    SummaryCardsViewModel.cs
    ChartsViewModel.cs
    DetailsTabsViewModel.cs
    AppSessionRowViewModel.cs
    WebSessionRowViewModel.cs
    LiveEventRowViewModel.cs
    MetricCardViewModel.cs
    StatusBadgeViewModel.cs
  Charts/
    HourlyActivityChartMapper.cs
    AppFocusChartMapper.cs
    DomainFocusChartMapper.cs
```

This decomposition should happen in tested slices. Keep existing public
automation IDs stable while moving markup into controls.

MainWindow responsibilities:

- Window title.
- Minimum size: `MinWidth=1024`, `MinHeight=768`.
- Centered startup and shell background.
- Host `DashboardView`.
- Thin code-behind only.

DashboardView responsibilities:

- One vertical `ScrollViewer`.
- Section ordering: header, control bar, current focus, summary cards, charts,
  details tabs.
- No business logic.

Reusable controls:

- `StatusBadge`: text plus kind/dot color for tracking/sync/privacy states.
- `MetricCard`: title, value, subtitle, optional kind/icon.
- `SectionCard`: title, optional top-right action, content presenter.
- `DetailRow`: label/value pair with trimming for long process/window text.
- `EmptyState`: reusable visible no-data message.

Styles/resource dictionaries:

- `Colors.xaml` for background/surface/border/text/accent brushes.
- `Typography.xaml` for heading, subtitle, section title, muted/body text.
- `Buttons.xaml` for base, primary, danger, secondary, period, and small link
  button styles.
- `Cards.xaml` for section/card surfaces.
- `Badges.xaml` for status badge shape and badge text typography without
  redefining tracking/sync/privacy color resources.
- `DataGrid.xaml` for session grid readability.
- `Tabs.xaml` for details tabs.

Details chart actions:

- App chart `상세보기` selects the App Sessions tab.
- Domain chart `상세보기` selects the Web Sessions tab.
- Initial behavior should be tab switching, not modal windows.
- Current implementation: `DashboardViewModel.SelectedDetailsTab` is the
  presentation state, chart buttons live in `ChartsPanel`, and `DashboardTabs`
  binds its selected value to this state.

Details pagination:

- Details rows-per-page pagination is in Milestone 31 scope.
- The default is 10 rows per page with `RowsPerPageOptions` of 10, 25, and 50.
- The SQLite-backed full row collections remain the source of truth.
- `VisibleAppSessionRows`, `VisibleWebSessionRows`, and
  `VisibleLiveEventRows` are derived presentation collections used by the
  DataGrids.
- `CurrentDetailsPage`, `DetailsPageText`, `PreviousDetailsPageCommand`, and
  `NextDetailsPageCommand` drive the shared details pager footer.

Current details tab implementation:

- `DashboardView` hosts `DetailsTabsPanel`.
- `DetailsTabsPanel` owns the `DashboardTabs` `TabControl` and App/Web/Live
  tab content.
- `DashboardTabs.SelectedValue` binds two-way to
  `DashboardViewModel.SelectedDetailsTab`.
- The App/Web/Live DataGrids bind to visible paged row collections and share a
  footer with rows-per-page and previous/next controls.
- `SettingsPanel` is now hosted inside the Settings tab and inherits the
  existing dashboard `DataContext`.

Current reusable control implementation:

- `MetricCard` renders summary card label/value/subtitle.
- `EmptyState` renders chart empty-state text with a stable inner TextBlock
  AutomationId.
- `StatusBadge` renders header tracking/sync/privacy badges and preserves the
  original badge AutomationIds.
- `DetailRow` renders Current Focus label/value rows while preserving the
  value AutomationIds used by semantic tests and snapshot automation.
- `SectionCard` renders a reusable surface with optional title, optional
  top-right action command, and a `CardContent` slot. `ChartsPanel` now uses it
  for the shared chart card surface while preserving chart AutomationIds and
  app/domain details tab-switch actions.

Current style dictionary implementation:

- `Styles/Buttons.xaml` defines the shared dashboard, primary, danger,
  secondary, period, and compact action button styles.
- `ControlBar`, `SettingsPanel`, and `DetailsTabsPanel` use the shared button
  dictionary instead of duplicate local button style definitions.
- `ChartsPanel` and `SectionCard` use `CompactActionButtonStyle` for small
  top-right action buttons instead of inline sizing setters.
- `Styles/Inputs.xaml` defines shared input styles such as
  `SettingsInputTextBoxStyle` for Settings text inputs and
  `SettingsCheckBoxStyle` for Settings privacy/sync checkboxes.
- `Styles/Cards.xaml` defines the shared dashboard card and compact surface
  border styles. `MetricCard`, `SectionCard`, `CurrentFocusPanel`,
  `DetailsTabsPanel`, and `ControlBar` now use those shared card surfaces.
- `Styles/Badges.xaml` defines `StatusBadgeBorderStyle` and
  `StatusBadgeTextStyle`. `StatusBadge` imports this color-free badge
  dictionary so its shape and text emphasis are reusable while
  `HeaderStatusBar` remains the owner of tracking/sync/privacy brush
  resources. This avoids duplicate brush instances for the same color policy.
- `Styles/Colors.xaml` defines the core app background, surface, border,
  primary text, and muted text brushes. `Cards.xaml` now consumes surface and
  border brushes, and both `MainWindow` and the dashboard shell content use the
  app background brush.
  Header tracking/sync/privacy badge brushes are also named resources so the
  status color policy stays centralized. Remaining color work includes
  replacing any newly discovered hard-coded status colors outside the shared
  resource dictionaries.
- `Styles/Typography.xaml` defines shared heading, subtitle, section title,
  body, muted, and metric-value `TextBlock` styles. `HeaderStatusBar`,
  `SectionCard`, `DetailRow`, and `MetricCard` consume those styles while
  preserving their AutomationIds and existing bindings. `MetricCard` uses
  `MetricLabelTextStyle` so card label emphasis lives in the typography
  dictionary instead of inline markup. `EmptyState` uses `EmptyStateTextStyle`,
  and `ChartsPanel` uses `SectionTitleTextStyle` for the three chart headings.
  `SettingsPanel` uses
  `SettingsSectionTitleTextStyle` for Privacy/Sync/Runtime headings and
  shared heading bottom spacing, and `SettingsMutedTextStyle` for muted
  privacy/sync/runtime helper text.
  `SettingsWarningTextStyle` plus `WarningTextBrush` carry the local-only sync
  warning/status color. `CurrentFocusPanel` now uses shared typography for its
  section title, last DB write value, and sync status helper text through
  `SectionTitleTextStyle`, `CurrentFocusValueTextStyle`, and
  `CurrentFocusSecondaryTextStyle`. `DetailsTabsPanel` uses `MutedTextStyle`
  and `BodyTextStyle` for the pager label and current page status.
- `Styles/DataGrid.xaml` defines `SessionDataGridStyle` for readable,
  read-only session grids. `DetailsTabsPanel` uses it for App Sessions, Web
  Sessions, and Live Event Log while keeping explicit column MinWidth values in
  the view and preserving grid-level horizontal scrolling at 1024px. The shared
  style also owns the common top spacing for the three session grids, while the
  column widths remain in the view because they encode product readability
  requirements for each table.
- `Styles/Tabs.xaml` defines shared dashboard tab styles. `DetailsTabsPanel`
  uses them for `DashboardTabs` while preserving selected-value binding, four
  tab headers, and minimum-size reachability.

Open `wpfelements.md` alignment decisions:

- Domain Focus chart now uses the same Cartesian/ranking chart shape as App
  Focus. This replaced the earlier `PieChart` mismatch and keeps domain labels
  and durations readable.
- SettingsPanel now exposes safe privacy/sync/storage controls for Capture page
  title, Domain-only browser storage, full URL opt-in disabled, sync endpoint
  disabled until opt-in, and guarded Clear local data disabled.
- Dashboard root scrolling is now vertical-only; horizontal scrolling remains
  on the wide App Sessions, Web Sessions, and Live Event Log grids.
- Follow-up style migration should remove remaining repeated chart/settings text
  setters and badge/status hard-coded brushes from extracted panels.

## Current Problems

- Header text can overlap with current process text.
- Buttons are too small and cramped.
- App Sessions grid columns are clipped.
- Chart axes can show meaningless labels such as `-0.5`, `0`, `0.5`.
- Summary cards do not clearly distinguish Active Focus, Foreground, Idle, and
  Web Focus.
- SQLite persistence status is not visible enough.
- Bottom tabs and grids are hard to read.
- Layout needs to remain usable at 1920, 1366, and 1024 widths.
- Current Focus lacks last poll time and last DB write time.
- The visual surface still feels closer to a debug screen than a product
  dashboard.
- App/Web session tables can be pushed below the initially visible area without
  enough affordance that they are reachable.

## Header

Title: `Woong Monitor Stack`

Subtitle: `Windows Focus Tracker`

Do not show the current process beside the title. Current process belongs in
the Current Focus panel.

Badges:

- Tracking Running / Tracking Stopped
- Sync Off / Sync On / Sync Error
- Privacy Safe / Privacy Custom

Badge colors:

- Running: green
- Stopped: gray
- Sync Off: orange or gray
- Sync Error: red
- Privacy Safe: blue or green

Acceptance:

- `Header_DoesNotOverlapTitleAndCurrentProcess`
- `Header_ShowsTrackingSyncPrivacyBadges`
- `Header_At1024Width_RemainsReadable`

## Control Bar

Required buttons:

- Start Tracking
- Stop Tracking
- Refresh
- Sync Now
- Today
- 1h
- 6h
- 24h
- Custom

Rules:

- Height >= 40.
- Text buttons MinWidth >= 96.
- Horizontal padding >= 12.
- Margin >= 6 between buttons.
- Buttons wrap or remain reachable at narrow widths.
- Sync Now attempts upload only when sync is enabled. When sync is off, the UI
  must show a clear local-only/skipped state.

Acceptance:

- `ControlBar_ButtonsHaveReadableMinimumSize`
- `ControlBar_ButtonsWrapOrRemainReachableAt1024Width`
- `StartButton_ChangesTrackingStatusToRunning`
- `StopButton_ChangesTrackingStatusToStopped`
- `RefreshButton_ReloadsSqliteDashboard`
- `SyncNow_WhenSyncOff_DoesNotUploadAndShowsSkippedStatus`

The `Custom` button may remain disabled until the custom range picker is
implemented, but it must be visible, readable, and clearly part of the period
selector.

## Current Focus Panel

Required fields:

- Tracking status
- Current app
- Current process
- Current window title
- Current browser domain
- Current session duration
- Last persisted session
- Last poll time
- Last DB write time
- Sync state

Privacy behavior:

- Window title is hidden by default.
- Browser domain can display when available.
- Full URL is not shown unless explicit future opt-in exists.

Acceptance:

- `CurrentFocusPanel_ShowsCurrentAppProcessAndDuration`
- `CurrentFocusPanel_CurrentSessionDurationAdvancesBeyondZero`
- `CurrentFocusPanel_ShowsLastPollTime`
- `CurrentFocusPanel_ShowsLastDbWriteTime`
- `CurrentFocusPanel_ShowsLastPersistedSession`
- `CurrentFocusPanel_RespectsWindowTitlePrivacy`

## Summary Cards

Required cards:

- Active Focus: foreground time excluding idle. This is the primary product
  metric.
- Foreground: total foreground app/window time including idle.
- Idle: foreground time where the user was idle.
- Web Focus: total web session time where browser domain metadata is available.

Each card must have a clear title, large value, short subtitle, and consistent
spacing.

Acceptance:

- `SummaryCards_ShowActiveForegroundIdleWebLabels`
- `SummaryCards_AfterSqliteSessionsPersisted_ShowNonZeroValues`
- `SummaryCards_DoNotUseFakeDataWhenSqliteHasNoRows`

## Charts

Required charts:

- Hourly Active Focus: hour labels such as `09`, `10`, `11`; Y labels in
  minutes such as `0m`, `30m`, `60m`.
- Top Apps by Focus Time: readable app labels and durations.
- Top Domains by Web Focus Time: readable domain labels and durations.

Rules:

- No meaningless axes such as `-0.5 / 0 / 0.5`.
- Empty data shows an empty state, not broken axes.
- Chart mapping must remain testable without WPF UI.

Acceptance:

- `Charts_ShowMeaningfulAxisLabels`
- `HourlyActiveFocusChart_UsesHourLabelsAndMinuteAxis`
- `TopAppsChart_ShowsAppLabelsAndDurations`
- `TopDomainsChart_ShowsDomainLabelsAndDurations`
- `Charts_WhenNoData_ShowEmptyState`
- `Charts_AfterFakePipeline_ShowNonZeroData`

## Details Tabs

Required tabs:

- App Sessions
- Web Sessions
- Live Event Log
- Settings

Tabs must remain reachable at minimum window size. Vertical scrolling is
allowed and preferred over clipped content.

Details pagination:

- The details area shows 10 rows per page by default.
- The user can choose 10, 25, or 50 rows per page.
- A shared previous/next pager applies to App Sessions, Web Sessions, and Live
  Event Log.
- The visible paged collections are presentation-only projections; the full
  SQLite-backed query result remains available for totals, charts, and future
  exports.

## App Sessions Grid

Columns:

- App, MinWidth >= 160
- Process, MinWidth >= 180
- Start, MinWidth >= 90
- End, MinWidth >= 90
- Duration, MinWidth >= 100
- State, MinWidth >= 80
- Window, MinWidth >= 260
- Source, MinWidth >= 100

Rows come from persisted SQLite `FocusSession` rows. Show active/idle state and
privacy-hidden title text when window title capture is disabled.

Acceptance:

- `AppSessionsGrid_ColumnsHaveReadableWidths`
- `AppSessionsGrid_AfterStop_ShowsPersistedFocusSession`
- `AppSessionsGrid_ShowsChromeAndVSCodeFromFakePipeline`
- `AppSessionsGrid_DoesNotClipHeadersAt1024Width`

## Web Sessions Grid

Columns:

- Domain, MinWidth >= 180
- Title, MinWidth >= 260
- URL Mode, MinWidth >= 120
- Start, MinWidth >= 90
- End, MinWidth >= 90
- Duration, MinWidth >= 100
- Browser, MinWidth >= 120
- Confidence, MinWidth >= 100

Rows come from persisted SQLite `WebSession` rows. Domain-only mode must not
show full URLs.

Acceptance:

- `WebSessionsGrid_ColumnsHaveReadableWidths`
- `WebSessionsGrid_ShowsGithubAndChatgptFromFakePipeline`
- `WebSessionsGrid_DomainOnlyMode_DoesNotShowFullUrl`
- `WebSessionsGrid_WhenNoWebData_ShowsReadableEmptyState`

## Live Event Log

Columns:

- Time
- Event Type
- App
- Domain
- Message

Expected events:

- Tracking started
- Foreground captured
- FocusSession started
- FocusSession closed
- FocusSession persisted
- WebSession started
- WebSession closed
- WebSession persisted
- Outbox row created
- Sync skipped because sync is off
- Tracking stopped

Acceptance:

- `LiveEventLog_AfterTrackingStart_ShowsTrackingStarted`
- `LiveEventLog_AfterAppSwitch_ShowsFocusSessionClosedAndStarted`
- `LiveEventLog_AfterBrowserDomainChange_ShowsWebSessionClosedAndStarted`
- `LiveEventLog_AfterStop_ShowsFlushEvents`

## Settings

Required settings:

- Capture window title: off by default.
- Capture page title: off by default or privacy-aware.
- Full URL capture: off by default; explicit opt-in only.
- Domain-only browser storage: on by default.
- Sync enabled: off by default.
- Sync status: Local only.
- Device/server status placeholder.
- Poll interval.
- Idle threshold.
- Open local DB folder.
- Open logs folder.
- Clear local data, guarded by confirmation.

Acceptance:

- `Settings_PrivacyControlsAreReadable`
- `Settings_DefaultPrivacyStateIsSafe`
- `Settings_FullUrlCaptureIsOffByDefault`
- `Settings_SyncIsOffByDefault`
- `Settings_OpenLocalDbFolderCommandExistsOrIsClearlyDisabled`

## Layout Rules

Preferred WPF primitives:

- `Grid`
- `DockPanel`
- `ScrollViewer`
- `WrapPanel`
- `UniformGrid` where useful
- `DataGrid` with `MinWidth` columns

Avoid:

- Hard-coded fixed width/height for the main layout.
- Tiny default buttons.
- Full-window `ViewBox`.
- Controls that push tabs off-screen.
- `DataGrid` columns without `MinWidth`.

Responsive targets:

- 1920 width: spacious dashboard.
- 1366 width: readable without clipping.
- 1024 width: buttons and tabs remain reachable.
- Minimum readable window: 1024 x 768.

## MVVM And Runtime Rules

Keep MVVM. Do not move data aggregation logic into code-behind.

All dashboard display state must be testable from ViewModel tests.

Tracking must update the UI while Running through a testable ticker abstraction:

```csharp
public interface ITrackingTicker
{
    event EventHandler? Tick;
    bool IsRunning { get; }
    void Start();
    void Stop();
}
```

Current implementation:

- `ITrackingTicker` is implemented in `Windows.App` as the runtime boundary.
- `DispatcherTrackingTicker` wraps WPF `DispatcherTimer` for production UI
  ticks.
- `MainWindow` receives an `ITrackingTicker`, starts it only when the visible
  window is loaded, stops it when the window closes, and unsubscribes its tick
  handler on close.
- Tracking tests use a manual fake ticker so foreground/window/browser-domain
  persistence behavior is deterministic and does not wait on wall-clock timer
  delays.
- Safety tests prove that showing the window starts only the ticker, not
  tracking collection: a manual tick before Start leaves tracking stopped and
  writes no focus, web, or outbox rows. Auto-start is also proven to occur only
  after `Loaded`, not during construction or DI resolution.

## SQLite Rules

- Refresh reads `FocusSession` rows from SQLite by selected range.
- Refresh reads linked `WebSession` rows for the selected focus sessions.
- Active Focus = non-idle focus duration.
- Foreground = all focus duration.
- Idle = idle focus duration.
- Web Focus = web-session duration.
- App chart groups focus sessions by app/process.
- Domain chart groups web sessions by domain.
- Hourly chart splits focus sessions into hour buckets.

## Acceptance Script

`scripts/run-wpf-ui-acceptance.ps1` must:

1. Launch WPF with a temp SQLite DB.
2. Use fake TrackingPipeline mode.
3. Start tracking.
4. Simulate VS Code foreground, Chrome foreground, `github.com`, and
   `chatgpt.com`.
5. Stop tracking.
6. Run Sync Now while sync is off or prove opt-in behavior clearly.
7. Verify SQLite focus/web/outbox rows.
8. Verify UI contents.
9. Capture screenshots at 1920, 1366, and 1024 widths, plus focused regions.
10. Generate `report.md`, `manifest.json`, and `visual-review-prompt.md`.

The script must never use the user's production/local database and must never
upload to a real server unless a future explicit opt-in flag is added and
documented.

Current implementation:

- `scripts/run-wpf-ui-acceptance.ps1` passes `--viewport-widths "1920,1366,1024"`
  to the local UI snapshot tool.
- The snapshot tool captures dashboard screenshots plus summary, chart,
  app-session, web-session, and live-event section screenshots for each
  requested viewport width.
- `manifest.json` records `viewportWidths` and `skippedScreenshotReasons` so
  reviewers can distinguish missing crops from successful captures.

## Test Inventory

Add or maintain tests in these groups before each implementation slice:

- ViewModel/UI state: badges, commands, current focus values, last poll/DB
  write, summary cards, SQLite refresh, and period selection.
- Layout semantics: header separation, readable button sizes, reachable
  controls at 1024 width, required labels, readable DataGrid columns, and
  settings controls.
- Chart mapping: hour labels, minute axes, app/domain labels, durations, and
  empty states.
- Runtime-to-UI: Start starts tracking, ticker advances duration, foreground
  changes persist sessions, browser domain changes persist web sessions, Stop
  flushes, and Sync Now stays local-only when sync is off.
- FlaUI/semantic acceptance: 1920/1366/1024 readability plus fake
  VS Code/Chrome/github.com/chatgpt.com pipeline evidence.

## Implementation Order

1. Keep this plan and `total_todolist.md` updated before UI code changes.
2. Add one failing behavior/layout test at a time.
3. Fix Header and Control Bar first.
4. Add Current Focus fields for domain, last poll, last DB write, and last
   persisted session.
5. Replace summary cards with Active Focus, Foreground, Idle, and Web Focus.
6. Fix chart mapper labels and empty states.
7. Fix App Sessions and Web Sessions grids with readable columns and scroll.
8. Improve Live Event Log and Settings readability.
9. Update acceptance script screenshots and semantic checks.
10. Run full tests/build/acceptance, update docs/TODO/resume, commit, and push.

## Prohibitions

Do not:

- Replace WPF with WebView for MVP.
- Move business logic into code-behind.
- Hard-code fake chart data in the production dashboard.
- Use the user's real local DB in tests.
- Capture key input, typed text, page contents, passwords, forms, messages, or
  clipboard contents.
- Enable full URL capture by default.
- Upload to a real server during UI acceptance.
- Weaken tests to pass.
- Mark UI work complete if grids are still clipped or charts still show
  meaningless axes.

## Definition Of Done

This WPF UI slice is complete only when:

- Header no longer overlaps title/current process.
- Buttons are readable and reachable.
- Current Focus shows current app/process/duration/domain, last poll, last DB
  write, and last persisted session.
- Summary cards show Active Focus, Foreground, Idle, and Web Focus.
- Charts have meaningful labels.
- App/Web grids and Live Event Log are readable.
- Settings privacy controls are readable.
- UI remains usable at 1920, 1366, and 1024 widths.
- Dashboard values come from SQLite-backed aggregation.
- Fake TrackingPipeline acceptance shows VS Code, Chrome, `github.com`, and
  `chatgpt.com`.
- Sync remains opt-in and local-only by default.
- Privacy defaults remain safe.
- Tests pass, build succeeds, acceptance artifacts are generated, docs/TODO are
  updated, commit is created, and push is attempted.
