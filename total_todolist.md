# Total TODO List: Woong Monitor Stack

Updated: 2026-05-02

This file is the durable source of truth for the full PRD. Every agent must keep
it current before and after work. A TODO is complete only when relevant tests
pass, build succeeds, docs are updated, privacy boundaries are respected, and
the finished slice is committed and pushed.

Status note: the checklist was reopened on 2026-04-29 for Original Intent
Restoration. The product is not considered complete again until the reopened
milestones below are finished.

## Always-On Workflow

- [x] Save PRD to `docs/prd.md`.
- [x] Initialize git repository.
- [x] Connect `origin` to `https://github.com/kimwoonggon/woong-wpf-android-monitor-stat.git`.
- [x] Install and verify Python for skill installer usage.
- [x] Install/verify default skills: `tdd`, `wpf-best-practices`, `find-skills`, `android-kotlin`, `android-device-automation`, `wpf-mvvm-generator`.
- [x] Add root `AGENTS.md`.
- [x] Document the rule that completed subagents must either be closed or reassigned to the next non-conflicting project slice.
- [x] Keep this `total_todolist.md` updated for every feature slice.
- [x] For each completed slice: run tests, run build, update docs, commit, push.
- [x] After every push: leave a concise resume summary for future context resets.

## Milestone 0: Repository/Workspace Bootstrap

- [x] Explore initial repository structure.
- [x] Confirm no existing Windows, Android, Server, Gradle, or test project structure existed at bootstrap.
- [x] Create `docs/`.
- [x] Save PRD as `docs/prd.md`.
- [x] Create `.NET` solution.
- [x] Create common domain project.
- [x] Create common domain xUnit test project.
- [x] Add `NuGet.config`.
- [x] Add `.gitignore`.
- [x] Document bootstrap state in `docs/bootstrap.md`.
- [x] Verify initial common tests pass.
- [x] Verify initial solution build succeeds with `-maxcpucount:1`.
- [x] Commit and push bootstrap/common-domain baseline.

## Milestone 1: Common Domain & Contracts

- [x] Define `TimeRange`.
- [x] Define initial `FocusSession`.
- [x] Define `LocalDateCalculator`.
- [x] Define `TimeBucket` and `TimeBucketAggregator`.
- [x] Test duration calculation.
- [x] Test UTC/local date conversion.
- [x] Test hour bucket split.
- [x] Add `FocusSession.FromUtc` factory that computes local date from timezone.
- [x] Define `Device`.
- [x] Define `Platform`.
- [x] Define `AppFamily`.
- [x] Define `PlatformApp`.
- [x] Define `WebSession`.
- [x] Define `DeviceStateSession`.
- [x] Define `DailySummary`.
- [x] Define upload/device DTO contracts.
- [x] Add `DomainNormalizer`.
- [x] Add `DailySummaryCalculator`.
- [x] Test idle exclusion in daily summaries.
- [x] Test grouping by local date in daily summaries.
- [x] Test registrable domain extraction.
- [x] Test device registration requires stable device key.
- [x] Test app family grouping in daily summaries.
- [x] Test upload batch request null validation.
- [x] Document time/date and DTO contract policy.
- [x] Run common domain tests.
- [x] Run solution build.
- [x] Commit and push Milestone 1 slice.

## Milestone 2: Windows Local Collector MVP

- [x] Create Windows collector/domain project structure.
- [x] Add user32.dll P/Invoke wrapper.
- [x] Define foreground window snapshot model.
- [x] Implement collector service.
- [x] Implement Windows focus sessionizer.
- [x] Implement idle detector.
- [x] Test app change closes previous session and starts new session.
- [x] Test same window extends current session.
- [x] Test idle threshold marks idle.
- [x] Test local midnight behavior.
- [x] Verify foreground app logging on Windows 10.
- [x] Commit and push Milestone 2.

## Milestone 3: Windows Local DB + Outbox

- [x] Choose EF Core SQLite or Dapper for Windows local DB.
- [x] Define SQLite schema/migrations.
- [x] Implement focus session repository.
- [x] Implement web session repository.
- [x] Implement sync outbox schema/repository.
- [x] Test repository insert/query/update.
- [x] Test focus session persistence and query.
- [x] Test web session links to focus session.
- [x] Test outbox pending to synced transition.
- [x] Test outbox failure increments retry count.
- [x] Verify local DB file creation/query.
- [x] Commit and push Milestone 3.

## Milestone 4: Windows WPF Dashboard MVP

- [x] Create WPF app project.
- [x] Create WPF presentation project for MVVM-testable logic.
- [x] Create WPF presentation xUnit test project.
- [x] Apply initial MVVM structure.
- [x] Add CommunityToolkit.Mvvm.
- [x] Add dashboard viewmodel foundation.
- [x] Add period filters: today, 1h, 6h, 24h, custom.
- [x] Add summary cards.
- [x] Bind WPF shell to dashboard summary cards.
- [x] Add dependency-free dashboard chart data mapper.
- [x] Add LiveCharts2 chart mapping.
- [x] Bind WPF shell to LiveCharts2 activity/domain charts.
- [x] Add app sessions table.
- [x] Add live event log.
- [x] Add web sessions view.
- [x] Add settings view.
- [x] Test DashboardViewModel filter changes refresh summary.
- [x] Test DashboardViewModel summary card models.
- [x] Test chart mapper behavior.
- [x] Test DashboardViewModel publishes chart points.
- [x] Test LiveCharts2 mapper behavior.
- [x] Test DashboardViewModel publishes LiveCharts2 series.
- [x] Harden WPF app/domain chart details so duplicate labels aggregate before
  top-N cutoff and detail windows show full labels with duration values.
- [x] Test DashboardViewModel publishes recent app session rows.
- [x] Test DashboardViewModel publishes web session rows.
- [x] Test DashboardViewModel publishes live event log rows.
- [x] Test settings default privacy state.
- [x] Test DashboardViewModel exposes settings.
- [x] Add WPF UI smoke path when tooling is ready.
- [x] Verify WPF build succeeds.
- [x] Commit and push WPF DashboardViewModel foundation slice.
- [x] Commit and push WPF summary card shell slice.
- [x] Commit and push WPF chart data mapper slice.
- [x] Commit and push WPF LiveCharts2 mapper slice.
- [x] Commit and push WPF app sessions table slice.
- [x] Commit and push WPF web/live event views slice.
- [x] Commit and push WPF settings view slice.
- [x] Commit and push WPF smoke path and Milestone 4 completion.
- [x] Commit and push Milestone 4.

## Milestone 4.5: Windows Chrome Extension + Native Messaging

- [x] Create Chrome extension project.
- [x] Define extension manifest.
- [x] Request tabs/webNavigation permissions.
- [x] Track active tab changed event.
- [x] Track URL/title/domain changes.
- [x] Implement native messaging host registration for Windows.
- [x] Implement extension -> native host message DTO.
- [x] Implement native host receiver in Windows app/service.
- [x] Add a console native host executable that reads persistent Chrome native
  messaging stdin and ingests domain-only tab events.
- [x] Add local Chrome native host install script that publishes the host,
  writes the manifest, and registers the HKCU native messaging host key.
- [x] Switch the Chrome extension to a persistent native port so ordered tab
  events share one sessionizer during the host lifetime.
- [x] Store browser raw events.
- [x] Convert browser events to web_session.
- [x] Test domain extraction from extension payload.
- [x] Test active tab event creates/updates web session.
- [x] Test duplicate tab events do not inflate duration.
- [x] Verify Chrome active tab URL appears in Windows local DB.
- [x] Commit and push Chrome extension/native messaging slice.

## Milestone 5: Server Integrated DB + API MVP

- [x] Create ASP.NET Core Web API project.
- [x] Create server test project.
- [x] Add EF Core PostgreSQL.
- [x] Decide server integration test DB strategy before PostgreSQL-specific tests.
- [x] Add relational test database reset strategy.
- [x] Verify idempotency and unique indexes against a relational provider.
- [x] Define integrated DB entities.
- [x] Define Device integrated DB entity.
- [x] Test Device unique index for idempotent registration.
- [x] Define FocusSession integrated DB entity.
- [x] Implement device registration API.
- [x] Persist device registration API through EF Core.
- [x] Implement focus session upload API.
- [x] Implement web session upload API.
- [x] Implement raw event upload API.
- [x] Enforce device/client session idempotency.
- [x] Return controlled upload errors for unregistered devices and missing web-session focus links under relational constraints.
- [x] Add daily summary calculator.
- [x] Add summary query API.
- [x] Add date range statistics query API.
- [x] Add WebApplicationFactory integration tests.
- [x] Test device registration idempotency.
- [x] Test device registration persists a server DB row.
- [x] Test duplicate clientSessionId ignored.
- [x] Test duplicate web session upload ignored.
- [x] Test duplicate raw event upload ignored.
- [x] Test daily summary generation.
- [x] Test date range statistics query.
- [x] Commit and push Milestone 5.

## Milestone 6: Windows Sync

- [x] Define sync API client.
- [x] Implement Windows sync worker.
- [x] Implement retry policy.
- [x] Add device token/auth placeholder.
- [x] Add sync checkpoint handling.
- [x] Test fake API sync success.
- [x] Test fake API sync failure retry.
- [x] Test duplicate upload remains safe.
- [x] Verify Windows local data uploads to server.
- [x] Commit and push Milestone 6.

## Milestone 7: Android Project Setup

- [x] Locate or import `WoongAndroidBasicProject` Gradle style if available.
- [x] Create Android project with Gradle wrapper.
- [x] Configure Kotlin.
- [x] Configure XML/View UI stack.
- [x] Enable ViewBinding.
- [x] Add ConstraintLayout.
- [x] Add RecyclerView.
- [x] Add Room dependencies.
- [x] Add WorkManager dependencies.
- [x] Add Retrofit/Moshi or OkHttp.
- [x] Add unit test dependencies.
- [x] Add Espresso/UI Automator dependencies.
- [x] Add empty JUnit test.
- [x] Add empty Espresso smoke test.
- [x] Verify `gradlew testDebugUnitTest`.
- [x] Verify `gradlew assembleDebug`.
- [x] Commit and push Milestone 7.

## Milestone 8: Android Usage Collection + Room

- [x] Implement Usage Access permission checker.
- [x] Implement permission settings navigation UI entry.
- [x] Implement UsageStats collector.
- [x] Implement UsageSessionizer.
- [x] Implement short consecutive event merge.
- [x] Define Room entities/DAO.
- [x] Implement collect worker.
- [x] Test resumed/paused creates session.
- [x] Test close same-app events merge.
- [x] Test different app starts close previous session.
- [x] Test Room DAO insert/query.
- [x] Test worker behavior.
- [x] Commit and push Milestone 8.

## Milestone 9: Android XML Dashboard MVP

- [x] Create DashboardActivity or DashboardFragment.
- [x] Create SessionsActivity or SessionsFragment.
- [x] Create SettingsActivity or SettingsFragment.
- [x] Add period filters.
- [x] Add total active time card.
- [x] Add top app card.
- [x] Add idle/inactive card.
- [x] Add MPAndroidChart time activity chart.
- [x] Add app usage chart.
- [x] Add recent sessions RecyclerView.
- [x] Test ViewModel state updates.
- [x] Add Room-backed dashboard aggregation.
- [x] Bind DashboardActivity to Room-backed dashboard data.
- [x] Add MPAndroidChart dependency and chart mapper.
- [x] Add real chart data aggregation beyond no-data placeholders.
- [x] Test DashboardActivity display with Espresso.
- [x] Test today/yesterday/recent 7 days filters.
- [x] Test empty state.
- [x] Test usage access guidance.
- [x] Test SettingsActivity privacy and sync defaults.
- [x] Commit and push Milestone 9.

## Milestone 10: Android Sync + Morning Summary

- [x] Define Android sync outbox.
- [x] Test Android sync outbox DAO insert/query/synced behavior.
- [x] Implement Retrofit/OkHttp sync client.
- [x] Test Android sync client focus-session upload contract.
- [x] Implement WorkManager sync worker.
- [x] Implement duplicate-safe upload handling.
- [x] Test Android sync outbox failed retry persistence.
- [x] Integrate daily summary API.
- [x] Test daily summary API client contract.
- [x] Add previous-day summary screen.
- [x] Add morning summary notification if feasible.
- [x] Test sync worker success.
- [x] Test sync worker failure retry.
- [x] Test duplicate upload.
- [x] Test summary display.
- [x] Commit and push Milestone 10.

## Milestone 11: Integrated Daily Summary

- [x] Implement server daily aggregation job.
- [x] Aggregate Windows + Android active time.
- [x] Exclude idle time from active totals.
- [x] Add app family mapping.
- [x] Compute top app.
- [x] Compute top domain.
- [x] Respect user timezone local date.
- [x] Add relational regression test proving focus sessions are grouped by requested user timezone across UTC midnight.
- [x] Test mixed Windows + Android data summary.
- [x] Test public HTTP runtime path: register Windows/Android devices, upload focus/web sessions, and query integrated daily summary with relational constraints.
- [x] Test timezone boundaries.
- [x] Test duplicate data does not inflate summary.
- [x] Verify Windows/Android can query integrated summary.
- [x] Commit and push Milestone 11.

## Milestone 12: Hardening & Release Candidate

- [x] Review DB migrations.
- [x] Define raw event retention policy.
- [x] Add Windows sync failure UI.
- [x] Add Android sync failure UI.
- [x] Add clear permission guidance text.
- [x] Check Windows CPU/memory usage.
- [x] Check Android CPU/memory/battery impact.
- [x] Stabilize all tests.
- [x] Write README.
- [x] Run all unit/component/integration tests.
- [x] Run WPF UI smoke tests.
- [x] Run Android connected tests when device/emulator is available.
- [x] Complete manual release checklist.
- [x] Verify Windows build.
- [x] Verify Android assemble.
- [x] Verify server integration tests.
- [x] Commit and push release candidate.

## Milestone 13: Post-RC Production Hardening

- [x] Generate production EF Core migrations before production PostgreSQL use.
- [x] Add migration review notes for generated PostgreSQL schema.
- [x] Accept emulator-backed Android resource measurements as the current completion baseline; physical-device measurement is optional future hardening.
- [x] Add longer-running Windows collector profiling once continuous background tracking is enabled.
- [x] Run full .NET tests.
- [x] Run full .NET build.
- [x] Run Android unit/build checks.
- [x] Commit and push post-RC hardening.

## Milestone 14: WPF/.NET Architecture Quality Gate

- [x] Document project reference rules in `docs/architecture/reference-rules.md`.
- [x] Add architecture tests for forbidden project and assembly dependencies.
- [x] Remove `Windows.Presentation` reference to `Windows` infrastructure.
- [x] Refactor WPF startup to Generic Host + DI.
- [x] Register `MainWindow`, `DashboardViewModel`, `IDashboardDataSource`, `IDashboardClock`, and typed `DashboardOptions`.
- [x] Keep `MainWindow.xaml.cs` thin.
- [x] Verify and update `MainWindow.xaml` as readable WPF XAML with summary cards, period commands, three chart areas, recent sessions, recent web sessions, live events, and settings.
- [x] Add MVVM behavior tests for period ranges, custom ranges, empty state, top domain, row ordering, invalid timezone behavior, chart mapper empty input, timezone labels, and retained LiveCharts mapper behavior.
- [x] Add repo coverage runsettings.
- [x] Add `scripts/test-coverage.ps1` and `scripts/test-coverage.sh`.
- [x] Add ReportGenerator local tool.
- [x] Ignore coverage output folders.
- [x] Document coverage expectations, current summary, gaps, and LiveCharts Presentation exception.
- [x] Run restore/build/test/coverage/report generation/smoke validation.
- [x] Commit and push architecture quality gate.

## Milestone 15: Completion Audit

- [x] Check `docs/prd.md` and `total_todolist.md` for conflicts.
- [x] Confirm the only unchecked TODO is physical Android resource measurement.
- [x] Search source, tests, tools, Android, and docs for hidden TODO/FIXME/HACK/NotImplemented markers.
- [x] Check Android device availability with `adb devices -l`.
- [x] Rerun .NET restore/build/test.
- [x] Rerun coverage collection and report generation.
- [x] Rerun Windows smoke tool.
- [x] Rerun Android unit tests and debug build.
- [x] Document completion audit and the external physical-device blocker.
- [x] Commit and push completion audit.

## Milestone 16: Local WPF UI Snapshot Automation

- [x] Add stable MainWindow AutomationIds for local UI automation.
- [x] Add Refresh command/button for snapshot interaction.
- [x] Add WPF app smoke test that verifies snapshot AutomationIds.
- [x] Create `tools/Woong.MonitorStack.Windows.UiSnapshots` with FlaUI.
- [x] Launch the WPF app from the snapshot tool.
- [x] Capture `01-startup.png`.
- [x] Click Refresh and capture `02-dashboard-after-refresh.png`.
- [x] Change period and capture `03-dashboard-period-change.png`.
- [x] Navigate to Settings and capture `04-settings.png`.
- [x] Save snapshots under `artifacts/ui-snapshots/<timestamp>/`.
- [x] Update `artifacts/ui-snapshots/latest/`.
- [x] Generate `report.md` with pass/fail notes.
- [x] Add optional region crops where visible.
- [x] Add `scripts/run-ui-snapshots.ps1`.
- [x] Ignore UI snapshot and diff artifacts.
- [x] Document local-only UI snapshot testing in `docs/ui-snapshot-testing.md`.
- [x] Document remaining UI automation gaps: visual regression gate, CI, installer flow, multi-DPI/theme matrix, strict pixel diffing, and more reliable chart-area crop support.
- [x] Verify `scripts/run-ui-snapshots.ps1` creates `artifacts/ui-snapshots/latest/report.md` and primary screenshots.
- [x] Run WPF app tests after AutomationId additions.
- [x] Run solution restore/build/test after adding the snapshot tool.
- [x] Commit and push local WPF UI snapshot automation.

## Milestone 17: WPF UI Expectation Test Coverage

- [x] Create WPF UI test plan in `docs/wpf-ui-test-plan.md`.
- [x] Add XAML-level tests for MainWindow title, DataContext, refresh command, and period command bindings.
- [x] Add UI tests that execute Today, 1h, 6h, and 24h period commands through bound buttons.
- [x] Add UI tests for summary card labels/values rendered from sample dashboard data.
- [x] Add UI tests for chart captions and chart controls.
- [x] Add UI tests for App Sessions, Web Sessions, and Live Event Log tabs and DataGrid columns.
- [x] Add UI tests for Settings tab collection visibility, sync opt-out, mode label, and status label.
- [x] Add stable AutomationIds for chart controls and settings controls.
- [x] First WPF App test run failed on missing AutomationIds and command invocation coverage as expected for TDD RED.
- [x] WPF App tests pass after XAML/test fixes.
- [x] Solution restore succeeds.
- [x] Solution build succeeds.
- [x] Solution tests pass.
- [x] Local WPF UI snapshot script succeeds and writes `artifacts/ui-snapshots/latest/report.md`.
- [x] Commit and push WPF UI expectation test coverage.

## Milestone 18: Coding Guide

- [x] Review current constraints from `AGENTS.md`, `docs/prd.md`, architecture docs, contracts docs, and installed skill guidance.
- [x] Add `docs/coding-guide.md` with project-wide coding, testing, privacy, architecture, platform, verification, TODO, and commit/push rules.
- [x] Run solution build after documentation update.
- [x] Run solution tests after documentation update.
- [x] Update resume state for the coding guide slice.
- [x] Commit and push coding guide slice.

## Milestone 19: WPF C# Coding Guide

- [x] Add a dedicated `docs/wpf-csharp-coding-guide.md` for WPF/C# architecture, MVVM, project layering, and placement rules.
- [x] Link the dedicated WPF/C# guide from `docs/coding-guide.md`.
- [x] Run solution build after WPF/C# guide update.
- [x] Run solution tests after WPF/C# guide update.
- [x] Update resume state for the WPF/C# guide slice.
- [x] Commit and push WPF/C# guide slice.

## Milestone 20: Original Intent Restoration Planning And Boundaries

- [x] Reopen `total_todolist.md` for Original Intent Restoration.
- [x] Document original product intent in `docs/original-product-intent-audit.md`.
- [x] Document metadata-only privacy boundaries in `docs/privacy-boundaries.md`.
- [x] Document runtime pipelines and acceptance modes in `docs/runtime-pipeline.md`.
- [x] Document browser capture policy in `docs/browser-tracking-policy.md`.
- [x] Document WPF semantic UI acceptance in `docs/wpf-ui-acceptance-checklist.md`.
- [x] Document Android screenshot/device automation scope in `docs/android-ui-screenshot-testing.md`.
- [x] Document future macOS feasibility only in `docs/future-macos-feasibility.md`.
- [x] Update `AGENTS.md` and coding guide with metadata-only reminder.
- [x] Ignore Android UI screenshot artifacts.
- [x] Run .NET restore/build/test after Original Intent Restoration docs.
- [x] Update resume state after Original Intent Restoration docs.
- [x] Commit and push Original Intent Restoration planning slice.

## Milestone 21: Windows Live Tracking UI Restoration

- [x] Add failing WPF App tests for required tracking AutomationIds: `StartTrackingButton`, `StopTrackingButton`, `SyncNowButton`, `TrackingStatusText`, `CurrentAppNameText`, `CurrentProcessNameText`, `CurrentWindowTitleText`, `CurrentSessionDurationText`, `LastPersistedSessionText`, `LastSyncStatusText`.
- [x] Add presentation state for tracking status, current app/process/window title, current duration, last persisted session, and last sync status.
- [x] Add WPF Start/Stop/Sync Now controls and current activity panel.
- [x] Add privacy-aware window title masking setting and tests.
- [x] Verify Start/Stop UI can transition using a fake tracking coordinator.
- [x] Run WPF App tests and solution build.
- [x] Commit and push Windows live tracking UI slice.

## Milestone 22: Windows Tracking Pipeline Persistence And Dashboard

