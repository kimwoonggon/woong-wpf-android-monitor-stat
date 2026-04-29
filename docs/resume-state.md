# Resume State

Updated: 2026-04-29

## Last Completed Slice

Milestone 31 ControlBar extraction slice. `Views/ControlBar.xaml` and
`Views/ControlBar.xaml.cs` were added; `DashboardView.xaml` now hosts
`<views:ControlBar>`; and
`DashboardView_HostsControlBarAndPreservesCommandBindings` proves the command
bindings and stable AutomationIds were preserved.

## Completed

- Added architecture/privacy boundary tests that fail if product code or
  manifests introduce keylogging hooks, raw keyboard state APIs, clipboard or
  screen capture APIs, Android Accessibility/text-input monitoring, or Chrome
  page-content/history/cookie/capture APIs.
- Added a WPF snapshot-tool privacy test proving the local screenshot helper
  captures only the target app window/elements rather than the desktop.
- Documented the automated privacy guardrails in `docs/privacy-boundaries.md`
  and `docs/hardening.md`.
- Added `MainWindowTrackingPipelineTests` for actual WPF Start/Stop behavior:
  UI Automation invokes the buttons, fake foreground changes are persisted to
  SQLite, outbox rows are queued, and dashboard cards/session rows are rendered
  from the SQLite-backed datasource.
- Disabled parallel execution in `Woong.MonitorStack.Windows.App.Tests` to make
  WPF XAML/STA window tests more deterministic.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal`, `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1 -v
  minimal`, and `scripts/test-coverage.ps1`; current .NET line coverage is
  92.9% overall, Domain 89.3%, Windows 92.5%, Windows.Presentation 97.6%,
  Windows.App 85.3%, and Server 96.3%.
- Verified `scripts/run-wpf-real-start-acceptance.ps1 -Seconds 2` exits
  successfully with a temp SQLite DB and no server sync.
- Added Windows local browser raw-event retention: a 30-day default
  `BrowserRawEventRetentionPolicy`, repository-level
  `DeleteOlderThan(...)`, and optional `ChromeNativeMessageIngestionFlow`
  pruning through `BrowserRawEventRetentionService`.
- Added `PollTrackingCommand` to the dashboard ViewModel and a WPF
  `DispatcherTimer` adapter in `MainWindow` so Running tracking continues to
  poll and advance the current session duration.
- Added semantic WPF runtime tests for poll-tick duration updates, foreground
  change refresh after persistence, fake browser github/chatgpt web sessions in
  the Web Sessions tab, and minimum-size scrolling for lower dashboard tabs.
- Verified the requested runtime confidence pass with
  `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
  (171 tests passed), `dotnet build Woong.MonitorStack.sln --no-restore
  -maxcpucount:1 -v minimal` (0 warnings/errors),
  `scripts/run-wpf-real-start-acceptance.ps1 -Seconds 2`, and
  `scripts/test-coverage.ps1`. Current .NET line coverage remains 92.9%
  overall; Domain 89.3%, Windows 92.5%, Windows.Presentation 97.6%,
  Windows.App 85.3%, Server 96.3%.
- Added `scripts/run-wpf-ui-acceptance.ps1`, which composes the RealStart
  semantic Start/Stop/temp-SQLite verification with local UI snapshot evidence
  under `artifacts/wpf-ui-acceptance/`.
- Added `WpfUiAcceptanceScriptTests` so the WPF App test suite guards the
  acceptance script's RealStart composition, temp SQLite focus/outbox checks,
  local artifact path, sync opt-in flag, and privacy warning text.
- Verified `scripts/run-wpf-ui-acceptance.ps1 -Seconds 2`; it launched the WPF
  app through FlaUI, invoked Start/Stop, persisted temp SQLite
  `focus_session` and `sync_outbox` rows, ran UI snapshots, and wrote
  `artifacts/wpf-ui-acceptance/latest/report.md`.
- Re-verified `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1
  -v minimal`; all 172 .NET tests passed.
- Re-verified `dotnet build Woong.MonitorStack.sln --no-restore
  -maxcpucount:1 -v minimal`; build passed with 0 warnings/errors.
- Re-verified `scripts/test-coverage.ps1`; current .NET line coverage is
  92.9% overall, Domain 89.3%, Windows 92.5%, Windows.Presentation 97.4%,
  Windows.App 86.6%, and Server 96.3%.
- Added `WindowsAppAcceptanceMode.TrackingPipeline` and an
  `AcceptanceTrackingDashboardCoordinator` for local-only WPF acceptance. It
  persists fake Code.exe and chrome.exe focus sessions to temp SQLite, creates
  linked `github.com` and `chatgpt.com` web sessions, queues focus/web outbox
  rows, and fake-syncs outbox rows only after the UI enables sync.
- Upgraded `tools/Woong.MonitorStack.Windows.UiSnapshots` with semantic FlaUI
  TrackingPipeline checks, required screenshot names, `manifest.json`, and a
  local `visual-review-prompt.md`.
- Hardened `scripts/run-wpf-ui-acceptance.ps1` so native `dotnet` exit codes
  fail the script, and split RealStart and TrackingPipeline temp SQLite DBs for
  deterministic fake-pipeline checks.
