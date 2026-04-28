# Resume State

Updated: 2026-04-28

## Last Completed Slice

Milestone 10 Android duplicate-safe upload handling.

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
- Added server test EF InMemory override for WebApplicationFactory.
- Moved device registration from singleton memory storage to scoped
  `MonitorDbContext` persistence.
- Preserved duplicate-safe registration behavior while proving a `DeviceEntity`
  row is persisted.
- Verified 52 tests pass across all current projects.
- Added `FocusSessionEntity` and unique `(DeviceId, ClientSessionId)` index.
- Added `POST /api/focus-sessions/upload`.
- Added EF-backed upload service returning `Accepted` and `Duplicate` item
  statuses.
- Verified duplicate retry does not insert a second focus session row.
- Verified 53 tests pass across all current projects.
- Added `WebSessionEntity` and unique duplicate-detection index for
  `(DeviceId, FocusSessionId, StartedAtUtc, EndedAtUtc, Url)`.
- Added `POST /api/web-sessions/upload`.
- Added EF-backed web session upload service returning `Accepted` and
  `Duplicate` item statuses.
- Verified duplicate retry does not insert a second web session row.
- Added `RawEventEntity` and unique `(DeviceId, ClientEventId)` index.
- Added `POST /api/raw-events/upload`.
- Added EF-backed raw event upload service returning `Accepted` and
  `Duplicate` item statuses.
- Verified duplicate retry does not insert a second raw event row.
- Added `GET /api/daily-summaries/{summaryDate}` with `userId` and
  `timezoneId` query parameters.
- Added EF-backed daily summary query service that combines all devices for a
  user, excludes idle focus sessions from active totals, reports idle totals
  separately, and groups web totals by requested timezone.
- Verified the summary API combines Windows + Android sessions for the same
  user and ignores another user's device data.
- Added `GET /api/statistics/range` with `userId`, inclusive `from`/`to`, and
  `timezoneId` query parameters.
- Added range aggregation response for total active, idle, web, top apps, and
  top domains.
- Verified the date-range API combines user devices across multiple local dates
  and excludes out-of-range plus other-user data.
- Added Windows sync API client abstraction.
- Added sync outbox repository interface and connected the SQLite outbox
  repository to it.
- Added `WindowsSyncWorker` for processing pending/failed outbox rows.
- Verified a fake API `Accepted` upload marks the outbox item synced.
- Added retry handling for server item-level `Error` upload results.
- Verified a fake API error marks the outbox item failed, increments retry
  count, and stores the server error message.
- Verified server `Duplicate` upload results are treated as synced so retrying
  an already accepted payload does not keep failing locally.
- Added HTTP Windows sync API client for focus, web, and raw outbox aggregate
  types.
- Added `WindowsSyncClientOptions` with a required device token placeholder.
- Verified the HTTP client posts the raw outbox payload to the matching server
  endpoint with the `X-Device-Token` header.
- Added sync checkpoint store abstraction.
- Verified the worker saves a checkpoint timestamp when at least one outbox
  item syncs successfully.
- Verified a real SQLite outbox row is uploaded through the HTTP sync client
  and then marked synced locally.
- Added Android project under `android/` using the local WoongViewActivity
  Gradle Wrapper/AGP 9.1 style because `WoongAndroidBasicProject` was not
  present in the workspace.
- Configured Kotlin source, XML/View UI, ViewBinding, AppCompat,
  ConstraintLayout, RecyclerView, Room runtime/ktx, WorkManager, Retrofit,
  Moshi, OkHttp, JUnit, Espresso, and AndroidX test dependencies.
- Added `MainActivity`, `activity_main.xml`, an empty JUnit smoke test, and an
  empty instrumentation smoke test.
- Verified `.\gradlew.bat testDebugUnitTest --no-daemon --stacktrace`.
- Verified `.\gradlew.bat assembleDebug --no-daemon --stacktrace`.
- Added `UsageAccessPermissionChecker` with a fake-reader JVM unit test.
- Added Android `AppOpsManager`-based usage access permission reader.
- Added Usage Access Settings intent factory and connected a ViewBinding button
  in `MainActivity` to launch Android settings.
- Verified the settings action factory with a JVM unit test.
- Added pure Kotlin usage event snapshot/session models.
- Added `UsageSessionizer` with resumed-to-paused session creation.
- Verified resumed/paused events create one app session with expected duration.
- Added configurable same-app merge gap to `UsageSessionizer`.
- Verified close same-app pause/resume pairs merge into one session.
- Verified a different app resume closes the previous active app session.
- Added KSP/Room compiler plus Robolectric/Room testing dependencies.
- Added Room `FocusSessionEntity`, `FocusSessionDao`, and `MonitorDatabase`.
- Verified Room in-memory DAO insert/query by local date range with a
  Robolectric component test.
- Added `UsageEventsReader`, Android `UsageStatsManager` event reader, and
  `UsageStatsCollector`.
- Verified the collector reads the requested UTC range and returns events sorted
  by timestamp.