- [x] Add failing tests for app-hosted tracking start/stop wiring through DI.
- [x] Wire `TrackingPoller` into a visible WPF tracking coordinator.
- [x] Test a foreground change closes the previous session and starts a new one through the app pipeline.
- [x] Persist closed focus sessions to Windows local SQLite.
- [x] Enqueue focus session outbox records after persistence.
- [x] Replace `EmptyDashboardDataSource` with a SQLite-backed dashboard data source.
- [x] Show current and recently persisted activity in WPF.
- [x] Add real coordinator timestamp evidence: Start/Poll/Stop snapshots expose `LastPollAtUtc`, and persistence snapshots expose `LastDbWriteAtUtc`.
- [x] Runtime timestamp verification: RED `PollOnce_WhenForegroundChanges_ReturnsLastPollAndLastDbWriteTimes` failed first on missing `LastPollAtUtc`.
- [x] Runtime timestamp verification: focused coordinator tests passed (5 tests).
- [x] Runtime timestamp verification: all Windows App tests passed (55 tests).
- [x] Runtime timestamp verification: full `.NET` tests passed (211 tests), `.NET` build passed, WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-145334`, and coverage report generated successfully with overall line coverage 92.0%.
- [x] Add RealStart local validation script `scripts/run-wpf-real-start-acceptance.ps1` with the required privacy warning.
- [x] Ensure RealStart uses a temp DB and does not upload unless `--AllowServerSync` is provided.
- [x] Run focused tests, WPF smoke, solution build/test.
- [x] Commit and push Windows tracking pipeline slice.

## Milestone 23: Browser Domain Tracking Restoration

- [x] Add `BrowserActivitySnapshot`, `CaptureMethod`, and `CaptureConfidence` domain/infrastructure models.
- [x] Add `IBrowserProcessClassifier`, `IBrowserActivityReader`, `IBrowserUrlSanitizer`, and `IWebSessionizer`.
- [x] Test supported browsers: `chrome.exe`, `msedge.exe`, `firefox.exe`, `brave.exe`.
- [x] Test non-browser process does not create a WebSession.
- [x] Test fake Chrome URL `github.com` creates a WebSession.
- [x] Test URL change from `github.com` to `chatgpt.com` closes/starts WebSessions.
- [x] Test URL unavailable falls back to FocusSession only.
- [x] Test domain-only privacy stores domain but not full URL.
- [x] Test full URL is stored only with explicit opt-in.
- [x] Persist WebSessions to SQLite with capture method/confidence/private indicators.
- [x] Create WebSession outbox items and upload payloads with domain/duration.
- [x] Verify duplicate WebSession uploads are idempotent.
- [x] Add native messaging host manifest generation.
- [x] Add Chrome native host runner, console host project, persistent extension
  connection, and install script for real extension/native-message ingestion.
- [x] Add WPF browser connection status after higher-priority non-UI tracking/schema work.
- [x] Document browser-domain capture status semantics for the
  current slice: Administrator is not enough; missing domain is a
  capture-connection status, not a privacy block.
- [x] Register a metadata-only WPF UI Automation address-bar fallback so
  domain-only metadata can appear immediately when the active browser exposes a
  recognizable address bar.
- [x] Keep extension/native messaging documented as the stable browser capture
  path while UI Automation remains best-effort.
- [x] Add WPF browser connection/status UI that distinguishes extension
  connected, UI Automation fallback active, unavailable, and error states.
- [x] Add URL sanitizer/redaction policy before storing raw browser events.
- [x] Harden Chrome native acceptance cleanup-only path so it runs before Chrome for Testing resolution and does not require browser discovery.
- [x] Prevent cleanup-only native-host restore/remove from running twice; the `finally` cleanup now skips native-host uninstall if the cleanup-only branch already performed it.
- [x] Chrome cleanup-only verification: RED `AcceptanceScript_CleanupOnlyRunsBeforeChromeResolution` failed first, then passed.
- [x] Chrome cleanup-only verification: RED `AcceptanceScript_CleanupOnlyDoesNotRunNativeHostCleanupTwice` failed first, then passed.
- [x] Chrome cleanup-only dry run passed with scoped HKCU test key output and sandbox temp-profile-only Chrome process cleanup.
- [x] Chrome cleanup-only verification: full `.NET` tests passed (303 tests).
- [x] Chrome cleanup-only verification: `.NET` build passed.
- [x] Chrome cleanup-only verification: coverage report generated successfully with overall line coverage 91.3%.
- [x] Run browser/domain tests, Windows tests, solution build/test.
- [x] Commit and push browser domain tracking non-UI slice through native manifest generation.

## Milestone 24: Integrated Schema Restoration

- [x] Add schema tests for required focus session process/window fields.
- [x] Add schema tests for required web session browser/capture/privacy fields.
- [x] Add server `device_state_sessions` table/entity/tests.
- [x] Add server `app_families` and `app_family_mappings` tables/entities/tests.
- [x] Decide and document whether Android app usage remains focus sessions or gets a dedicated app usage upload contract.
- [x] Update DTO contracts for nullable URL, domain, capture method/confidence, process/window metadata, and client idempotency.
- [x] Generate/review EF migration for restored schema.
- [x] Update production migration notes.
- [x] Run server relational tests and solution build/test.
- [x] Commit and push integrated schema restoration focus/browser metadata slice.
- [x] Commit and push integrated schema restoration device-state/app-family slice.
- [x] Commit and push Android app usage contract decision slice.

## Milestone 25: WPF Semantic UI Acceptance

- [x] Add WPF App semantic test proving Start/Stop buttons drive fake foreground tracking into temp SQLite/outbox and dashboard renders the persisted session from SQLite.
- [x] Add testable `PollTrackingCommand` and WPF timer path so Running tracking advances current session duration beyond zero.
- [x] Add WPF App semantic test proving fake browser github/chatgpt web sessions render in the Web Sessions tab from SQLite.
- [x] Add WPF App semantic test proving minimum-size windows provide scrolling while keeping dashboard tabs reachable.
- [x] Create `scripts/run-wpf-ui-acceptance.ps1`.
- [x] Upgrade `tools/Woong.MonitorStack.Windows.UiSnapshots` or add a new tool for semantic FlaUI checks.
- [x] Rework dashboard vertical layout or add scrolling so App Sessions, Web Sessions, and Live Event Log are not cramped below the current activity and chart areas.
- [x] Implement EmptyData mode acceptance.
- [x] Implement SampleDashboard mode acceptance.
- [x] Implement TrackingPipeline mode with fake foreground/browser readers and temp SQLite.
- [x] TrackingPipeline/RealStart acceptance tolerate product auto-start when the app is already Running.
- [x] EmptyData snapshot mode explicitly disables product auto-start so empty-state evidence remains deterministic.
- [x] EmptyData mode verifies temp SQLite `focus_session`, `web_session`, and `sync_outbox` row counts stay zero.
- [x] TrackingPipeline mode verifies temp SQLite `focus_session`, `web_session`, and `sync_outbox` row counts.
- [x] Add WPF architecture guard that `MainWindow.xaml` remains a thin `DashboardView` shell.
- [x] Add WPF architecture guard that `DashboardView.xaml` composes reusable section controls inside a vertical `ScrollViewer`.
- [x] Add WPF App guard for stable dashboard AutomationIds used by UI acceptance.
- [x] Add WPF snapshot-tool guard that runtime selectors are present in semantic pass/fail evidence.
- [x] Verify Start changes tracking status to Running.
- [x] Verify fake pipeline shows Visual Studio Code, Chrome, `github.com`, and `chatgpt.com`.
- [x] Verify Stop changes tracking status to Stopped.
- [x] Verify Sync Now updates last sync status using a fake sync client.
- [x] Verify StartTracking immediately attempts sync and reports local-only skipped status while sync is off.
- [x] Browser-domain empty state copy no longer implies privacy is the blocker; it says browser capture must be connected while app focus is tracked.
- [x] Capture required screenshots: startup, after start, generated activity, after stop, after sync, settings, current activity, summary cards, sessions, web sessions, live events, and chart area when visible.
- [x] Generate `report.md`, `manifest.json`, and `visual-review-prompt.md`, including TrackingPipeline SQLite evidence.
- [x] Keep screenshot review local-only and optional for GPT/human review.
- [x] Run current composed RealStart + UI snapshot WPF acceptance tool locally.
- [x] Run full semantic WPF acceptance tool locally after fake TrackingPipeline/report upgrades.
- [x] Commit and push WPF semantic acceptance slice.

## Milestone 26: Android Usage Tracking Restoration

- [x] Add UI Automator dependency and Usage Access settings navigation smoke test.
- [x] Add persisted sync opt-in setting and default-off enforcement.
- [x] Wire WorkManager periodic scheduling for usage collection only when allowed and visible.
- [x] Enqueue sync outbox rows when UsageStats sessions are collected.
- [x] Ensure sync worker refuses/suppresses upload unless sync opt-in is true.
- [x] Review and disable/constrain `android:allowBackup` for local usage metadata.
- [x] Make SessionsActivity display real Room-backed sessions.
- [x] Make DailySummaryActivity load previous-day summary through repository/client rather than intent extras only.
- [x] Add Android 13+ notification permission UX before morning summary notifications.
- [x] Run Android unit tests and debug build for Android sync opt-in slice.
- [x] Run Android unit tests and debug build for Android collection outbox slice.
- [x] Run Android unit tests and debug build for Android backup hardening slice.
- [x] Run Android unit tests/debug build and .NET regression for Android WorkManager scheduling slice.
- [x] Run Android unit tests/debug build and .NET regression for Room-backed SessionsActivity slice.
- [x] Run Android unit tests/debug/androidTest build and .NET regression for DailySummaryActivity repository/client slice.
- [x] Run Android unit tests/debug/androidTest build and .NET regression for notification permission UX slice.
- [x] Run Android unit tests/debug/androidTest build and .NET regression for Usage Access UI Automator smoke slice.
- [x] Check connected Android device availability for Usage Access UI Automator smoke; blocked because `adb devices -l` returned no attached devices.
- [x] Run .NET regression tests and coverage report for Android usage restoration slices; current .NET line coverage is 92.4%.
- [x] Commit and push Android sync opt-in enforcement slice.
- [x] Commit and push Android backup hardening slice.
- [x] Commit and push Android WorkManager scheduling slice.
- [x] Commit and push Room-backed SessionsActivity slice.
- [x] Commit and push DailySummaryActivity repository/client slice.
- [x] Commit and push notification permission UX slice.
- [x] Commit and push Usage Access UI Automator smoke slice.
- [x] Commit and push Android usage restoration slice.

## Milestone 27: Android UI Screenshot And Device Automation

- [x] Add `docs/android-ui-screenshot-testing.md` follow-up implementation notes after tooling exists.
- [x] Add local Android screenshot script/tool that writes `artifacts/android-ui-snapshots/<timestamp>/`.
- [x] Generate Android `report.md`, `manifest.json`, and `visual-review-prompt.md`.
- [x] Implement connected-device script branch that installs/launches the app and captures dashboard/settings/sessions/daily-summary screenshots when a device is available.
- [x] Capture dashboard, settings, sessions, and daily summary screens on the `Medium_Phone` emulator.
- [x] Seed deterministic sample app usage where possible through androidTest `SnapshotSeedTest`.
- [x] Run screenshot flow availability check; initial no-device run wrote
  blocked evidence, then `Medium_Phone` emulator run passed and generated
  `artifacts/android-ui-snapshots/20260430-091721` plus latest report/manifest.
- [x] Verified Android snapshot slice with focused architecture tests, full
  `.NET` tests (249), full `.NET` build, coverage generation (91.2% line),
  `testDebugUnitTest`, `assembleDebug`, and `assembleDebugAndroidTest`.
- [x] Accept emulator-backed Android resource measurements as complete for the current environment; physical-device measurement is optional future hardening.
- [x] Commit and push Android screenshot/device automation slice.

## Milestone 27.5: Android Optional Location Context UI

- [x] Add Android UI plan for optional latitude/longitude location context.
- [x] Add architecture documentation guardrail test for Android latitude/longitude opt-in wording.
- [x] Verify Android location plan slice with Architecture tests, full .NET tests, full .NET build, WPF acceptance after XAML resource changes, coverage generation (91.3% line), and Android `testDebugUnitTest assembleDebug`.
- [x] Update PRD/privacy/screenshot docs so ?꾨룄/寃쎈룄 is explicit opt-in metadata, not default tracking.
- [x] Add Android settings tests: location context defaults Off, approximate mode preferred, precise latitude/longitude requires explicit opt-in.
- [x] Add Android foreground-only manifest guard for optional location context: coarse/fine permissions allowed, background location forbidden.
- [x] Add Android permission UI tests: location permission request remains disabled until location context opt-in and guidance shows no coordinates by default.
- [x] Add Android location permission policy/controller tests: approximate requests coarse only, precise latitude/longitude requests fine after separate opt-in.
- [x] Add no-hardware runtime location capture policy/provider seam: no snapshot unless location context is enabled and foreground permission is granted; approximate mode keeps precise coordinates null; precise coordinates require separate precise opt-in.
- [x] Add no-hardware local location-context collection/persistence runner: provider snapshot writes Room-facing `LocationContextSnapshotEntity` and enqueues `location_context` outbox; null provider writes nothing.
- [x] Wire no-hardware WorkManager scheduling through the existing usage collection worker so it invokes the local location-context collector, reports captured/skipped, and leaves sync upload to the sync worker.
- [x] Add Room/component tests for nullable `latitude`, `longitude`, `accuracyMeters`, and `capturedAtUtc` storage.
- [x] Add Dashboard tests for location status card and fake opt-in latitude/longitude display.
- [x] Ensure sync payload excludes location while sync is off and includes nullable coordinates only when both sync and location opt-in are enabled.
- [x] Add server `location_contexts` upload contract, nullable coordinate storage, idempotency tests, and PostgreSQL migration.
  - [x] Verify server location context tests, full `.NET` tests/build, and coverage generation at 91.3% line coverage.
- [x] Integrate Android sync runner/client with server `location_contexts` upload endpoint while preserving sync/location opt-in gates.
  - [x] Add Android sync API/client method for `/api/location-contexts/upload`.
  - [x] Align Android location-context upload payload with the server contract: top-level `deviceId` and `contexts` with `clientContextId`, UTC/local date, timezone, nullable coordinates, capture mode, permission state, and source.
  - [x] Wire local location-context outbox/worker processing into the sync runner while preserving sync opt-in and location opt-in gates.
  - [x] Add no-hardware Robolectric worker coverage proving pending `location_context` rows stay local while sync is off, upload only when sync and location opt-in are both enabled, and retry failed location uploads.
  - [x] Verify the WorkManager `AndroidSyncWorker` runtime path with a real `AndroidOutboxSyncProcessor` fake-outbox setup for sync-off skip, accepted location upload, and failed-upload retry.
- [x] Add Android UI snapshot script/androidTest coverage for Dashboard location card and Settings location section.
  - [x] Generate no-device `BLOCKED` snapshot artifacts that explicitly list expected location-card/settings-section checks.
  - [x] Capture real connected-device screenshots for Dashboard location card and Settings location section on the `Medium_Phone` emulator.
- [x] Commit and push Android optional location context slices.

## Milestone 27.6: Android Launcher And Resource Measurement Evidence

- [x] Add RED Robolectric launcher test proving `MainActivity` immediately opens `DashboardActivity` and finishes, so a normal app launch starts at the dashboard.
- [x] Keep `DashboardActivity` non-exported and expose an internal `createIntent(...)` factory for launcher routing.
- [x] Add `scripts/run-android-resource-measurement.ps1` for local package-scoped process, memory, and graphics diagnostics.
- [x] Add architecture tests for the Android resource measurement script, including no-device `BLOCKED` artifacts and connected-device fake ADB behavior.
- [x] Verify resource measurement on the `Medium_Phone` emulator with artifact `artifacts/android-resource-measurements/20260430-093728`.
- [x] Document the local-only resource measurement contract in `docs/android-resource-measurement.md` and update completion/resume docs.
- [x] Verify Android launcher/resource slice with focused Gradle launcher test, Android unit/build/connected tests, focused architecture script tests, temp-output resource script run, full `.NET` tests/build, and coverage generation.
- [x] Harden `scripts/run-android-resource-measurement.ps1 -SkipBuild` so a missing app launcher writes a clear `BLOCKED` report before invoking `monkey`.
- [x] Verify resource measurement SkipBuild hardening with focused RED/GREEN script test, full AndroidResourceMeasurementScriptTests, full `.NET` tests/build, and coverage generation.
- [x] Accept emulator-backed Android resource measurements as complete for the current environment; physical-device measurement is optional future hardening.

## Milestone 27.7: Android Wireframe XML Alignment

- [x] Add RED architecture tests for Android Dashboard/Settings wireframe card, chip, scroll, and navigation structure.
- [x] Add RED architecture tests for readable period button styling and card-based Sessions/Daily Summary layouts.
- [x] Preserve Activity/ViewBinding runtime contracts while aligning XML to the provided Android wireframe skeleton.
- [x] Add shared Android `wms_*` color tokens, `WmsCard`, status chip, section title, key/value, and period button styles.
- [x] Rework Dashboard XML into card/chip/period/current-focus/summary/chart/recent-session sections.
- [x] Rework Settings XML into grouped Permissions, Sync, Privacy, Location, and Storage cards.
- [x] Rework Sessions and Daily Summary XML into product card screens instead of debug-like plain layouts.
- [x] Extend Android screenshot automation and androidTest capture flow to produce numbered feature screenshots `01` through `08`.
- [x] Capture connected-emulator screenshots at `artifacts/android-ui-snapshots/20260430-110459`.
- [x] Verify Android wireframe XML alignment with focused architecture tests, full `.NET` tests (368), full `.NET` build, Android `testDebugUnitTest assembleDebug assembleDebugAndroidTest`, connected-emulator screenshot capture, and coverage generation at 91.7% line coverage.
- [x] Document remaining UI polish gaps: reusable rounded summary cards and structured session row items.
- [x] Fix Android primary Activity layouts so Dashboard, Sessions, Settings, and Daily Summary avoid status-bar overlap.
- [x] Verify session row/system-bar polish with focused architecture tests, focused RoomSessionsRepository test, Android `testDebugUnitTest assembleDebug assembleDebugAndroidTest`, connected-emulator screenshots at `artifacts/android-ui-snapshots/20260430-112341`, full `.NET` tests (371), full `.NET` build, and coverage generation at 91.7% line coverage.
- [x] Follow-up: convert Android summary tiles to rounded Material card containers.
- [x] Follow-up: convert Android sessions RecyclerView rows to structured app/package/time/duration rows.
- [x] Follow-up: migrate launcher `MainActivity` to a `FragmentContainerView` + `BottomNavigationView` shell while preserving existing Activity runtime surfaces.
- [x] Add fragment XML skeletons for Splash, Permission onboarding, Dashboard, Sessions, App detail, Report, and Settings based on the provided Android wireframe skeleton.
- [x] Add RED/GREEN tests proving the Android shell, fragment layout resources, compact toolbar title style, distinct fragment summary labels, and shell screenshot contract.
- [x] Capture connected-emulator screenshots at `artifacts/android-ui-snapshots/20260430-115054`, including `09-main-shell.png`.
- [x] Wire `DashboardFragment` to `DashboardViewModel`/`RoomDashboardRepository` and remove hardcoded fake runtime data from `fragment_dashboard.xml`.
- [x] Verify `09-main-shell.png` shows seeded Room-backed values (`com.android.chrome`, non-zero Active/Screen/Idle totals) at `artifacts/android-ui-snapshots/20260430-120317`.
- [x] Follow-up: wire the new Sessions, Report, and Settings fragments to the same runtime behavior as the existing Activity screens.

## Milestone 28: Privacy And Retention Hardening

- [x] Add tests proving forbidden scopes are not represented by permissions, services, or product code.
- [x] Add browser raw event retention enforcement.
- [x] Add client-side raw event retention policy for Windows local SQLite.
- [x] Add UI copy for browser URL/domain privacy levels.
- [x] Add sync opt-in enforcement tests for Windows and Android.
- [x] Verify UI screenshot tools only capture this app's UI.
- [x] Run full validation matrix.
- [x] Commit and push privacy hardening slice.

## Milestone 29: Original Intent Completion Gate

- [x] Windows real foreground process/window tracking works from WPF Start/Stop.
- [x] Windows local SQLite persistence and outbox are proven through fake pipeline and RealStart local validation.
- [x] Browser domain tracking is explicit, privacy-aware, and covered by tests.
- [x] Android UsageStats collection, Room persistence, WorkManager scheduling, and sync opt-in are proven.
- [x] Server schema supports required relationships and idempotent integrated storage.
- [x] Server WebSession idempotency hardening: `WebSessionUploadItem` now
  requires `clientSessionId`, server `web_sessions` has required
  `ClientSessionId`, upload duplicate detection uses `deviceId +
  clientSessionId`, and production migration
  `20260429101507_AddWebSessionClientSessionId` backfills legacy rows before
  enforcing unique `(DeviceId, ClientSessionId)`.
- [x] Server idempotency verification: RED contract/model/migration tests were
  added first, relational SQLite duplicate enforcement passed for domain-only
  web sessions, full `.NET` tests passed (277), `.NET` build passed with 0
  warnings/errors, and coverage generated at 91.2% line coverage.
- [x] Server relationship constraint hardening: RED model tests first proved
  missing Device FKs and missing WebSession-to-FocusSession linkage; server
  schema now enforces focus/web/raw/device-state session `DeviceId` FKs and
  `web_sessions(DeviceId, FocusSessionId)` to
  `focus_sessions(DeviceId, ClientSessionId)` with restrict delete behavior.
- [x] Server relationship verification: migration
  `20260429102602_AddServerSessionForeignKeys` added the FK constraints,
  migration contract tests verify the generated names and restrict behavior,
  relational SQLite tests prove missing parent rows are rejected, full `.NET`
  tests passed (281), `.NET` build passed with 0 warnings/errors, and coverage
  generated at 91.3% line coverage.
- [x] WPF semantic UI acceptance passes with expected content.
- [x] Android UI screenshot/device automation evidence is generated or blocked only by unavailable device.
- [x] Unsafe/impossible/out-of-scope features are documented and not implemented.
- [x] Full .NET tests/build pass.
- [x] Android tests/build pass.
- [x] Completion audit updated.
- [x] Commit and push Original Intent completion.

## Milestone 30: WPF Product UI Goal Implementation

- [x] Save the 2026-04-29 WPF product UI goal as `docs/wpf-ui-plan.md`.
- [x] Preserve the provided UI goal image intent in durable docs and use it as the visual reference for review.
- [x] Add failing Header tests: `Header_DoesNotOverlapTitleAndCurrentProcess`, `Header_ShowsTrackingSyncPrivacyBadges`, and `Header_At1024Width_RemainsReadable`.
- [x] Remove current process/top app text from the Header and move runtime focus state into the Current Focus panel.
- [x] Add Header tracking/sync/privacy badges with stable AutomationIds and responsive layout.
- [x] Add failing Control Bar tests for readable button sizing, wrapping/reachability, and the visible Custom period control.
- [x] Update Control Bar buttons to readable product labels: Start Tracking, Stop Tracking, Refresh, Sync Now, Today, 1h, 6h, 24h, Custom.
- [x] Add Current Focus panel tests for current domain, last poll time, last DB write time, last persisted session, and privacy-aware window title.
- [x] Add Current Focus UI fields for tracking status, current app, current process, current window title, current browser domain, current duration, last persisted session, last poll, last DB write, and sync state.
- [x] Add or verify testable ticker behavior proving current session duration advances while tracking is Running.
- [x] Add Summary Card tests for Active Focus, Foreground, Idle, and Web Focus labels and SQLite-backed non-zero/empty behavior.
- [x] Replace summary cards with Active Focus, Foreground, Idle, and Web Focus using clear values and subtitles.
- [x] Add Chart mapper tests for meaningful hour labels, minute axis labels, app/domain labels, durations, and empty states.
- [x] Update LiveCharts data and XAML bindings to avoid meaningless `-0.5 / 0 / 0.5` axes.
- [x] Add visible empty-state overlays for all chart panels.
- [x] Add App Sessions grid tests for required columns, readable widths, horizontal scrolling, and fake pipeline VS Code/Chrome rows.
- [x] Update App Sessions grid columns: App, Process, Start, End, Duration, State, Window, Source with required MinWidths.
- [x] Add Web Sessions grid tests for required columns, readable widths, domain-only privacy, empty state, and fake github/chatgpt rows.
- [x] Update Web Sessions grid columns: Domain, Title, URL Mode, Start, End, Duration, Browser, Confidence with required MinWidths.
- [x] Add Live Event Log tests for Tracking started, FocusSession closed/started, WebSession closed/started, persisted/outbox, sync skipped, and stopped events.
- [x] Update Live Event Log columns: Time, Event Type, App, Domain, Message.
- [x] Add Settings tests for readable privacy controls, full URL off by default, sync off by default, local-only status, and local DB/log folder commands disabled or available.
- [x] Add Settings tests for Capture page title off by default, domain-only browser storage on by default, sync endpoint disabled until opt-in, and guarded Clear local data disabled.
- [x] Update Settings tab with privacy, sync, and runtime controls/copy while keeping safe defaults.
- [x] Make the main layout usable at 1024 width with reachable tabs and no clipped grid headers.
- [x] Make the main layout explicitly verified at 1920 and 1366 widths with screenshots.
- [x] Update `scripts/run-wpf-ui-acceptance.ps1` and/or UI snapshot tool to capture 1920/1366/1024 screenshots and section screenshots.
- [x] Viewport acceptance verification: RED script/tool contract tests failed first on missing `--viewport-widths`, viewport screenshot names, bring-into-view path, and manifest metadata.
- [x] Viewport acceptance verification: `tools/Woong.MonitorStack.Windows.UiSnapshots` supports `--viewport-widths` and captures 1920/1366/1024 dashboard plus section screenshots.
- [x] Viewport acceptance verification: manifest includes `viewportWidths` and `skippedScreenshotReasons`.
- [x] Viewport acceptance verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-144429` with no skipped screenshots.
- [x] Viewport acceptance verification: full `.NET` tests passed (210 tests), `.NET` build passed, and coverage report generated successfully with overall line coverage 91.9%.
- [x] Ensure WPF semantic acceptance verifies SQLite focus/web/outbox rows plus UI values for VS Code, Chrome, `github.com`, and `chatgpt.com`.
- [x] TrackingPipeline SQLite evidence verification: UiSnapshots checks temp `focus_session`, `web_session`, and `sync_outbox` row counts.
- [x] TrackingPipeline SQLite evidence verification: `report.md` and `manifest.json` include the semantic DB checks and statuses.
- [x] TrackingPipeline SQLite evidence verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-155615`.
- [x] EmptyData SQLite evidence verification: `run-ui-snapshots.ps1` passed and reported zero `focus_session`, `web_session`, and `sync_outbox` rows in `artifacts/ui-snapshots/latest`.
- [x] SampleDashboard acceptance verification: RED tests added
  `WindowsAppAcceptanceMode.SampleDashboard`, deterministic
  `SampleDashboardDataSource`, snapshot-tool `--mode SampleDashboard`, and
  `scripts/run-ui-snapshots.ps1 -Mode SampleDashboard`; full `.NET` tests
  passed (247), `.NET` build passed with 0 warnings/errors, coverage generated
  at 91.2% line coverage, SampleDashboard snapshots passed at
  `artifacts/ui-snapshots/latest`, WPF UI acceptance passed at
  `artifacts/wpf-ui-acceptance/20260429-172417`, and the Windows smoke tool
  reported real foreground metadata only.
- [x] Update `docs/wpf-ui-acceptance-checklist.md` after UI implementation.
- [x] Update `docs/runtime-pipeline.md` for last poll time, last DB write time, current duration, and flush behavior if changed.
- [x] Update `docs/browser-tracking-policy.md` for the browser-domain-not-connected safe privacy UI label.
- [x] Document that Administrator rights are not a reliable active-tab URL capture solution; explicit browser integration remains required.
- [x] Update browser-domain docs for current capture-status copy,
  immediate domain-only display when capture is available, production
  extension/native messaging path, and best-effort UI Automation fallback.
- [x] Run focused tests proving immediate Start browser-domain display,
  production browser reader DI registration, and address-bar fallback behavior.
- [x] Browser immediate capture verification: full `.NET` tests passed (233),
  `.NET` build passed with 0 warnings/errors, coverage generated at 90.6% line
  coverage, WPF UI acceptance passed at
  `artifacts/wpf-ui-acceptance/20260429-163949`, and the Windows smoke tool
  reported current foreground metadata without capturing keystrokes or screen
  contents.
- [x] Browser connection status UI verification: RED tests added
  `DashboardBrowserCaptureStatus` and `BrowserCaptureStatusText`; UI now
  distinguishes unavailable, extension connected, address-bar fallback active,
  and capture error states without overloading the domain value.
- [x] Browser connection status verification: full `.NET` tests passed (238),
  `.NET` build passed with 0 warnings/errors, coverage generated at 90.6% line
  coverage, WPF UI acceptance passed at
  `artifacts/wpf-ui-acceptance/20260429-164942`, and the Windows smoke tool
  reported foreground metadata only.
- [x] Native browser host verification: RED tests added
  `ChromeNativeMessageHostRunner`, persistent `connectNative` extension
  behavior, and `scripts/install-chrome-native-host.ps1`; full `.NET` tests
  passed (241), `.NET` build passed with 0 warnings/errors, coverage generated
  at 90.8% line coverage, and WPF UI acceptance passed at
  `artifacts/wpf-ui-acceptance/20260429-170610`; the Windows smoke tool
  reported real Chrome foreground metadata without content capture.
- [x] Chrome native messaging safety hardening: acceptance now uses a temp
  Chrome profile, stops only sandbox Chrome processes whose command line
  contains that temp profile, rejects blank/malformed native host names before
  registry paths are built, writes dry-run artifacts under
  `artifacts/chrome-native-acceptance/latest`, and requires an explicit temp
  DB during acceptance so the native host cannot silently fall back to the
  user's real local DB.
- [x] Chrome native messaging safety docs added at
  `docs/chrome-native-messaging-acceptance.md`.
- [x] Chrome native messaging safety verification: focused script/registry/
  parser/receiver tests passed (19), dry-run acceptance passed without
  launching Chrome or writing HKCU values, full `.NET` tests passed (267),
  `.NET` build passed with 0 warnings/errors, and coverage generated at 91.2%
  line coverage.
- [x] Fixed full headed Chrome native-message acceptance by using Chrome for
  Testing from `.cache/chrome-for-testing`, quoting host resolver rules,
  enumerating SQLite JSON rows correctly in PowerShell, and redacting page
  titles in DomainOnly mode so outbox payloads do not leak paths.
- [x] Chrome native-message acceptance passed at
  `artifacts/chrome-native-acceptance/20260429-185325`: extension/native host
  wrote `github.example` and `chatgpt.example` domain-only web sessions plus
  pending outbox rows to temp SQLite, full URL/page title remained null,
  cleanup removed only the scoped HKCU test host key, and no user Chrome
  process was stopped.
- [x] Chrome native-message completion verification: full `.NET` tests passed
  (271), `.NET` build passed with 0 warnings/errors, and coverage generated at
  91.2% line coverage after the acceptance fix.
- [x] Chrome acceptance sandbox follow-up: default run no longer falls back to
  the user's installed Chrome. It now requires Chrome for Testing from the
  ignored local cache, `-InstallChromeForTesting`, or explicit `-ChromePath`;
  installed Chrome fallback is guarded by explicit
  `-AllowInstalledChromeFallback` for isolated manual debugging only.
- [x] Chrome acceptance dry-run cleanup follow-up: final uninstall/cleanup now
  receives `-DryRun:$DryRun`, so dry-run reports the scoped HKCU key it would
  remove or restore without changing registry values.
- [x] Chrome acceptance sandbox follow-up verification: Chrome-native focused
  tests passed (33), dry-run acceptance passed without launching Chrome or
  writing HKCU values, full Chrome for Testing acceptance passed at
  `artifacts/chrome-native-acceptance/20260429-190639`, scoped HKCU test key
  was absent after cleanup, full `.NET` tests passed (273), `.NET` build passed
  with 0 warnings/errors, and coverage generated at 91.2% line coverage.
- [x] Chrome acceptance sandbox guard follow-up: RED test now requires cleanup
  to refuse non-temp Chrome profile paths before process enumeration, and
  `scripts/run-chrome-native-message-acceptance.ps1` verifies the profile lives
  under the `woong-chrome-native-*` acceptance temp root before stopping any
  Chrome process; focused Chrome script tests, dry-run acceptance, full `.NET`
  tests (289), `.NET` build, and coverage generation (91.3%) passed.
- [x] Browser capture docs-only clarification: `docs/browser-tracking-policy.md`
  and `docs/runtime-pipeline.md` now state that Administrator rights are not a
  reliable active-tab URL capture path, Chrome is the installed
  extension/native-messaging path today, Edge/Brave/Firefox need
  browser-specific native-messaging installers/manifests, domain-only metadata
  should display immediately when reported, and keylogging/page content/full
  URL capture remain out of scope by default. No code or tests changed.
- [x] Live Event Log runtime verification: RED tests added command-level
  coverage for Tracking started, FocusSession closed/started, WebSession
  closed/started, FocusSession/WebSession persisted, outbox row created, sync
  skipped, and Tracking stopped events; full `.NET` tests passed (244), `.NET`
  build passed with 0 warnings/errors, coverage generated at 91.0% line
  coverage, WPF UI acceptance passed at
  `artifacts/wpf-ui-acceptance/20260429-171411`, and the Windows smoke tool
  reported real foreground metadata only.
- [x] Update `docs/resume-state.md` after each completed WPF UI slice.
- [x] Run focused WPF/App tests for each slice.
- [x] Run full `.NET` tests and build.
- [x] Run WPF UI acceptance when the slice touches runtime/UI behavior.
- [x] Commit and push each completed WPF product UI slice.

## Milestone 31: WPF Componentized Dashboard Architecture

- [x] Save componentization guidance in `docs/wpf-ui-plan.md`.
- [x] Add tests proving `MainWindow` is a thin shell that hosts `DashboardView`.
- [x] Add `Views/DashboardView.xaml` and move the vertical dashboard layout out of `MainWindow.xaml`.
- [x] DashboardView extraction verification: focused `MainWindow_ExposesDashboardControlsAndCommandBindings` passed.
- [x] DashboardView extraction verification: all Windows App tests passed (26 tests).
- [x] DashboardView extraction verification: full `.NET` tests passed.
- [x] DashboardView extraction verification: `.NET` build passed.
- [x] DashboardView extraction verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-102017`.
- [x] DashboardView extraction verification: coverage report generated successfully with `scripts/test-coverage.ps1`.
- [x] Add `Views/HeaderStatusBar.xaml` while preserving Header automation IDs and behavior tests.
- [x] HeaderStatusBar extraction verification: focused `DashboardView_HostsHeaderStatusBarAndPreservesHeaderContent` passed.
- [x] HeaderStatusBar extraction verification: all Windows App tests passed (27 tests).
- [x] HeaderStatusBar extraction verification: full `.NET` tests passed.
- [x] HeaderStatusBar extraction verification: `.NET` build passed.
- [x] HeaderStatusBar extraction verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-103315`.
- [x] HeaderStatusBar extraction verification: coverage report generated successfully with overall line coverage 92.3%.
- [x] Add `Views/ControlBar.xaml` while preserving button commands and readable sizing tests.
- [x] ControlBar extraction verification: focused `DashboardView_HostsControlBarAndPreservesCommandBindings` passed.
- [x] ControlBar extraction verification: all Windows App tests passed (28 tests).
- [x] ControlBar extraction verification: full `.NET` tests passed.
- [x] ControlBar extraction verification: `.NET` build passed.
- [x] ControlBar extraction verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-104336`.
- [x] ControlBar extraction verification: coverage report generated successfully with overall line coverage about 92.3%.
- [x] Close runtime poll-tick persistence quality gap before continuing `CurrentFocusPanel`: prove WPF poll-tick foreground changes persist SQLite/outbox rows and refresh the dashboard before Stop.
- [x] Runtime poll-tick gap verification: focused `MainWindowTrackingPipelineTests.PollTick_WhenForegroundChanges_PersistsClosedSessionAndRefreshesDashboardBeforeStop` passed.
- [x] Runtime poll-tick gap verification: all Windows App tests passed (29 tests).
- [x] Runtime poll-tick gap verification: full `.NET` tests passed.
- [x] Runtime poll-tick gap verification: `.NET` build passed.
- [x] Runtime poll-tick gap verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-105310`.
- [x] Runtime poll-tick gap verification: coverage report generated successfully with `scripts/test-coverage.ps1`.
- [x] Add `Views/CurrentFocusPanel.xaml` while preserving current focus automation IDs.
- [x] CurrentFocusPanel extraction verification: focused `DashboardView_HostsCurrentFocusPanelAndPreservesCurrentFocusBindings` passed.
- [x] CurrentFocusPanel extraction verification: `CurrentActivityPanel` and all child AutomationIds were preserved.
- [x] CurrentFocusPanel extraction verification: all Windows App tests passed (30 tests).
- [x] CurrentFocusPanel extraction verification: full `.NET` tests passed.
- [x] CurrentFocusPanel extraction verification: `.NET` build passed.
- [x] CurrentFocusPanel extraction verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-110649`.
- [x] CurrentFocusPanel extraction verification: coverage report generated successfully with overall line coverage 92.4%.
- [x] Add `Views/SummaryCardsPanel.xaml` and reusable `Controls/MetricCard.xaml`.
- [x] SummaryCardsPanel extraction verification: focused `DashboardView_HostsSummaryCardsPanelAndPreservesSummaryCardContent` and `MetricCard_RendersLabelValueAndSubtitle` passed.
- [x] SummaryCardsPanel extraction verification: `DashboardView` hosts `SummaryCardsPanel`, `SummaryCardsContainer` AutomationId was preserved, and summary card content remains intact.
- [x] SummaryCardsPanel extraction verification: all Windows App tests passed (32 tests).
- [x] SummaryCardsPanel extraction verification: full `.NET` tests passed.
- [x] SummaryCardsPanel extraction verification: `.NET` build passed.
- [x] SummaryCardsPanel extraction verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-112103`.
- [x] SummaryCardsPanel extraction verification: coverage report generated successfully with overall line coverage 92.3%.
- [x] Add `Views/ChartsPanel.xaml` and reusable `Controls/EmptyState.xaml`.
- [x] ChartsPanel extraction verification: focused `DashboardView_HostsChartsPanelAndPreservesChartContent` and `EmptyState_RendersBoundTextWithTextAutomationId` passed.
- [x] ChartsPanel extraction verification: `ChartArea`, chart AutomationIds, and empty-state TextBlock AutomationIds were preserved.
- [x] ChartsPanel extraction verification: all Windows App tests passed (34 tests).
- [x] ChartsPanel extraction verification: full `.NET` tests passed.
- [x] ChartsPanel extraction verification: `.NET` build passed.
- [x] ChartsPanel extraction verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-113407`.
- [x] ChartsPanel extraction verification: coverage report generated successfully with overall line coverage 92.3%.
- [x] Resolve `wpfelements.md` Domain Focus chart type mismatch by switching domain focus from `PieChart` to a readable Cartesian/ranking chart.
- [x] Domain chart verification: RED WPF chart-type test failed first against `PieChart`, then Presentation and WPF focused tests passed.
- [x] Domain chart verification: full `.NET` tests passed (204 tests).
- [x] Domain chart verification: `.NET` build passed.
- [x] Domain chart verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-140652`.
- [x] Domain chart verification: coverage report generated successfully with overall line coverage 92.1%.
- [x] Settings privacy coverage verification: RED compile failures exposed missing Settings VM safety properties and missing UI controls.
- [x] Settings privacy coverage verification: added visible/disabled page title capture, domain-only browser storage, sync endpoint, and guarded clear local data controls.
- [x] Settings privacy coverage verification: focused Settings VM and WPF SettingsPanel tests passed.
- [x] Settings privacy coverage verification: full `.NET` tests passed (204 tests).
- [x] Settings privacy coverage verification: `.NET` build passed.
- [x] Settings privacy coverage verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-141606`.
- [x] Settings privacy coverage verification: coverage report generated successfully with overall line coverage 92.1%.
- [x] Add app/domain chart `?곸꽭蹂닿린` actions that select App Sessions and Web Sessions tabs.
- [x] Add tests: `AppChartDetailsCommand_SelectsAppSessionsTab` and `DomainChartDetailsCommand_SelectsWebSessionsTab`.
- [x] Chart details verification: `ShowAppFocusDetailsCommand_SelectsAppSessionsTab`, `ShowDomainFocusDetailsCommand_SelectsWebSessionsTab`, and `DashboardView_ChartDetailButtonsSelectExpectedDetailsTabs` passed.
- [x] Chart details verification: full `.NET` tests passed (189 tests).
- [x] Chart details verification: `.NET` build passed.
- [x] Chart details verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-114947`.
- [x] Chart details verification: coverage report generated successfully with overall line coverage 92.3%.
- [x] Add `Views/DetailsTabsPanel.xaml` while preserving App/Web/Live/Settings tab automation IDs.
- [x] DetailsTabsPanel extraction verification: `DashboardView_HostsDetailsTabsPanelAndPreservesTabsBinding` passed.
- [x] DetailsTabsPanel extraction verification: all Windows App tests passed (36 tests).
- [x] DetailsTabsPanel extraction verification: full `.NET` tests passed (190 tests).
- [x] DetailsTabsPanel extraction verification: `.NET` build passed.
- [x] DetailsTabsPanel extraction verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-121155`.
- [x] DetailsTabsPanel extraction verification: coverage report generated successfully with overall line coverage 92.3%.
- [x] Decide and document whether Details rows-per-page pagination is Milestone 31 or future; if Milestone 31, add `DetailsTabs_ShowRowsPerPageRows`, `RowsPerPageOptions`, `CurrentPage`, and visible row collections.
- [x] Details pagination verification: RED Presentation tests covered default 10 rows per page and previous/next visible-row paging.
- [x] Details pagination verification: WPF test caught the missing `Buttons.xaml` resource merge before the footer could render.
- [x] Details pagination verification: App/Web/Live DataGrids bind to `VisibleAppSessionRows`, `VisibleWebSessionRows`, and `VisibleLiveEventRows`.
- [x] Details pagination verification: all Windows App tests passed (50 tests).
- [x] Details pagination verification: full `.NET` tests passed (206 tests).
- [x] Details pagination verification: `.NET` build passed.
- [x] Details pagination verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-143059`.
- [x] Details pagination verification: coverage report generated successfully with overall line coverage 91.9%.
- [x] WPF web persistence refresh signal: RED `PollTrackingCommand_WhenWebSessionPersistsWithoutFocusChange_RefreshesDashboard` failed first because `DashboardTrackingSnapshot` had no public web-persistence signal.
- [x] WPF web persistence refresh signal: added `HasPersistedWebSession` and refreshed SQLite-backed dashboard data when a web-only persistence event occurs.
- [x] WPF web persistence refresh signal verification: focused Presentation test passed.
- [x] WPF web persistence refresh signal verification: all Presentation tests passed (42 tests).
- [x] WPF web persistence refresh signal verification: full `.NET` tests passed (212 tests).
- [x] WPF web persistence refresh signal verification: `.NET` build passed.
- [x] WPF web persistence refresh signal verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-150004`.
- [x] WPF web persistence refresh signal verification: coverage report generated successfully with overall line coverage 92.0%.
- [x] WPF browser persistence coordinator: RED coordinator test proved Chrome domain changes must persist completed WebSessions to SQLite, enqueue `web_session` outbox rows, and set `HasPersistedWebSession`.
- [x] WPF browser persistence coordinator: `TrackingPoller` now carries foreground snapshots through `FocusSessionizerResult` so browser readers can inspect the current foreground safely.
- [x] WPF browser persistence coordinator: coordinator sanitizes browser snapshots with DomainOnly storage by default and keeps full URL null unless future opt-in exists.
- [x] WPF browser persistence coordinator: RED test caught a wrong web upload `deviceId`; fixed payloads to use the current FocusSession device id.
- [x] WPF browser persistence UI path: Start button plus dispatcher tick from Chrome `github.com` to `chatgpt.com` persists a `github.com` WebSession and refreshes the Web Sessions grid before Stop.
- [x] WPF browser persistence UI path: `SqliteDashboardDataSource` now reads web sessions by their own time range so completed web sessions are visible while the browser focus session is still open.
- [x] WPF browser persistence verification: focused coordinator and MainWindow tests passed.
- [x] WPF browser persistence verification: all Windows App tests passed (57 tests).
- [x] WPF browser persistence verification: browser/storage related Windows tests passed (33 tests).
- [x] WPF browser persistence verification: full `.NET` tests passed (214 tests).
- [x] WPF browser persistence verification: `.NET` build passed.
- [x] WPF browser persistence verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-151739`.
- [x] WPF browser persistence verification: coverage report generated successfully with overall line coverage 92.2%.
- [x] WPF RealStart UI evidence: RED source-contract test required RealStart to verify the persisted focus session appears in `RecentAppSessionsList`.
- [x] WPF RealStart UI evidence: tool now reads the latest focus-session process/app name from temp SQLite and searches the WPF automation tree after Stop.
- [x] WPF RealStart UI evidence verification: focused App test passed.
- [x] WPF RealStart UI evidence verification: `scripts/run-wpf-real-start-acceptance.ps1 -Seconds 2` passed and reported the persisted row in `RecentAppSessionsList`.
- [x] WPF RealStart UI evidence verification: all Windows App tests passed (58 tests).
- [x] WPF RealStart UI evidence verification: full `.NET` tests passed (215 tests).
- [x] WPF RealStart UI evidence verification: `.NET` build passed.
- [x] WPF RealStart UI evidence verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-152658`.
- [x] WPF RealStart UI evidence verification: coverage report generated successfully with overall line coverage 92.2%.
- [x] WPF auto-start/sync-at-start verification: RED tests proved StartTracking auto-syncs while sync-off remains local-only.
- [x] WPF auto-start/sync-at-start verification: normal WPF startup defaults to auto-start via `WindowsAppOptions.AutoStartTracking`, with `WOONG_MONITOR_AUTO_START_TRACKING` override.
- [x] WPF auto-start/sync-at-start verification: RealStart and UI snapshot tools accept an already Running app and no longer require Start to be initially enabled.
- [x] WPF auto-start/sync-at-start verification: browser-domain fallback copy now says `Browser domain not connected yet. Domain-only privacy is safe.`
- [x] WPF auto-start/sync-at-start verification: full `.NET` tests passed (222 tests), `.NET` build passed, WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-154548`, and coverage generated with overall line coverage 92.0%.
- [x] Add `Views/SettingsPanel.xaml` for privacy/sync/runtime settings.
- [x] SettingsPanel must include or explicitly disable Sync endpoint, Capture page title, Domain-only browser storage, Poll interval, Idle threshold, Open DB/log folder, and guarded Clear local data controls.
- [x] SettingsPanel extraction verification: `DetailsTabsPanel_HostsSettingsPanelInsideSettingsTab`, `SettingsPanel_PreservesPrivacyControlsAndSafeDefaults`, `SettingsPanel_PreservesSyncControlsAndTwoWayBinding`, and `SettingsPanel_PreservesRuntimeAndStorageActions` passed.
- [x] SettingsPanel extraction verification: all Windows App tests passed (40 tests).
- [x] SettingsPanel extraction verification: full `.NET` tests passed (194 tests).
- [x] SettingsPanel extraction verification: `.NET` build passed.
- [x] SettingsPanel extraction verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-122321`.
- [x] SettingsPanel extraction verification: coverage report generated successfully with overall line coverage 92.4%.
- [x] Add reusable `Controls/StatusBadge.xaml` and replace Header badges while preserving `TrackingStatusBadge`, `SyncStatusBadge`, and `PrivacyStatusBadge`.
- [x] StatusBadge extraction verification: focused `StatusBadge_RendersTextAndPreservesAutomationId` and `DashboardView_HostsHeaderStatusBarAndPreservesHeaderContent` passed.
- [x] StatusBadge extraction verification: all Windows App tests passed (41 tests).
- [x] StatusBadge extraction verification: full `.NET` tests passed (195 tests).
- [x] StatusBadge extraction verification: `.NET` build passed.
- [x] StatusBadge extraction verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-123308`.
- [x] StatusBadge extraction verification: coverage report generated successfully with overall line coverage 92.3%.
- [x] Add reusable `Controls/DetailRow.xaml` and replace Current Focus label/value rows while preserving value AutomationIds.
- [x] DetailRow extraction verification: focused `DetailRow_RendersLabelAndValueWithStableValueAutomationId` and `DashboardView_HostsCurrentFocusPanelAndPreservesCurrentFocusBindings` passed.
- [x] DetailRow extraction verification: all Windows App tests passed (42 tests).
- [x] DetailRow extraction verification: full `.NET` tests passed (196 tests).
- [x] DetailRow extraction verification: `.NET` build passed.
- [x] DetailRow extraction verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-124239`.
- [x] DetailRow extraction verification: coverage report generated successfully with overall line coverage 92.0%.
- [x] Add reusable `Controls/SectionCard.xaml`.
- [x] SectionCard extraction verification: `SectionCard_RendersContentAndOptionalActionCommand`, `DashboardView_HostsChartsPanelAndPreservesChartContent`, and `DashboardView_ChartDetailButtonsSelectExpectedDetailsTabs` passed.
- [x] SectionCard extraction verification: all Windows App tests passed (43 tests).
- [x] SectionCard extraction verification: full `.NET` tests passed (197 tests).
- [x] SectionCard extraction verification: `.NET` build passed.
- [x] SectionCard extraction verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-125444`.
- [x] SectionCard extraction verification: coverage report generated successfully with overall line coverage 92.1%.
- [x] Add `Styles/Buttons.xaml` with shared dashboard, primary, danger, secondary, and period button styles.
- [x] Replace duplicate local `DashboardButtonStyle` blocks in `ControlBar` and `SettingsPanel` with the shared button style dictionary.
- [x] Button style dictionary verification: focused button/resource, ControlBar, SettingsPanel, MainWindow, and tabs tests passed.
- [x] Button style dictionary verification: all Windows App tests passed (44 tests).
- [x] Button style dictionary verification: full `.NET` tests passed (198 tests).
- [x] Button style dictionary verification: `.NET` build passed.
- [x] Button style dictionary verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-130753`.
- [x] Product UI goal polish: RED `ControlBar_RendersGoalActionAndPeriodGroups` verified action/period grouping and readable icon/text labels.
- [x] Product UI goal polish: RED `MetricCard_RendersGoalIconAccentSlot` verified summary cards expose an icon/accent slot matching the provided UI reference.
- [x] Product UI goal polish: RED `DashboardView_ChartsPanelUsesSeparateGoalCardSurfaces` verified chart cards remain separate, readable dashboard surfaces.
- [x] Button style dictionary verification: coverage report generated successfully with overall line coverage 92.1%.
- [x] Align DashboardView root scrolling with `wpfelements.md`: root scroll is vertical-only and wide session grids keep their own horizontal scroll.
- [x] Root scrolling verification: `DashboardView_UsesVerticalRootScrollAndKeepsGridHorizontalScroll` and `MainWindow_AtMinimumSize_KeepsTabsReachableOrProvidesScrolling` passed.
- [x] Root scrolling verification: all Windows App tests passed (45 tests).
- [x] Root scrolling verification: full `.NET` tests passed (199 tests).
- [x] Root scrolling verification: `.NET` build passed.
- [x] Root scrolling verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-131541`.
- [x] Root scrolling verification: coverage report generated successfully with overall line coverage 92.1%.
- [x] Add `Styles/Cards.xaml` with reusable dashboard card and compact surface border styles.
- [x] Replace duplicated card border setters in `MetricCard`, `SectionCard`, `CurrentFocusPanel`, `DetailsTabsPanel`, and `ControlBar`.
- [x] Cards style dictionary verification: focused card/resource and affected panel tests passed.
- [x] Cards style dictionary verification: all Windows App tests passed (46 tests).
- [x] Cards style dictionary verification: full `.NET` tests passed (200 tests).
- [x] Cards style dictionary verification: `.NET` build passed.
- [x] Cards style dictionary verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-132227`.
- [x] Cards style dictionary verification: coverage report generated successfully with overall line coverage 92.1%.
- [x] Add `Styles/Colors.xaml` with core app background, surface, border, primary text, and muted text brushes.
- [x] Wire core color resources into `App.xaml`, `Cards.xaml`, and the dashboard content background.
- [x] Colors style dictionary verification: focused color/resource, card, MainWindow, MetricCard, and SectionCard tests passed.
- [x] Colors style dictionary verification: all Windows App tests passed (47 tests).
- [x] Colors style dictionary verification: full `.NET` tests passed (201 tests).
- [x] Colors style dictionary verification: `.NET` build passed.
- [x] Colors style dictionary verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-132937`.
- [x] Colors style dictionary verification: coverage report generated successfully with overall line coverage 92.1%.
- [x] Add `Styles/Typography.xaml` with heading, subtitle, section title, body, muted, and metric value text styles.
- [x] Wire typography resources into `App.xaml`, `HeaderStatusBar`, `SectionCard`, `DetailRow`, and `MetricCard`.
- [x] Typography style dictionary verification: RED missing-resource test failed first, then focused resource/header/detail/metric/section tests passed.
- [x] Typography style dictionary verification: all Windows App tests passed (48 tests).
- [x] Typography style dictionary verification: full `.NET` tests passed (202 tests).
- [x] Typography style dictionary verification: `.NET` build passed.
- [x] Typography style dictionary verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-134019`.
- [x] Typography style dictionary verification: coverage report generated successfully with overall line coverage 92.1%.
- [x] Add `Styles/DataGrid.xaml` with shared readable session grid behavior.
- [x] Wire App/Web/Live event DataGrids to `SessionDataGridStyle` while preserving explicit column MinWidth values and grid-level horizontal scrolling.
- [x] DataGrid style dictionary verification: RED missing-resource test failed first, then focused DataGrid/root-scroll/tabs tests passed.
- [x] DataGrid style dictionary verification: all Windows App tests passed (49 tests).
- [x] DataGrid style dictionary verification: full `.NET` tests passed (203 tests).
- [x] DataGrid style dictionary verification: `.NET` build passed.
- [x] DataGrid style dictionary verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-134913`.
- [x] DataGrid style dictionary verification: coverage report generated successfully with overall line coverage 92.1%.
- [x] Add `Styles/Tabs.xaml` with readable dashboard TabControl and TabItem styles.
- [x] Wire `DashboardTabs` to shared tab styles while preserving selection binding, tab headers, and 1024px reachability.
- [x] Tabs style dictionary verification: RED missing-resource test failed first, then focused tabs and minimum-size tests passed.
- [x] Tabs style dictionary verification: all Windows App tests passed (50 tests).
- [x] Tabs style dictionary verification: full `.NET` tests passed (204 tests).
- [x] Tabs style dictionary verification: `.NET` build passed.
- [x] Tabs style dictionary verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-135614`.
- [x] Tabs style dictionary verification: coverage report generated successfully with overall line coverage 92.1%.
- [x] Add style dictionaries: `Colors.xaml`, `Typography.xaml`, `Buttons.xaml`, `Cards.xaml`, `DataGrid.xaml`, `Tabs.xaml`, `Inputs.xaml`.
- [x] Merge style dictionaries from `App.xaml` and replace hard-coded colors/duplicate local button/card styles in extracted panels.
  - [x] Add `CompactActionButtonStyle` to `Styles/Buttons.xaml` for reusable chart/card action buttons.
  - [x] Replace duplicate inline compact action button sizing in `ChartsPanel` and `SectionCard`.
  - [x] Compact action style verification: RED `ButtonStyleDictionary_DefinesReadableDashboardButtonStyles` failed first, then passed after the shared style was added.
  - [x] Compact action style verification: focused SectionCard/chart details tests passed.
  - [x] Compact action style verification: all Windows App tests passed (77 tests).
  - [x] Compact action style verification: full `.NET` tests passed (281 tests).
  - [x] Compact action style verification: `.NET` build passed.
  - [x] Compact action style verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-193853`.
  - [x] Compact action style verification: coverage report generated successfully with overall line coverage 91.3%.
  - [x] Add `EmptyStateTextStyle` to `Styles/Typography.xaml` and replace `EmptyState` inline muted text color/font setters.
  - [x] Replace chart heading inline font/color setters in `ChartsPanel` with shared `SectionTitleTextStyle`.
  - [x] Typography cleanup verification: RED `EmptyState_RendersBoundTextWithTextAutomationId` style assertion failed first, then passed.
  - [x] Typography cleanup verification: RED `DashboardView_ChartsPanelUsesSharedSectionTitleTypography` failed first, then passed.
  - [x] Typography cleanup verification: all Windows App tests passed (78 tests).
  - [x] Typography cleanup verification: full `.NET` tests passed (282 tests).
  - [x] Typography cleanup verification: `.NET` build passed.
  - [x] Typography cleanup verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-195534`.
  - [x] Typography cleanup verification: coverage report generated successfully with overall line coverage 91.3%.
  - [x] Add named header badge brushes to `Styles/Colors.xaml` for tracking, sync, and privacy status colors.
  - [x] Replace `HeaderStatusBar` inline badge color values with shared brush resources.
  - [x] Header badge color verification: RED `HeaderStatusBar_UsesSharedBadgeColorResources` failed first, then passed.
  - [x] Header badge color verification: all Windows App tests passed (79 tests).
  - [x] Header badge color verification: full `.NET` tests passed (283 tests).
  - [x] Header badge color verification: `.NET` build passed.
  - [x] Header badge color verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-200235`.
  - [x] Header badge color verification: coverage report generated successfully with overall line coverage 91.3%.
  - [x] Add `SettingsSectionTitleTextStyle` to `Styles/Typography.xaml` and replace Settings panel Privacy/Sync/Runtime heading inline text setters.
  - [x] Settings heading typography verification: RED `SettingsPanel_UsesSharedSectionHeadingTypography` failed first, then passed.
  - [x] Settings heading typography verification: all Windows App tests passed (80 tests).
  - [x] Settings heading typography verification: full `.NET` tests passed (284 tests).
  - [x] Settings heading typography verification: `.NET` build passed.
  - [x] Settings heading typography verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-200823`.
  - [x] Settings heading typography verification: coverage report generated successfully with overall line coverage 91.3%.
  - [x] Add `SettingsMutedTextStyle` to `Styles/Typography.xaml` and replace Settings panel muted helper text inline color/font setters.
  - [x] Settings muted text verification: RED `SettingsPanel_UsesSharedMutedTextTypography` failed first, then passed.
  - [x] Settings muted text verification: all Windows App tests passed (81 tests).
  - [x] Settings muted text verification: full `.NET` tests passed (285 tests).
  - [x] Settings muted text verification: `.NET` build passed.
  - [x] Settings muted text verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-201406`.
  - [x] Settings muted text verification: coverage report generated successfully with overall line coverage 91.3%.
  - [x] Add `WarningTextBrush` and `SettingsWarningTextStyle` for Settings sync status warning text.
  - [x] Settings warning text verification: RED `SettingsPanel_UsesSharedWarningTextTypography` failed first, then passed.
  - [x] Settings warning text verification: all Windows App tests passed (82 tests).
  - [x] Settings warning text verification: full `.NET` tests passed (286 tests).
  - [x] Settings warning text verification: `.NET` build passed.
  - [x] Settings warning text verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-201950`.
  - [x] Settings warning text verification: coverage report generated successfully with overall line coverage 91.3%.
  - [x] Add `Styles/Inputs.xaml` with `SettingsInputTextBoxStyle` and replace Settings sync endpoint TextBox inline font setter.
  - [x] Settings input style verification: RED `SettingsPanel_UsesSharedInputStyle` failed first, then passed.
  - [x] Settings input style verification: all Windows App tests passed (83 tests).
  - [x] Settings input style verification: full `.NET` tests passed (287 tests).
  - [x] Settings input style verification: `.NET` build passed.
  - [x] Settings input style verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-202524`.
  - [x] Settings input style verification: coverage report generated successfully with overall line coverage 91.3%.
  - [x] Add `SettingsCheckBoxStyle` to `Styles/Inputs.xaml` and replace Settings checkbox inline font/margin setters.
  - [x] Settings checkbox style verification: RED `SettingsPanel_UsesSharedCheckBoxStyle` failed first, then passed.
  - [x] Settings checkbox style verification: all Windows App tests passed (84 tests).
  - [x] Settings checkbox style verification: full `.NET` tests passed (288 tests).
  - [x] Settings checkbox style verification: `.NET` build passed.
  - [x] Settings checkbox style verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-203158`.
  - [x] Settings checkbox style verification: coverage report generated successfully with overall line coverage 91.3%.
  - [x] Add `CurrentFocusValueTextStyle` and `CurrentFocusSecondaryTextStyle` to `Styles/Typography.xaml`.
  - [x] Replace `CurrentFocusPanel` inline title, last DB write, and sync status font/color setters with shared typography resources.
  - [x] Current Focus typography verification: RED `CurrentFocusPanel_UsesSharedTypographyForTitleAndPersistenceStatus` failed first, then passed.
  - [x] Current Focus typography verification: all Windows App tests passed (85 tests).
  - [x] Current Focus typography verification: full `.NET` tests passed (290 tests).
  - [x] Current Focus typography verification: `.NET` build passed.
  - [x] Current Focus typography verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-204901`.
  - [x] Current Focus typography verification: coverage report generated successfully with overall line coverage 91.3%.
  - [x] Replace `MainWindow` hard-coded background color with shared `AppBackgroundBrush`.
  - [x] MainWindow background verification: RED `MainWindow_UsesSharedBackgroundBrush` failed first, then passed with `DynamicResource` after WPF resource lookup order was handled.
  - [x] MainWindow background verification: all Windows App tests passed (86 tests).
  - [x] MainWindow background verification: full `.NET` tests passed (291 tests).
  - [x] MainWindow background verification: `.NET` build passed.
  - [x] MainWindow background verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-205628`.
  - [x] MainWindow background verification: coverage report generated successfully with overall line coverage 91.3%.
  - [x] Replace `DetailsTabsPanel` pager label/status inline text styling with shared typography resources.
  - [x] Details pager typography verification: RED `DetailsTabsPanel_UsesSharedPagerTypography` failed first, then passed.
  - [x] Details pager typography verification: all Windows App tests passed (87 tests).
  - [x] Details pager typography verification: full `.NET` tests passed (292 tests).
  - [x] Details pager typography verification: `.NET` build passed.
  - [x] Details pager typography verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-210315`.
  - [x] Details pager typography verification: coverage report generated successfully with overall line coverage 91.3%.
  - [x] Add `MetricLabelTextStyle` to `Styles/Typography.xaml` and replace `MetricCard` inline label font weight.
  - [x] Metric card typography verification: RED `MetricCard_UsesSharedLabelTypography` failed first, then passed.
  - [x] Metric card typography verification: all Windows App tests passed (88 tests).
  - [x] Metric card typography verification: full `.NET` tests passed (293 tests).
  - [x] Metric card typography verification: `.NET` build passed.
  - [x] Metric card typography verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-211007`.
  - [x] Metric card typography verification: coverage report generated successfully with overall line coverage 91.3%.
  - [x] Add color-free `Styles/Badges.xaml` with `StatusBadgeBorderStyle` and `StatusBadgeTextStyle`.
  - [x] Replace `StatusBadge` inline shape/typography setters with shared badge styles while preserving parent-owned tracking/sync/privacy brush resources.
  - [x] Status badge style verification: RED `StatusBadge_UsesSharedShapeAndTextStyles` failed first, then passed.
  - [x] Status badge style verification: adjacent badge/header tests passed and confirmed brush resource identity was preserved.
  - [x] Status badge style verification: all Windows App tests passed (89 tests).
  - [x] Status badge style verification: full `.NET` tests passed (294 tests).
  - [x] Status badge style verification: `.NET` build passed.
  - [x] Status badge style verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-212314`.
  - [x] Status badge style verification: coverage report generated successfully with overall line coverage 91.3%.
  - [x] Move repeated App/Web/Live session DataGrid top margins into `SessionDataGridStyle`.
  - [x] Details DataGrid spacing verification: RED `DataGridStyleDictionary_DefinesReadableSessionGridStyle` failed first on missing `Margin`, then passed.
  - [x] Details DataGrid spacing verification: adjacent Details tab tests passed.
  - [x] Details DataGrid spacing verification: all Windows App tests passed (89 tests).
  - [x] Details DataGrid spacing verification: full `.NET` tests passed (294 tests).
  - [x] Details DataGrid spacing verification: `.NET` build passed.
  - [x] Details DataGrid spacing verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-213053`.
  - [x] Details DataGrid spacing verification: coverage report generated successfully with overall line coverage 91.3%.
  - [x] Add architecture guard `WpfViewsAndControls_DoNotDefineLocalStyles` so WPF Views/Controls consume shared style dictionaries instead of defining local `Style` resources.
  - [x] Move `SummaryMetricCardStyle` from `SummaryCardsPanel.xaml` to `Styles/Cards.xaml`.
  - [x] Move `DetailsTabsPanel` tab header and app-session icon styles to `Styles/Tabs.xaml`.
  - [x] Local style cleanup verification: RED architecture guard failed first on DetailsTabsPanel/SummaryCardsPanel local styles, then passed.
  - [x] Local style cleanup verification: focused architecture color/style/root-dictionary guards passed.
  - [x] Local style cleanup verification: focused WPF App expectation/accessibility tests passed (48 tests).
  - [x] Local style cleanup verification: `.NET` build passed.
  - [x] Move repeated Settings section-heading bottom margins into `SettingsSectionTitleTextStyle`.
  - [x] Settings section heading spacing verification: RED `SettingsPanel_UsesSharedSectionHeadingTypography` failed first on missing shared `Margin`, then passed.
  - [x] Settings section heading spacing verification: adjacent Settings privacy/sync/readability tests passed.
  - [x] Settings section heading spacing verification: all Windows App tests passed (89 tests).
  - [x] Settings section heading spacing verification: full `.NET` tests passed (294 tests).
  - [x] Settings section heading spacing verification: `.NET` build passed.
  - [x] Settings section heading spacing verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-213752`.
  - [x] Settings section heading spacing verification: coverage report generated successfully with overall line coverage 91.3%.
  - [x] Add `ITrackingTicker` and `DispatcherTrackingTicker` so WPF tracking ticks are a testable runtime boundary.
  - [x] Replace `MainWindow` direct `DispatcherTimer` construction with injected `ITrackingTicker`.
  - [x] Ensure `MainWindow` starts the ticker on Loaded, stops it on Closed, and unsubscribes the Tick handler on close.
  - [x] Register `DispatcherTrackingTicker` in DI and construct `MainWindow` via an explicit factory to avoid ambiguous constructors.
  - [x] Tracking ticker verification: RED tests failed first on missing `ITrackingTicker`, then passed.
  - [x] Tracking ticker verification: manual ticker tests now cover duration advancement, foreground change persistence, and browser domain web-session persistence without wall-clock timer waits.
  - [x] Tracking ticker verification: safety tests prove a visible ticker tick does not collect/persist anything before the user starts tracking.
  - [x] Tracking ticker verification: safety tests prove auto-start does not start tracking before `MainWindow.Loaded`.
  - [x] Tracking ticker verification: all Windows App tests passed (93 tests).
  - [x] Tracking ticker verification: full `.NET` tests passed (298 tests).
  - [x] Tracking ticker verification: `.NET` build passed.
  - [x] Tracking ticker verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-215459`.
  - [x] Tracking ticker verification: coverage report generated successfully with overall line coverage 91.2%.
  - [x] Add `BrowserWebSessionizer.CompleteCurrent` so Stop can close an open domain session without waiting for another tab/domain change.
  - [x] Stop flush verification: RED `CompleteCurrent_WhenTrackingStops_CreatesWebSessionForOpenDomain` failed first, then passed.
  - [x] Stop flush verification: RED `StopButton_WhenBrowserSessionIsOpen_PersistsWebSessionAndRefreshesDashboard` failed first on missing SQLite `web_session`, then passed.
  - [x] Stop flush verification: stopping a running Chrome/github.com session persists a domain-only `web_session`, creates a pending `web_session` outbox row, and refreshes the WPF Web Focus card and Web Sessions grid.
  - [x] Stop flush verification: full `.NET` tests passed (300 tests).
  - [x] Stop flush verification: `.NET` build passed.
  - [x] Stop flush verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-220702`.
  - [x] Stop flush verification: coverage report generated successfully with overall line coverage 91.2%.
  - [x] Add WPF close flush behavior so closing the window while tracking is Running executes the same Stop flush path before ticker disposal.
  - [x] Close flush verification: RED `MainWindow_WhenClosedWhileTracking_FlushesCurrentSessionToSqliteOutboxAndStopsTicker` failed first on missing SQLite focus session, then passed.
  - [x] Close flush verification: full `.NET` tests passed (301 tests).
  - [x] Close flush verification: `.NET` build passed.
  - [x] Close flush verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-221314`.
  - [x] Close flush verification: coverage report generated successfully with overall line coverage 91.3%.
  - [x] Add WPF runtime integration test for Sync Now while sync is off after focus/web outbox rows are queued.
  - [x] Sync-off pending verification: `MainWindow_SyncNowButton_WhenSyncOffAfterQueuedRows_LeavesOutboxPendingAndShowsSkippedStatus` passed and proved focus/web outbox rows remain pending with no synced timestamp or retry count changes.
  - [x] Sync-off pending verification: full `.NET` tests passed (304 tests).
  - [x] Sync-off pending verification: `.NET` build passed.
  - [x] Sync-off pending verification: WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260429-222624`.
  - [x] Sync-off pending verification: coverage report generated successfully with overall line coverage 91.3%.
  - [x] App root style merge verification: RED architecture tests first failed because `Inputs.xaml` was not merged at `App.xaml` root and `MainWindow.xaml` duplicated the Colors dictionary.
  - [x] App root style merge verification: `App.xaml` now merges every shared style dictionary, including `Inputs.xaml`, and `MainWindow.xaml` no longer duplicates application-level style dictionaries.
  - [x] App root style merge verification: all Windows App tests passed (96 tests) after the MainWindow background expectation was updated to use test-local resources instead of creating a process-wide `Application`.
  - [x] WPF color resource guard: RED `WpfXaml_DoesNotUseColorLiteralsOutsideColorsDictionary` failed first on `Tabs.xaml`, then passed after adding `TransparentBrush` to `Colors.xaml`.
  - [x] Dashboard componentization guard: RED `DashboardView_LaysOutDashboardSectionsAsDirectGridRows` failed first, then passed after making chart/details sections direct `DashboardView` grid rows.
  - [x] WPF UI automation stability: RED `DashboardView_DirectSectionsExposeStableAutomationIds` failed first, then passed after adding stable section AutomationIds.
  - [x] WPF acceptance clock hardening: RED `AcceptanceScenarioClock_DefaultStartUsesLocalNoonToAvoidMidnightFilterFlakes` failed first at the local midnight boundary, then passed after pinning fake TrackingPipeline data to local noon.
  - [x] WPF chart/details visual evidence: RED App tests required chart header icons, Details tab icons, compact pager icon buttons, and an App Sessions template column, then passed after XAML/resource updates.
  - [x] WPF Live Event acceptance evidence: RED snapshot-tool guard required Live Event Log checks across reachable details pages, then passed after the tool aggregated paged `LiveEventsList` text.
  - [x] WPF selected-tab details pager evidence: RED Presentation test proved switching from App Sessions page 2 to a shorter Web Sessions tab must reset to page 1 and use the selected tab's page count, then passed after ViewModel paging logic was scoped to `SelectedDetailsTab`.
  - [x] WPF details pager automation names: RED App test required readable `AutomationProperties.Name` values for compact previous/next/page-status controls, then passed after preserving icon buttons with semantic names.
  - [x] WPF Presentation pager/status behavior: RED tests required chart details commands to reset the selected tab pager, rows-per-page changes to clamp page state, and sync failure/off transitions to update dashboard sync status safely.
  - [x] WPF App accessibility names: RED App tests required SettingsPanel primary controls, chart detail buttons, and reusable SectionCard action buttons to expose readable `AutomationProperties.Name` values.
  - [x] WPF App accessibility helper/details names: refactored duplicate STA/visual-tree test helpers into `WpfTestHelpers` and added RED DetailsTabsPanel tests for readable tab/list/rows-per-page automation names.
  - [x] WPF focus persistence extraction: RED Windows infrastructure test required `WindowsFocusSessionPersistenceService` to save privacy-safe focus sessions, queue `focus_session` outbox payloads, and keep window titles null.
  - [x] WPF web persistence extraction: RED Windows infrastructure test required `WindowsWebSessionPersistenceService` to save domain-only web sessions, queue `web_session` outbox payloads, keep full URLs/page titles null by default, and preserve client session ids/durations.
- [x] Add presentation child ViewModels or adapter properties only where they improve testability without breaking existing behavior.
  - [x] Decision: no additional child ViewModels were added in this slice because current Presentation behavior is already covered through public `DashboardViewModel`, row models, settings models, chart mappers, and semantic WPF/App tests; extra splitting would add churn without improving the current test surface.
- [x] Extract WPF focus/web session SQLite and outbox payload creation from `Windows.App` coordinator into Windows infrastructure services.
- [x] Extract WPF startup lifecycle orchestration into an app startup service if auto-start, initial refresh, sync-at-start, permission checks, or tracking timer policy grow beyond simple MainWindow composition glue.
  - [x] Startup lifecycle extraction rationale: `App.xaml.cs` had grown past simple host glue by resolving `MainWindow`, selecting the dashboard Today period, and showing the window directly.
  - [x] Startup lifecycle extraction verification: RED `WindowsAppStartupService_Start_SelectsTodayAndShowsMainWindow` failed first on missing service, then passed after moving initial dashboard refresh/show orchestration into `WindowsAppStartupService`.
  - [x] Startup lifecycle extraction verification: `AppStartup_CodeBehindDelegatesWindowInitializationToStartupService` guards `App.xaml.cs` as Generic Host + DI glue with no direct `MainWindow`, `SelectPeriod`, or `Show` orchestration.
  - [x] Startup lifecycle extraction verification: focused Windows App composition tests and architecture startup guard passed.
  - [x] Startup lifecycle extraction verification: `.NET` build passed.
- [x] Keep all current WPF UI expectation, semantic pipeline, and acceptance tests passing during current component/style guard extraction.
- [x] Run full `.NET` tests and build after current component/style guard extraction.
- [x] Run WPF UI acceptance after current component/style guard extraction at `artifacts/wpf-ui-acceptance/20260430-012621`.
- [x] Update `docs/wpf-csharp-coding-guide.md`, `docs/wpf-ui-plan.md`, `docs/resume-state.md`, and this TODO after componentization.
  - [x] Compact action style slice docs/TODO updated.
  - [x] App root style merge slice docs/TODO updated.
  - [x] WPF color/direct-section/AutomationId/acceptance-clock guard slice docs/TODO updated.
  - [x] WPF chart/details icon and Live Event paged acceptance docs/TODO updated.
- [x] Commit and push WPF componentization slices.

## Milestone 32: Android SVG UI Flow Alignment

- [x] Compare `artifacts/android-ui-flow/woong-monitor-android-ui-flow.figma-import.svg` against the current Android XML dashboard.
- [x] Add a failing Robolectric layout test for the planned dashboard core surface.
- [x] Update `activity_dashboard.xml` and Android string resources so the SVG-planned status chips, Current Focus panel, summary cards, chart/list sections, and bottom navigation exist in XML/ViewBinding.
- [x] Run focused Android dashboard UI tests.
- [x] Run Android debug build.
- [x] Update resume state after Android SVG UI alignment.

## Milestone 33: WPF Control Bar Accessibility Polish

- [x] Add a failing WPF App accessibility test requiring readable semantic names for Control Bar tracking and period buttons.
- [x] Add explicit `AutomationProperties.Name` values to Control Bar buttons without changing visible labels, commands, or privacy behavior.
- [x] Run focused WPF App accessibility tests.
- [x] Run WPF App tests.
- [x] Run solution build.
- [x] Update WPF resume state after the accessibility polish.

## Milestone 34: WPF Current Focus Runtime Accessibility

- [x] Add a failing WPF App accessibility test requiring readable semantic names for Current Focus runtime state fields.
- [x] Add minimal XAML/control metadata so Current Focus values remain selectable by stable AutomationIds and readable names.
- [x] Run focused WPF Current Focus accessibility tests.
- [x] Run WPF App tests.
- [x] Run solution build.
- [x] Update WPF resume state after the Current Focus accessibility slice.
## Milestone 35: WPF Header StatusBadge Accessibility

- [x] Add a failing WPF App accessibility test requiring Header Tracking/Sync/Privacy badges to expose readable names matching their status text.
- [x] Add minimal StatusBadge/Header XAML metadata so badge AutomationProperties.Name follows bound status text.
- [x] Run focused WPF Header StatusBadge accessibility tests.
- [x] Run WPF App tests.
- [x] Run solution build.
- [x] Update WPF resume state after the Header StatusBadge accessibility slice.
## Milestone 36: WPF Acceptance Header Badge Evidence

- [x] Add a failing WPF App/tool test requiring Header badge semantic evidence in snapshot report/manifest checks.
- [x] Add Header Tracking/Sync/Privacy badge readable-name checks to `Woong.MonitorStack.Windows.UiSnapshots`.
- [x] Preserve Current Focus readable names while exposing runtime values through `AutomationProperties.ItemStatus` for acceptance tools.
- [x] Run focused WPF acceptance-tool test.
- [x] Run WPF App tests.
- [x] Run solution build.
- [x] Run WPF UI acceptance script and confirm Header badge semantic evidence appears in report/manifest.
- [x] Update WPF resume state after the acceptance evidence slice.

## Milestone 37: WPF Current Focus Acceptance Semantic Evidence

- [x] Add a failing WPF App/tool test requiring Current Focus runtime field semantic evidence in snapshot report/manifest checks.
- [x] Add Current app/process/window title/domain/session duration/last poll/last DB write/last persisted session/sync state readable-name checks to `Woong.MonitorStack.Windows.UiSnapshots`.
- [x] Add Current Focus runtime status evidence through the existing `AutomationProperties.ItemStatus`/text reader path.
- [x] Run focused WPF acceptance-tool test.
- [x] Run WPF App tests.
- [x] Run solution build.
- [x] Run WPF UI acceptance script and confirm Current Focus semantic evidence appears in report/manifest.
- [x] Update WPF resume state after the Current Focus acceptance evidence slice.

## Milestone 38: WPF Current Focus Report Table Evidence

- [x] Add a failing WPF App/tool test requiring a human-readable Current Focus semantic evidence table in `report.md`.
- [x] Add `## Current Focus Runtime Semantic Evidence` to the snapshot report with Field, AutomationId, Readable Name, Runtime Value, and Status columns.
- [x] Populate the table from Current Focus semantic evidence without changing Android, Android scripts, Android docs, telemetry collection, or server code.
- [x] Run focused WPF acceptance-tool test.
- [x] Run WPF App tests.
- [x] Run solution build.
- [x] Run WPF UI acceptance script and confirm the table appears in the latest report.
- [x] Update WPF resume state after the report-table evidence slice.