- Verified TrackingPipeline acceptance with
  `scripts/run-wpf-ui-acceptance.ps1 -Seconds 2`; latest detailed snapshot
  report is PASS and confirms Running/Stopped state, Code.exe, chrome.exe,
  `github.com`, `chatgpt.com`, summary `15m`, live Focus/Web events, and fake
  Sync Now status.
- Re-verified `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1
  -v minimal`; all 175 .NET tests passed.
- Re-verified `dotnet build Woong.MonitorStack.sln --no-restore
  -maxcpucount:1 -v minimal`; build passed with 0 warnings/errors.
- Re-verified `scripts/test-coverage.ps1`; current .NET line coverage is
  92.2% overall, Domain 89.3%, Windows 92.5%, Windows.Presentation 97.4%,
  Windows.App 85.7%, and Server 96.3%.
- Added a Windows coordinator opt-in enforcement test proving `SyncNow(false)`
  returns the skipped status and leaves queued outbox rows pending, unsynced,
  and retry count zero.
- Verified the existing Android `AndroidSyncWorkerTest` still proves disabled
  sync returns skipped success and does not invoke the sync runner.
- Added WPF Settings copy for browser URL/domain privacy:
  `Browser URL storage is domain-only by default. Full URLs require explicit
  future opt-in.`
- Re-verified `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1
  -v minimal`; all 176 .NET tests passed.
- Re-verified `dotnet build Woong.MonitorStack.sln --no-restore
  -maxcpucount:1 -v minimal`; build passed with 0 warnings/errors.
- Re-verified Android `testDebugUnitTest` and `assembleDebug` from `android/`.
- Re-verified `scripts/run-wpf-ui-acceptance.ps1 -Seconds 2`; the latest WPF
  acceptance report remains PASS.
- Re-verified `scripts/test-coverage.ps1`; current .NET line coverage is
  92.3% overall.
- Completed Milestone 21 with Presentation-first MVVM state and tests.
- Added `IDashboardTrackingCoordinator`, `NoopDashboardTrackingCoordinator`,
  `DashboardTrackingSnapshot`, structured `DashboardPersistedSessionSnapshot`,
  and `DashboardSyncResult`.
- Added WPF current activity panel and stable AutomationIds:
  `StartTrackingButton`, `StopTrackingButton`, `SyncNowButton`,
  `TrackingStatusText`, `CurrentAppNameText`, `CurrentProcessNameText`,
  `CurrentWindowTitleText`, `CurrentSessionDurationText`,
  `LastPersistedSessionText`, and `LastSyncStatusText`.
- Added `WindowTitleVisibleCheckBox`; window and web page titles are hidden by
  default and previously hidden raw titles are not retained for later reveal.
- Verified Start/Stop commands through a fake tracking coordinator and verified
  Sync Now receives the current sync opt-in state.
- Reduced the default WPF shell width to fit local snapshot automation better
  and added a UI expectation test for the default width ceiling.
- Verified `dotnet restore Woong.MonitorStack.sln`, `dotnet build
  Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`, and `dotnet
  test Woong.MonitorStack.sln --no-build -maxcpucount:1 -v minimal`; all 120
  .NET tests passed.
- Verified coverage generation with `scripts/test-coverage.ps1`; current
  overall line coverage is 91.9%, Domain 88.6%, Windows.Presentation 98.2%,
  Windows 91.1%, Windows.App 52.0%, and Server 96.0%.
- Verified local WPF UI snapshots with `scripts/run-ui-snapshots.ps1`;
  `artifacts/ui-snapshots/latest/report.md` reports PASS. `ChartArea` crop
  remains skipped when not visible in the first viewport and should be handled
  in Milestone 25 semantic acceptance.
- Deferred deeper WPF layout tuning to Milestone 25 after user feedback: the
  lower App Sessions, Web Sessions, and Live Event Log areas are more important
  than perfect first-viewport visibility of the current activity panel.
- Added `WindowsAppOptions` for typed Windows app composition settings:
  dashboard timezone, device id, local SQLite connection string, and idle
  threshold.
- Added `WindowsTrackingDashboardCoordinator`, which creates a fresh
  `TrackingPoller` per Start, persists closed focus sessions to
  `SqliteFocusSessionRepository`, enqueues `focus_session` upload payloads in
  `SqliteSyncOutboxRepository`, and keeps Stop->Start from extending a previous
  stopped session.
- Added `SqliteDashboardDataSource` so the WPF dashboard reads focus and linked
  web sessions from Windows local SQLite rather than `EmptyDashboardDataSource`.
- Updated WPF DI to register real Windows tracking readers, idle detection,
  sessionizer, tracking poller factory, SQLite repositories, dashboard data
  source, and tracking coordinator.
- Added app-level tests for DI registration, fake foreground change
  persistence/outbox enqueueing, Stop flush, Stop->Start session separation,
  and SQLite dashboard source focus/web reads.
- Added a Presentation test proving Stop refreshes the current dashboard period
  after flushed data becomes available.
- Verified `dotnet restore Woong.MonitorStack.sln`, `dotnet build
  Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`, and `dotnet
  test Woong.MonitorStack.sln --no-build -maxcpucount:1 -v minimal`; all 127
  .NET tests passed.
