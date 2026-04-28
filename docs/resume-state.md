# Resume State

Updated: 2026-04-28

## Last Completed Slice

Milestone 4 Windows dashboard chart data mapper foundation.

## Completed

- Added root `AGENTS.md` with mandatory installed skill usage and always-on
  todo/test/build/commit/push workflow.
- Added `total_todolist.md` as the full PRD checklist.
- Added common domain models: `Device`, `Platform`, `AppFamily`,
  `PlatformApp`, `FocusSession`, `WebSession`, `DeviceStateSession`,
  `DailySummary`.
- Added contract DTOs for device registration and focus/web/raw upload batches.
- Added `DomainNormalizer` and `DailySummaryCalculator`.
- Added app-family grouping support so platform app keys can roll up to a
  shared family label in daily top apps.
- Added explicit null guards for upload batch request item lists.
- Added Windows tracking project and tests.
- Added `FocusSessionizer` app-change, same-window, and idle-state behavior.
- Added pure `IdleDetector`.
- Added foreground window collector abstraction with fake-testable reader/clock.
- Added Windows `user32.dll` foreground window and last-input readers behind
  interfaces.
- Added `TrackingPoller` orchestration and Windows smoke console.
- Smoke run captured foreground window `Codex.exe` / title `Codex` on
  2026-04-28.
- Added `Microsoft.Data.Sqlite` local persistence.
- Added focus session repository with duplicate-safe insert and range query.
- Added web session repository linked by `focusSessionId`.
- Added sync outbox repository with pending, synced, failed, and retry count.
- Added WPF app project targeting `net10.0-windows`.
- Added WPF presentation project with CommunityToolkit.Mvvm.
- Added dashboard presentation test project.
- Added `DashboardViewModel` with today, 1h, 6h, 24h, and custom period
  selection.
- Added dashboard summary refresh behavior that reports active, idle, web, top
  app, and top domain totals through public ViewModel properties.
- Added `DashboardSummaryCard` models and tested the public summary-card
  collection.
- Added `SelectDashboardPeriodCommand` for XAML period filter buttons.
- Bound the WPF shell to `DashboardViewModel` with period buttons and summary
  cards.
- Added an empty dashboard data source placeholder for safe WPF startup before
  the local SQLite dashboard adapter is introduced.
- Added dependency-free dashboard chart points and mapper logic for hourly
  activity, app usage, and domain usage.
- Added ViewModel chart point publication for the dashboard.
- Checked `LiveChartsCore.SkiaSharpView.WPF` 2.0.0 as the current NuGet stable
  package and kept it out of the commit until the WPF adapter can be added
  without introducing unused package warnings.
- Added `docs/contracts.md` for time/date, device, upload idempotency, and web
  domain policy.
- Verified `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`.
- Pushed latest completed slice to `origin/main`.

## Next Highest Priority

Continue Milestone 4 Windows WPF Dashboard MVP:

1. Add LiveCharts2 WPF adapter/control binding, resolving package warnings
   deliberately.
2. Add app sessions table and bind it to recent focus sessions.
3. Add live event log, web sessions view, and settings view.