## Milestone 39: WPF Current Focus Manifest Evidence

- [x] Add a failing WPF App/tool test requiring a dedicated `currentFocusRuntimeEvidence` manifest array separate from generic `checks`.
- [x] Add machine-readable Current Focus rows with field, readableName, automationId, runtimeValue, and status.
- [x] Include Current app, Current process, Current window title, Current browser domain, Current session duration, Last poll, Last DB write, Last persisted session, and Sync state.
- [x] Preserve the existing generic `checks` manifest array for backward compatibility.
- [x] Run focused WPF acceptance-tool test.
- [x] Run WPF App tests.
- [x] Run solution build.
- [x] Run WPF UI acceptance script and confirm the latest manifest includes both `currentFocusRuntimeEvidence` and `checks`.
- [x] Update WPF resume state after the manifest evidence slice.

## Milestone 40: WPF Section Screenshot Evidence

- [x] Audit remaining WPF acceptance screenshot artifact shape after Current Focus manifest evidence.
- [x] Add a failing WPF App/tool test requiring grouped section screenshot evidence in report and manifest.
- [x] Add section evidence rows for Current activity, Summary cards, Sessions, Web sessions, Live events, Chart area, and Settings.
- [x] Include section, automationId, screenshot, skippedReason, and status while preserving generic screenshot/skipped arrays for backward compatibility.
- [x] Run focused WPF acceptance-tool test.
- [x] Run WPF App tests.
- [x] Run solution build.
- [x] Run WPF UI acceptance script and confirm grouped section screenshot evidence appears in latest report/manifest.
- [x] Update WPF resume state after the section screenshot evidence slice.