- Verified coverage generation with `scripts/test-coverage.ps1`; current
  overall line coverage is 92.0%, Domain 88.6%, Windows.Presentation 97.6%,
  Windows 91.1%, Windows.App 83.6%, and Server 96.0%.
- Verified WPF local snapshot smoke with `scripts/run-ui-snapshots.ps1`;
  `artifacts/ui-snapshots/latest/report.md` reports PASS.
- Added `WOONG_MONITOR_LOCAL_DB` and `WOONG_MONITOR_DEVICE_ID` app option
  overrides so local acceptance can force the WPF app onto a temp SQLite DB and
  stable test device id.
- Added `scripts/run-wpf-real-start-acceptance.ps1` with the required privacy
  warning: it observes foreground window metadata only, does not record
  keystrokes, does not capture screen contents, and uses a temp DB by default.
- Added `tools/Woong.MonitorStack.Windows.RealStartAcceptance`, which launches
  the WPF app, clicks Start/Stop through FlaUI, and verifies persisted
  `focus_session` plus `sync_outbox` rows.
- Verified RealStart locally with a temp DB containing one `focus_session` row
  and one `sync_outbox` row.
- Verified `dotnet restore Woong.MonitorStack.sln`, `dotnet build
  Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`, and `dotnet
  test Woong.MonitorStack.sln --no-build -maxcpucount:1 -v minimal`; all 129
  .NET tests passed.
- Verified coverage generation with `scripts/test-coverage.ps1`; current
  overall line coverage is 92.1%, Domain 88.6%, Windows.Presentation 97.6%,
  Windows 91.1%, Windows.App 85.0%, and Server 96.0%.
- Added `BrowserActivitySnapshot`, `CaptureMethod`, `CaptureConfidence`,
  `BrowserProcessClassification`, `IBrowserProcessClassifier`,
  `IBrowserActivityReader`, `IBrowserUrlSanitizer`, `IWebSessionizer`, and the
  initial `BrowserProcessClassifier`.
- Verified supported browser process classification for `chrome.exe`,
  `msedge.exe`, `firefox.exe`, `brave.exe`, uppercase variants, and process
  names reported without `.exe`.
- Verified non-browser process names do not classify as browsers.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal` and `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1
  -v minimal`; all 140 .NET tests passed.
- Verified coverage generation with `scripts/test-coverage.ps1`; current
  overall line coverage is 92.3%, Domain 88.6%, Windows.Presentation 97.6%,
  Windows 91.5%, Windows.App 85.0%, and Server 96.0%.
- Added `BrowserUrlSanitizer`, which clears URL/domain capture when browser
  URL storage is Off, stores registrable domain without full URL in DomainOnly
  mode, and strips URL fragments when FullUrl storage is explicitly enabled.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal` and `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1
  -v minimal`; all 143 .NET tests passed.
- Verified coverage generation with `scripts/test-coverage.ps1`; current
  overall line coverage is 92.0%, Domain 88.6%, Windows.Presentation 97.6%,
  Windows 91.0%, Windows.App 85.0%, and Server 96.0%.
- Updated `WebSession` and `WebSessionUploadItem` so `Url` and `PageTitle` are
  nullable while `Domain` remains required for persisted web sessions.
- Updated `SqliteWebSessionRepository` to write/read nullable URL and page
  title values for domain-only browser privacy mode.
- Updated `BrowserWebSessionizer` to accept sanitized
  `BrowserActivitySnapshot` inputs, create domain-only `WebSession` rows
  without a full URL, close a prior domain on domain change, and ignore
  snapshots with neither URL nor domain so they fall back to FocusSession-only
  tracking.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal` and `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1
  -v minimal`; all 145 .NET tests passed.
- Verified coverage generation with `scripts/test-coverage.ps1`; current
  overall line coverage is 92.1%, Domain 88.7%, Windows.Presentation 97.6%,
  Windows 91.2%, Windows.App 85.0%, and Server 96.0%.
- Added optional `CaptureMethod`, `CaptureConfidence`, and
  `IsPrivateOrUnknown` fields to shared `WebSession`, web-session upload
  contracts, and server web-session entity shape.
- Updated `SqliteWebSessionRepository` to create/read/write
  `capture_method`, `capture_confidence`, and `is_private_or_unknown` columns
  in Windows local SQLite.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal` and `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1
  -v minimal`; all 146 .NET tests passed.
- Verified coverage generation with `scripts/test-coverage.ps1`; current
  overall line coverage is 92.2%, Domain 88.8%, Windows.Presentation 97.6%,
  Windows 91.4%, Windows.App 85.0%, and Server 96.0%.
- Updated `ChromeNativeMessageIngestionFlow` so completed web sessions can
  enqueue `web_session` outbox items when configured with a device id and
  outbox repository.
