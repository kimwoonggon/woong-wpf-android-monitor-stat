# Total TODO List: Woong Monitor Stack

Updated: 2026-04-28

This file is the durable source of truth for the full PRD. Every agent must keep
it current before and after work. A TODO is complete only when relevant tests
pass, build succeeds, docs are updated, privacy boundaries are respected, and
the finished slice is committed and pushed.

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

## Milestone 5: Server Integrated DB + API MVP

- [x] Create ASP.NET Core Web API project.
- [x] Create server test project.
- [x] Add EF Core PostgreSQL.
- [ ] Define integrated DB entities.
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

- [ ] Define sync API client.
- [ ] Implement Windows sync worker.
- [ ] Implement retry policy.
- [ ] Add device token/auth placeholder.
- [ ] Add sync checkpoint handling.
- [ ] Test fake API sync success.
- [ ] Test fake API sync failure retry.
- [ ] Test duplicate upload remains safe.
- [ ] Verify Windows local data uploads to server.
- [ ] Commit and push Milestone 6.

## Milestone 7: Android Project Setup

- [ ] Locate or import `WoongAndroidBasicProject` Gradle style if available.
- [ ] Create Android project with Gradle wrapper.
- [ ] Configure Kotlin.
- [ ] Configure XML/View UI stack.
- [ ] Enable ViewBinding.
- [ ] Add ConstraintLayout.
- [ ] Add RecyclerView.
- [ ] Add Room dependencies.
- [ ] Add WorkManager dependencies.
- [ ] Add Retrofit/Moshi or OkHttp.
- [ ] Add unit test dependencies.
- [ ] Add Espresso/UI Automator dependencies.
- [ ] Add empty JUnit test.
- [ ] Add empty Espresso smoke test.
- [ ] Verify `gradlew testDebugUnitTest`.
- [ ] Verify `gradlew assembleDebug`.
- [ ] Commit and push Milestone 7.

## Milestone 8: Android Usage Collection + Room

- [ ] Implement Usage Access permission checker.
- [ ] Implement permission settings navigation UI entry.
- [ ] Implement UsageStats collector.
- [ ] Implement UsageSessionizer.
- [ ] Implement short consecutive event merge.
- [ ] Define Room entities/DAO.
- [ ] Implement collect worker.
- [ ] Test resumed/paused creates session.
- [ ] Test close same-app events merge.
- [ ] Test different app starts close previous session.
- [ ] Test Room DAO insert/query.
- [ ] Test worker behavior.
- [ ] Commit and push Milestone 8.

## Milestone 9: Android XML Dashboard MVP

- [ ] Create DashboardActivity or DashboardFragment.
- [ ] Create SessionsActivity or SessionsFragment.
- [ ] Create SettingsActivity or SettingsFragment.
- [ ] Add period filters.
- [ ] Add total active time card.
- [ ] Add top app card.
- [ ] Add idle/inactive card.
- [ ] Add MPAndroidChart time activity chart.
- [ ] Add app usage chart.
- [ ] Add recent sessions RecyclerView.
- [ ] Test ViewModel state updates.
- [ ] Test DashboardActivity display with Espresso.
- [ ] Test today/yesterday/recent 7 days filters.
- [ ] Test empty state.
- [ ] Test usage access guidance.
- [ ] Commit and push Milestone 9.

## Milestone 10: Android Sync + Morning Summary

- [ ] Define Android sync outbox.
- [ ] Implement Retrofit/OkHttp sync client.
- [ ] Implement WorkManager sync worker.
- [ ] Implement duplicate-safe upload handling.
- [ ] Integrate daily summary API.
- [ ] Add previous-day summary screen.
- [ ] Add morning summary notification if feasible.
- [ ] Test sync worker success.
- [ ] Test sync worker failure retry.
- [ ] Test duplicate upload.
- [ ] Test summary display.
- [ ] Commit and push Milestone 10.

## Milestone 11: Integrated Daily Summary

- [ ] Implement server daily aggregation job.
- [ ] Aggregate Windows + Android active time.
- [ ] Exclude idle time from active totals.
- [ ] Add app family mapping.
- [ ] Compute top app.
- [ ] Compute top domain.
- [ ] Respect user timezone local date.
- [ ] Test mixed Windows + Android data summary.
- [ ] Test timezone boundaries.
- [ ] Test duplicate data does not inflate summary.
- [ ] Verify Windows/Android can query integrated summary.
- [ ] Commit and push Milestone 11.

## Milestone 12: Hardening & Release Candidate

- [ ] Review DB migrations.
- [ ] Define raw event retention policy.
- [ ] Add sync failure UI.
- [ ] Add clear permission guidance text.
- [ ] Check Windows CPU/memory usage.
- [ ] Check Android CPU/memory/battery impact.
- [ ] Stabilize all tests.
- [ ] Write README.
- [ ] Run all unit/component/integration tests.
- [ ] Run WPF UI smoke tests.
- [ ] Run Android connected tests when device/emulator is available.
- [ ] Complete manual release checklist.
- [ ] Verify Windows build.
- [ ] Verify Android assemble.
- [ ] Verify server integration tests.
- [ ] Commit and push release candidate.

## Final Definition Of Done

- [ ] All PRD requirements reflected in code/tests/docs.
- [ ] All core logic built TDD-first.
- [ ] All relevant tests pass.
- [ ] All builds pass.
- [ ] Safety/privacy excluded scopes are not implemented.
- [ ] Local DB/server integrated DB separation is preserved.
- [ ] Daily integrated summary works across Windows + Android.
- [ ] Final documentation is complete.
- [ ] Final commit is pushed to `origin`.