## Milestone 41: WPF Control Action Evidence

- [x] Audit remaining WPF runtime acceptance evidence after section screenshot grouping.
- [x] Add a failing WPF App/tool test requiring grouped Start/Stop/Sync action evidence in report and manifest.
- [x] Add control action rows for Start tracking, Stop tracking, Sync local-only, and Sync enabled with action, AutomationId, result, and status.
- [x] Preserve the existing generic `checks` report/manifest evidence for backward compatibility.
- [x] Run focused WPF acceptance-tool test.
- [x] Run WPF App tests.
- [x] Run solution build.
- [x] Run WPF UI acceptance script and confirm grouped control action evidence appears in latest report/manifest.
- [x] Update WPF resume state after the control action evidence slice.

## Milestone 42: WPF SQLite Runtime Evidence

- [x] Audit remaining WPF acceptance runtime proof after grouped control action evidence.
- [x] Add a failing WPF App/tool test requiring grouped SQLite runtime evidence in report and manifest.
- [x] Add SQLite runtime evidence rows for `focus_session`, `web_session`, and `sync_outbox` with expected row count, actual row count, and status.
- [x] Preserve existing `databaseEvidence` and generic checks for backward compatibility.
- [x] Run focused WPF acceptance-tool test.
- [x] Run WPF App tests.
- [x] Run solution build.
- [x] Run WPF UI acceptance script and confirm grouped SQLite runtime evidence appears in latest report/manifest.
- [x] Update WPF resume state after the SQLite runtime evidence slice.

## Milestone 43: WPF Browser Domain Privacy Evidence

- [x] Add a failing WPF App/tool test requiring grouped browser-domain privacy evidence in report and manifest.
- [x] Add browser-domain privacy evidence rows for `github.com`, `chatgpt.com`, absent full URL values, absent page title values, and absent content-like storage.
- [x] Preserve existing generic checks, SQLite runtime evidence, and `databaseEvidence` for backward compatibility.
- [x] Run focused WPF acceptance-tool test.
- [x] Run WPF App tests.
- [x] Run solution build.
- [x] Run WPF UI acceptance script and confirm grouped browser-domain privacy evidence appears in latest report/manifest.
- [x] Update WPF resume state after the browser-domain privacy evidence slice.
## Milestone 44: WPF Minimum Size Reachability Evidence

- [x] Add a failing WPF App/tool test requiring grouped 1024x768 reachability evidence in report and manifest.
- [x] Add minimum-size reachability rows for Header, ControlBar, CurrentFocus, App Sessions, Web Sessions, Live Events, and Settings.
- [x] Capture or reference supporting 1024x768 screenshots for the minimum-size rows.
- [x] Update the WPF UI acceptance checklist with minimum-size semantic expectations.
- [x] Run focused WPF acceptance-tool test.
- [x] Run WPF App tests.
- [x] Run solution build.
- [x] Run WPF UI acceptance script and confirm grouped minimum-size evidence appears in latest report/manifest.
- [x] Update WPF resume state after the minimum-size reachability evidence slice.
## Milestone 46: WPF RealStart Local DB Evidence Artifacts