- Verified the web-session upload payload includes device id, focus session id,
  domain, duration, nullable URL/title fields, and capture provenance.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal` and `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1
  -v minimal`; all 147 .NET tests passed.
- Verified coverage generation with `scripts/test-coverage.ps1`; current
  overall line coverage is 92.3%, Domain 88.8%, Windows.Presentation 97.6%,
  Windows 91.7%, Windows.App 85.0%, and Server 96.0%.
- Added nullable raw browser event records so browser raw events can preserve
  domain-only data without storing full URLs.
- Updated Chrome native-message ingestion to sanitize messages before raw-event
  persistence and before WebSession creation.
- Verified DomainOnly ingestion stores `Url = null` in both raw browser events
  and completed WebSessions while retaining the normalized domain.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal` and `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1
  -v minimal`; all 149 .NET tests passed.
- Verified coverage generation with `scripts/test-coverage.ps1`; current
  overall line coverage is 92.6%, Domain 88.8%, Windows.Presentation 97.6%,
  Windows 92.3%, Windows.App 85.0%, and Server 96.0%.
- Updated server EF web-session model configuration so URL and page title are
  optional, while capture method/confidence have bounded lengths.
- Verified duplicate web-session upload retries remain idempotent when URL and
  page title are null and capture provenance is present.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal` and `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1
  -v minimal`; all 148 .NET tests passed.
- Verified coverage generation with `scripts/test-coverage.ps1`; current
  overall line coverage is 92.3%, Domain 88.8%, Windows.Presentation 97.6%,
  Windows 91.7%, Windows.App 85.0%, and Server 96.0%.
- Added `NativeMessagingHostManifestGenerator`, which emits the Chrome native
  messaging host manifest JSON with the stable host name, description, host
  executable path, `stdio` transport type, and explicit allowed extension
  origins.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal` and `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1
  -v minimal`; all 150 .NET tests passed.
- Verified coverage generation with `scripts/test-coverage.ps1`; current
  overall line coverage is 92.7%, Domain 88.8%, Windows.Presentation 97.6%,
  Windows 92.4%, Windows.App 85.0%, and Server 96.0%.
- Added nullable process/window metadata fields to `FocusSession` and
  `FocusSessionUploadItem`: `ProcessId`, `ProcessName`, `ProcessPath`,
  `WindowHandle`, and `WindowTitle`.
- Updated `FocusSessionizer` to preserve safe process id/name/path and window
  handle from foreground snapshots while leaving `WindowTitle` null until a
  privacy setting explicitly allows it.
- Updated `SqliteFocusSessionRepository` to create and backfill local
  `focus_session` metadata columns and round-trip them in queries.
- Updated the WPF tracking coordinator's `focus_session` outbox payload to
  include the same metadata.
- Updated server `FocusSessionEntity`, EF mapping, and upload service so the
  integrated DB persists the metadata.
- Added server schema tests for required focus process/window fields and web
  nullable URL/title plus capture provenance fields.
- Generated server migration
  `20260428165251_AddFocusSessionWindowMetadata`, which also captures prior
  web-session nullable URL/title and capture provenance schema changes.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal` and `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1
  -v minimal`; all 154 .NET tests passed.
- Verified coverage generation with `scripts/test-coverage.ps1`; current
  overall line coverage is 92.8%, Domain 89.3%, Windows.Presentation 97.6%,
  Windows 92.5%, Windows.App 85.3%, and Server 96.1%.
- Added `DeviceStateSessionEntity`, `AppFamilyEntity`, and
  `AppFamilyMappingEntity` to the server integrated DB model.
- Added EF mappings for `device_state_sessions`, `app_families`, and
  `app_family_mappings`, including unique indexes for `(DeviceId,
  ClientSessionId)`, `Key`, and `(MappingType, MatchKey)`.
- Added relational SQLite fallback tests proving duplicate
  `device_state_sessions` and `app_family_mappings` rows are rejected by a real
  relational provider rather than EF InMemory.
- Generated server migration
  `20260428170042_AddDeviceStateAndAppFamilyTables`.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal` with 0 warnings/errors.
- The first full `dotnet test --no-build` run hit the known intermittent WPF
  XAML lazy-load failure in `MainWindowSmokeTests`; rerunning the WPF App test
  project passed, then rerunning full `dotnet test Woong.MonitorStack.sln
  --no-build -maxcpucount:1 -v minimal` passed all 158 tests.
- Verified coverage generation with `scripts/test-coverage.ps1`; current
  overall line coverage is 92.9%, Domain 89.3%, Windows.Presentation 97.6%,
  Windows 92.5%, Windows.App 85.3%, and Server 96.3%.
- Added `docs/android-app-usage-contract-decision.md`.
- Decided that Android app usage sync remains on `FocusSessionUploadItem` for
  MVP instead of introducing a separate `AppUsageSession` server contract.
- Documented the Android mapping: package name to `platformAppKey`,
  `source = android_usage_stats`, and null Windows process/window metadata.
- Added `docs/coding-guide.md` as the project-wide coding guide for future
  slices.
- Reopened `total_todolist.md` for Original Intent Restoration and changed the
  Final Definition of Done back to incomplete until the new restoration
  milestones are implemented and verified.
- Added `docs/original-product-intent-audit.md`, `docs/runtime-pipeline.md`,
  `docs/browser-tracking-policy.md`, `docs/privacy-boundaries.md`,
  `docs/wpf-ui-acceptance-checklist.md`,
  `docs/android-ui-screenshot-testing.md`, and
  `docs/future-macos-feasibility.md`.