- Added `CollectUsageWorker` with injectable collection runner for WorkManager
  testing.
- Added `AndroidUsageCollectionRunner` to collect UsageStats events, sessionize
  them, and store deterministic `focus_sessions` rows in Room.
- Added Room bulk insert support and a singleton `MonitorDatabase` opener for
  worker execution.
- Declared `PACKAGE_USAGE_STATS` in the Android manifest; permission remains
  explicit through the existing Usage Access settings flow.
- Verified worker behavior with a Robolectric WorkManager test.
- Verified `.\gradlew.bat testDebugUnitTest --no-daemon --stacktrace`.
- Verified `assembleDebug`; AGP reports the expected experimental
  `android.disallowKotlinSourceSets=false` warning required for KSP with
  built-in Kotlin.
- Added pure Kotlin dashboard state models: `DashboardPeriod`,
  `DashboardSnapshot`, `DashboardSessionRow`, and `DashboardUiState`.
- Added `DashboardRepository` and a dependency-light `DashboardViewModel`.
- Verified selecting a dashboard period loads a repository snapshot and exposes
  total active time, top app, idle time, and recent sessions through public
  ViewModel state.
- Added `RoomDashboardRepository` for Today/Yesterday/Recent 7 Days dashboard
  snapshots from local Android Room data.
- Verified Today aggregation excludes idle sessions from active total, tracks
  idle separately, picks the top active package, and formats recent session
  rows using the requested timezone.
- Added XML/ViewBinding `DashboardActivity`.
- Added dashboard XML surface with Today/Yesterday/Recent 7 Days filters,
  active/top-app/idle cards, Usage Access settings action, and recent sessions
  `RecyclerView`.
- Added Espresso smoke test for DashboardActivity display and verified the
  androidTest APK compiles.
- Bound DashboardActivity period buttons to the Room-backed dashboard
  repository through `DashboardViewModel`.
- Added visible value fields for active time, top app, and idle time.
- Added empty recent-session state text and covered it from the Espresso smoke
  surface.
- Added explicit Usage Access guidance text and covered it from the Espresso
  smoke surface.
- Added XML/ViewBinding `SessionsActivity` with a sessions `RecyclerView` and
  empty-state text.
- Added Espresso smoke test for SessionsActivity and verified the androidTest
  APK compiles.
- Checked installed `find-skills` guidance and ran
  `npx skills find android chart mpandroidchart`; no Android-specific
  MPAndroidChart skill was selected.
- Verified MPAndroidChart `v3.1.0` from the official GitHub release page and
  added JitPack repository/dependency configuration.
- Added `DashboardChartMapper` and chart data models that convert durations to
  minute-based MPAndroidChart `Entry`, `BarEntry`, and `PieEntry` values.
- Added MPAndroidChart `LineChart` and `BarChart` views to DashboardActivity.
- Configured chart no-data states and covered chart view presence from the
  Espresso smoke surface.
- Added selected-period label and Espresso click assertions for Today,
  Yesterday, and Recent 7 Days filters.
- Added XML/ViewBinding `SettingsActivity` with visible collection and opt-in
  sync defaults.
- Added Espresso smoke test for SettingsActivity privacy/sync defaults and
  verified the androidTest APK compiles.
- Extended dashboard snapshots and ViewModel state with chart data.
- Added Room-backed active-only chart aggregation for hourly activity and app
  usage.
- Bound non-empty chart data into DashboardActivity MPAndroidChart line/bar
  views.
- Added Android Room `sync_outbox` table, DAO, status enum, and v1-to-v2
  database migration.
- Verified pending outbox insert/query and synced transition with a Robolectric
  Room test.
- Added OkHttp/Moshi Android sync client for focus session upload.
- Added Moshi Kotlin dependency and numeric upload status adapter.
- Verified the Android sync client posts the contract path/payload and parses
  numeric upload status responses.
- Added `AndroidOutboxSyncProcessor` for focus-session outbox upload batches.
- Added `AndroidSyncApi` and `SyncOutboxStore` interfaces so the processor can
  be unit-tested without WorkManager or HTTP.
- Verified `Accepted` and `Duplicate` server item results mark outbox items as
  synced, while `Error` marks the item failed and retryable with the server
  error message.
- Added Room DAO `markFailed` behavior that increments retry count, stores the
  last error, and keeps the row in the pending query.
- Verified `.\gradlew.bat testDebugUnitTest --no-daemon --stacktrace`.
- Verified `.\gradlew.bat assembleDebug --no-daemon --stacktrace`.
- Verified `.\gradlew.bat assembleDebugAndroidTest --no-daemon --stacktrace`.
- Added `docs/contracts.md` for time/date, device, upload idempotency, and web
  domain policy.
- Verified `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`.
- Pushed latest completed slice to `origin/main`.

## Next Highest Priority

Continue Milestone 10 Android Sync + Morning Summary:

1. Add sync worker tests before implementation.
2. Implement WorkManager sync worker.
3. Integrate daily summary API / previous-day summary screen.