- [x] Audit WPF runtime acceptance evidence after minimum-size reachability and identify RealStart console-only DB proof as the next WPF-only gap.
- [x] Add a failing WPF App/tool test requiring RealStart local DB persistence evidence artifacts.
- [x] Add `real-start-report.md` and `real-start-manifest.json` beside the RealStart temp SQLite DB.
- [x] Include `focus_session` persistence, `sync_outbox` queueing, readable latest app/process text, and sync-disabled evidence.
- [x] Update WPF acceptance documentation and resume state for the RealStart evidence artifacts.
- [x] Run focused WPF acceptance-tool test.
- [x] Run WPF App tests.
- [x] Run solution build and solution tests.
- [x] Run WPF UI acceptance script and confirm RealStart evidence appears in latest artifacts.
## Milestone 45: Android Fragment Shell Runtime And Wireframe Correction

- [x] Add a failing architecture test requiring `SessionsFragment` to use the Room-backed sessions repository and expose a readable empty state.
- [x] Wire `SessionsFragment` to `RoomSessionsRepository` and `MonitorDatabase` instead of leaving it as a blank skeleton.
- [x] Add `10-main-shell-sessions.png` to Android screenshot automation and connected-device artifact pulling.
- [x] Capture emulator evidence at `artifacts/android-ui-snapshots/20260430-121642`.
- [x] Document that the user-provided XML wireframe skeleton is the target shape for the Android fragment shell.
- [x] Verify Android Sessions fragment slice with Gradle unit/build/androidTest build, emulator screenshots, full `.NET` tests, full `.NET` build, and coverage generation.
- [x] Verify bottom-navigation readability slice with focused architecture test, Gradle unit/build/androidTest build, emulator screenshot artifact `artifacts/android-ui-snapshots/20260430-124333`, full `.NET` tests, full `.NET` build, and coverage generation.
- [x] Normalize the launcher toolbar/header so it matches the compact MaterialToolbar skeleton.
- [x] Fix bottom-navigation readability so icons and labels stay visible above system/gesture navigation.
- [x] Correct the launcher shell back to the compact 72dp Material bottom navigation from the user-provided Android XML skeleton and remove the oversized overlay label row.
- [x] Capture emulator evidence for compact shell, period-filter ordering, and labeled latitude/longitude location context at `artifacts/android-ui-snapshots/20260430-130812`.
- [x] Fix Android dashboard chart axes to use human-readable hour, minute, and app labels instead of decimal placeholder labels.
- [x] Repair Android screenshot scrolling so `03-dashboard-charts.png` captures the chart section instead of the previous viewport.
- [x] Increase shared Android focus-session row height so app name, package, time range, state, and duration are readable without clipping.
- [x] Capture emulator evidence for chart labels, app labels, and readable session rows at `artifacts/android-ui-snapshots/20260430-133732`.
- [x] Add fragment Dashboard location-context card parity with the Activity dashboard, including latitude/longitude only after explicit opt-in.
- [x] Prefer user-facing app labels over package names in fragment Sessions rows when labels are available.
- [x] Wire `ReportFragment` to runtime summary repository/client behavior.
- [x] Wire `SettingsFragment` to runtime privacy/sync/location settings behavior.
- [x] Add feature screenshots for each newly runtime-backed shell tab.
- [x] Capture emulator evidence for SettingsFragment runtime wiring at `artifacts/android-ui-snapshots/20260430-140141`, including `11-main-shell-settings.png` and the scrolled location-context screenshot.
- [x] Add RED/GREEN Robolectric coverage proving the launcher Report tab loads Recent 7 days summary cards and top-app rows from Room-backed focus sessions.
- [x] Add `12-main-shell-report.png` to Android screenshot automation and connected-device artifact pulling.
- [x] Capture emulator evidence for ReportFragment runtime wiring at `artifacts/android-ui-snapshots/20260430-142758`, including Room-backed Active Focus, Daily Avg, Top App, and top-app rows.
- [x] Verify ReportFragment runtime wiring with Android Gradle unit/build/androidTest build, emulator screenshots, full `.NET` tests (397), full `.NET` build, and coverage generation at 91.7% line coverage.
- [x] Add RED/GREEN architecture coverage requiring the launcher toolbar to use explicit 56dp actionBarSize, matching minHeight, 16dp title insets, and 16sp title text.
- [x] Capture emulator evidence for the compact toolbar contract at `artifacts/android-ui-snapshots/20260430-143723`.
- [x] Verify compact toolbar slice with Android Gradle unit/build/androidTest build, emulator screenshots, full `.NET` tests (398), full `.NET` build, and coverage generation at 91.7% line coverage.
- [x] Install and read `mobile-android-design`; apply only the Material/accessibility guidance that does not conflict with the PRD's Kotlin XML/View MVP.
- [x] Add RED/GREEN architecture coverage requiring compact bottom navigation labels to stay visible above Android system navigation without restoring the oversized overlay label row.
- [x] Capture emulator evidence for compact readable bottom navigation labels at `artifacts/android-ui-snapshots/20260430-151005`, including `09-main-shell.png`.
- [x] Verify compact bottom-navigation label slice with Android Gradle unit/build/androidTest build, emulator screenshots, full `.NET` tests (400), full `.NET` build, and coverage generation at 91.7% line coverage.

## Milestone 47: Chrome Native Messaging Sandbox Evidence

- [x] Audit Chrome/native messaging acceptance docs, script, and Windows browser tests for sandbox safety coverage.
- [x] Add a failing Windows browser script test requiring grouped sandbox safety evidence in report and manifest artifacts.
- [x] Add `nativeMessagingSafetyEvidence` rows for sandboxed Chrome profile, user Chrome preservation, scoped HKCU test host, temp acceptance DB, and cleanup restore/remove behavior.
- [x] Update browser tracking policy and WPF resume state for the native messaging safety evidence slice.
- [x] Run focused Windows browser script test.
- [x] Run Windows tests.
- [x] Run solution build and solution tests.
- [x] Run Chrome native messaging cleanup-only dry-run and confirm report/manifest safety evidence.
## Milestone 48: WPF RealStart Safety Evidence

- [x] Audit WPF/browser/runtime acceptance gaps after Chrome native messaging sandbox evidence.
- [x] Add a failing WPF App/tool test requiring grouped RealStart safety evidence in report and manifest artifacts.
- [x] Add `realStartSafetyEvidence` rows for explicit local SQLite DB, test-only device id, server sync opt-in state, and launched-process-scoped cleanup.
- [x] Update WPF resume state for the RealStart safety evidence slice.
- [x] Run focused WPF App/tool test.
- [x] Run WPF App tests.
- [x] Run solution build and solution tests.
- [x] Run WPF UI acceptance script and confirm RealStart safety evidence appears in latest artifacts.
## Milestone 49: WPF Acceptance Root RealStart Evidence Links

- [x] Audit WPF/browser/runtime acceptance TODOs after Milestone 48 and identify the composed root WPF report as the next WPF-only evidence gap.
- [x] Add a failing WPF App script test requiring the root acceptance report to link RealStart report/manifest evidence.
- [x] Add `## RealStart Evidence Artifacts` with `real-start-report.md`, `real-start-manifest.json`, `realStartEvidence`, and `realStartSafetyEvidence` references.
- [x] Update WPF resume state for the root-report evidence link slice.
- [x] Run focused WPF App script test.
- [x] Run WPF App tests.
- [x] Run solution build and solution tests.
- [x] Run WPF UI acceptance script and confirm root report links RealStart evidence artifacts.
## Milestone 50: WPF Acceptance Root Manifest

- [x] Audit WPF/browser/runtime acceptance TODOs after Milestone 49 and identify the missing composed root manifest as the next WPF-only evidence gap.
- [x] Add a failing WPF App script test requiring the root acceptance run to emit `manifest.json` summarizing RealStart and TrackingPipeline artifacts.
- [x] Add root manifest fields for RealStart report/manifest, `realStartEvidence`, `realStartSafetyEvidence`, TrackingPipeline snapshot report/manifest, and visual review prompt.
- [x] Update WPF resume state for the root-manifest evidence slice.
- [x] Run focused WPF App script test.
- [x] Run WPF App tests.
- [x] Run solution build and solution tests.
- [x] Run WPF UI acceptance script and confirm root manifest summarizes child evidence artifacts.
## Milestone 51: WPF Acceptance Root Manifest Privacy Boundary

- [x] Audit WPF/browser/runtime acceptance TODOs after Milestone 50 and identify missing machine-readable privacy boundary evidence in the root WPF acceptance manifest.
- [x] Add a failing WPF App script test requiring root `manifest.json` privacy boundary entries.
- [x] Add root manifest `privacyBoundary` rows for no keystrokes, no product screen telemetry, local-only app UI screenshots, server sync disabled unless explicitly allowed, and temp SQLite databases only.
- [x] Update WPF resume state for the root-manifest privacy boundary slice.
- [x] Run focused WPF App script test.
- [x] Run WPF App tests.
- [x] Run solution build and solution tests.
- [x] Run WPF UI acceptance script and confirm root manifest includes privacy boundary evidence.

## Milestone 52: WPF Root Report Privacy Boundary Evidence

- [x] Audit WPF/browser/runtime acceptance TODOs after Milestone 51 and identify the human-readable root report privacy boundary evidence gap.
- [x] Add a failing WPF App script test requiring root `report.md` Privacy Boundary rows for explicit server sync opt-in and temp SQLite-only acceptance.
- [x] Update the WPF UI acceptance script so the root report mirrors the root manifest privacy boundary evidence.
- [x] Update WPF resume state for the root-report privacy boundary evidence slice.
- [x] Run focused WPF App script test.
- [x] Run WPF App tests.
- [x] Run solution tests and solution build.
- [x] Run WPF UI acceptance script and confirm root report includes complete privacy boundary evidence at `artifacts/wpf-ui-acceptance/20260430-141948`.

## Milestone 53: WPF Acceptance Run Configuration Evidence

- [x] Audit WPF/browser/runtime acceptance TODOs after Milestone 52 and identify the missing grouped root run-configuration evidence.
- [x] Add a failing WPF App script test requiring root acceptance report/manifest run configuration evidence.
- [x] Add root report `## Run Configuration` rows for acceptance seconds, server sync allowed, and app path.
- [x] Add root manifest `runConfiguration` with `seconds`, `allowServerSync`, and `appPath`.
- [x] Update WPF resume state for the run-configuration evidence slice.
- [x] Run focused WPF App script test.
- [x] Run WPF App tests.
- [x] Run solution tests and solution build.
- [x] Run WPF UI acceptance script and confirm root run-configuration evidence at `artifacts/wpf-ui-acceptance/20260430-143517`.

## Milestone 54: WPF Acceptance Snapshot Matrix Evidence

- [x] Audit WPF/browser/runtime acceptance TODOs after Milestone 53 and identify the missing root snapshot mode/viewport matrix run-configuration evidence.
- [x] Add a failing WPF App script test requiring root acceptance report/manifest run configuration to include snapshot mode and viewport widths.
- [x] Name snapshot mode and viewport widths once in `scripts/run-wpf-ui-acceptance.ps1`, pass them to the snapshot tool, and write them into root report/manifest evidence.
- [x] Update WPF resume state for the snapshot matrix evidence slice.
- [x] Run focused WPF App script test.
- [x] Run WPF App tests.
- [x] Run solution tests and solution build.
- [x] Run WPF UI acceptance script and confirm root snapshot mode/viewport matrix evidence at `artifacts/wpf-ui-acceptance/20260430-144509`.

## Milestone 55: WPF Acceptance Final-Audit Recommendation Evidence

- [x] Audit WPF acceptance/runtime evidence against `total_todolist.md`, `docs/wpf-ui-acceptance-checklist.md`, `docs/runtime-pipeline.md`, and current WPF scripts after Milestone 54.
- [x] Confirm no remaining WPF implementation TODO is open.
- [x] Add a failing WPF App script test requiring root report next-recommended WPF checks/fixes evidence.
- [x] Add root report `## Next Recommended WPF Checks` with no-open-WPF-runtime-TODO, final WPF gate, and Android-owned physical-device measurement guidance.
- [x] Update WPF resume state for the final-audit recommendation evidence slice.
- [x] Run focused WPF App script test.
- [x] Run WPF App tests.
- [x] Run solution build.
- [x] Run WPF UI acceptance script and confirm root next-recommended WPF checks at `artifacts/wpf-ui-acceptance/20260430-152612`.
- [x] Attempt solution tests; WPF-owned projects passed before the run failed in Android-owned architecture coverage.

## Milestone 55: Android Current Focus Wireframe Parity

- [x] Audit the launcher fragment UI against the user-provided Android XML skeleton and Figma import artifact.
- [x] Add a failing Android architecture/layout test for the highest-impact feasible mismatch.
- [x] Correct the Dashboard fragment Current Focus card to match the compact horizontal wireframe shape.
- [x] Run focused Android architecture tests.
- [x] Run Android Gradle `testDebugUnitTest assembleDebug assembleDebugAndroidTest`.
- [x] Run Android screenshot automation if an emulator or device is available.
- [x] Update Android docs and resume state for this slice.
- [x] Commit and push the Android slice.

## Final Definition Of Done

- [x] All PRD requirements reflected in code/tests/docs after Original Intent Restoration; physical-device resource measurement remains tracked as an external hardware blocker.
- [x] All core logic built TDD-first.
- [x] All relevant tests pass: latest full `.NET` run passed 411 tests, Android Gradle unit/build/androidTest build passed, WPF acceptance passed, Chrome native cleanup-only acceptance passed, and Android screenshots passed.
- [x] All builds pass: latest `.NET` solution build and Android debug/androidTest builds passed.
- [x] Safety/privacy excluded scopes are not implemented.
- [x] Local DB/server integrated DB separation is preserved.
- [x] Daily integrated summary works across Windows + Android.
- [x] Final documentation is complete for the current environment; physical-device resource measurement remains documented as an external blocker.
- [x] Final commit is pushed to `origin`.

## Android Check Package 2026-04-30

- [x] Add explicit Android feature verification checklist as `android_check_todo.md`.
- [x] Run connected-emulator Android architecture tests, Gradle unit/build/androidTest build, UI screenshot automation, and resource measurement for the Android check package.
- [x] Generate before/after PNG evidence for Android features A01-A17 under `artifacts/android-check/latest/`.
- [x] Document that emulator evidence does not close the remaining physical-device resource measurement TODO.

## WPF Check Package 2026-04-30

- [x] Add explicit WPF feature verification checklist as `wpf_check_todo.md`.
- [x] Map WPF runtime, SQLite, outbox, dashboard, browser, native messaging, UI acceptance, and privacy features to test/evidence targets.
- [x] Document WPF validation commands and expected visual review artifacts.

## WPF Same-Window Browser Navigation Evidence 2026-04-30

- [x] Add TDD coverage for Chrome same-window navigation (`youtube.com -> github.com -> chatgpt.com`) without closing the Chrome FocusSession.
- [x] Update WPF TrackingPipeline acceptance PNG capture names and semantic checks for same-window navigation, second Chrome process/window, Notepad, and File Explorer.
- [x] Regenerate local WPF check package at `artifacts/wpf-check/latest/`.
- [x] Run full solution test/build, WPF UI acceptance, and coverage.
- [x] Current coverage: line 91.9% (3774/4104), branch 70.8% (536/757).

## Milestone 56: WPF Same-Window Browser Navigation Regression Tests

- [x] Audit post-acceptance WPF/browser coverage and identify repeated same-window Chrome domain changes as the next regression gap.
- [x] Add coordinator behavior coverage for `youtube.com -> github.com -> chatgpt.com` in one Chrome HWND/PID.
- [x] Add MainWindow vertical behavior coverage proving current domain, SQLite WebSessions, outbox rows, Web Focus summary, and Web Sessions grid update before Stop.
- [x] Confirm domain-only storage redacts full URL path/query values from SQLite and outbox payloads.
- [x] Run focused WPF App tests.
- [x] Run full solution tests and solution build.
- [x] Run coverage generation and WPF UI acceptance.
- [x] Update WPF resume/checklist docs for the regression slice.

## Milestone 57: Chrome Native Messaging Cleanup Failure Evidence

- [x] Add RED test requiring sandbox Chrome process cleanup, temp profile cleanup, and temp work root cleanup failures to appear in Chrome native acceptance artifacts.
- [x] Add `cleanupFailures` report/manifest evidence and a grouped `Cleanup failures` safety row.
- [x] Replace silent cleanup catches for sandbox Chrome/temp profile/temp root with explicit warnings and artifact evidence.
- [x] Run focused Chrome native messaging script tests.
- [x] Run Chrome native cleanup-only dry-run and confirm artifacts.

## Milestone 58: Server Raw Event Unknown Device Guard

- [x] Add relational API test proving unknown-device raw-event upload returns controlled per-item Error.
- [x] Prevent orphan raw-event persistence before relational FK failures.
- [x] Update accepted/duplicate raw-event test to seed a registered device.
- [x] Run focused server raw-event API tests.

## Milestone 59: Android Usage Access Onboarding Gate

- [x] Add MainActivity Usage Access gate with Dashboard when granted and permission onboarding when missing.
- [x] Add permission onboarding settings button intent handling.
- [x] Add Robolectric tests for missing/granted Usage Access and settings intent.
- [x] Add explicit permission onboarding screenshot target `13-permission-onboarding.png`.
- [x] Run Android unit/build/androidTest verification.
- [x] Run Android emulator screenshot automation and capture `artifacts/android-ui-snapshots/20260430-171242`.

## Milestone 60: Cross-Slice Verification 2026-04-30 17:15