- Updated `AGENTS.md` and `docs/coding-guide.md` to state the core rule:
  Woong Monitor Stack measures app/window/site usage metadata, not typed input,
  screen contents, clipboard contents, messages, forms, or covert activity.
- Used subagent audits for Windows/WPF/browser and Android. Key findings:
  Windows has foreground/idle/sessionizer/SQLite/browser primitives but lacks
  WPF Start/Stop/Sync UI, app-hosted tracking orchestration, SQLite-backed
  dashboard wiring, and semantic FlaUI acceptance. Android has UsageStats,
  Room, workers, dashboard, settings, and tests, but lacks WorkManager
  scheduling UX, collection-to-outbox wiring, persisted sync opt-in
  enforcement, UI Automator navigation, and Android screenshot tooling.
- Verified the planning/docs slice with `dotnet restore Woong.MonitorStack.sln`,
  `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`,
  and `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1 -v
  minimal`; all 110 .NET tests passed.
- Verified the coding-guide documentation slice with `dotnet restore
  Woong.MonitorStack.sln`, `dotnet build Woong.MonitorStack.sln --no-restore
  -maxcpucount:1 -v minimal`, and `dotnet test Woong.MonitorStack.sln
  --no-build -maxcpucount:1 -v minimal`; all 110 .NET tests passed.
- Added `docs/wpf-csharp-coding-guide.md` as a dedicated WPF/.NET layering
  guide that explains what belongs in Domain, Windows.Presentation, Windows
  infrastructure, Windows.App, and Server.
- Linked the WPF/C# guide from `docs/coding-guide.md`.
- Verified the WPF/C# guide documentation slice with `dotnet restore
  Woong.MonitorStack.sln`, `dotnet build Woong.MonitorStack.sln --no-restore
  -maxcpucount:1 -v minimal`, and `dotnet test Woong.MonitorStack.sln
  --no-build -maxcpucount:1 -v minimal`; all 110 .NET tests passed.
- Added `docs/architecture/reference-rules.md` with project dependency
  direction and the explicit LiveCharts Presentation adapter exception.
- Added `tests/Woong.MonitorStack.Architecture.Tests` with NetArchTest and
  csproj/source checks for forbidden project, package, WPF, server, and Windows
  dependencies.
- Removed the accidental `Windows.Presentation` reference to the Windows
  infrastructure project.
- Refactored WPF startup to `Microsoft.Extensions.Hosting`; `App.xaml.cs` owns
  host lifecycle and resolves `MainWindow` from DI.
- Added typed `DashboardOptions` and registered `MainWindow`,
  `DashboardViewModel`, `IDashboardDataSource`, and `IDashboardClock` through
  DI.
- Kept `MainWindow.xaml.cs` thin and added a DI composition smoke test.
- Verified `MainWindow.xaml` is readable WPF XAML and added the missing app
  usage chart binding alongside activity and domain charts.
- Expanded dashboard ViewModel tests for Today, rolling, and custom ranges,
  empty state, invalid timezone behavior, top domain, row ordering, chart mapper
  empty input, timezone labels, and retained LiveCharts mapper behavior.
- Added `coverage.runsettings`, `scripts/test-coverage.ps1`,
  `scripts/test-coverage.sh`, and ReportGenerator local tool registration.
- Added `docs/architecture/coverage-quality-gate.md`; current coverage snapshot
  is overall 91.7%, Domain 88.6%, Windows.Presentation 99.0%, Windows 91.1%,
  Windows.App 51.0%, Server 96.0%.
- Verified `dotnet restore`, `dotnet build --no-restore`,
  `dotnet test --no-build`, coverage collection/report generation, and Windows
  smoke tool.
- Added `docs/completion-audit.md` after checking PRD/TODO consistency,
  hidden TODO/FIXME markers, Android device availability, and the full
  validation matrix. The only remaining item is physical Android resource
  measurement, blocked by no attached device.
- Added local WPF UI snapshot automation with FlaUI under
  `tools/Woong.MonitorStack.Windows.UiSnapshots`, stable MainWindow
  AutomationIds, `scripts/run-ui-snapshots.ps1`, and
  `docs/ui-snapshot-testing.md`.
- Verified `scripts/run-ui-snapshots.ps1` locally. It created
  `artifacts/ui-snapshots/latest/` with `01-startup.png`,
  `02-dashboard-after-refresh.png`, `03-dashboard-period-change.png`,
  `04-settings.png`, optional visible crops, and `report.md`.
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
- Added `docs/server-test-db-strategy.md`.
- Added `RelationalTestDatabase` using SQLite in-memory as the current
  relational fallback because Docker/Testcontainers is unavailable here.
- Verified the server device unique index is enforced by a relational provider.
- Verified the relational reset strategy recreates an empty schema.
- Added local `dotnet-ef` 10.0.4 tool manifest and EF Core design package.
- Generated `20260428131352_InitialCreate` for server PostgreSQL schema.
- Added `docs/production-migrations.md` with migration review notes.
- Verified migration files define the core PostgreSQL tables.
- Added `tools/Woong.MonitorStack.Windows.Profile`.
- Ran a 30-second Windows collector polling profile: 59 polls, 406.25 ms CPU,
  23.80 MB peak working set.
- Checked `adb devices -l`; no physical Android device was connected, so
  physical-device resource measurement remains blocked.
- Verified latest .NET restore/test/build after migration and profiling changes.
- Verified Android `testDebugUnitTest` and `assembleDebug` after hardening
  changes.
- Added `AndroidSyncSettings` and
  `SharedPreferencesAndroidSyncSettings`; sync opt-in defaults to false and
  persists enabled state.
- Updated `AndroidSyncWorker` so disabled sync returns a skipped success result
  with zero upload counts and does not invoke the sync runner.
- Verified `.\gradlew.bat testDebugUnitTest --no-daemon --stacktrace` from
  `android/`.
- Verified `.\gradlew.bat assembleDebug --no-daemon --stacktrace` from
  `android/`.
- Verified `.NET` coverage generation with `scripts/test-coverage.ps1`;
  current overall line coverage is 92.9%, Domain 89.3%, Windows 92.5%,
  Windows.Presentation 97.6%, Windows.App 85.3%, and Server 96.3%.
  Android coverage collection is not configured yet, so Android validation for
  this slice is unit-test/build based.
- Recreated ignored `android/local.properties` locally to point Gradle at the
  installed Android SDK; this file remains untracked and must not be committed.
- Added `UsageSyncOutboxEnqueuer` and `FocusSessionSyncOutboxEnqueuer`.
- Updated `AndroidUsageCollectionRunner` to enqueue collected focus sessions
  into `sync_outbox` after local Room storage.
- Updated the Android UsageStats source value to `android_usage_stats` to match
  the shared contract decision.
- Added a `SyncOutboxWriter` interface implemented by `SyncOutboxDao`.
- Updated `scripts/test-coverage.ps1` to pass `-maxcpucount:1`, matching the
  repository's stable .NET validation convention after coverage collection hit
  the known intermittent WPF XAML lazy-load failure once.
- Verified `.\gradlew.bat testDebugUnitTest --no-daemon --stacktrace` from
  `android/`.
- Verified `.\gradlew.bat assembleDebug --no-daemon --stacktrace` from
  `android/`.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal`.
