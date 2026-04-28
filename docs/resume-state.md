# Resume State

Updated: 2026-04-28

## Last Completed Slice

Milestone 4.5 extension payload domain extraction.

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
- Added `AndroidSyncRunner` and `AndroidRoomSyncRunner` to connect WorkManager
  execution to the Room outbox plus OkHttp sync client.
- Added `AndroidSyncWorker` with device/base URL input keys, pending limit,
  success output counts, and retry behavior when any item fails or sync
  configuration is missing.
- Verified sync worker success output and failed-item retry behavior with a
  Robolectric WorkManager test.
- Added `DailySummaryClient` for
  `GET /api/daily-summaries/{summaryDate}?userId=...&timezoneId=...`.
- Added Android summary response models for total active, idle, web, top apps,
  and top domains.
- Verified summary client path/query construction and Moshi response parsing
  against the server's daily summary contract.
- Added `DailySummaryViewModel` and repository abstraction for loading the
  previous local day and formatting active, idle, web, top app, and top domain
  summary state.
- Added XML/ViewBinding `DailySummaryActivity` registered in the manifest.
- Added summary Activity smoke coverage that verifies previous-day date and
  summary values render from intent-provided state.
- Added `MorningSummaryNotificationWorker` and notification runner seam for
  WorkManager-triggered morning summary notifications.
- Declared `POST_NOTIFICATIONS`; runtime permission UX remains a hardening
  concern before release.
- Verified the notification worker delegates title/text to the runner and
  returns success.
- Added server `DailySummaryEntity` and `daily_summaries` EF model mapping with
  a unique `(UserId, SummaryDate, TimezoneId)` index.
- Added `DailySummaryAggregationService` that generates and upserts persisted
  daily summaries from integrated focus/web session data.
- Verified persisted daily summary totals combine Windows + Android devices,
  exclude idle time from active totals, compute top apps/domains, and ignore
  another user's data.
- Added initial server `AppFamilyMapper` so known Windows/Android platform app
  keys such as `chrome.exe` and `com.android.chrome` roll up into `Chrome`.
- Updated daily summary and date-range top app aggregation to group by app
  family labels.
- Verified persisted and API summary top apps combine Windows + Android Chrome
  durations under one `Chrome` row.
- Added persisted summary coverage for a web session that crosses a UTC date
  boundary but belongs to the requested `Asia/Seoul` local date.
- Verified server daily aggregation includes web duration and top domain by
  requested user timezone, not raw UTC date.
- Added persisted summary rerun coverage proving the aggregation service
  updates a single `(UserId, SummaryDate, TimezoneId)` row instead of creating
  duplicates or inflating totals.
- Added Windows `HttpWindowsSummaryApiClient` for
  `GET /api/daily-summaries/{summaryDate}`.
- Verified Windows summary client path/query construction and deserialization
  of integrated summary totals/top apps/top domains.
- Android summary client verification was completed earlier in Milestone 10,
  so both Windows and Android clients now have summary query coverage.
- Added `docs/hardening.md` with DB migration review notes, server/local DB
  separation reminders, required idempotency indexes, raw event retention
  policy, and Android notification permission follow-up.
- Added Windows Settings sync failure status in the dashboard presentation
  model and WPF Settings tab.
- Added Android Settings permission guidance, explicit privacy boundary text,
  Usage Access action, local-only sync status, and retryable sync failure status.
- Verified Android Settings UI hardening with Robolectric and Espresso compile
  coverage.
- Added `docs/performance-checks.md` with Windows collector smoke CPU/memory
  measurements and Android emulator CPU/memory/batterystats smoke results.
- Added root `README.md` and `docs/release-checklist.md` for the final
  release-candidate pass.
- Verified `.\gradlew.bat testDebugUnitTest --no-daemon --stacktrace`.
- Verified `.\gradlew.bat assembleDebug --no-daemon --stacktrace`.
- Verified `.\gradlew.bat assembleDebugAndroidTest --no-daemon --stacktrace`.
- Verified `.\gradlew.bat testDebugUnitTest --no-daemon --stacktrace`.
- Verified `.\gradlew.bat assembleDebug --no-daemon --stacktrace`.
- Verified `.\gradlew.bat assembleDebugAndroidTest --no-daemon --stacktrace`.
- Verified `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`.
- Added `docs/contracts.md` for time/date, device, upload idempotency, and web
  domain policy.
- Verified `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`.
- Pushed latest completed slice to `origin/main`.
- Completed the full release-candidate validation matrix:
  `dotnet test`, `dotnet build`, Windows foreground smoke,
  `testDebugUnitTest`, `assembleDebug`, `assembleDebugAndroidTest`, and
  `connectedDebugAndroidTest`.
- Verified Android Usage Access settings navigation manually on
  `emulator-5554`; tapping `usageAccessSettingsButton` moved focus to
  `com.android.settings/com.android.settings.spa.SpaActivity`.
- Marked `docs/release-checklist.md` and `total_todolist.md` complete for the
  PRD/MVP release-candidate pass.
- Reopened project completion after user clarified that Chrome Extension +
  Native Messaging must be a dedicated milestone rather than an informal
  post-RC follow-up.
- Added Milestone 4.5 to `docs/prd.md` and `total_todolist.md`.
- Added `AGENTS.md` rules for Chrome extension privacy boundaries and
  PostgreSQL/Testcontainers-style relational server integration testing.
- Added Windows-side `ChromeTabChangedMessage` as the first native-message DTO
  seam.
- Verified extension URL payload domain extraction with a TDD test.
- Added `docs/chrome-native-messaging.md` with privacy boundaries and current
  Milestone 4.5 status.
- Added `extensions/chrome/` with an MV3 manifest and `background.js`.
- Verified the manifest declares the required service worker and permissions.
- Added Windows native messaging host registration abstraction and a
  Windows-only registry writer.
- Verified registration writes the expected Chrome HKCU native host key using a
  fake registry writer.
- Added `ChromeNativeMessageParser` for the extension `activeTabChanged` JSON
  payload.
- Verified parsed messages preserve browser family, tab/window ids, domain, and
  UTC timestamp.
- Added `ChromeNativeMessageReceiver` for Chrome's length-prefixed native
  messaging stream protocol.
- Verified the receiver reads a length-prefixed active tab message and returns a
  normalized tab DTO.
- Added `SqliteBrowserRawEventRepository` with a `browser_raw_event` local
  SQLite table.
- Verified browser raw event save/query round-trips Chrome active tab metadata.
- Added `BrowserWebSessionizer` to convert active tab messages into
  `WebSession` intervals.
- Verified active tab changes create a web session for the previous URL.
- Verified duplicate tab events do not inflate duration.
- Added `ChromeNativeMessageIngestionFlow` for receiver -> raw event repository
  -> web-session repository.
- Verified a Chrome active tab URL appears in the Windows local SQLite raw event
  table and that the previous tab is persisted as a `web_session`.

## Next Highest Priority

Continue Milestone 4.5:

1. Continue server hardening by replacing EF InMemory-only relational assurance
   with a documented relational/PostgreSQL test DB strategy.
2. Add reset strategy coverage for server integration tests.