- [x] Full `.NET` solution tests passed: 411 tests.
- [x] Full `.NET` solution build passed.
- [x] Coverage generated: line 91.9% (3783/4113), branch 70.8% (538/759).
- [x] WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260430-170819`.
- [x] Chrome native cleanup-only dry-run passed at `artifacts/chrome-native-acceptance/20260430-170711`.
- [x] Android UI screenshots passed at `artifacts/android-ui-snapshots/20260430-171242`.

## Milestone 61: Chrome Native Messaging Test Host And Allowed Origins Evidence

- [x] Add RED tests requiring Chrome native acceptance to reject the production host name before any production registry path can be targeted.
- [x] Add RED tests requiring acceptance artifacts to expose deterministic `allowed_origins` evidence.
- [x] Restrict `scripts/run-chrome-native-message-acceptance.ps1` to the test-only host `com.woong.monitorstack.chrome_test`.
- [x] Add `allowedOrigins` to the Chrome native acceptance manifest and grouped sandbox safety evidence.
- [x] Run focused Chrome native script tests.
- [x] Run production-host rejection dry-run.
- [x] Run test-host cleanup-only dry-run.

### Milestone 61 Verification Update

- [x] Non-launch Chrome native dry-run passed and wrote `allowedOrigins` evidence to `artifacts/chrome-native-acceptance/latest/manifest.json`.
- [x] Full `.NET` solution tests passed: 414 tests.
- [x] Full `.NET` solution build passed with 0 warnings and 0 errors.
- [x] Coverage generated: line 91.7% (3823/4166), branch 70.9% (544/767).

## Milestone 62: Android Usage Access Return Recheck And Collection Status

- [x] Add Android behavior tests for Usage Access missing, granted, and granted-after-settings-return flows.
- [x] Re-check Usage Access in `MainActivity.onResume` when the Dashboard tab is active.
- [x] Reconcile UsageStats collection scheduling with `AndroidUsageCollectionScheduler`.
- [x] Show visible permission/collection status text on the onboarding screen.
- [x] Run Android Gradle unit/build/androidTest build.
- [x] Regenerate Android emulator screenshots at `artifacts/android-ui-snapshots/20260430-172700`.

## Milestone 63: Server Daily Summary Local Midnight Split

- [x] Add relational server test for active, idle, and web sessions crossing `Asia/Seoul` local midnight.
- [x] Split focus and web durations into requested local-date segments during daily summary generation.
- [x] Preserve idle exclusion from active totals and include web durations on the correct local date.
- [x] Verify through full `.NET` test/build and coverage runs.

## Milestone 64: Android Runtime Last-Known Location Reader

- [x] Add RED tests for Android last-known GPS/network/passive location reading.
- [x] Implement `AndroidLastKnownLocationReader` with provider failure handling and freshest-reading selection.
- [x] Wire production `LocationContextCollectionRunner.create` to use the Android reader instead of `NoopRuntimeLocationReader`.
- [x] Preserve existing privacy gates: location context opt-in, foreground permission, and precise coordinate opt-in remain required before coordinates are stored.
- [x] Run focused location tests.
- [x] Run Android Gradle unit/build/androidTest build.
- [x] Regenerate Android emulator screenshots at `artifacts/android-ui-snapshots/20260430-174439`.

## Milestone 65: QA Checklists And Coverage Triage

- [x] Add `server_check_todo.md` mapping server API/idempotency/summary/provider validation status and gaps.
- [x] Add `docs/coverage-gap-triage.md` separating intentional OS/bootstrap low coverage from actionable test gaps.
- [x] Update WPF checklist to point at coverage triage.
- [x] Note WPF check pointer package commit `4a89444` from the WPF agent.

### Milestone 64-65 Verification Update

- [x] Full `.NET` solution tests passed: 415 tests.
- [x] Full `.NET` solution build passed with 0 warnings and 0 errors.
- [x] Coverage generated: line 91.7% (3823/4166), branch 70.9% (544/767).
- [x] Android Gradle unit/build/androidTest build passed.
- [x] Android UI screenshots passed at `artifacts/android-ui-snapshots/20260430-174439`.

## Milestone 66: Server Device Duplicate Registration Update Coverage

- [x] Add API regression test proving duplicate `userId + platform + deviceKey` registration updates device name, timezone, and `LastSeenAtUtc`.
- [x] Verify the duplicate registration keeps one persisted device row and returns `isNew = false`.
- [x] Update `server_check_todo.md` device-registration coverage.
- [x] Run focused server device registration test.

### Milestone 66 Verification Update

- [x] Full `.NET` solution tests passed: 417 tests.
- [x] Full `.NET` solution build passed with 0 warnings and 0 errors.
- [x] Coverage generated: line 91.7% (3823/4166), branch 70.9% (544/767).
- [x] Android emulator UI snapshots refreshed at `artifacts/android-ui-snapshots/20260430-174439` and resource measurements at `artifacts/android-resource-measurements/20260430-174552`.

## Milestone 67: Server Focus Upload Mixed-Batch Idempotency

- [x] Add RED relational test for focus upload batch containing an existing duplicate, one new session, and an intra-batch duplicate of that new session.
- [x] Fix `FocusSessionUploadService` to track existing and newly accepted `clientSessionId` values within the request batch.
- [x] Verify per-item statuses are Duplicate, Accepted, Duplicate and only existing + one new row persist.
- [x] Run focused mixed-batch test and focus-session upload tests.
- [x] Update `server_check_todo.md` focus upload coverage.

### Milestone 67 Verification Update

- [x] Full `.NET` solution tests passed: 418 tests.
- [x] Full `.NET` solution build passed with 0 warnings and 0 errors.
- [x] Coverage generated: line 91.7% (3830/4173), branch 70.9% (544/767).

## Milestone 68: Server Web Upload Mixed-Batch Idempotency

- [x] Add RED relational test for web upload batch containing an existing duplicate, one new session, an intra-batch duplicate, and a missing focus parent.
- [x] Fix `WebSessionUploadService` to track existing and newly accepted `clientSessionId` values within the request batch.
- [x] Verify per-item statuses are Duplicate, Accepted, Duplicate, Error and only existing + one new row persist.
- [x] Run focused mixed-batch test and web-session upload tests.
- [x] Update `server_check_todo.md` web upload coverage.

### Milestone 68 Verification Update

- [x] Full `.NET` solution tests passed: 419 tests.
- [x] Full `.NET` solution build passed with 0 warnings and 0 errors.
- [x] Coverage generated: line 91.7% (3838/4181), branch 70.9% (544/767).

## Milestone 69: Server Raw Event Mixed-Batch Idempotency

- [x] Add RED relational test for raw event upload batch containing an existing duplicate, one new event, and an intra-batch duplicate.
- [x] Fix `RawEventUploadService` to track existing and newly accepted `clientEventId` values within the request batch.
- [x] Verify per-item statuses are Duplicate, Accepted, Duplicate and only existing + one new row persist.
- [x] Run focused mixed-batch test and raw-event upload tests.
- [x] Update `server_check_todo.md` raw event upload coverage.

### Milestone 69 Verification Update

- [x] Full `.NET` solution tests passed: 421 tests.
- [x] Full `.NET` solution build passed with 0 warnings and 0 errors.
- [x] Coverage generated: line 91.8% (3851/4194), branch 71.0% (546/769).
- [x] Concurrent WPF commits `ddfc617` and `024dec1` preserve domain-only browser metadata in fallback and current-focus UI.
- [x] Concurrent Android commit `4fd5416` records location opt-in UI evidence.

## Milestone 70: Server Location Context Relational Upload Coverage

- [x] Add RED relational test for location upload batch containing an existing duplicate, one new context, and an intra-batch duplicate.
- [x] Fix `LocationContextUploadService` to track existing and newly accepted `clientContextId` values within the request batch.
- [x] Add relational API coverage for unregistered location device returning per-item `Error` and zero persisted rows.
- [x] Verify focused location context upload tests.
- [x] Update `server_check_todo.md` location context coverage.

### Milestone 70 Verification Update

- [x] Full `.NET` solution tests passed: 423 tests.
- [x] Full `.NET` solution build passed with 0 warnings and 0 errors.
- [x] Coverage generated: line 92.0% (3865/4201), branch 71.1% (547/769).

## Milestone 71: Server Date Range Invalid Input HTTP Handling

- [x] Add RED HTTP tests for malformed `from`, malformed `to`, `from > to`, invalid timezone id, and missing query value.
- [x] Replace unhandled date parsing/range/timezone failures in `/api/statistics/range` with controlled `400 BadRequest` responses.
- [x] Preserve successful date-range aggregation behavior.
- [x] Update `server_check_todo.md` invalid input coverage.

### Milestone 71 Verification Update

- [x] Full `.NET` solution tests passed: 429 tests.
- [x] Full `.NET` solution build passed with 0 warnings and 0 errors.
- [x] Coverage generated: line 91.7% (3916/4268), branch 71.2% (555/779).
- [x] Concurrent WPF commit `04e9fd7` clarifies origin-only web URL mode as domain-only.
- [x] Concurrent Android commit `50f8790` records Usage Access Settings handoff screenshots/evidence.

## Milestone 72: Server Date Range Local-Midnight Partial Allocation

- [x] Add RED range statistics test proving a cross-midnight active focus session contributes only the in-range local-date portion.
- [x] Cover equivalent idle and web-session portions in the same HTTP behavior test.
- [x] Reuse local-date duration splitting for `DailySummaryQueryService.GetRangeAsync`.
- [x] Update `server_check_todo.md` cross-midnight range coverage.

### Milestone 72 Verification Update

- [x] Full `.NET` solution tests passed: 430 tests.
- [x] Full `.NET` solution build passed with 0 warnings and 0 errors.
- [x] Coverage generated: line 91.7% (3908/4260), branch 71.5% (557/779).

## Milestone 73: Server Raw Event Payload Privacy Guard

- [x] Add RED relational test proving safe raw-event metadata can persist while forbidden user-input/content payload fields return per-item `Error`.
- [x] Reject recursive forbidden raw-event payload property names such as `typedText`, `password`, `clipboardText`, `pageContent`, `screenshot`, and touch-coordinate fields.
- [x] Preserve duplicate-first idempotency and safe metadata upload behavior.
- [x] Update `server_check_todo.md` raw-event privacy guard coverage.

### Milestone 73 Verification Update

- [x] Full `.NET` solution tests passed: 431 tests.
- [x] Full `.NET` solution build passed with 0 warnings and 0 errors.
- [x] Coverage generated: line 91.5% (3957/4320), branch 71.5% (570/797).
- [x] Concurrent WPF commit `f6916e9` updates DB write time when a web session persists without a focus close.
- [x] Concurrent Android commit `e127a52` records Room seeded-dashboard evidence.

### Milestone 73 Server Checklist Closure Note

- [x] Marked PostgreSQL-dependent concurrency idempotency work as `[blocked]` because it requires a real PostgreSQL/Testcontainers fixture.
- [x] Current `server_check_todo.md` has no remaining non-blocked unchecked server validation items.

## Milestone 74: Checklist Evidence Hygiene

- [x] Update `wpf_check_todo.md` so `artifacts/wpf-check/latest/` is documented as the current consolidated WPF check package, not a future target.
- [x] Update `android_check_todo.md` with latest Android resource measurement artifact `artifacts/android-resource-measurements/20260430-174552/`.
- [x] Preserve external blocker wording for physical-device Android resource measurement.

## Milestone 75: External Blocker Readiness Check

- [x] Add RED architecture tests for a read-only external blocker checker.
- [x] Add `scripts/check-external-blockers.ps1` to report physical Android device and Docker daemon readiness.
- [x] Verify the script writes `report.md` and `manifest.json` without registry, screenshot, or input-capture behavior.
- [x] Run the checker locally and record that only the emulator is connected and Docker daemon is unavailable.
- [x] Ignore `artifacts/external-blockers/` outputs.

### Milestone 75 Verification Update

- [x] Full `.NET` solution tests passed: 436 tests.
- [x] Full `.NET` solution build passed with 0 warnings and 0 errors.
- [x] Coverage generated: line 91.5% (3957/4320), branch 71.5% (570/797).
- [x] External blocker check artifact generated at `artifacts/external-blockers/20260430-184601/` with status `BLOCKED` for physical-device and Docker-daemon readiness.

## Milestone 76: Server PostgreSQL Testcontainers Validation

- [x] Add RED PostgreSQL/Testcontainers tests for Npgsql migration application and provider constraint enforcement.
- [x] Add PostgreSQL-specific legacy `web_sessions.ClientSessionId` backfill test before the required unique index is applied.
- [x] Add `PostgresTestDatabase` Testcontainers fixture and explicit `PostgresFact` gating for routine `dotnet test` runs.
- [x] Add `scripts/run-server-postgres-validation.ps1` for explicit Docker-backed validation.
- [x] Run PostgreSQL validation successfully with artifact `artifacts/server-postgres-validation/20260430-185823/`.
- [x] Update server DB strategy, server checklist, completion audit, and QA blocker docs.

### Milestone 76 Verification Update

- [x] `dotnet restore Woong.MonitorStack.sln --configfile NuGet.config` passed.
- [x] Standard `.NET` solution tests passed: 436 passed, 2 PostgreSQL-explicit tests skipped unless `WOONG_MONITOR_RUN_POSTGRES_TESTS=1`.
- [x] Full `.NET` solution build passed with 0 warnings and 0 errors.
- [x] Coverage generated: line 91.5% (3957/4320), branch 71.5% (570/797).
- [x] Explicit PostgreSQL/Testcontainers validation passed via `scripts/run-server-postgres-validation.ps1`.

## Milestone 77: Server PostgreSQL Concurrent Idempotency

- [x] Add RED PostgreSQL concurrency tests for duplicate focus, web, raw event, and location uploads.
- [x] Fix focus/web/raw/location upload services so save-time unique index races return idempotent `Duplicate` statuses instead of surfacing `DbUpdateException`.
- [x] Verify each concurrent upload leaves one persisted row in PostgreSQL.
- [x] Run explicit PostgreSQL/Testcontainers validation successfully with artifact `artifacts/server-postgres-validation/20260430-190958/`.
- [x] Close the server PostgreSQL concurrency checklist item.

### Milestone 77 Verification Update

- [x] Standard `.NET` solution tests passed: 436 passed, 6 explicit PostgreSQL tests skipped unless `WOONG_MONITOR_RUN_POSTGRES_TESTS=1`.
- [x] Full `.NET` solution build passed with 0 warnings and 0 errors.
- [x] Coverage generated: line 90.1% (3965/4398), branch 70.6% (570/807).
- [x] Explicit PostgreSQL/Testcontainers validation passed with 6 PostgreSQL tests run and 0 skipped.

## Milestone 78: Android Physical Device Measurement Guard

- [x] Add RED architecture test for physical-device-required resource measurement mode.
- [x] Add `-RequirePhysicalDevice` to `scripts/run-android-resource-measurement.ps1`.
- [x] Ensure emulator-only device lists write `BLOCKED` artifacts instead of closing physical-device TODOs.
- [x] Run local physical-device-required check and record artifact `artifacts/android-resource-measurements/20260430-191835/`.
- [x] Update Android resource measurement docs and Android checklist.

### Milestone 78 Verification Update

- [x] Standard `.NET` solution tests passed: 437 passed, 6 explicit PostgreSQL tests skipped unless `WOONG_MONITOR_RUN_POSTGRES_TESTS=1`.
- [x] Full `.NET` solution build passed with 0 warnings and 0 errors.
- [x] Coverage generated: line 90.1% (3965/4398), branch 70.6% (570/807).

## Milestone 79: README Runbook Refresh

- [x] Rewrite README with WPF app execution, temp SQLite DB usage, WPF smoke/acceptance/screenshot commands.
- [x] Add Android emulator startup, Gradle build/test/install/launch, UI snapshot, resource measurement, and physical-device-only measurement commands.
- [x] Add ASP.NET Core server, PostgreSQL connection string, EF migration, server run, and PostgreSQL/Testcontainers validation instructions.
- [x] Add standard .NET, Android, coverage, and focused QA test commands.
- [x] Preserve privacy boundaries and local DB/server DB separation in README.

### Milestone 79 Verification Update

- [x] `git diff --check` passed.
- [x] Standard `.NET` solution tests passed: 437 passed, 6 explicit PostgreSQL tests skipped unless `WOONG_MONITOR_RUN_POSTGRES_TESTS=1`.
- [x] Full `.NET` solution build passed with 0 warnings and 0 errors.

## 2026-05-01 WPF Legacy SQLite Startup Fix

- [x] Reproduced WPF startup failure from the default local SQLite DB: old `web_session` schema lacked `capture_method`.
- [x] Added regression coverage proving `SqliteWebSessionRepository.Initialize()` migrates legacy `web_session` tables without losing rows.
- [x] Added automatic nullable-column migration for `capture_method`, `capture_confidence`, and `is_private_or_unknown`.
- [x] Verified the WPF exe starts against the default `%LOCALAPPDATA%\WoongMonitorStack\windows-local.db` and shows `Woong Monitor Stack`.
- [x] Added a README Quick Start clarifying that SQLite does not need to be launched separately.

## 2026-05-01 WPF Local Database Management UI

- [x] Added Settings UI for current SQLite DB path/status, create/switch DB, load existing DB, and delete/recreate local DB.
- [x] Added a testable dashboard database controller port so Presentation stays free of WPF dialogs and filesystem logic.
- [x] Added WPF App database controller implementation with SaveFileDialog/OpenFileDialog/confirmation dialog in the App layer only.
- [x] Made SQLite repositories use a switchable connection-string provider so dashboard/tracking reads and writes follow the selected DB without restarting.
- [x] Added controller, ViewModel, XAML AutomationId, and legacy schema migration regression tests.
- [x] Verified exact default local DB startup by launching the WPF exe; window title `Woong Monitor Stack` appeared and process was responding.
- [x] Validation: full `.NET` tests passed, full `.NET` build passed, coverage generated at 88.4% line and 69.7% branch coverage.

## 2026-05-01 WPF Chrome Switch Crash Fix And Runtime Logging

- [x] Diagnose Chrome/browser switch crash from Windows Application Event Log.
- [x] Add regression test for legacy `web_session.url TEXT NOT NULL` SQLite schema.
- [x] Rebuild legacy `web_session` tables to allow nullable URL for domain-only privacy storage without losing existing rows.
- [x] Add WPF runtime log sink and Settings runtime log path display.
- [x] Catch dashboard Start/Stop/Poll/Sync command exceptions and surface `Runtime error` live-event rows instead of terminating the app.
- [x] Add WPF tick regression coverage proving poll errors keep the window open.
- [x] Added a Settings command to open the runtime log folder and include the latest log excerpt in WPF acceptance artifacts.
- [x] Added behavior tests for the runtime log folder command, Settings AutomationIds, and root WPF acceptance report/manifest runtime-log evidence.
- [x] Validation: full `.NET` tests passed (478 passed, 6 skipped), Release build passed, WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260501-150730`, and coverage generated at 88.0% line / 69.5% branch.

### Validation Update

- [x] Full `.NET` solution tests passed: 450 passed, 6 explicit PostgreSQL/Testcontainers tests skipped by default.
- [x] Full `.NET` solution build passed with 0 warnings and 0 errors.
- [x] Coverage generated: line 88.6% (4277/4822), branch 69.7% (610/875).
- [x] WPF exe launched against the default local DB and remained responsive: `Woong Monitor Stack` window was running.

## 2026-05-01 Windows Taskbar, Release CI, And MSIX Packaging

- [x] Explicitly set `MainWindow.ShowInTaskbar=True`.
- [x] Changed the system close/X path so it minimizes the WPF app to the Windows taskbar instead of closing the process.
- [x] Added Settings `Exit app` as the explicit shutdown path.
- [x] Added presentation/app tests for explicit application lifetime, taskbar minimize-on-X behavior, and Settings Exit command binding.
- [x] Documented Release build/run commands:
  - `dotnet build Woong.MonitorStack.sln -c Release --no-restore -m:1 -v minimal`
  - `dotnet run --configuration Release --project src\Woong.MonitorStack.Windows.App\Woong.MonitorStack.Windows.App.csproj`
- [x] Added Windows GitHub Actions workflow for restore, Release build, Release test, publish, unsigned MSIX package, and artifact upload.
- [x] Added MSIX manifest template and local packaging/install scripts.
- [x] MSIX install trust stays explicit and CurrentUser-scoped; no LocalMachine certificate store is used.
- [x] Fixed MSIX packaging script to copy published app files into the package layout and fail when native tools fail.
- [x] Generated local unsigned MSIX at `artifacts/windows-msix/WoongMonitorStack.Windows.msix`.
- [x] Added real release signing certificate/secrets strategy for CI MSIX artifacts; CI falls back to ephemeral test certificate only when secrets are absent.

### Validation Update

- [x] `dotnet restore Woong.MonitorStack.sln --configfile NuGet.config` passed.
- [x] `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed: 460 passed, 6 explicit PostgreSQL/Testcontainers tests skipped by default.
- [x] `dotnet build Woong.MonitorStack.sln -c Release --no-restore -m:1 -v minimal` passed with 0 warnings and 0 errors.
- [x] `dotnet test Woong.MonitorStack.sln -c Release --no-build -m:1 -v minimal` passed: 460 passed, 6 explicit PostgreSQL/Testcontainers tests skipped by default.
- [x] `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\package-windows-msix.ps1` passed and created an unsigned MSIX.
- [x] Coverage generated: line 87.2% (4232/4851), branch 68.1% (607/891).
- [x] Release WPF run smoke passed with temp DB and auto-start disabled: `dotnet run --configuration Release --project src\Woong.MonitorStack.Windows.App\Woong.MonitorStack.Windows.App.csproj` started `Woong.MonitorStack.Windows.App` and the process was responding.

## 2026-05-01 Windows Signed MSIX CI Artifact

- [x] Added architecture tests requiring the Windows CI workflow to create a signed MSIX with `-CreateTestCertificate`.
- [x] CI uploads the installable MSIX bundle: `.msix`, public `.cer`, `install-windows-msix.ps1`, and artifact `README.md`.
- [x] CI workflow does not upload generated `.pfx` private keys.
- [x] `scripts\package-windows-msix.ps1 -CreateTestCertificate` generates a per-run CurrentUser test signing certificate, signs the MSIX, exports only the public `.cer` for installation, and removes the transient cert from `CurrentUser\My`.
- [x] Install trust remains explicit and CurrentUser-scoped through `scripts\install-windows-msix.ps1 -TrustCertificate`; no LocalMachine certificate store is used.
- [x] Documented how to download `woong-monitor-windows-msix` from GitHub Actions and install it locally.
- [x] Local signed MSIX generated at `artifacts/windows-msix/WoongMonitorStack.Windows.msix`.
- [x] Public certificate generated at `artifacts/windows-msix/certificates/WoongMonitorStack.Windows.TestSigning.cer`.
- [x] `Get-AuthenticodeSignature` shows signer `CN=WoongMonitorStack`; untrusted-root status before certificate trust is expected.

### Validation Update

- [x] Focused Windows release packaging architecture tests passed: 6 passed.
- [x] `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\package-windows-msix.ps1 -CreateTestCertificate` passed and signed the MSIX.
- [x] `artifacts\windows-msix\install-windows-msix.ps1 ... -TrustCertificate -WhatIf` passed and showed CurrentUser-only trust/install actions.
- [x] `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed: 460 passed, 6 explicit PostgreSQL/Testcontainers tests skipped by default.
- [x] `dotnet build Woong.MonitorStack.sln -c Release --no-restore -m:1 -v minimal` passed with 0 warnings and 0 errors.
- [x] `dotnet test Woong.MonitorStack.sln -c Release --no-build -m:1 -v minimal` passed: 460 passed, 6 explicit PostgreSQL/Testcontainers tests skipped by default.
- [x] Coverage generated: line 88.6% (4299/4851), branch 69.4% (619/891).

### Remaining Windows Release Work

- [x] Replace per-run test certificate with a stable release signing certificate/secrets strategy before public distribution.
- [x] Add a tag-based release workflow after signing policy is finalized.

## 2026-05-01 Windows MSIX 0x800B010A Certificate Trust Fix

- [x] Reproduced the documentation/script gap with RED architecture tests for `0x800B010A`, `TrustScope`, `Cert:\LocalMachine\TrustedPeople`, and Administrator guidance.
- [x] Updated `scripts\install-windows-msix.ps1` with explicit `-TrustScope LocalMachine|CurrentUser`.
- [x] Default MSIX install trust now targets `Cert:\LocalMachine\TrustedPeople`, which is the reliable store for Windows App Installer validation of the self-signed test certificate.
- [x] Added an Administrator guard before importing into `LocalMachine` unless `-WhatIf` is used.
- [x] Regenerated local signed MSIX artifact and artifact README with the LocalMachine install command.
- [x] Updated README and `docs/windows-release-msix.md` with the `0x800B010A` fix path.

### Validation Update

- [x] Focused Windows release packaging architecture tests passed: 6 passed.
- [x] `scripts\package-windows-msix.ps1 -CreateTestCertificate` passed and signed the MSIX.
- [x] `artifacts\windows-msix\install-windows-msix.ps1 ... -TrustScope LocalMachine -WhatIf` passed and showed LocalMachine TrustedPeople trust action.
- [x] `artifacts\windows-msix\install-windows-msix.ps1 ... -TrustScope CurrentUser -WhatIf` passed and remains available for development experiments.
- [x] `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed: 460 passed, 6 explicit PostgreSQL/Testcontainers tests skipped by default.
- [x] `dotnet build Woong.MonitorStack.sln -c Release --no-restore -m:1 -v minimal` passed with 0 warnings and 0 errors.
- [x] `dotnet test Woong.MonitorStack.sln -c Release --no-build -m:1 -v minimal` passed: 460 passed, 6 explicit PostgreSQL/Testcontainers tests skipped by default.
- [x] Coverage generated: line 88.6% (4299/4851), branch 69.4% (619/891).

## 2026-05-01 WPF Range-Based Charts And Custom Period Picker

- [x] App Focus and Domain Focus chart data now uses horizontal LiveCharts RowSeries, with app/process and site/domain labels on the left category axis.
- [x] Dashboard summary cards, top apps, and top domains aggregate the selected SQLite-backed query range instead of filtering again to only the current local date.
- [x] Today/1h/6h/24h ranges remain available and query persisted local DB data.
- [x] Custom period selection now exposes start/end date pickers, HH:mm time inputs, an Apply button, and a status label in the WPF ControlBar.
- [x] Custom local date/time input is converted to UTC before querying the dashboard data source.
- [x] Added tests for last-24h cross-local-date aggregation, custom range parsing, horizontal chart axes, and custom range UI AutomationIds.

### Validation Update

- [x] Focused Windows Presentation tests passed: 67 passed.
- [x] Focused Windows App tests passed: 155 passed.
- [x] Full solution test/build passed for this slice.

### Validation Update

- [x] dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal passed: 464 passed, 6 explicit PostgreSQL/Testcontainers tests skipped by default.
- [x] dotnet build Woong.MonitorStack.sln -c Release --no-restore -m:1 -v minimal passed with 0 warnings and 0 errors after closing a stale running WPF process that locked Release binaries.

- [x] Coverage collection passed for this slice: line 89.1% (4548/5100), branch 71.6% (734/1024). Report: artifacts/coverage/SummaryGithub.md.

## 2026-05-01 WPF Compact Top Charts, Detail Windows, And App Icons

- [x] Dashboard App Focus chart now shows only the top 3 app/process bars for at-a-glance readability.
- [x] Dashboard Domain Focus chart now shows only the top 3 domain bars for at-a-glance readability.
- [x] Added App Focus and Domain Focus detail window support through a Presentation-level chart details presenter port.
- [x] Detail chart requests are capped at 10 bars.
- [x] App Sessions rows now carry ProcessPath so the WPF app layer can render process icons without leaking WPF into Presentation.
- [x] Added WPF process icon extraction from executable paths with cached ImageSource output and fallback glyphs when the path is missing.
- [x] Wired App Sessions grid to show extracted process icons when available.

### Validation Update

- [x] Focused Windows Presentation tests passed: 70 passed.
- [x] Focused Windows App tests passed: 158 passed.
- [x] dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal passed: 470 passed, 6 explicit PostgreSQL/Testcontainers tests skipped by default.
- [x] dotnet build Woong.MonitorStack.sln -c Release --no-restore -m:1 -v minimal passed with 0 warnings and 0 errors after closing a stale running WPF process that locked Release binaries.
- [x] Coverage collection passed: line 89.4% (4714/5270), branch 73.4% (859/1169). Report: artifacts/coverage/SummaryGithub.md.

## 2026-05-01 WPF Session Duration Display Precision Fix

- [x] Confirmed App/Web session rows were not losing persisted duration data; the UI row formatter was rounding/flooring all positive sub-minute and near-minute durations into `1m`.
- [x] Added RED tests for App Sessions and Web Sessions row durations with second precision (`45s`, `1m 15s`).
- [x] Updated WPF presentation row formatting so App/Web session grids show seconds for short sessions while summary cards keep coarse minute-level totals.

### Validation Update

- [x] Focused Windows Presentation tests passed: 72 passed.
- [x] dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal passed: 472 passed, 6 explicit PostgreSQL/Testcontainers tests skipped by default.
- [x] dotnet build Woong.MonitorStack.sln -c Release --no-restore -m:1 -v minimal passed with 0 warnings and 0 errors.
- [x] Coverage collection passed: line 89.3% (4737/5300), branch 73.4% (870/1185). Report: artifacts/coverage/SummaryGithub.md.

## 2026-05-01 WPF App Focus Chart Long Label Visibility Fix

- [x] Reproduced the user-reported App Focus card issue as a chart-label behavior regression: long executable labels can consume the compact card category axis and make the top 3 bars look absent.
- [x] Added RED tests proving compact dashboard horizontal charts shorten long executable labels while preserving short labels.
- [x] Added detail-window behavior so chart detail views can disable label compaction and preserve full app/process labels for inspection.
- [x] Updated `docs/wpf-ui-plan.md` to document the dashboard-card compaction versus detail-window full-label behavior.

### Validation Update

- [x] Focused Windows Presentation tests passed: 74 passed.
- [x] Focused Windows App tests passed: 158 passed.
- [x] dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal passed: 474 passed, 6 explicit PostgreSQL/Testcontainers tests skipped by default.
- [x] dotnet build Woong.MonitorStack.sln -c Release --no-restore -m:1 -v minimal passed with 0 warnings and 0 errors after closing the stale local Release WPF process that was locking binaries.
- [x] Coverage collection passed: line 88.3% (4480/5073), branch 69.6% (667/957). Report: artifacts/coverage/SummaryGithub.md.

## 2026-05-01 WPF Current Focus Icons And Rounded Badge Surface Fix

- [x] Added RED WPF tests proving `StatusBadge` uses a dedicated `BadgeBackground` instead of painting `UserControl.Background` as a rectangular surface behind the rounded badge.
- [x] Updated Header status badges so Tracking Running, Sync Off, and Privacy Safe colors apply only inside the rounded badge shape.
- [x] Added RED WPF tests requiring Current Focus runtime rows to expose stable left-side icon AutomationIds.
- [x] Added icon/glyph support to reusable `DetailRow` and wired Current Focus rows for tracking state, current app, process, window title, browser domain, session duration, persisted session, poll time, browser capture, DB write, and sync state.
- [x] Split the previous combined `Last DB write / Sync state` label into clearer icon-backed `Last DB write time` and `Sync state` rows.
- [x] Updated `docs/wpf-ui-plan.md` for the badge-surface and Current Focus icon rules.

### Validation Update

- [x] Focused badge RED/GREEN tests passed.
- [x] Focused Current Focus icon RED/GREEN tests passed.
- [x] Focused Windows App tests passed: 158 passed.
- [x] Added RED Presentation test proving current browser-domain display preserves host labels such as `learn.microsoft.com` while still stripping URL path/query secrets.
- [x] WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260501-040320` after restoring host-label display.
- [x] dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal passed: 475 passed, 6 explicit PostgreSQL/Testcontainers tests skipped by default.
- [x] dotnet build Woong.MonitorStack.sln -c Release --no-restore -m:1 -v minimal passed with 0 warnings and 0 errors.
- [x] Coverage collection passed: line 88.2% (4511/5112), branch 69.7% (673/965). Report: artifacts/coverage/SummaryGithub.md.

## 2026-05-01 Windows Stable MSIX Signing Secrets

- [x] Added CI secret path for stable release signing: `WINDOWS_MSIX_CERTIFICATE_BASE64` and `WINDOWS_MSIX_CERTIFICATE_PASSWORD`.
- [x] Kept ephemeral test certificate fallback when release signing secrets are absent.
- [x] Updated MSIX packaging script to export public `WoongMonitorStack.Windows.Signing.cer` from a provided PFX without uploading private keys.
- [x] Updated README and Windows MSIX documentation.
- [x] Validation: full solution tests passed (479 passed, 6 skipped), Release build passed, coverage generated at 88.0% line / 69.5% branch; commit/push pending.
## 2026-05-01 Windows Tag-Based Release Workflow

- [x] Added `.github/workflows/windows-wpf-release.yml` for `v*` tags and manual dispatch.
- [x] Release workflow requires stable signing secrets and does not fall back to test certificates.
- [x] Release workflow builds, tests, publishes, signs, zips the MSIX bundle, uploads artifact, and attaches it to GitHub Release.
- [x] Updated README and Windows MSIX documentation.
- [x] Validation: focused release workflow test passed, full solution tests passed, Release build passed, coverage generated at 88.0% line / 69.5% branch, commit/push pending.
- [x] Added `xunit.runner.json` for Presentation tests to disable collection parallelism and stabilize LiveCharts coverage runs.

## 2026-05-01 WPF domain chart title wrap follow-up

- [x] Split the domain focus chart header so `도메인별` and `집중 시간` render on separate lines while keeping the `상세보기` action button fixed on the right.
- [x] Added WPF behavior coverage for the split title and retained details button placement.
- [x] Ran focused WPF chart tests, full `dotnet test`, and full `dotnet build`.
## 2026-05-01 Android emulator README handoff

- [x] Documented Android SDK environment variables, AVD listing, `Medium_Phone` startup, and boot-complete check in README.
- [x] Documented Android Gradle unit test/build, APK install, launcher start, Usage Access settings handoff, and manual screenshot capture commands.
- [x] Added architecture coverage that keeps the Android emulator/build/install/launch/screenshot README flow documented.
- [x] Ran Android Gradle `testDebugUnitTest assembleDebug`, full `dotnet test`, and full `dotnet build`.
## 2026-05-01 Android emulator manual launch follow-up

- [x] Started the `Medium_Phone` emulator and confirmed `sys.boot_completed=1` on `emulator-5554`.
- [x] Installed and launched the Android debug APK on the emulator.
- [x] Captured a manual Android screenshot at `artifacts/android-check/manual/dashboard.png`.
- [x] Corrected README screenshot guidance to use `adb shell screencap` plus `adb pull` instead of PowerShell raw `exec-out` redirection.
- [x] Re-ran focused README architecture test, full `dotnet test`, and full `dotnet build`.
## 2026-05-01 Android Immediate Usage Collection And Bottom Navigation Fix

- [x] Compact bottom navigation now uses a 72dp base height plus runtime system navigation inset so Dashboard/Sessions/Report/Settings stay visible without hardcoded extra bottom padding.
- [x] MainActivity now performs a foreground immediate UsageStats collection when Dashboard is shown and Usage Access is granted, then refreshes the Room-backed dashboard after collection.
- [x] Collection remains metadata-only: package names and foreground intervals from UsageStatsManager; no typed text, screen content, touch coordinates, or page content are captured.
- [x] Android usage collection defaults to enabled after explicit Usage Access grant while sync remains off/local-only by default.
- [x] Superseded historical behavior: Current Focus previously displayed the latest meaningful tracked external session from Room, so returning from Chrome showed Chrome as the current/latest tracked app. The 2026-05-02 foreground policy now shows `Woong Monitor / com.woong.monitorstack` when Woong is foreground.
- [x] Emulator evidence captured:
  - `artifacts/android-check/manual/android-insets-start2.png`
  - `artifacts/android-check/manual/after-chrome-current-fixed.png`