- Verified `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1 -v
  minimal`.
- Verified `.NET` coverage generation with `scripts/test-coverage.ps1`;
  current overall line coverage is 92.4%, Domain 89.3%, Windows 92.5%,
  Windows.Presentation 97.6%, Windows.App 79.9%, and Server 96.3%.
- Added `AndroidManifestPrivacyTest` to enforce
  `android:allowBackup="false"` for local usage metadata.
- Updated `AndroidManifest.xml` to disable app backup.
- Documented Android local metadata backup hardening in
  `docs/privacy-boundaries.md` and `docs/hardening.md`.
- Verified `.\gradlew.bat testDebugUnitTest --no-daemon --stacktrace` from
  `android/`.
- Verified `.\gradlew.bat assembleDebug --no-daemon --stacktrace` from
  `android/`.
- Added `AndroidUsageCollectionSettings` with a SharedPreferences
  implementation that defaults collection scheduling to disabled.
- Added `AndroidUsageCollectionScheduler`, `UsageCollectionWorkScheduler`, and
  `WorkManagerUsageCollectionWorkScheduler` for unique 15-minute
  `CollectUsageWorker` scheduling.
- Verified the scheduler cancels periodic work when collection is disabled,
  cancels when Usage Access is missing, and schedules only when both persisted
  collection setting and Usage Access permission allow it.
- Updated `docs/runtime-pipeline.md` so local outbox creation is separate from
  server upload opt-in: outbox rows are local retry metadata, and upload remains
  suppressed while sync opt-in is off.
- Verified `.\gradlew.bat testDebugUnitTest --no-daemon --stacktrace` from
  `android/`.
- Verified `.\gradlew.bat assembleDebug --no-daemon --stacktrace` from
  `android/`.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal`.
- Verified `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1 -v
  minimal`.
- Added `FocusSessionDao.queryRecent(limit)` and verified newest sessions are
  returned first.
- Added `RoomSessionsRepository`, `SessionRow`, and duration formatting for
  Android app usage rows.
- Updated `SessionsActivity` to load recent sessions from `MonitorDatabase` on
  a background thread and hide the empty state when rows exist.
- Verified `.\gradlew.bat testDebugUnitTest --no-daemon --stacktrace` from
  `android/`.
- Verified `.\gradlew.bat assembleDebug --no-daemon --stacktrace` from
  `android/`.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal`.
- Verified `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1 -v
  minimal`.
- Added `DailySummaryActivityLoader` and `DailySummaryActivityLoadRequest`.
- Updated `DailySummaryActivity` so `EXTRA_BASE_URL` + `EXTRA_USER_ID` triggers
  repository/client loading for the previous local day while preserving the
  existing intent-display fallback used by smoke tests.
- Verified `.\gradlew.bat testDebugUnitTest --no-daemon --stacktrace` from
  `android/`.
- Verified `.\gradlew.bat assembleDebug --no-daemon --stacktrace` from
  `android/`.
