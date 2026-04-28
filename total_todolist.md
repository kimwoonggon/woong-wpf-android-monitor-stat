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
- [ ] Add native messaging host manifest generation and WPF browser connection status.
- [ ] Add URL sanitizer/redaction policy before storing raw browser events.
- [ ] Run browser/domain tests, Windows tests, solution build/test.
- [ ] Commit and push browser domain tracking slice.

## Milestone 24: Integrated Schema Restoration

- [ ] Add schema tests for required focus session process/window fields.
- [ ] Add schema tests for required web session browser/capture/privacy fields.
- [ ] Add server `device_state_sessions` table/entity/tests.
- [ ] Add server `app_families` and `app_family_mappings` tables/entities/tests.
- [ ] Decide and document whether Android app usage remains focus sessions or gets a dedicated app usage upload contract.
- [ ] Update DTO contracts for nullable URL, domain, capture method/confidence, process/window metadata, and client idempotency.
- [ ] Generate/review EF migration for restored schema.
- [ ] Update production migration notes.
- [ ] Run server relational tests and solution build/test.
- [ ] Commit and push integrated schema restoration slice.

## Milestone 25: WPF Semantic UI Acceptance

- [ ] Create `scripts/run-wpf-ui-acceptance.ps1`.
- [ ] Upgrade `tools/Woong.MonitorStack.Windows.UiSnapshots` or add a new tool for semantic FlaUI checks.
- [ ] Rework dashboard vertical layout or add scrolling so App Sessions, Web Sessions, and Live Event Log are not cramped below the current activity and chart areas.
- [ ] Implement EmptyData mode acceptance.
- [ ] Implement SampleDashboard mode acceptance.
- [ ] Implement TrackingPipeline mode with fake foreground/browser readers and temp SQLite.
- [ ] Verify Start changes tracking status to Running.
- [ ] Verify fake pipeline shows Visual Studio Code, Chrome, `github.com`, and `chatgpt.com`.
- [ ] Verify Stop changes tracking status to Stopped.
- [ ] Verify Sync Now updates last sync status using a fake sync client.
- [ ] Capture required screenshots: startup, after start, generated activity, after stop, after sync, settings, current activity, summary cards, sessions, web sessions, live events, and chart area when visible.
- [ ] Generate `report.md`, `manifest.json`, and `visual-review-prompt.md`.
- [ ] Keep screenshot review local-only and optional for GPT/human review.
- [ ] Run WPF acceptance tool locally.
- [ ] Commit and push WPF semantic acceptance slice.

## Milestone 26: Android Usage Tracking Restoration

- [ ] Add UI Automator dependency and Usage Access settings navigation smoke test.
- [ ] Add persisted sync opt-in setting and default-off enforcement.
- [ ] Wire WorkManager periodic scheduling for usage collection only when allowed and visible.
- [ ] Enqueue sync outbox rows when UsageStats sessions are collected.
- [ ] Ensure sync worker refuses/suppresses upload unless sync opt-in is true.
- [ ] Review and disable/constrain `android:allowBackup` for local usage metadata.
- [ ] Make SessionsActivity display real Room-backed sessions.
- [ ] Make DailySummaryActivity load previous-day summary through repository/client rather than intent extras only.
- [ ] Add Android 13+ notification permission UX before morning summary notifications.
- [ ] Run Android unit tests and debug build.
- [ ] Commit and push Android usage restoration slice.

## Milestone 27: Android UI Screenshot And Device Automation

- [ ] Add `docs/android-ui-screenshot-testing.md` follow-up implementation notes after tooling exists.
- [ ] Add local Android screenshot script/tool that writes `artifacts/android-ui-snapshots/<timestamp>/`.
- [ ] Generate Android `report.md`, `manifest.json`, and `visual-review-prompt.md`.
- [ ] Capture dashboard, settings, sessions, and daily summary screens.
- [ ] Seed deterministic sample app usage where possible.
- [ ] Run screenshot flow on emulator when available.
- [ ] Repeat Android resource measurements on a physical device when connected.
- [ ] Commit and push Android screenshot/device automation slice.

## Milestone 28: Privacy And Retention Hardening

- [ ] Add tests proving forbidden scopes are not represented by permissions, services, or product code.
- [ ] Add browser raw event retention enforcement.
- [ ] Add client-side raw event retention policy for Windows local SQLite.
- [ ] Add UI copy for browser URL/domain privacy levels.
- [ ] Add sync opt-in enforcement tests for Windows and Android.
- [ ] Verify UI screenshot tools only capture this app's UI.
- [ ] Run full validation matrix.
- [ ] Commit and push privacy hardening slice.

## Milestone 29: Original Intent Completion Gate

- [x] Windows real foreground process/window tracking works from WPF Start/Stop.
- [x] Windows local SQLite persistence and outbox are proven through fake pipeline and RealStart local validation.
- [ ] Browser domain tracking is explicit, privacy-aware, and covered by tests.
- [ ] Android UsageStats collection, Room persistence, WorkManager scheduling, and sync opt-in are proven.
- [ ] Server schema supports required relationships and idempotent integrated storage.
- [ ] WPF semantic UI acceptance passes with expected content.
- [ ] Android UI screenshot/device automation evidence is generated or blocked only by unavailable device.
- [ ] Unsafe/impossible/out-of-scope features are documented and not implemented.
- [ ] Full .NET tests/build pass.
- [ ] Android tests/build pass.
- [ ] Completion audit updated.
- [ ] Commit and push Original Intent completion.

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