- [x] Local emulator DB proof after Chrome return: `focus_sessions=9`, `sync_outbox=9`, with latest rows including `com.android.chrome`.
- [x] Validation: `android\\gradlew.bat testDebugUnitTest assembleDebug --no-daemon --stacktrace`, `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`, and `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed.

## 2026-05-01 Android Splash, Permission, And Current Focus Correction

- [x] Added a real app Splash route before Dashboard/Permission routing and kept shell chrome hidden during Splash and permission onboarding.
- [x] Reworked Android 12+ launch splash branding so the OS splash no longer shows the default Android icon; it now uses the Woong bar logo on a white surface.
- [x] Reworked `fragment_splash.xml` toward the supplied app-loading reference: centered blue logo tile, `Woong Monitor`, `Android Focus Tracker`, and Korean loading text.
- [x] Reworked `fragment_permission_onboarding.xml` toward the supplied permission reference: centered shield, Korean headline/body, principles card, and `?ㅼ젙 ?닿린` primary action.
- [x] Superseded historical behavior: Dashboard Current Focus once preferred the latest meaningful tracked external Room session such as `Chrome / com.android.chrome`. The 2026-05-02 foreground policy now prefers proven foreground truth, including Woong Monitor itself when it is foreground.
- [x] Kept Room-backed usage totals and recent sessions separate from Current Focus: persisted app usage still drives Active Focus/Sessions, and the current app panel reports foreground app truth when Android can prove it.
- [x] Added/updated tests for cold-start Splash routing, permission routing, foreground Current Focus precedence, Android wireframe contracts, and Android 12 launch splash branding.
- [x] Emulator evidence captured under `artifacts/android-check/latest/`:
  - `00-os-splash-branded.png`
  - `02-permission-late2.png`
  - `05-dashboard-final.png`
- [x] Validation: `android\\gradlew.bat testDebugUnitTest assembleDebug --no-daemon --stacktrace`, `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`, and `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed.
- [x] Remaining Android UI gap resolved: latest seven-screen visual comparison completed and the largest first-viewport gaps, including dense Dashboard chart/card labels, were tightened.

## 2026-05-01 Android Sessions/App Detail/Report/Settings Parity Slice

- [x] Added failing architecture tests for the reference-style Sessions, App Detail, Report, and Settings structures.
- [x] Added failing Room repository test for selected-package app detail aggregation.
- [x] Implemented FocusSessionDao.queryByPackage and RoomSessionsRepository.loadAppDetail.
- [x] Added AppDetailFragment and Sessions row navigation into package-specific detail.
- [x] Reworked Sessions XML with subtitle, period filters, filter action, total count, card list, and empty state.
- [x] Reworked App Detail XML with selected app identity, total usage, session count, chart container, and package session list.
- [x] Reworked Report XML with period filters, date-range label, summary cards, trend chart container, and top apps card.
- [x] Reworked Settings XML with grouped Permissions, Collection, Sync, Privacy, Location, Privacy/Storage, and Storage sections.
- [x] Fixed Android Korean string mojibake and restored Korean screen titles for the reference screens.
- [x] Captured emulator screenshots for dashboard, sessions, app detail, report, and settings under artifacts/android-check/latest/.
- [x] Validation passed: Android Gradle unit/build, full solution tests, and full solution build.
- [x] Next Android UI TODO resolved: App Detail hourly chart and Report trend chart now use real Room-backed datasets.
- [x] Next Android UI TODO resolved: bottom navigation/system-inset visual height was tightened in emulator screenshots.
- [x] Android parity note documented: Android cannot mirror WPF browser domain/window-title tracking through UsageStatsManager; keep Android app/package usage only unless a future explicit safe API scope is approved.

## 2026-05-01 Android Current Focus External App Follow-Up

- [x] Superseded historical behavior: RED/GREEN Robolectric coverage once expected `Chrome / com.android.chrome` after Chrome, launcher, and Monitor usage rows existed. The 2026-05-02 foreground policy supersedes this with Woong foreground truth.
- [x] Removed self-package injection from MainActivity into DashboardFragment.
- [x] Superseded historical behavior: Dashboard Current Focus skipped Monitor self rows and Nexus Launcher noise when a meaningful external tracked app was available. Current behavior keeps launcher/SystemUI as noise but no longer treats Woong Monitor as noise when foreground.
- [x] Validation passed: focused Current Focus tests and Android Gradle `testDebugUnitTest assembleDebug`.
- [x] Emulator screenshot captured after opening Chrome and returning to Dashboard: `artifacts/android-check/latest/android-current-focus-after-chrome-dashboard.png`.
## 2026-05-01 Android Room-Backed Charts, Outbox, And Screenshot Reliability

- [x] Added Room-backed hourly chart data for App Detail selected-package analysis.
- [x] Added Room-backed daily trend chart data for the Report 7-day summary.
- [x] Added chart formatter tests for hour labels, day labels, and minute axes.
- [x] Fixed sync outbox idempotency so duplicate focus-session enqueue does not reset a synced item to pending.
- [x] Fixed UsageSessionizer open-span behavior so a resumed app with no pause is closed at the collection window end.
- [x] Extended Android UI snapshot automation with App Detail screenshot `14-app-detail.png`.
- [x] Added `-DeviceSerial` support to Android UI snapshot script for deterministic emulator targeting.
- [x] Generated latest emulator screenshot evidence at `artifacts/android-ui-snapshots/20260501-191127/`.
- [x] Validation passed: Android Gradle unit/build/androidTest APK, Android UI snapshot script on `emulator-5554`, full dotnet test/build, coverage line 88.0% / branch 69.5%, focused architecture tests.
- [x] Next Android TODO resolved: Settings sync opt-in/manual sync UI now wires to real sync settings and status.
- [x] Next Android TODO resolved: Dashboard/Sessions/Report period-filter behavior now reloads Room-backed ranges.
## 2026-05-01 Android Settings Sync Opt-In And Sessions Period Filters

- [x] Settings Sync switch now persists opt-in/off state through SharedPreferences.
- [x] Manual Sync shows a local-only skipped status when sync is off and does not upload.
- [x] Sessions Today/1h/6h/24h/7d controls now reload Room-backed rows by selected period.
- [x] Added RED/GREEN tests for Settings sync behavior and Sessions period filtering.
- [x] Generated latest emulator screenshot evidence at `artifacts/android-ui-snapshots/20260501-192752/`.
- [x] Validation passed: Android Gradle unit/build/androidTest APK and screenshot automation on `emulator-5554`.
- [x] Next Android TODO resolved: Dashboard period buttons now perform real Room-backed range reloads.
- [x] Next Android TODO resolved: Report 30d/90d/custom range reloads now use Room-backed ranges.
- [x] Android Settings sync config now includes server URL/device ID fields before real manual sync enqueue.
- [x] Full solution validation for Android Settings Sync Opt-In and Sessions Period Filters passed: `dotnet test` 491 passed / 6 skipped, `dotnet build` 0 warnings / 0 errors.
- [x] Coverage refreshed for Android Settings Sync Opt-In and Sessions Period Filters: line 88.0%, branch 69.5%, report at `artifacts/coverage/SummaryGithub.md`.

## 2026-05-01 Android Dashboard Period Filters And Room-Backed Charts

- [x] Dashboard Today/1h/6h/24h/7d controls reload persisted Room sessions for the selected range.
- [x] Dashboard rolling windows use UTC persisted timestamps and a testable clock, not fake in-memory rows.
- [x] Dashboard top-app list displays the top 3 app usage slices for the selected period.
- [x] Dashboard hourly focus chart renders Room-backed bar data for the selected period.
- [x] RED/GREEN tests added for Dashboard last-hour repository behavior and MainActivity period-button behavior.
- [x] Latest Android screenshot evidence: `artifacts/android-ui-snapshots/20260501-195308/`.
- [x] Validation passed: Android Gradle unit/build/androidTest APK, UI snapshot script on `emulator-5554`, full dotnet test/build, coverage line 88.0% / branch 69.5%.
- [x] Next Android TODO resolved: Report 30d/90d/custom filters now use real Room ranges.
- [x] Next Android TODO resolved: UsageStats anchored-lookback collection now clips apps resumed before collection start into the requested window.

## 2026-05-01 Android UsageStats Anchored Lookback And Report Ranges

- [x] UsageSessionizer clamps sessions to the requested collection start/end so pre-window resumes are not lost.
- [x] AndroidUsageCollectionRunner reads an anchored lookback window and persists only the requested clipped interval.
- [x] Report 7d/30d/90d aggregation now uses a report-specific Room repository instead of piggybacking on Dashboard periods.
- [x] Report 7d/30d/90d buttons reload total focus, daily average, date range, trend chart, and top apps.
- [x] RED/GREEN tests added for UsageStats clamping, runner anchored lookback, Report 30d/90d aggregation, and Report tab button reloads.
- [x] Latest Android screenshot evidence: `artifacts/android-ui-snapshots/20260501-201248/`.
- [x] Validation passed: Android Gradle unit/build/androidTest APK, UI snapshot script on `emulator-5554`, full dotnet test/build, coverage line 88.0% / branch 69.5%.
- [x] Next Android TODO: implement Report custom date range UI.
- [x] Next Android TODO: add no-wait emulator validation for anchored UsageStats current-focus behavior.
- [x] Next Android TODO resolved: Dashboard/Report/App Detail charts now share readable MPAndroidChart visual styling against the supplied reference; dense Dashboard label handling remains open separately.

## 2026-05-01 Android Report Custom Range And Current-Focus Evidence

- [x] Added RED/GREEN Room repository coverage for inclusive custom report ranges.
- [x] Reworked `ReportPeriod` to support `Custom(from, to)` while preserving 7d/30d/90d behavior.
- [x] Added RED/GREEN MainActivity coverage for Report custom range apply flow.
- [x] Added invalid custom range coverage: reversed dates show a visible error and preserve the current summary.
- [x] Added Report custom range UI with start/end `yyyy-MM-dd` inputs and `Apply range`.
- [x] Extended Android UI screenshot automation with `15-report-custom-range.png`.
- [x] Added `scripts/run-android-usage-current-focus-validation.ps1` and `docs/android-usage-current-focus-validation.md` for safe Chrome -> Woong UsageStats current-focus emulator evidence.
- [x] Ignored `artifacts/android-usage-current-focus/` generated outputs.
- [x] Latest Android UI screenshot evidence: `artifacts/android-ui-snapshots/20260501-203803/`.
- [x] Latest Android current-focus evidence: `artifacts/android-usage-current-focus/20260502-012243/`.
- [x] Validation passed: Android Gradle `testDebugUnitTest assembleDebug assembleDebugAndroidTest`, Android UI snapshots on `emulator-5554`, current-focus validation on `emulator-5554`, full solution `dotnet test`/`dotnet build`, and coverage line 88.0% / branch 69.5%.
- [x] Report period buttons now show selected state for 7d, 30d, 90d, and valid Custom ranges.
- [x] Latest selected Custom range screenshot evidence: `artifacts/android-ui-snapshots/20260501-210345/15-report-custom-range.png`.
- [x] Validation passed for Report selected-state polish: Android Gradle unit/build/androidTest APK, UI snapshot script on `emulator-5554`, full dotnet test/build, coverage line 88.0% / branch 69.5%.
- [x] Dashboard and Sessions period buttons now share selected/unselected styling with Report through `PeriodButtonStyler`.
- [x] Added RED/GREEN UI-state tests for Dashboard and Sessions selected-period behavior.
- [x] Android UI screenshot automation now captures `16-dashboard-1h-selected.png` and `17-sessions-6h-selected.png`.
- [x] Latest selected-period screenshot evidence: `artifacts/android-ui-snapshots/20260501-214011/`.
- [x] Validation passed for Dashboard/Sessions selected-state polish: Android Gradle unit/build/androidTest APK, UI snapshot script on `emulator-5554`, full dotnet test/build, coverage line 88.0% / branch 69.5%.
- [x] Dashboard, Report, and App Detail charts now share readable MPAndroidChart styling and branded datasets through `DashboardChartConfigurator`.
- [x] Added RED/GREEN tests for chart visual contract, hidden raw value labels, and branded bar/line datasets.
- [x] Latest chart screenshot evidence: `artifacts/android-ui-snapshots/20260501-221704/`.
- [x] Validation passed for Android chart visual polish: Android Gradle unit/build/androidTest APK, UI snapshot script on `emulator-5554`, full dotnet test/build, coverage line 88.0% / branch 69.5%.
- [x] Superseded historical behavior: Current Focus ignored AOSP launcher/SystemUI noise after returning from Chrome and kept the latest meaningful external app visible. Current behavior still ignores launcher/SystemUI noise but shows Woong Monitor when Woong is foreground.
- [x] Current Focus session duration now uses the selected session duration instead of the whole selected-period total.
- [x] Added RED/GREEN current-focus noise test and refreshed Android UI screenshot evidence: `artifacts/android-ui-snapshots/20260501-224147/`.
- [x] Added Android GitHub Actions CI/CD workflow for unit tests, debug/release APK builds, Android test APK build, and artifact uploads.
- [x] Added `scripts/validate-android-ci.ps1`; RED failed before workflow existed and GREEN passes after adding it.
- [x] Local equivalent Android CI command passed: `android\gradlew.bat testDebugUnitTest assembleDebug assembleRelease assembleDebugAndroidTest --no-daemon --stacktrace`.
- [x] Android CI artifact hardening: push/PR workflow uses the Gradle wrapper from `android/`, builds debug/release/androidTest APKs, uploads unit-test reports and any produced APKs on failure, and does not require emulator/connected tests.
- [x] Added Android release workflow `.github/workflows/android-release.yml`.
- [x] Added release workflow validator `scripts/validate-android-release-workflow.ps1` and focused architecture coverage `AndroidReleaseWorkflowTests.cs`.
- [x] Android release workflow runs from `android/`, uses the Gradle wrapper, and does not require connected/emulator tests.
- [x] Tag-based Android release publishing requires `ANDROID_KEYSTORE_*` secrets and publishes only the signed release APK; unsigned/debug/test APKs remain CI artifacts, not release assets.
- [x] Validation passed: local release workflow validator, focused architecture tests, and Android Gradle `testDebugUnitTest assembleDebug assembleRelease assembleDebugAndroidTest`.
- [x] Android manual emulator workflow added for optional `connectedDebugAndroidTest` evidence; it stays manual/opt-in rather than a required release gate.
- [ ] Next Android release TODO: configure real `ANDROID_KEYSTORE_*` signing secrets and Play Store publishing only after explicit release requirements are defined.
- [x] Android release workflow now writes `artifacts/android-release/release-readiness.json` beside the signed APK, recording version, signed APK SHA-256, endpoint configuration state, sync default opt-in=false, manual Play publishing mode, emulator-evidence requirement, and concrete `emulatorEvidenceStatus`/`emulatorEvidencePath` fields before public promotion.
- [x] Android release readiness manifest now records concrete emulator evidence
  state through `ANDROID_EMULATOR_EVIDENCE_PATH`,
  `emulatorEvidenceStatus`, and `emulatorEvidencePath`.
- [x] Validation passed for Android release readiness manifest slice: local release workflow validator, focused `AndroidReleaseWorkflowTests`, Android Gradle `testDebugUnitTest assembleRelease`, full solution `dotnet test`, full solution `dotnet build`, and coverage collection.
- [x] Current-focus validation screenshot timing/retry follow-up is superseded by the 2026-05-02 foreground validation pass at `artifacts/android-usage-current-focus/20260502-012243/`.
- [x] Dashboard/Report top-app rows now reduce dead space and render proportional usage bars for ranked app lists.
- [x] Focused visual test: `ReportTopAppsVisualTest.reportTopAppsRenderProportionalUsageBars`.
- [x] Next Android TODO resolved: Dashboard chart/card density was tightened so ranked app content appears earlier and remains readable.

## 2026-05-02 Android Settings Sync Config

- [x] SharedPreferences sync server base URL/device ID now default blank, persist trimmed values, and keep sync opt-in off by default.
- [x] Settings UI exposes server URL and device ID fields beside Sync controls.
- [x] Manual Sync stays local-only/no-worker while sync is off; sync-on missing config shows configuration required and does not enqueue work.
- [x] Manual Sync with valid config enqueues AndroidSyncWorker with KEY_BASE_URL, KEY_DEVICE_ID, and pending limit.
- [x] AndroidSyncWorker missing config returns a clear failure status without calling the sync runner.
- [x] Latest Android UI snapshot evidence: artifacts/android-ui-snapshots/20260502-063728/ PASS; crash buffer empty.
- [x] Validation passed: Android Gradle test/build/androidTest APK, UI snapshots, dotnet test, and dotnet build.
- [x] Settings now rejects invalid sync server URLs before enqueueing Manual Sync work: production URLs require HTTPS, loopback HTTP is allowed for local development, and embedded credentials are rejected.
- [x] Android upload calls now attach the persisted server-issued `X-Device-Token` header and classify missing-token/auth-required failures explicitly.
- [x] Settings now includes a visible Register/Repair device flow; Manual Sync with sync on but no registered device/token shows registration required and does not enqueue work.
- [x] Android sync now treats IO failures as retryable, auth/validation failures as nonretrying, server duplicate results as idempotent success, and duplicate outbox enqueue no longer resets synced rows.
- [x] Server integration tests now cover Android `android_usage_stats` focus-session uploads with Windows-only fields omitted/null.
- [x] Android production network policy rejects broad cleartext traffic while allowing only explicit loopback HTTP for local development.
- [x] Android device tokens now use a token-store abstraction with Android Keystore AES-GCM runtime storage; ordinary `woong_monitor_settings` no longer stores plaintext `device_token`, and legacy plaintext tokens are migrated/removed.
- [x] Server device-token rotation is implemented: current token required, old token invalidated, new token works, and existing sync rows are preserved.
- [x] Production endpoint release behavior is documented and statically guarded: unset production endpoint keeps sync disabled, release builds do not silently fall back to local/blank/example endpoints, loopback HTTP is local-dev nonproduction only, and user-entered release endpoints are advanced/manual configuration only.
- [x] Android production endpoint source/config path implemented: `BuildConfig.PRODUCTION_SYNC_BASE_URL` is populated by `woongProductionSyncBaseUrl` or `WOONG_ANDROID_PRODUCTION_SYNC_BASE_URL`, defaults blank, rejects local/example/invalid endpoints, and preserves explicit user-entered advanced endpoints.
- [x] Settings now shows sync/registration/auth state without making sync appear enabled by default; worker auth-required status is persisted and surfaces as a repair-needed Settings state.
- [x] Android UsageStats collection now exposes an optional no-op-by-default debug/test hook for requested and anchored query windows, so emulator validation can prove `from/to` behavior without relying on natural event timing.
- [ ] Remaining Android sync hardening: Play signing/publishing policy,
  public user-auth registration/re-registration ownership policy, and
  server/user cross-device management policy stay open before release use.
- [ ] Release blockers before public Android/server sync: decide the production
  user/session provider, define public cross-device management ownership, and
  decide Android Play signing/publishing requirements.
- [x] Secure token storage implementation plan completed: tests prove `device_token` is absent from `woong_monitor_settings`, legacy plaintext token is migrated/cleared, Settings/Register/Manual Sync behavior stays unchanged, and runtime storage uses Android Keystore-backed AES-GCM without adding AndroidX security-crypto.
- [x] Connected-emulator secure token evidence passed with `AndroidKeystoreSyncTokenStoreInstrumentedTest`, and Settings sync/auth UI screenshot evidence refreshed at `artifacts/android-ui-snapshots/20260502-131615/`.
- [x] Server registration now has an option-gated authenticated-user identity path: `X-Woong-User-Id` overrides payload `userId`, strict mode returns 401 when identity is missing, and dev/MVP payload behavior remains compatible by default.
- [x] Settings now renders real Usage Access granted/missing state and persists the background collection switch through the Android collection settings abstraction.
- [x] Android UI snapshot automation now creates beginner-review before/after aliases for the seven canonical Woong UI screens without capturing other apps.
- [x] Server device-token revocation endpoint requires the current token, invalidates future upload/rotation attempts with the revoked token, and preserves existing stored sessions.
- [x] Dashboard recent sessions are now kept before optional location context so core Room-backed usage rows remain visible before non-critical location metadata.
- [x] Latest Android Dashboard recent-session visibility evidence: `artifacts/android-ui-snapshots/20260502-132522/` with Dashboard overview, chart, recent sessions, and safe location/scroll states.
- [x] Server strict-mode device token checks now validate authenticated user ownership for token-protected endpoints; a token issued to user A cannot upload as user B when `DeviceRegistrationAuth:RequireAuthenticatedUser=true`.
- [x] Server registration policy tests now prove the same device key can produce separate devices for separate authenticated users and payload `userId` cannot steal another user's device token.
- [x] Server token-protected endpoint response policy is now explicit in code/tests/docs: missing, malformed, revoked, or wrong tokens return `401 Unauthorized`, while a valid token presented by a different authenticated user returns `403 Forbidden` and persists no upload rows.
- [x] Public Android/server sync release policy now separates dev/MVP payload mode from production strict-auth mode and guards the release checklist: production must set `DeviceRegistrationAuth:RequireAuthenticatedUser=true`, select a real user/session provider, and cannot ship with the `X-Woong-User-Id` header stub as the production identity provider.
- [x] Server startup/config validation now rejects Production + strict auth when `DeviceRegistrationAuth:UserIdentityProviderMode=HeaderStub`, keeping the dev/MVP header identity path from being accidentally shipped as the production provider.
- [x] Server startup/config validation now also rejects non-`HeaderStub` user identity provider modes until they are wired to a concrete `IRegistrationUserIdentitySource`, so future names such as `Oidc` cannot silently keep using the header stub.
- [x] Server registration identity now has a concrete `ClaimsPrincipal` provider mode: it reads the stable user id from a configured authenticated claim, ignores the header stub in claims mode, rejects missing claims by returning no user, requires `RequiredAuthenticationScheme` for production strict-auth startup, and keeps server registration decoupled from Windows SQLite/Android Room.
- [x] Validation passed for server unwired-provider guard: focused `DeviceRegistrationPolicyTests`, full solution `dotnet test`, full solution `dotnet build`, and coverage collection.
- [x] Android seven-screen snapshot evidence now pulls the residual Sessions row-tap, Sessions empty-state, second App Detail, and Settings storage/privacy scroll captures. The snapshot seed uses past sessions relative to the emulator clock so Today/Sessions evidence does not go empty when the emulator local time is before 09:00.
- [x] Latest Android seven-screen/residual evidence: `artifacts/android-ui-snapshots/20260502-150542/`, including `25-sessions-row-tap-app-detail.png`, `26-sessions-empty-state.png`, `27-app-detail-youtube.png`, and `28-settings-storage-scrolled.png`.
- [x] Validation passed for the Android snapshot residual + server HeaderStub guard slice: Android UI snapshot PASS, full solution `dotnet test` (579 passed / 6 skipped), full solution `dotnet build`, Android Gradle `testDebugUnitTest assembleDebug assembleRelease assembleDebugAndroidTest`, and coverage collection.
- [x] Latest coverage after this slice: line 87.7%, branch 69.7%, report at `artifacts/coverage/SummaryGithub.md`.
- [x] Android Dashboard Current Focus now suppresses NexusLauncher/SystemUI-only
  noise and shows `No app` / `No package` instead of a misleading current app.
- [x] Latest Android launcher-noise evidence:
  `artifacts/android-ui-snapshots/20260502-141105/`.
- [x] Validation passed for the Android Dashboard visibility + server strict ownership slice: full solution `dotnet test` (571 passed / 6 skipped), full solution `dotnet build`, Android Gradle `testDebugUnitTest assembleDebug assembleRelease assembleDebugAndroidTest`, and coverage collection.
- [x] Latest coverage after this slice: line 87.7%, branch 69.5%, report at `artifacts/coverage/SummaryGithub.md`.

## 2026-05-02 WPF Runtime Exception Logging Hardening

- [x] Extracted WPF runtime exception logging into a testable `RuntimeExceptionLogger`.
- [x] Dispatcher, AppDomain, and unobserved task exceptions route through `IDashboardRuntimeLogSink` when available.
- [x] Runtime exception logging now has a non-throwing fallback diagnostic path if the sink is unavailable or throws.
- [x] Removed the empty crash-logging catch from `App.xaml.cs` without moving business logic into code-behind.
- [x] Validation passed: focused WPF App runtime logger/composition tests, WPF App build, full solution `dotnet test`, full solution `dotnet build`, and coverage collection.
- [x] WPF close-to-taskbar lifecycle is now extracted into a testable service: X/system close minimizes the window and keeps it visible in the taskbar, explicit Exit closes/shuts down, restore activates the window, and lifecycle logging has a non-throwing fallback.
- [x] Validation passed for WPF taskbar close lifecycle: focused WPF App lifecycle tests, WPF App build after cleaning stale temp output, full solution `dotnet test`, full solution `dotnet build`, and coverage collection.

## Next Android Production Sync Queue

- [x] Added client-side device disconnect/revocation UX in Settings with TDD: sync stays opt-in/off by default, current token is sent only as `X-Device-Token`, local token/device state clears only after success or local sync-off disconnect, and failure keeps local registration plus a clear local-data-safe status.
- [x] Validation passed for Android disconnect/revocation UX: focused Settings/sync tests, Android Gradle `testDebugUnitTest assembleDebug assembleRelease assembleDebugAndroidTest`, full solution `dotnet test`, full solution `dotnet build`, and coverage collection.
- [x] Latest coverage after disconnect/taskbar slice: line 87.6%, branch 69.3%, report at `artifacts/coverage/SummaryGithub.md`.
- [x] Add Android token refresh/re-registration behavior tests for auth-required status: Register/Repair replaces the old token, clears auth-required status, and does not enqueue sync until registration succeeds.
- [x] Current multi-agent validation passed after Android launcher-noise and
  server 401/403 policy slices: `dotnet test` 577 passed / 6 skipped,
  `dotnet build` 0 warnings / 0 errors, Android Gradle
  `testDebugUnitTest assembleDebug assembleRelease assembleDebugAndroidTest`,
  and coverage line 87.5%, branch 69.4%.