- Verified `.\gradlew.bat assembleDebugAndroidTest --no-daemon --stacktrace`
  from `android/`.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal`.
- Verified `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1 -v
  minimal`.
- Added `NotificationPermissionPolicy` and `NotificationPermissionController`
  for Android 13+ `POST_NOTIFICATIONS` runtime requests.
- Updated Android Settings XML and strings with notification permission
  guidance plus an explicit request button.
- Extended Settings Robolectric coverage for notification permission copy and
  button binding.
- Verified `.\gradlew.bat testDebugUnitTest --no-daemon --stacktrace` from
  `android/`.
- Verified `.\gradlew.bat assembleDebug --no-daemon --stacktrace` from
  `android/`.
- Verified `.\gradlew.bat assembleDebugAndroidTest --no-daemon --stacktrace`
  from `android/`.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal`.
- Verified `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1 -v
  minimal`.
- Added AndroidX UI Automator to the Gradle version catalog and androidTest
  dependencies.
- Added `UsageAccessSettingsNavigationTest`, which launches Settings, taps the
  Usage Access button, and waits for the system Settings package.
- Verified `.\gradlew.bat assembleDebugAndroidTest --no-daemon --stacktrace`
  from `android/`.
- Verified `.\gradlew.bat testDebugUnitTest --no-daemon --stacktrace` from
  `android/`.
- Verified `.\gradlew.bat assembleDebug --no-daemon --stacktrace` from
  `android/`.
- Checked `adb devices -l`; no device/emulator was attached, so
  `connectedDebugAndroidTest` remains blocked for this smoke.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal`.
- Verified `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1 -v
  minimal`.

## 2026-04-29 WPF Product UI Goal Slice

- Added `docs/wpf-ui-plan.md` as the durable text version of the provided WPF
  UI goal image and instructions.
- Reopened `total_todolist.md` with Milestone 30 for the WPF product UI goal.
- Updated `MainWindow.xaml` with a larger product dashboard layout:
  header title/subtitle, tracking/sync/privacy badges, readable control bar,
  Current Focus panel, four metric cards, chart titles, wider App/Web/Live grids,
  and clearer Settings sections.
- Moved current process/top app text out of the Header.
- Added Current Focus fields for browser domain, last poll time, and last DB
  write time.
- Updated ViewModel display state for `TrackingBadgeText`, `SyncBadgeText`,
  `PrivacyBadgeText`, `CurrentBrowserDomainText`, `LastPollTimeText`, and
  `LastDbWriteTimeText`.
- Updated summary cards to the product metric vocabulary: Active Focus,
  Foreground, Idle, and Web Focus.
- Expanded dashboard row models so the UI can show process, start/end, state,
  window/source, URL mode, browser, confidence, app, and domain columns.
- Fixed a runtime UI bug: a later poll with no newly persisted session no
  longer clears the last persisted session text.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter MainWindowUiExpectationTests`
