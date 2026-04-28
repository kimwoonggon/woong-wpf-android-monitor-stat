# Resume State

Updated: 2026-04-28

## Last Completed Slice

Milestone 5 Server EF PostgreSQL device model foundation.

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
- Added LiveCharts2 series mapping through `LiveChartsCore.SkiaSharpView`
  2.0.0.
- Added WPF LiveCharts2 controls through `LiveChartsCore.SkiaSharpView.WPF`
  2.0.0.
- Suppressed the known transitive `NU1701` warnings only in the WPF app project
  with an inline csproj explanation; final build reports 0 warnings.
- Added recent app session row models with timezone-adjusted start time,
  formatted duration, and idle flag.
- Bound the WPF shell to a read-only app sessions `DataGrid`.
- Added recent web session row models with domain, page title, local start
  time, and duration.
- Added live event row models derived from focus and web sessions until raw
  events are wired into the dashboard.
- Replaced the lower dashboard panel with tabs for App Sessions, Web Sessions,
  and Live Event Log.
- Added settings ViewModel defaults: collection visible and sync opt-out.
- Exposed settings through the dashboard ViewModel.
- Added WPF Settings tab bound to the privacy/sync settings state.
- Added WPF app smoke test project targeting `net10.0-windows`.
- Added STA-thread smoke test that constructs `MainWindow` with a
  `DashboardViewModel`.
- Verified 49 tests pass across domain, Windows, presentation, and WPF app
  tests.
- Added ASP.NET Core Web API project and server xUnit integration test project.
- Added `Microsoft.AspNetCore.Mvc.Testing` 10.0.1 for server API tests.
- Replaced the template weather endpoint with `POST /api/devices/register`.
- Added in-memory duplicate-safe device registration service as the first
  vertical API slice.
- Verified 50 tests pass across all current projects.
- Added `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.1 to the server.
- Added `MonitorDbContext` and `DeviceEntity`.
- Configured the server with a local `MonitorDb` PostgreSQL connection string.
- Added model metadata test for the unique `(UserId, Platform, DeviceKey)`
  device index.
- Verified 51 tests pass across all current projects.
- Added `docs/contracts.md` for time/date, device, upload idempotency, and web
  domain policy.
- Verified `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`.
- Pushed latest completed slice to `origin/main`.

## Next Highest Priority

Continue Milestone 5 Server Integrated DB + API MVP:

1. Move device registration storage from in-memory registry to EF repository
   while preserving the API idempotency test.
2. Add FocusSession integrated DB entity and upload API.
3. Add duplicate `deviceId + clientSessionId` tests.
