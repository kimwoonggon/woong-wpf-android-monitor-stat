# Total TODO List: Woong Monitor Stack

Updated: 2026-04-29

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
- [x] Test mixed Windows + Android data summary.
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
- [ ] Repeat Android resource measurements on a physical device.
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
- [ ] Capture dashboard, settings, sessions, and daily summary screens.
- [ ] Seed deterministic sample app usage where possible.
- [x] Run screenshot flow availability check; blocked because `adb devices -l`
  reported no connected Android device or running emulator, and the script wrote
  `artifacts/android-ui-snapshots/latest/report.md` plus manifest evidence.
- [x] Verified Android snapshot slice with focused architecture tests, full
  `.NET` tests (249), full `.NET` build, coverage generation (91.2% line),
  `testDebugUnitTest`, `assembleDebug`, and `assembleDebugAndroidTest`.
- [ ] Repeat Android resource measurements on a physical device when connected.
- [x] Commit and push Android screenshot/device automation slice.

## Milestone 28: Privacy And Retention Hardening

- [x] Add tests proving forbidden scopes are not represented by permissions, services, or product code.
- [x] Add browser raw event retention enforcement.
- [x] Add client-side raw event retention policy for Windows local SQLite.
- [x] Add UI copy for browser URL/domain privacy levels.
- [x] Add sync opt-in enforcement tests for Windows and Android.
- [x] Verify UI screenshot tools only capture this app's UI.
- [ ] Run full validation matrix.
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
- [ ] Unsafe/impossible/out-of-scope features are documented and not implemented.
- [x] Full .NET tests/build pass.
- [x] Android tests/build pass.
- [ ] Completion audit updated.
- [ ] Commit and push Original Intent completion.

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
- [x] Add app/domain chart `상세보기` actions that select App Sessions and Web Sessions tabs.
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
- [ ] Merge style dictionaries from `App.xaml` and replace hard-coded colors/duplicate local button/card styles in extracted panels.
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
- [ ] Add presentation child ViewModels or adapter properties only where they improve testability without breaking existing behavior.
- [ ] Extract WPF tracking/browser persistence orchestration from `Windows.App` coordinator into a Windows infrastructure/application service if the coordinator grows beyond composition/adaptation.
- [ ] Extract WPF startup lifecycle orchestration into an app startup service if auto-start, initial refresh, sync-at-start, permission checks, or tracking timer policy grow beyond simple MainWindow composition glue.
- [ ] Keep all current WPF UI expectation, semantic pipeline, and acceptance tests passing during component extraction.
- [ ] Run full `.NET` tests and build after componentization.
- [ ] Run WPF UI acceptance after componentization.
- [ ] Update `docs/wpf-csharp-coding-guide.md`, `docs/wpf-ui-plan.md`, `docs/resume-state.md`, and this TODO after componentization.
  - [x] Compact action style slice docs/TODO updated.
- [ ] Commit and push WPF componentization slices.

## Final Definition Of Done

- [ ] All PRD requirements reflected in code/tests/docs after Original Intent Restoration.
- [ ] All core logic built TDD-first.
- [ ] All relevant tests pass.
- [ ] All builds pass.
- [ ] Safety/privacy excluded scopes are not implemented.
- [ ] Local DB/server integrated DB separation is preserved.
- [ ] Daily integrated summary works across Windows + Android.
- [ ] Final documentation is complete.
- [ ] Final commit is pushed to `origin`.
