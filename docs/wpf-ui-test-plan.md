# WPF UI Test Plan

Updated: 2026-04-29

This plan covers the current WPF MVP shell in `Woong.MonitorStack.Windows.App`.
The UI tests are local Windows tests because WPF requires an STA desktop
environment.

## Scope

- Main dashboard window construction and data context.
- Refresh and period selector buttons.
- Start, Stop, and Sync Now tracking controls.
- Current activity status, app/process metadata, privacy-aware window title,
  session duration, last persisted session, and last sync status.
- Summary cards for active, idle, and web usage.
- Chart surface for activity, app usage, and domain usage.
- App sessions, web sessions, and live event log tabs.
- Settings tab privacy/sync controls.
- Stable automation IDs used by local snapshot automation.

## Test Levels

- `Windows.Presentation.Tests` verifies ViewModel behavior and formatting without
  WPF controls.
- `Windows.App.Tests` verifies the actual XAML surface, command bindings, tab
  content, DataGrid columns, and automation IDs.
- `tools/Woong.MonitorStack.Windows.UiSnapshots` is a local visual review tool
  that launches the real app and captures screenshots.
- Milestone 25 semantic FlaUI acceptance is the UI pass/fail gate. It must
  launch the app, click controls, wait for observable changes, and verify the
  expected dashboard state or SQLite/sync side effect.

## Pass/Fail Principle

UI tests must judge whether the feature actually works, not only whether the
screen was constructed. XAML-level checks, AutomationIds, and screenshots are
supporting checks. A behavior is accepted only when the launched app responds
correctly to user-like interaction.

## Expected UI Behaviors

- The main window is titled `Woong Monitor Stack` and is backed by
  `DashboardViewModel`.
- The refresh button invokes `RefreshDashboardCommand`.
- The Start button invokes `StartTrackingCommand` and transitions visible
  status to `Running` through a fake-testable tracking coordinator.
- The Stop button invokes `StopTrackingCommand` and transitions visible status
  back to `Stopped`.
- The Sync Now button invokes `SyncNowCommand` and receives the current sync
  opt-in state.
- Current window and web page titles are hidden by default; previously hidden
  raw titles are not retained for later reveal.
- The Today, 1h, 6h, and 24h buttons invoke `SelectDashboardPeriodCommand` with
  the correct `DashboardPeriod` command parameter.
- Refreshing with sample dashboard data renders the expected summary card labels
  and values.
- The chart area exposes Activity, Apps, and Domains sections with three
  Cartesian charts: one hourly column chart and two horizontal bar charts.
- The App Sessions tab exposes `App`, `Started`, `Duration`, and `Idle`
  columns bound to `RecentSessions`.
- The Web Sessions tab exposes `Domain`, `Page`, `Started`, and `Duration`
  columns bound to `RecentWebSessions`.
- The Live Event Log tab exposes `Time`, `Kind`, and `Message` columns bound to
  `LiveEvents`.
- The Settings tab exposes visible collection status, window title visibility,
  sync enabled state, sync mode label, and sync status label.
- Acceptance tooling invokes the Settings `Exit app` explicit shutdown path for
  cleanup when available. X close is close-to-tray behavior and must not be
  treated as app exit; forced leftover process kills must be called out in the
  report/manifest cleanup evidence.

## Commands

Run WPF app tests only:

```powershell
dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -v minimal
```

Run all .NET tests:

```powershell
dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1 -v minimal
```

Run local UI snapshots:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-ui-snapshots.ps1
```

## Not Covered Yet

- Pixel-perfect visual regression gates.
- CI execution for WPF UI automation.
- Multi-DPI, high contrast, and Windows theme matrix.
- Installer-based smoke testing.
- Full semantic FlaUI TrackingPipeline mode that proves fake app/browser events
  appear in the dashboard after Start/Stop/Sync interactions.

## Current Verification

2026-04-29:

- Added `MainWindowTrackingPipelineTests.StartStopButtons_PersistForegroundSessionsAndDashboardRendersFromSqlite`.
  This test creates the real WPF window with a real SQLite-backed dashboard
  data source, a real `WindowsTrackingDashboardCoordinator`, fake foreground
  and clock readers, and temp SQLite repositories. It invokes the actual
  Start/Stop buttons through WPF UI Automation peers, then verifies
  `focus_session` persistence, `sync_outbox` enqueueing, visible tracking
  status, and dashboard rows/cards rendered back from SQLite.
- Added semantic tests for the WPF runtime polling path:
  `CurrentSessionDuration_WhenPollTicks_AdvancesBeyondZero`,
  `PollTrackingCommand_WhenForegroundChanged_RefreshesDashboardAfterClosedSessionPersists`,
  `MainWindow_WithFakeBrowserPipeline_ShowsGithubAndChatgptInWebSessions`, and
  `MainWindow_AtMinimumSize_KeepsTabsReachableOrProvidesScrolling`.
- Added a WPF `DispatcherTimer` adapter in `MainWindow` that invokes
  `PollTrackingCommand` while tracking is running. This is intentionally a UI
  lifecycle adapter; tracking logic remains in Presentation/Windows services.
- Disabled parallel execution for `Woong.MonitorStack.Windows.App.Tests` so
  WPF XAML loading and STA-window tests run deterministically in one assembly.
- Added Milestone 21 tests for WPF Start/Stop/Sync controls, current-activity
  AutomationIds, fake coordinator transitions, title privacy masking, structured
  persisted-session display, and a smaller default window width for local
  snapshot automation.
- Verified `Woong.MonitorStack.Windows.Presentation.Tests` passes with 33
  tests.
- Verified `Woong.MonitorStack.Windows.App.Tests` passes with 8 tests.
- Verified solution restore, build, and all 120 .NET tests pass.
- Verified `scripts/test-coverage.ps1` and `scripts/run-ui-snapshots.ps1`.

2026-04-28:

- Added `MainWindowUiExpectationTests` for XAML-level dashboard controls,
  command bindings, period button behavior, chart surface, tab grids, and
  settings controls.
- Added chart and settings AutomationIds used by UI tests and snapshot tooling.
- Verified `Woong.MonitorStack.Windows.App.Tests` passes.
- Verified solution restore, build, and test pass.
- Verified `scripts/run-ui-snapshots.ps1` launches the app and writes
  `artifacts/ui-snapshots/latest/report.md`.