- `dotnet test tests\Woong.MonitorStack.Windows.Presentation.Tests\Woong.MonitorStack.Windows.Presentation.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter UpdateCurrentActivity_WhenLaterPollHasNoPersistedSession_KeepsLastPersistedSession`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1 -Seconds 2`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Current .NET test count is 177 passing tests. Coverage report generated at
`artifacts/coverage/SummaryGithub.md`; current overall line coverage is 92.2%,
Windows.Presentation is 95.7%, and Windows.App is 86.3%.

Latest WPF acceptance artifact:

`artifacts/wpf-ui-acceptance/20260429-095154`

Remaining Milestone 30 work:

- Add visible empty-state overlays for all chart panels.
- Add richer Live Event Log semantic tests for start/close/persist/outbox/sync
  event names.
- Add explicit Settings tests for full URL off, sync off, and folder command
  availability/disabled state.
- Capture explicit 1920/1366/1024 WPF snapshots.
- Update browser UI docs if new browser connection/status labels are added.

## Next Highest Priority

Pause Milestone 31 component extraction to close the runtime poll-tick
persistence quality gap: prove WPF poll-tick foreground changes persist
SQLite/outbox rows and refresh the dashboard before Stop. After that gap is
covered, continue Milestone 31 with `CurrentFocusPanel` extraction while
preserving existing automation IDs, bindings, behavior tests, and semantic WPF
acceptance.

## 2026-04-29 WPF Chart Axis Slice

- Updated `DashboardChartMapper` hour labels from `HH:mm` to compact `HH`
  labels for hourly Active Focus.
- Extended `DashboardLiveChartsData` with tested `XAxes`, `YAxes`, and
  `EmptyStateText`.
- Updated `DashboardLiveChartsMapper` so Cartesian charts expose category
  labels on X axes and minute labels on Y axes.
- Bound `HourlyActivityChart` and `AppUsageChart` X/Y axes in `MainWindow.xaml`.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.Presentation.Tests\Woong.MonitorStack.Windows.Presentation.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "DashboardChartMapperTests|DashboardLiveChartsMapperTests"`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1 -Seconds 2`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF acceptance artifact:

`artifacts/wpf-ui-acceptance/20260429-100159`

Current overall line coverage is 92.3%, Windows.Presentation is 96.6%, and
Windows.App is 86.3%.

## 2026-04-29 WPF Chart Empty-State Slice

- Added visible chart empty-state overlays for hourly activity, app usage, and
  domain usage panels.
- Added `MainWindow_WithEmptyData_ShowsReadableChartEmptyStates` to verify the
  WPF UI exposes `No data for selected period` instead of relying on broken
  empty chart axes.
- Kept the semantic WPF acceptance path passing after the chart UI change.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter MainWindow_WithEmptyData_ShowsReadableChartEmptyStates`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1 -Seconds 2`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF acceptance artifact:

`artifacts/wpf-ui-acceptance/20260429-100746`

Current overall line coverage is 92.3%, Windows.Presentation is 96.6%, and
Windows.App is 86.3%.

## 2026-04-29 WPF Componentization Guidance

The user provided a component architecture direction for the WPF dashboard:
`MainWindow -> DashboardView -> HeaderStatusBar / ControlBar /
CurrentFocusPanel / SummaryCardsPanel / ChartsPanel / DetailsTabsPanel`, plus
reusable controls (`StatusBadge`, `MetricCard`, `SectionCard`, `DetailRow`,
`EmptyState`) and style dictionaries. This guidance is saved in
`docs/wpf-ui-plan.md` and tracked as Milestone 31 in `total_todolist.md`.

The `DashboardView`, `HeaderStatusBar`, and `ControlBar` extractions are now
complete. Before continuing with `CurrentFocusPanel`, close the runtime
poll-tick persistence quality gap so foreground changes observed during a timer
tick are proven to persist SQLite/outbox rows and refresh the dashboard before
Stop.

## 2026-04-29 WPF DashboardView Extraction Slice

- Extracted the existing vertical dashboard layout from `MainWindow.xaml` into
  `Views/DashboardView.xaml`.
- Added `Views/DashboardView.xaml.cs`.
- Kept `MainWindow.xaml` as a thin shell that hosts `DashboardView`.
- Updated `MainWindowUiExpectationTests` so
  `MainWindow_ExposesDashboardControlsAndCommandBindings` asserts the hosted
  `DashboardView` is present while preserving the dashboard control and command
  binding expectations.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter MainWindow_ExposesDashboardControlsAndCommandBindings`
  passed.
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
  passed all 26 Windows App tests.
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
  passed.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
  passed.
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1 -Seconds 2`
  passed and wrote WPF acceptance evidence at
  `artifacts/wpf-ui-acceptance/20260429-102017`.
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`
  completed successfully and generated the coverage report.

Remaining Milestone 31 componentization work stays open, including
`CurrentFocusPanel`, `SummaryCardsPanel`,
`ChartsPanel`, `DetailsTabsPanel`, `SettingsPanel`, reusable controls, style
dictionaries, chart details commands, final componentization verification, and
commit/push.

## 2026-04-29 WPF HeaderStatusBar Extraction Slice

- Added a RED componentization test,
  `DashboardView_HostsHeaderStatusBarAndPreservesHeaderContent`, proving the
  dashboard hosts a `HeaderStatusBar` while preserving header title/subtitle,
  tracking/sync/privacy badges, and the absence of current process text in the
  header.
- Extracted the header markup from `Views/DashboardView.xaml` into
  `Views/HeaderStatusBar.xaml`.
- Added `Views/HeaderStatusBar.xaml.cs`.
- Kept existing AutomationIds: `HeaderArea`, `TrackingStatusBadge`,
  `SyncStatusBadge`, and `PrivacyStatusBadge`.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter DashboardView_HostsHeaderStatusBarAndPreservesHeaderContent`
  failed before implementation and passed after extraction.
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
  passed all 27 Windows App tests.
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
  passed.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
  passed.
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1 -Seconds 2`
  passed and wrote WPF acceptance evidence at
  `artifacts/wpf-ui-acceptance/20260429-103315`.
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`
  completed successfully; overall line coverage is 92.3%.

ControlBar is the next completed componentization slice after HeaderStatusBar.

## 2026-04-29 WPF ControlBar Extraction Slice

- Added a RED componentization test,
  `DashboardView_HostsControlBarAndPreservesCommandBindings`, proving the
  dashboard hosts a `ControlBar` while preserving Start/Stop/Refresh/Sync
  command bindings, period/custom controls, readable control-bar expectations,
  and stable AutomationIds.
- Extracted the control-bar markup from `Views/DashboardView.xaml` into
  `Views/ControlBar.xaml`.
- Added `Views/ControlBar.xaml.cs`.
- Kept existing command bindings and AutomationIds intact through
  `<views:ControlBar>`.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter DashboardView_HostsControlBarAndPreservesCommandBindings`
  failed before implementation and passed after extraction.
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
  passed all 28 Windows App tests.
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
  passed.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
  passed.
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1 -Seconds 2`
  passed and wrote WPF acceptance evidence at
  `artifacts/wpf-ui-acceptance/20260429-104336`.
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`
  completed successfully; overall line coverage remains about 92.3%.

Next highest priority is the runtime poll-tick persistence quality gap before
continuing with `CurrentFocusPanel` extraction.
