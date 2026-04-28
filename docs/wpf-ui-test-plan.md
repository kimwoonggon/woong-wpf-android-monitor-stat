# WPF UI Test Plan

Updated: 2026-04-28

This plan covers the current WPF MVP shell in `Woong.MonitorStack.Windows.App`.
The UI tests are local Windows tests because WPF requires an STA desktop
environment.

## Scope

- Main dashboard window construction and data context.
- Refresh and period selector buttons.
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

## Expected UI Behaviors

- The main window is titled `Woong Monitor Stack` and is backed by
  `DashboardViewModel`.
- The refresh button invokes `RefreshDashboardCommand`.
- The Today, 1h, 6h, and 24h buttons invoke `SelectDashboardPeriodCommand` with
  the correct `DashboardPeriod` command parameter.
- Refreshing with sample dashboard data renders the expected summary card labels
  and values.
- The chart area exposes Activity, Apps, and Domains sections with two
  Cartesian charts and one pie chart.
- The App Sessions tab exposes `App`, `Started`, `Duration`, and `Idle`
  columns bound to `RecentSessions`.
- The Web Sessions tab exposes `Domain`, `Page`, `Started`, and `Duration`
  columns bound to `RecentWebSessions`.
- The Live Event Log tab exposes `Time`, `Kind`, and `Message` columns bound to
  `LiveEvents`.
- The Settings tab exposes visible collection status, sync enabled state, sync
  mode label, and sync status label.

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

## Current Verification

2026-04-28:

- Added `MainWindowUiExpectationTests` for XAML-level dashboard controls,
  command bindings, period button behavior, chart surface, tab grids, and
  settings controls.
- Added chart and settings AutomationIds used by UI tests and snapshot tooling.
- Verified `Woong.MonitorStack.Windows.App.Tests` passes.
- Verified solution restore, build, and test pass.
- Verified `scripts/run-ui-snapshots.ps1` launches the app and writes
  `artifacts/ui-snapshots/latest/report.md`.