- [x] Current multi-agent validation passed after Android auth-required repair
  and server production strict-auth release-policy slices: `dotnet test` 578
  passed / 6 skipped, `dotnet build` 0 warnings / 0 errors, Android Gradle
  `testDebugUnitTest assembleDebug assembleRelease assembleDebugAndroidTest`,
  and coverage line 87.5%, branch 69.4%.
- [x] Current multi-agent validation passed after Android disconnect repair,
  server revoked-device repair, and WPF chart detail aggregation slices:
  `dotnet test` 596 passed / 6 skipped, `dotnet build` 0 warnings / 0 errors,
  Android Gradle `testDebugUnitTest assembleDebug` and `assembleRelease`, and
  coverage line 87.5%, branch 70.1%.
- [x] Android bottom-navigation/content clearance guard added so Dashboard,
  Sessions, Report, and Settings tab content keeps a readable buffer above the
  bottom navigation.
- [x] Android seven-screen snapshot freshness guard added through
  `scripts/validate-android-ui-snapshot-report.ps1`.
- [x] Android current-focus emulator validation script now launches Woong
  synchronously, retries foreground detection, and produced PASS evidence at
  `artifacts/android-usage-current-focus/20260502-172024/`.
- [x] Server production strict-auth ClaimsPrincipal startup now fails when the
  configured `RequiredAuthenticationScheme` has no registered authentication
  handler, keeping public release blocked until real upstream auth is wired.

## 2026-05-01 Windows WebSession Flush And MSIX Install Recovery

- [x] Fixed WPF runtime tracking bug: leaving a browser foreground focus now flushes the open WebSession before the coordinator starts tracking the next app.
- [x] Added RED/GREEN coverage for a 15-minute `chatgpt.com` session followed by switching to `Code.exe`; SQLite now contains a 900000ms `chatgpt.com` web_session and a pending web_session outbox item.
- [x] Strengthened MSIX install artifact UX: generated artifacts now include `Install-WoongMonitorStack.Windows.cmd` to launch the installer script elevated for the bundled package/certificate pair.
- [x] `install-windows-msix.ps1` now verifies that the supplied `.cer` thumbprint matches the MSIX signer before importing trust, catching mismatched/old CI artifact certificates.
- [x] README and `docs/windows-release-msix.md` now explain that double-clicking a self-signed CI `.msix` before certificate trust is expected to fail with `0x800B010A`; use the same-artifact `.cer` or a real trusted signing path for double-click install.
- [x] Validation passed: focused WPF tracking tests (26 passed) and focused Windows MSIX architecture tests (3 passed).

## 2026-05-01 Windows Live WebSession Duration Follow-Up

- [x] Fixed live dashboard undercount: a current same-domain browser session now exposes active WebSession duration before domain change/Stop, so Web Focus/top domains/Web Sessions show the ongoing `chatgpt.com` duration instead of only an old short row.
- [x] Added RED/GREEN coordinator coverage for a 15-minute same-domain `chatgpt.com` active WebSession without prematurely persisting a closed row.
- [x] Added RED/GREEN dashboard coverage that overlays the active WebSession into Web Focus, top domains, charts, and Web Sessions before the session closes.
- [x] Validation passed: focused browser/sessionizer tests (15 passed), focused WPF tracking tests (27 passed), and focused WPF dashboard presentation tests (66 passed).
- [x] Full solution validation passed for Windows WebSession/MSIX recovery: `dotnet test` 494 passed / 6 skipped, Release `dotnet build` 0 warnings / 0 errors.
- [x] Coverage refreshed for Windows WebSession/MSIX recovery: line 88.1%, branch 70.2%, report at `artifacts/coverage/SummaryGithub.md`.
- [x] Local MSIX packaging validation passed with generated test certificate, bundled README/install script, and `Install-WoongMonitorStack.Windows.cmd`.
- [x] MSIX install script `-WhatIf` validation passed with the same-artifact package/certificate pair and LocalMachine TrustedPeople target.

## 2026-05-02 WPF Chart Detail Single-Bar Regression

- [x] Add RED WPF chart detail coverage proving duplicate app/domain labels render as one horizontal bar with aligned total values.
- [x] Fix the chart detail mapping/presenter path with the smallest scoped change.
- [x] Run focused WPF chart detail tests and focused WPF App test build.

## 2026-05-02 Android Current Foreground App Correction

- [x] Add RED Android coverage proving Current Focus shows `Woong Monitor / com.woong.monitorstack` when the Woong app itself is foreground after returning from Chrome.
- [x] Keep launcher/SystemUI packages as noise while no longer treating Woong Monitor as noise.
- [x] Supersedes older current-focus TODO wording that kept the latest external app visible after returning to Woong.
- [x] Update current-focus validation script/docs to expect the actual foreground Woong app and retry blank screenshots.
- [x] Capture emulator evidence at `artifacts/android-manual-run/woong-monitor-current-focus-fixed-dashboard.png`.
- [x] Validation passed: Android Gradle `testDebugUnitTest assembleDebug`, full solution `dotnet test`, and full solution `dotnet build`.
- [x] Updated current-focus validation script passed on `emulator-5554`, writing artifacts to `artifacts/android-usage-current-focus/20260502-012243/`.

## 2026-05-02 Android Figma 7-Screen Acceptance Evidence

- [x] Reopened `android_check_todo.md` with the full Android Figma parity backlog covering Splash, Permission, Dashboard, Sessions, App Detail, Report, Settings, legacy Activity cleanup, Chrome/app-switch QA, and emulator evidence.
- [x] Added `docs/android-figma-7-screen-acceptance.md` mapping all seven Figma screens to XML layout, runtime surface, behavior gate, and canonical screenshot artifact.
- [x] Added RED/GREEN architecture coverage requiring canonical screenshot files `figma-01-splash.png` through `figma-07-settings.png`.
- [x] Stabilized `SnapshotCaptureTest` with deterministic MainActivity Usage Access gates and Splash routing waits before shell tab/filter captures.
- [x] Added RED/GREEN MainActivity regression coverage for delayed Splash routing after Activity destruction.
- [x] Added RED/GREEN snapshot-script coverage and implementation that clears external browser/system dialog interference before instrumentation capture.
- [x] Latest clean Android UI screenshot evidence: `artifacts/android-ui-snapshots/20260502-063728/`.
- [x] Added per-screen PASS/WARN rows to Android UI snapshot `report.md`.
- [x] Fixed hidden-shell screenshot margins so Splash/Permission captures stay aligned without showing toolbar or bottom navigation.
- [x] Crash buffer was cleared before the latest run and remained empty after screenshot capture.
- [x] Android UI snapshot rerun `report.md` status PASS with all seven canonical Figma screenshots PASS; latest points to `artifacts/android-ui-snapshots/20260502-063728/`.
- [x] Dashboard first viewport now shows Hourly focus immediately after filters, with Top apps next below.
- [x] Report `figma-06-report.png` now shows a seeded multi-day, multi-point trend.
- [x] Sessions rows are visually compact while preserving readable app/package metadata.
- [x] Settings now uses clearer Permissions, Collection, Sync, and Privacy visual grouping.
- [x] Dashboard/Report top-app rows now render proportional usage bars with focused test `ReportTopAppsVisualTest.reportTopAppsRenderProportionalUsageBars`.
- [x] Compared the latest seven canonical screenshots against `artifacts/android-ui-flow/woong-monitor-android-ui-flow.figma-import.svg` and closed the largest remaining first-viewport visual gaps for Dashboard, App Detail, Report, and Settings.
- [x] Latest seven-screen evidence: `artifacts/android-ui-snapshots/20260502-073133/` and `artifacts/android-ui-snapshots/latest/`, with all canonical Figma screenshots `PASS`.
- [x] Legacy `DashboardActivity`, `SessionsActivity`, and `SettingsActivity` now host canonical fragments; obsolete standalone `activity_dashboard.xml`, `activity_sessions.xml`, and `activity_settings.xml` were removed.
- [x] Android app-switch QA script now fails when Room assertions fail, classifies Woong process death/emulator stability cases, and retries blank Woong-owned screenshots without capturing Chrome UI.
- [x] Chrome/app-switch QA PASS verified UsageStats -> Room -> Dashboard/Sessions refresh after returning from Chrome.
- [x] App-switch QA evidence refreshed: `artifacts/android-app-switch-qa/20260502-073336/` and `artifacts/android-app-switch-qa/latest/`.
- [x] `report.md` PASS and `room-assertions.json` PASS with Chrome focus rows and pending sync outbox rows.
- [x] Foreground-after-return shows `com.woong.monitorstack`; `dashboard-after-app-switch.png` and `sessions-after-app-switch.png` exist; no Chrome screenshots or Chrome UI dumps were captured.

## 2026-05-02 Server CI And Raw Event Retention Hardening

- [x] Added dedicated server CI workflow `.github/workflows/server-ci.yml`.
- [x] Added local server CI validator `scripts/validate-server-ci-workflow.ps1`.
- [x] Added architecture tests for the server CI workflow contract.
- [x] Added `RawEventRetentionService` to delete old `raw_events` while preserving durable `focus_sessions` and `web_sessions`.
- [x] Added `RawEventRetentionMaintenanceService` with configurable `Enabled`, `RetentionDays`, and `Interval` options.
- [x] Added option-gated `RawEventRetentionBackgroundService` and disabled its hosted-service registration in the `Testing` WebApplicationFactory host.
- [x] Added production/development config defaults for `RawEventRetention`.
- [x] Added environment-specific raw-event retention alerting config values: production enables alert policy with consecutive-failure and high-delete thresholds, development keeps alert delivery disabled with the same thresholds for validation.
- [x] Added runtime raw-event retention alert delivery through `IRawEventRetentionAlertSink`; repeated failures and high delete counts emit operational metadata only, without raw-event payload contents.
- [x] Added structured logging and hardening docs for raw-event retention runs.
- [x] Added `docs/android-server-sync-hardening-plan.md` for remaining Android production sync requirements.
- [x] Validation passed: server workflow validator, focused server retention tests, full solution `dotnet test`, and full solution `dotnet build`.
- [x] Server production migration deployment path is documented and has a bundle helper/contract coverage; bundles are built explicitly and never applied automatically.
- [x] Server PostgreSQL validation runner now distinguishes environment
  capacity blockers (`BLOCKED`/exit 2) from validation failures (`FAIL`/exit
  1), removes stale test output on blocked runs, and restores
  `WOONG_MONITOR_RUN_POSTGRES_TESTS` after execution.
- [x] WPF RealStart evidence now has a DB-only verification path for
  domain-only `web_session` duration persistence: it reads a temp SQLite DB,
  writes `real-start-report.md`/`real-start-manifest.json`, keeps
  `AllowServerSync=false`, and avoids launching browsers or capturing external
  app screenshots.
- [ ] Remaining server production hardening: device token issuance/enforcement, rotation, revocation, strict-mode ownership checks, token-protected 401/403 response policy, and runtime retention alert delivery have focused coverage; production user/session provider selection still needs implementation/validation.

## 2026-05-02 Android Location Visit Statistics And Bottom Navigation Compacting

- [x] Add RED Android tests for location visit recording and Room persistence.
- [x] Add local-only `location_visits` Room table/DAO and migration v3 -> v4.
- [x] Add `LocationVisitRecorder` that merges same rounded coordinate cells
  within a merge gap instead of producing one statistics row per location poll.
- [x] Wire `LocationContextCollectionRunner` to record location visits when an
  opt-in coordinate snapshot exists.
- [x] Show Room-backed `Location visits` and `Top location` statistics on the
  Android Dashboard location card.
- [x] Compact Android bottom navigation to avoid the excessive blank padding
  below Dashboard/Sessions/Report/Settings.
- [x] Update Android UI/privacy/TODO documentation with the location-statistics
  storage policy.
- [x] Validate with focused Android tests, full Android `testDebugUnitTest`,
  and `assembleDebug`.
- [x] Refresh Android emulator UI snapshot evidence after the bottom-navigation
  and location-statistics changes: `artifacts/android-ui-snapshots/20260502-181343/`.
- [x] Refresh .NET coverage after this slice: line 87.6%, branch 70.4%,
  report at `artifacts/coverage/SummaryGithub.md`.

## 2026-05-02 Integrated Blazor Dashboard Bootstrap

- [x] Search/install Blazor-related skill guidance: installed
  `github/awesome-copilot@fluentui-blazor` for future Fluent UI Blazor work.
- [x] Assign parallel subagents to document Android and Windows local data
  structures with Mermaid diagrams and Figma-importable SVG artifacts.
- [x] Add `IntegratedDashboardQueryService` to aggregate PostgreSQL
  Windows/Android facts by device, platform, app family, browser domain, and
  opted-in coarse location samples.
- [x] Add RED/GREEN server tests proving Windows and Android device data are
  combined into one dashboard snapshot.
- [x] Add `/api/dashboard/integrated` JSON endpoint for machine-readable
  integrated dashboard data.
- [x] Add initial Blazor SSR `/dashboard` page that renders Windows + Android
  active focus, idle, web focus, platform totals, top apps, top domains,
  location samples, and device rows.
- [x] Add `docs/data/integrated-data-structure.md` and
  `artifacts/data-diagrams/integrated-data-structure.svg` for shared review.
- [x] Validation passed for integrated Blazor bootstrap: full solution
  `dotnet test` passed 610 tests / 6 PostgreSQL-environment tests skipped,
  full solution `dotnet build` passed with 0 warnings and 0 errors, and
  coverage refreshed at line 87.9% / branch 70.4%.
- [x] Follow-up completed: `IntegratedDashboardQueryService` now filters web
  sessions/location samples by the requested timezone local-date range and
  splits overlapping focus/web durations at the requested range boundary.
- [ ] Follow-up: add authenticated production user/session provider before
  exposing the dashboard beyond local/dev use.
- [x] Follow-up completed: added
  `scripts/run-integrated-dashboard-acceptance.ps1`, architecture coverage,
  Figma-importable design SVG, and Playwright screenshots for the Blazor
  dashboard with seeded PostgreSQL Windows + Android data.
- [x] Follow-up completed: `/dashboard` now separates Combined View,
  Windows View, and Android View so WPF sessions/apps/domains and Android
  UsageStats sessions/apps are readable independently before being integrated.
- [x] Follow-up completed: integrated dashboard renders an opted-in
  latitude/longitude movement route as local SVG from PostgreSQL
  `LocationContext` metadata without loading external map tiles.
- [x] Follow-up completed: added `docs/blazor-integrated-dashboard-wireflow.md`
  plus WPF/Android data inventory docs and Figma-importable SVG artifacts under
  `docs/data/` and `artifacts/data-diagrams/`.

## 2026-05-02 Docker PostgreSQL Local Server Flow

- [x] Add RED architecture tests requiring Docker Compose PostgreSQL, local env
  defaults, start/stop scripts, Development connection string, and README
  commands for the Blazor dashboard.
- [x] Add root `docker-compose.yml` with `postgres:16-alpine`,
  `woong-monitor-postgres`, health check, named volume, and host port `55432`.
- [x] Add `.env.example` with local-development-only PostgreSQL defaults and
  keep real `.env` ignored.
- [x] Add `scripts/start-server-postgres.ps1` with `-DryRun`, optional
  migration application, health wait, and optional server run.
- [x] Add `scripts/stop-server-postgres.ps1` with `-DryRun` and guarded
  `-RemoveVolumes` reset.
- [x] Update `README.md` and server Development config to use Docker
  PostgreSQL at `localhost:55432`.
- [x] Actual local validation: Docker daemon available, PostgreSQL container
  started, `.env` created from `.env.example`, and EF Core migrations applied.
- [x] PostgreSQL/Testcontainers validation passed with artifacts at
  `artifacts/server-postgres-validation/20260502-191025/`.
- [x] Full validation passed after Docker PostgreSQL and subagent follow-ups:
  `dotnet test` passed 616 tests / 6 PostgreSQL-environment skips,
  `dotnet build` passed with 0 warnings and 0 errors, and coverage refreshed at
  line 87.9% / branch 70.8%.
- [x] Follow-up completed: browser/Playwright screenshot evidence for
  `/dashboard` with seeded PostgreSQL data exists at
  `artifacts/integrated-dashboard-acceptance/latest/`.

## 2026-05-02 Android Bottom Navigation Safe-Inset Regression

- [x] Reproduce the Android bottom navigation regression with a RED
  Robolectric test: compacting the bar too far let navigation items drift into
  the Android system navigation area, making Dashboard/Sessions/Report/Settings
  look missing or untappable on the emulator.
- [x] Update `SystemInsetsLayoutCalculator` so the bottom navigation height
  includes exactly one required system-navigation safe inset while avoiding
  duplicate Material inset expansion.
- [x] Consume handled root window insets in `MainActivity` so child Material
  views do not add a second blank navigation area.
- [x] Add/adjust tests for safe bottom navigation padding, fragment bottom
  margin, and system inset consumption.
- [x] Validate Android unit tests and debug build:
  `.\gradlew.bat :app:testDebugUnitTest :app:assembleDebug`.
- [x] Install the fixed debug build on `emulator-5554` and capture evidence at
  `artifacts/android-ui-regression/latest/android-dashboard-bottom-nav-safe-inset.png`.
- [x] Follow-up bottom-navigation floor regression fixed after visual review:
  the bar stays compact and attached to the bottom of the app instead of
  expanding into a large white safe-area panel.
- [x] RED/GREEN tests updated for compact bottom navigation with system insets:
  `SystemInsetsLayoutCalculatorTest.calculateAddsOnlyRequiredTappableInsetToBottomNavigation`
  and
  `MainActivityTest.mainShellKeepsBottomNavigationCompactAtBottomWhenGestureNavigationHasLargeInset`.
- [x] Disabled the Android navigation-bar contrast scrim for the main shell so
  bottom tab labels stay readable at the screen floor.
- [x] Android unit/build validation passed:
  `.\gradlew.bat :app:testDebugUnitTest :app:assembleDebug`.
- [x] Installed the fixed debug APK on `emulator-5554` and captured visual
  evidence at
  `artifacts/android-ui-regression/bottom-nav-floor/android-bottom-nav-floor.png`.

## 2026-05-02 Android Location Mini Map Follow-Up

- [x] Answered the location-statistics gap: raw coordinate snapshots alone were
  not enough; the dashboard needs persisted `location_visits` aggregation for
  "where did time go?" statistics.
- [x] Added RED/GREEN repository coverage proving Room-backed location visits
  produce ordered `LocationMapPoint` data for the dashboard.
- [x] Added `LocationMiniMapView`, a local-only no-network bubble map that
  visualizes persisted visit points by duration without external map tiles or
  map-provider calls.
- [x] Wired the Android Dashboard location card to pass Room-backed map points
  into `LocationMiniMapView`.
- [x] Updated deterministic Android screenshot seeding to include multiple
  location visit rows, so snapshot evidence can show both text statistics and
  the local mini map.
- [x] Updated `scripts/run-android-ui-snapshots.ps1` expected location checks
  to include `locationMiniMapView`.
- [x] Added `docs/android-location-statistics.md` documenting why stats may be
  empty, what metadata is stored, and how a real map could be added later.
- [x] Validation passed: focused RED/GREEN tests,
  `.\gradlew.bat :app:testDebugUnitTest :app:assembleDebug :app:assembleDebugAndroidTest`,
  and emulator UI snapshot acceptance at
  `artifacts/android-ui-snapshots/20260502-202141/`.

## 2026-05-02 Android Branded First Launch And Bootstrap Noise Filtering

- [x] Added RED/GREEN UsageSessionizer coverage proving bootstrap launcher and
  SystemUI events are ignored before the Woong Monitor foreground session is
  created.
- [x] Added shared Android foreground-noise package list for UsageStats
  sessionization and Dashboard current-focus resolution.
- [x] Wired production Android usage collection to ignore launcher/SystemUI
  packages before sessions are persisted to Room or enqueued to outbox.
- [x] Added RED/GREEN architecture coverage requiring `MainActivity` to use a
  dedicated branded `Theme.WoongMonitor.Starting` launch theme.
- [x] Added Android launch background and `values-v31` splash theme so the OS
  launch window follows Woong branding before the app `SplashFragment` renders.
- [x] `MainActivity` immediately switches back to `Theme.WoongMonitor` in
  `onCreate`, avoiding a long-lived launch-only theme.
- [x] Validation passed: focused Android/architecture tests, full Android
  `.\gradlew.bat :app:testDebugUnitTest :app:assembleDebug :app:assembleDebugAndroidTest`,
  and Android UI snapshot acceptance at
  `artifacts/android-ui-snapshots/20260502-204431/`.

## 2026-05-02 Local WPF + Android Emulator To Blazor Dashboard

- [x] Add RED architecture coverage requiring a local-only runbook/script for
  WPF SQLite + Android emulator Room -> API DTO upload -> PostgreSQL ->
  Blazor dashboard.
- [x] Add `tools/Woong.MonitorStack.LocalDashboardBridge` to register local
  Windows/Android devices and upload WPF focus/web rows plus Android
  focus/location rows through the existing server APIs.
- [x] Add `scripts/run-local-integrated-dashboard.ps1` to start local Docker
  PostgreSQL, start ASP.NET Core/Blazor, pull Android emulator
  `woong-monitor.db` with `adb`, upload local metadata, and open `/dashboard`.
- [x] Add `docs/local-integrated-dashboard.md` and README commands for the
  local-only workflow.
- [x] Keep privacy boundaries explicit: the local bridge uploads metadata only
  and does not read typed text, messages, passwords, clipboard contents, page
  contents, screenshots, or Android touch coordinates.

## 2026-05-02 Local UX Follow-Up: WPF Charts, Tray, Blazor Polling, Android Map

- [x] WPF: add a suitable application icon and ensure the window/tray use it.
- [x] WPF: make the top App Focus chart show the top 3 apps when data exists.
- [x] WPF: make App Focus detail chart/table show up to top 10 labels with one
  visible label per horizontal bar.
- [x] WPF: make Domain Focus detail chart/table show up to top 10 labels with
  one visible label per horizontal bar.
- [x] WPF: ensure clicking X minimizes to notification-area hidden icons
  instead of remaining as a visible taskbar button; explicit Exit still shuts
  down the app.
- [x] Blazor: add dashboard polling controls for Off/1s/5s/10s/1h and make the
  page refresh PostgreSQL-backed dashboard data at the selected interval.
- [x] Android: verify location context continues capturing over time or make
  the UI show stale-capture status clearly.
- [x] Android: improve location map so moving locations add visible time-linked
  points; points expose timestamp and latitude/longitude labels.
- [x] Android: add a Google-map-like local visual context without uploading
  screenshots or using invasive tracking; external map tiles must remain
  explicitly documented if introduced later.
- [x] Android: keep Dashboard/Sessions/Report/Settings bottom navigation fixed
  at the screen bottom, with Android system navigation buttons visible beneath
  the app content where the emulator displays them.
- [x] Validation passed for this follow-up: WPF App focused tests, WPF
  Presentation chart tests, Blazor architecture polling tests, Android focused
  location/inset/current-focus tests, and bridge dry-run/build checks.
- [x] Full validation passed before commit: `dotnet restore`, `dotnet test`,
  `dotnet build`, Android `:app:testDebugUnitTest`, Android
  `:app:assembleDebug`, and `run-local-integrated-dashboard.ps1 -DryRun`.

## 2026-05-03 WPF Detail Filters And Android Map/Nav Regression Follow-Up

- [x] WPF: detail windows now expose visible `Today`, `1h`, `6h`, `24h`, and
  `Custom` period selectors instead of hiding the period choice behind a single
  collapsed control.
- [x] WPF: app/domain detail windows group duplicate labels case-insensitively,
  sort by duration descending, cap the chart/table at top 10, and keep all
  chart category labels aligned one-to-one with their horizontal bars.
- [x] WPF: detail chart Y axes force a one-category step so LiveCharts does not
  thin ten bars down to only two visible labels.
- [x] Android: Dashboard location card shows the local mini map above
  latitude/longitude rows, and the mini map still renders a map-like local
  preview when only the latest location snapshot exists.
- [x] Android: Dashboard location map falls back to the latest opt-in snapshot
  as one point until persisted `location_visits` rows accumulate.
- [x] Android: bottom navigation remains compact at the app floor while the
  Android back/home/recent system navigation area stays visible underneath with
  dark icons on a stable white system bar.
- [x] Validation passed: focused WPF detail tests, focused WPF architecture
  tests, full solution `dotnet test`, full solution `dotnet build`, Android
  focused location/inset tests, Android full `:app:testDebugUnitTest`, and
  Android `:app:assembleDebug`.

## 2026-05-03 WPF Detail Label And Android Map Evidence Follow-Up

- [x] WPF: reduced full-label detail chart category text size and kept a
  minimum chart height so top-10 horizontal bar labels no longer pile on top of
  each other.
- [x] Android: strengthened `LocationMiniMapView` so the location card renders
  visible no-network map context with roads, blocks, grid, point outlines, and
  timestamp labels instead of looking like isolated blue dots.
- [x] Android: normalized dotted hour-minute labels such as `16.48` to `16:48`
  and added Room repository coverage proving UTC location visits display as
  Asia/Seoul `HH:mm` labels for Korean review evidence.
- [x] Android: captured emulator evidence at
  `artifacts/android-map-evidence/latest/dashboard-location-map.png`.
- [x] Android: added and ran focused instrumentation evidence test
  `LocationMapSnapshotEvidenceTest`, producing
  `artifacts/android-map-evidence/latest-instrumentation/dashboard-location-map.png`.
- [x] Validation passed: WPF detail chart tests, Android location map/time
  focused tests, full solution `dotnet test`, full solution `dotnet build`,
  Android full `:app:testDebugUnitTest`, and Android `:app:assembleDebug`.

## 2026-05-03 Android Optional Google Maps SDK Follow-Up

- [x] Added RED/GREEN Android configuration tests requiring an optional Google
  Maps SDK dependency, manifest API-key metadata, and a dashboard Google map
  container with local fallback.
- [x] Added `GoogleMapsAvailabilityPolicy` so blank keys use the local
  no-network preview and configured keys enable the Google Maps path.
- [x] Added `DashboardLocationMapController` to create a Google `MapView` only
  when `woongGoogleMapsApiKey` or `WOONG_ANDROID_GOOGLE_MAPS_API_KEY` is
  configured.
- [x] Wired the Dashboard location card so persisted Room location visits render
  as Google map markers/route line when a key is present, or as the local map
  preview when no key is present.
- [x] Documented API-key setup, emulator requirements, and the privacy meaning
  of external map tiles in `docs/android-google-maps.md`, README, and Android
  location docs.
- [ ] Capture emulator evidence with a real Google Maps API key configured;
  default no-key validation can only prove the safe local fallback path.
