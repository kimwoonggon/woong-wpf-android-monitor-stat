# Android Check TODO

Updated: 2026-05-02

This checklist is the Android feature verification map for Woong Monitor Stack.
The Android app measures metadata only: which apps were foreground for how long,
local collection/sync status, optional location context, and daily summaries.
It must not collect typed text, passwords, form contents, clipboard contents,
other-app screen contents, or global touch coordinates.

## Evidence Folder

Latest run target:

```text
artifacts/android-check/latest/
```

Latest completed emulator run:

```text
artifacts/android-check/20260430-155023/
```

Latest Android UI screenshot evidence:

```text
artifacts/android-ui-snapshots/20260502-055751/
```

Latest Android UsageStats current-focus evidence:

```text
artifacts/android-usage-current-focus/20260502-012243/
```

Latest Android app-switch QA evidence:

```text
artifacts/android-app-switch-qa/20260502-052729/
artifacts/android-app-switch-qa/latest/
```

Latest location opt-in emulator evidence:

```text
artifacts/android-check/20260430-180504/
```

Latest Usage Access settings handoff evidence:

```text
artifacts/android-check/20260430-181643/
```

Latest Room seeded dashboard/sessions evidence:

```text
artifacts/android-check/20260430-183032/
```

Latest Android resource measurement evidence:

```text
artifacts/android-resource-measurements/20260430-223105/
```

Each checked feature should have:

- a `before-*.png` image showing the pre-check state or the expected test gate;
- an `after-*.png` image showing the passing state or resulting UI;
- a note in `report.md` when the feature is not directly visual.

## 2026-05-02 Reopened Android Figma 7-Screen Parity Backlog

This is the active Android backlog for restoring the user-provided Figma/SVG
intent across all major Android surfaces. The app is not considered Android UI
complete until this section is checked off with tests, emulator evidence, and
fresh screenshots.

Product boundary reminder:

- Android measures app-usage metadata through `UsageStatsManager`.
- Android stores local data in Room only.
- Sync is opt-in and off by default.
- Location context is opt-in and permission-gated.
- Do not collect typed text, passwords, form contents, clipboard contents,
  browser/page contents, other-app screenshots, or global touch coordinates.

### 2026-05-02 Android 7-Screen Acceptance Evidence Slice

- [x] Added `docs/android-figma-7-screen-acceptance.md` mapping Splash,
  Permission, Dashboard, Sessions, App Detail, Report, and Settings to concrete
  XML layouts, runtime surfaces, behavior gates, and canonical screenshot names.
- [x] Extended Android screenshot automation with canonical Figma evidence files:
  `figma-01-splash.png` through `figma-07-settings.png`.
- [x] Stabilized `SnapshotCaptureTest` so MainActivity shell screenshots use
  deterministic Usage Access gates and wait for Splash routing before selecting
  tabs or filters.
- [x] Fixed MainActivity delayed Splash routing so it does not enqueue a
  Fragment transaction after the Activity/FragmentManager has been destroyed.
- [x] Added Android snapshot script cleanup for external browser/system dialog
  interference before instrumentation captures, including Chrome force-stop and
  `CLOSE_SYSTEM_DIALOGS`.
- [x] Latest clean emulator evidence:
  `artifacts/android-ui-snapshots/20260502-055751/`.
- [x] Latest snapshot `report.md` status is `PASS`; all seven canonical Figma
  screenshots are `PASS`.
- [x] Latest snapshot crash buffer remained empty.
- [x] Dashboard first viewport now shows Hourly focus immediately after the
  period filters; Top apps are next below.
- [x] Report `figma-06-report.png` now shows a seeded multi-day, multi-point
  trend.
- [x] Sessions rows are visually compact while preserving readable app/package
  metadata.
- [x] Settings now uses clearer Permissions, Collection, Sync, and Privacy
  grouping.
- [x] Snapshot report now includes per-screen PASS/WARN rows for the seven
  canonical Figma screenshots.
- [x] Hidden-shell captures now keep Splash/Permission margins aligned without
  leaking the main toolbar or bottom navigation into those screens.
- [x] Crash buffer was cleared before the latest run and remained empty after
  the successful screenshot capture.
- [x] Validated focused architecture/Robolectric tests and the emulator
  screenshot script on `emulator-5554`.

### A. Acceptance Inventory And Evidence

- [x] Create/update an Android 7-screen acceptance inventory that maps every
  Figma screen to one concrete layout, Fragment/Activity, ViewModel/repository
  path, test class, and screenshot artifact.
- [x] Confirm the seven required user-facing screens are explicitly covered:
  Splash, Permission, Dashboard, Sessions, App Detail, Report, Settings.
- [x] Mark legacy standalone Activity screens as either temporary compatibility
  surfaces or cleanup targets.
- [x] Add a `report.md` section that states PASS/FAIL/WARN for each screen.
- [x] Capture fresh emulator screenshots for all seven screens after each major
  Android parity slice.
- [x] Store the latest full-screen evidence under
  `artifacts/android-ui-snapshots/latest/`.
- [ ] Store manual app-switch/performance evidence under
  `artifacts/android-manual-run/latest/` or a dated run folder.
- [ ] Add visual review notes comparing the latest screenshots to
  `artifacts/android-ui-flow/woong-monitor-android-ui-flow.figma-import.svg`.

### B. Splash Screen Parity

- [ ] Add/verify tests that cold start shows Splash before routing.
- [ ] Match the Figma Splash hierarchy: phone-safe top spacing, Woong logo
  mark, `Woong Monitor`, `Android Focus Tracker`, loading indicator, loading
  copy.
- [ ] Keep shell toolbar and bottom navigation hidden on Splash.
- [ ] Keep Android 12+ system splash branding consistent with the in-app Splash.
- [ ] Capture `01-splash-before.png` and `01-splash-after.png`.
- [ ] Ensure Splash does not delay normal relaunch longer than necessary after
  process death.

### C. Permission Onboarding Parity

- [ ] Add/verify tests that missing Usage Access routes to Permission, not
  Dashboard.
- [ ] Match the Figma Permission screen: back affordance, shield/lock visual,
  title, explanation, principles card, and primary Settings button.
- [ ] Explain exactly what is collected: app name, package name, start/end time,
  duration.
- [ ] Explain exactly what is not collected: keyboard input, screen contents,
  passwords, touch coordinates.
- [ ] Open Android Usage Access settings from the permission button.
- [ ] Re-check permission after returning from Settings.
- [ ] Capture permission missing, settings handoff, and post-return screenshots.

### D. Dashboard Parity And Runtime Truth

- [ ] Add/verify tests that Dashboard Current Focus is Room/UsageStats-backed
  and not fake in production paths.
- [ ] Current Focus must show the currently foreground app when Android can
  prove it; when a foreground app cannot be proven, it may fall back to the
  most recent meaningful UsageStats app according to Android constraints.
- [ ] If Woong Monitor itself is foreground immediately after return, Dashboard
  must make that state explicit and not incorrectly show stale Chrome as if it
  is still active.
- [ ] Add a clear copy distinction between `current foreground app`, `latest
  collected external app`, and `last collection time` if Android cannot provide
  true live foreground state while Woong is open.
- [ ] Match Figma status chips: Usage OK, Sync Off, Privacy Safe.
- [ ] Match Figma Current Focus card: app icon placeholder, app label, package,
  session duration, last collected time.
- [ ] Summary cards must show Active Focus, Screen On/Foreground, Idle/Gap, and
  local sync state.
- [ ] Period buttons Today/1h/6h/24h/7d must reload Room-backed data and visibly
  show selected state.
- [ ] Hourly chart must show meaningful labels and non-broken empty state.
- [x] Top apps list must show readable ranked app rows with proportional bars or
  an equivalent visual cue.
- [ ] Recent sessions must be visible without bottom navigation clipping.
- [ ] Optional location card must appear only when enabled or as a clear safe
  disabled state.
- [ ] Capture dashboard top, chart, recent sessions, and scrolled states.

### E. Sessions Screen Parity

- [ ] Add/verify tests for Sessions period filters: Today, 1h, 6h, 24h, 7d.
- [ ] Match Figma Sessions layout: title/subtitle, filter row, total count,
  scrollable session list, bottom navigation reachable.
- [ ] Rows must show app label, package, local time range, duration, and
  active/idle state without clipping.
- [ ] Tapping a session row must open the selected app detail screen.
- [ ] Add empty-state copy for no sessions in the selected period.
- [ ] Capture Sessions default, filtered, empty-state if possible, and row-tap
  screenshots.

### F. App Detail Screen Parity

- [ ] Add/verify tests that App Detail loads only the selected package from Room.
- [ ] Match Figma App Detail layout: back button, app icon, app name, package,
  total usage, session count, hourly chart, session list.
- [ ] Hourly chart must use real selected-app Room data.
- [ ] Session list must show selected package only.
- [ ] Back returns to Sessions without losing selected tab state.
- [ ] Capture App Detail for a seeded Chrome row and another seeded app row.

### G. Report Screen Parity

- [ ] Add/verify tests for 7d, 30d, 90d, and Custom report ranges.
- [ ] Match Figma Report layout: title, period filters, date range, total usage,
  daily average, trend chart, top apps.
- [ ] Custom range must support valid date input, invalid/reversed error state,
  and a visible selected state after apply.
- [ ] Trend chart must use Room-backed aggregate data and readable day labels.
- [x] Top apps list must show ranked apps with readable labels/durations and
  proportional usage bars.
- [ ] Capture Report 7d, 30d/90d, Custom valid, and Custom invalid states.

### H. Settings Screen Parity

- [ ] Add/verify tests that Settings shows permissions, collection, sync,
  privacy, location, and storage sections.
- [ ] Match Figma Settings card grouping and spacing.
- [ ] Usage Access permission status must be visible.
- [ ] Background/periodic collection setting must be visible and explain
  WorkManager-based collection.
- [ ] Sync enabled must default off and show local-only status.
- [ ] Manual sync must skip upload while sync is off and show a clear result.
- [ ] Privacy defaults must be safe.
- [ ] Location context must default off, with precise coordinates requiring
  separate opt-in.
- [ ] Storage/local data controls must be visible or explicitly disabled with
  reason.
- [ ] Capture Settings top, collection/sync, privacy/location, and storage
  scrolled states.

### I. Legacy Activity Cleanup

- [ ] Inventory legacy `DashboardActivity`, `SessionsActivity`,
  `DailySummaryActivity`, and `SettingsActivity`.
- [ ] Decide which legacy Activities remain as deep-link/dev compatibility and
  which should be removed.
- [ ] If removed, update AndroidManifest, tests, screenshots, and docs.
- [ ] If retained, document why they coexist with the MainActivity Fragment
  shell.
- [ ] Ensure no duplicate screen path shows stale UI that differs from the
  Figma shell.

### J. Chrome/App-Switch UsageStats Regression QA

- [x] Classify the run using the documented gate states:
  `PASS`, `BLOCKED` by emulator/adb/install/device readiness, or `FAIL` by
  product behavior after the flow is reachable.
- [x] Preserve the privacy rule: never capture Chrome screenshots, Chrome UI
  hierarchy dumps, page text, page titles, URL paths, typed text, form
  contents, or browser page contents; use package/process/window metadata such
  as `dumpsys` for external foreground proof.
- [x] App-switch QA PASS evidence:
  `artifacts/android-app-switch-qa/20260502-052729/` and
  `artifacts/android-app-switch-qa/latest/`.
- [x] `report.md` status is `PASS`.
- [x] `room-assertions.json` status is `PASS`, with
  `focusSessionChromeRows=4` and `syncOutboxChromeRows=4`.
- [x] Foreground-after-return evidence shows `com.woong.monitorstack`.
- [x] Woong-only screenshots exist:
  `dashboard-after-app-switch.png` and `sessions-after-app-switch.png`.
- [x] No Chrome screenshots or Chrome UI dumps were captured.
- [ ] Use `scripts/start-android-emulator-stable.ps1 -AvdName Medium_Phone
  -Restart` for repeatable emulator launch.
- [ ] Add an app-switch QA script that performs:
  Woong launch -> Chrome launch -> wait -> Woong return -> collection refresh.
- [x] Capture only Woong Monitor UI screenshots before launch, after return,
  Dashboard, and Sessions; prove Chrome foreground via package/process/window
  metadata such as `dumpsys`, not screenshots of Chrome page contents.
- [x] Assert Room `focus_session` includes Chrome after returning.
- [x] Assert `sync_outbox` rows are created for collected sessions.
- [x] Assert Dashboard summary and Sessions list refresh from Room after return.
- [ ] Record process ids before/during/after Chrome to detect emulator process
  death/relaunch.
- [ ] Capture logcat crash buffer, app logcat, `dumpsys meminfo`, and
  `dumpsys gfxinfo`.
- [ ] If emulator low-memory kills the app, classify as emulator stability issue
  unless `AndroidRuntime` crash evidence exists.
- [ ] Add retry/timing logic when screenshot capture is blank.

### K. Test Plan To Add Before Implementation

- [ ] `MainActivity_ColdStart_ShowsSplashThenRoutes`
- [ ] `MainActivity_WhenUsageAccessMissing_ShowsPermissionOnboarding`
- [ ] `PermissionOnboarding_OpenSettingsButton_UsesUsageAccessIntent`
- [ ] `Dashboard_CurrentFocus_SeparatesCurrentMonitorFromLatestExternalApp`
- [ ] `Dashboard_PeriodButtons_ReloadRoomBackedSummary`
- [ ] `Dashboard_TopApps_ShowRankedRowsWithDurations`
- [ ] `Sessions_PeriodButtons_FilterRoomRows`
- [ ] `Sessions_RowClick_OpensAppDetailForSelectedPackage`
- [ ] `AppDetail_LoadsOnlySelectedPackageSessions`
- [ ] `Report_PeriodButtons_ReloadRoomBackedRanges`
- [ ] `Report_CustomRange_ValidatesAndAppliesDateRange`
- [ ] `Settings_DefaultsArePrivacySafeAndSyncOff`
- [ ] `Settings_ManualSyncWhenOff_ShowsLocalOnlySkipped`
- [ ] `AndroidUiSnapshots_CapturesAllSevenFigmaScreens`
- [ ] `AndroidAppSwitch_ChromeReturn_PersistsRoomSessionAndRefreshesDashboard`

### L. Verification Commands For This Backlog

Run from `android/`:

```powershell
.\gradlew.bat testDebugUnitTest assembleDebug assembleDebugAndroidTest --no-daemon --stacktrace
```

Run from repository root:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\start-android-emulator-stable.ps1 -AvdName Medium_Phone -Restart
powershell -ExecutionPolicy Bypass -File scripts\run-android-ui-snapshots.ps1 -DeviceSerial emulator-5554
powershell -ExecutionPolicy Bypass -File scripts\run-android-usage-current-focus-validation.ps1 -DeviceSerial emulator-5554
powershell -ExecutionPolicy Bypass -File scripts\run-android-resource-measurement.ps1
```

Full repository safety checks:

```powershell
dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal
dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal
```

### M. Definition Of Done For Android Figma Parity

- [ ] All seven Figma screens have current emulator PNG evidence.
- [ ] Each screen has at least one behavior test or screenshot automation gate.
- [ ] Dashboard and Sessions are Room-backed and refresh after collection.
- [x] Chrome/app-switch QA proves collected UsageStats sessions persist to Room
  and update Dashboard/Sessions after returning to Woong.
- [ ] Report and App Detail charts use real Room-backed data.
- [ ] Settings exposes safe privacy/sync/location defaults.
- [ ] Legacy Activity coexistence is resolved or documented.
- [ ] Android Gradle tests/build pass.
- [ ] Full solution test/build pass.
- [ ] Docs and this checklist are updated.
- [ ] Commit and push are completed.

## Feature Checklist

| ID | Feature | Primary verification | PNG evidence |
|---|---|---|---|
| A01 | Usage Access permission guidance | Robolectric/architecture tests plus Settings/permission UI screenshot | before/after permission guidance |
| A02 | UsageStats app usage collection | Unit tests for reader/sessionizer/collector | before/after collector result card |
| A03 | Sessionization and short-event merge | `UsageSessionizerTest` | before/after sessionizer result card |
| A04 | Room local persistence | Room DAO/repository tests | before/after Room result card |
| A05 | WorkManager collection scheduling | Worker/scheduler tests | before/after WorkManager result card |
| A06 | Sync outbox and retry | Outbox DAO/sync processor/worker tests | before/after sync result card |
| A07 | Sync opt-in default off | Settings UI plus sync tests | before/after sync setting |
| A08 | Dashboard shell and current focus | Main shell screenshot and Robolectric tests | before/after dashboard/current-focus |
| A09 | Dashboard summary cards/charts | Dashboard tests and chart screenshot | before/after charts |
| A10 | Sessions list | Sessions fragment/activity tests and screenshot | before/after sessions |
| A11 | Report/daily summary | Report fragment and daily summary tests/screenshots | before/after report |
| A12 | Settings privacy/sync/storage | Settings tests and screenshot | before/after settings |
| A13 | Optional latitude/longitude context | Location settings/DAO/sync tests plus location screenshot | before/after location |
| A14 | Notification/morning summary | Notification policy and worker tests | before/after notification result card |
| A15 | Android UI screenshot automation | `scripts/run-android-ui-snapshots.ps1` | before/after screenshot run |
| A16 | Resource measurement | `scripts/run-android-resource-measurement.ps1` | before/after resource result card |
| A17 | Privacy forbidden scopes absent | Manifest/privacy/source guard tests | before/after privacy result card |

## Latest Emulator Result

Status: PASS on `emulator-5554`.

Generated evidence:

- `artifacts/android-check/latest/report.md`
- `artifacts/android-check/latest/manifest.json`
- `artifacts/android-check/latest/visual-review-prompt.md`
- `artifacts/android-ui-snapshots/latest/15-report-custom-range.png`
- `artifacts/android-usage-current-focus/latest/report.md`
- `artifacts/android-resource-measurements/20260430-174552/manifest.json`
- before/after PNG evidence for A01-A17

Commands completed for this run:

```powershell
dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~Android" -maxcpucount:1 -v minimal
```

```powershell
.\gradlew.bat testDebugUnitTest assembleDebug assembleDebugAndroidTest --no-daemon --stacktrace
```

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-android-ui-snapshots.ps1
powershell -ExecutionPolicy Bypass -File scripts\run-android-resource-measurement.ps1
```

Visual review entry points:

- Dashboard/current focus: `artifacts/android-check/latest/after-A08-dashboard-current-focus.png`
- Charts: `artifacts/android-check/latest/after-A09-dashboard-summary-charts.png`
- Sessions: `artifacts/android-check/latest/after-A10-sessions-list.png`
- Report: `artifacts/android-check/latest/after-A11-report-daily-summary.png`
- Settings: `artifacts/android-check/latest/after-A12-settings-privacy-sync-storage.png`
- Location dashboard/settings: `artifacts/android-check/latest/after-A13-location-context-dashboard.png`, `artifacts/android-check/latest/after-A13-location-context-settings.png`

## Verification Commands

Run from repository root unless noted:

```powershell
dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~Android" -maxcpucount:1 -v minimal
```

Run from `android/`:

```powershell
.\gradlew.bat testDebugUnitTest assembleDebug assembleDebugAndroidTest --no-daemon --stacktrace
```

Run from repository root with an emulator or device connected:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-android-ui-snapshots.ps1
powershell -ExecutionPolicy Bypass -File scripts\run-android-resource-measurement.ps1
powershell -ExecutionPolicy Bypass -File scripts\run-android-usage-current-focus-validation.ps1 -DeviceSerial emulator-5554
```

## Current Acceptance Baseline

Android validation is accepted on the connected emulator for this project state.
Physical-device resource measurement is optional future hardening, not a
release blocker, because a real Android device is not available in the current
workspace.

## Usage Access Onboarding Gate 2026-04-30

- [x] MainActivity shows permission onboarding when Usage Access is missing.
- [x] MainActivity shows Dashboard when Usage Access is granted.
- [x] Permission onboarding button opens Android Usage Access Settings.
- [x] Android UI snapshot package now includes `13-permission-onboarding.png`.
- [x] Latest emulator evidence: `artifacts/android-ui-snapshots/20260430-171242`.

## Usage Access Return Recheck And Collection Status 2026-04-30

- [x] MainActivity re-checks Usage Access when returning from Settings while Dashboard is selected.
- [x] Returning with newly granted Usage Access swaps permission onboarding to Dashboard.
- [x] MainActivity reconciles UsageStats collection scheduling through `AndroidUsageCollectionScheduler`.
- [x] Permission onboarding shows visible collection status text while permission is missing.
- [x] Latest emulator screenshot evidence: `artifacts/android-ui-snapshots/20260430-172700`.

## Usage Access Settings Handoff Evidence 2026-04-30

- [x] Onboarding explains Usage Access while collection remains disabled.
- [x] Open Usage Access settings button launches Android App usage data settings.
- [x] Android settings shows Woong Monitor as not allowed before user grant.
- [x] Returning without grant keeps explicit onboarding visible.
- [x] Before/action/after emulator evidence: `artifacts/android-check/20260430-181643`.

## Runtime Last-Known Location Reader 2026-04-30

- [x] Add `AndroidLastKnownLocationReader` for framework last-known foreground location metadata.
- [x] Read GPS/network/passive providers and choose the freshest available reading.
- [x] Skip provider `SecurityException`/`IllegalArgumentException` without crashing.
- [x] Preserve existing privacy gate: no snapshot unless location context opt-in and foreground permission are present.
- [x] Production `LocationContextCollectionRunner.create` uses the Android reader instead of the no-op reader.
- [x] Latest emulator screenshot evidence: `artifacts/android-ui-snapshots/20260430-174439`.

## Location Opt-In UI Evidence 2026-04-30

- [x] Settings location section shows location context off by default.
- [x] Precise latitude/longitude remains a separate unchecked opt-in until explicitly selected.
- [x] Location permission action is disabled before location context opt-in and enabled after opt-in.
- [x] Before/action/after emulator evidence: `artifacts/android-check/20260430-180504`.

## Room Seeded Dashboard And Sessions Evidence 2026-04-30

- [x] Clean Room state shows an empty Dashboard with no app/session data.
- [x] Android instrumentation seed writes deterministic focus sessions and location context into Room.
- [x] Dashboard renders seeded active, idle, sync, and location context values from Room.
- [x] Sessions tab renders seeded active and idle session rows from Room.
- [x] Before/action/after emulator evidence: `artifacts/android-check/20260430-183032`.

## Emulator Screenshot And Resource Evidence Refresh 2026-04-30

- [x] Re-ran Android UI screenshot automation on `emulator-5554` using the already-installed debug/debugAndroidTest APKs.
- [x] Captured dashboard, settings, sessions, daily summary, launcher shell tabs, permission onboarding, and location UI screenshots.
- [x] Re-ran package-scoped resource measurement on `emulator-5554` with a 3 second sample window.
- [x] Captured app-only `pidof`, `dumpsys meminfo`, and `dumpsys gfxinfo` diagnostics.
- [x] Fresh UI evidence: `artifacts/android-ui-snapshots/20260430-184358`.
- [x] Fresh resource evidence: `artifacts/android-resource-measurements/20260430-184442`.
- [x] Physical-device resource measurement is optional future hardening; emulator evidence is accepted as the current completion baseline.

## Physical Device Required Measurement Guard 2026-04-30

- [x] Added `-RequirePhysicalDevice` support to `scripts/run-android-resource-measurement.ps1`.
- [x] Added architecture tests proving emulator-only device lists produce `BLOCKED` physical-device artifacts.
- [x] Local physical-device-required run produced `artifacts/android-resource-measurements/20260430-191835` with status `BLOCKED` because only `emulator-5554` is connected.
- [x] Physical-device resource measurement is preserved as an optional future check; emulator PASS evidence is the current acceptance baseline.

## Emulator Acceptance Refresh 2026-04-30

- [x] Re-ran Android UI screenshot automation on `emulator-5554`.
- [x] Captured dashboard, settings, sessions, daily summary, launcher shell tabs, permission onboarding, and location UI screenshots.
- [x] Re-ran package-scoped resource measurement on `emulator-5554` with a 3 second sample window.
- [x] Captured app-only `pidof`, `dumpsys meminfo`, and `dumpsys gfxinfo` diagnostics.
- [x] Fresh UI evidence: `artifacts/android-ui-snapshots/20260430-222943`.
- [x] Fresh resource evidence: `artifacts/android-resource-measurements/20260430-223105`.
- [x] Accepted emulator evidence as the completion baseline; physical Android device measurement is deferred optional hardening.

## 2026-05-01 Android Immediate Usage Collection And Bottom Navigation Fix

- [x] Compact bottom navigation now uses a 72dp base height plus runtime system navigation inset so Dashboard/Sessions/Report/Settings stay visible without hardcoded extra bottom padding.
- [x] MainActivity now performs a foreground immediate UsageStats collection when Dashboard is shown and Usage Access is granted, then refreshes the Room-backed dashboard after collection.
- [x] Collection remains metadata-only: package names and foreground intervals from UsageStatsManager; no typed text, screen content, touch coordinates, or page content are captured.
- [x] Android usage collection defaults to enabled after explicit Usage Access grant while sync remains off/local-only by default.
- [x] Superseded historical behavior: Current Focus previously displayed the latest meaningful tracked external session from Room, so returning from Chrome showed Chrome as the current/latest tracked app. The current runtime-truth policy is documented in the 2026-05-02 correction: if Woong Monitor is foreground, Current Focus shows Woong Monitor instead of stale Chrome.
- [x] Emulator evidence captured:
  - `artifacts/android-check/manual/android-insets-start2.png`
  - `artifacts/android-check/manual/after-chrome-current-fixed.png`
- [x] Local emulator DB proof after Chrome return: `focus_sessions=9`, `sync_outbox=9`, with latest rows including `com.android.chrome`.
- [x] Validation: `android\\gradlew.bat testDebugUnitTest assembleDebug --no-daemon --stacktrace`, `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`, and `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed.

## 2026-05-01 Android Splash, Permission, And Current Focus Correction

- [x] `MainActivity` now shows a real Splash fragment on cold start before routing to Permission or Dashboard.
- [x] Top app bar and bottom navigation stay hidden on Splash and permission onboarding.
- [x] Android 12+ system splash branding uses the Woong bar logo instead of the default Android icon.
- [x] Permission onboarding uses the supplied shield/principles-card layout and keeps collection status separate from the static explanation text.
- [x] Superseded historical behavior: Dashboard Current Focus reported the latest meaningful tracked external app from Room, such as `Chrome / com.android.chrome`, while persisted Room sessions drove totals and recent session rows. Current policy prefers proven foreground truth, including `Woong Monitor / com.woong.monitorstack` when the app itself is foreground.
- [x] RED/GREEN coverage added for Splash routing, permission routing, foreground Current Focus precedence, Splash branding, and permission XML contract.
- [x] Android Gradle `testDebugUnitTest assembleDebug` passed.
- [x] Full solution `dotnet test` and `dotnet build` passed.
- [x] Latest emulator screenshots:
  - `artifacts/android-check/latest/00-os-splash-branded.png`
  - `artifacts/android-check/latest/02-permission-late2.png`
  - `artifacts/android-check/latest/05-dashboard-final.png`
- [ ] Continue UI parity pass: dashboard top-app/chart/report/detail visuals still need work against the provided Figma/SVG reference.

## 2026-05-01 Android Sessions, App Detail, Report, And Settings Parity Slice

- [x] Added RED architecture checks for reference-style Sessions, App Detail, Report, and Settings fragment structures.
- [x] Added RED Room repository behavior for selected-package app detail aggregation.
- [x] Sessions now use a scrollable reference-style list surface with subtitle, period filters, filter action, total count, and Room rows.
- [x] Tapping a session row opens an App Detail screen for that package.
- [x] App Detail now aggregates the selected package from Room and shows app identity, total usage, session count, chart container, and package-specific session rows.
- [x] Report now exposes reference-style period filters, date-range label, summary cards, trend chart container, and top apps list.
- [x] Settings now exposes grouped Permissions, Collection, Sync, Privacy, Location, Privacy/Storage, and Storage sections while keeping sync opt-in/off by default.
- [x] Fixed corrupted Korean Android strings for Splash/Permission and added Korean screen titles for Sessions, App Detail, Report, and Settings.
- [x] Emulator screenshots captured under `artifacts/android-check/latest/`:
  - `android-01-dashboard.png`
  - `android-02-sessions.png`
  - `android-03-app-detail.png`
  - `android-04-report.png`
  - `android-05-settings.png`
- [x] Validation passed: `android\\gradlew.bat testDebugUnitTest assembleDebug --no-daemon --stacktrace`, `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`, and `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`.
- [ ] Remaining Android UI gap: wire real chart entries for App Detail hourly usage and Report trend instead of chart containers/no-data states.
- [ ] Remaining Android parity gap: WPF browser domain/window-title tracking is not available through Android UsageStatsManager; Android should show app/package usage only unless a future explicit, safe Android API scope is approved.

## 2026-05-01 Android Current Focus External App Follow-Up

- [x] Superseded historical behavior: Robolectric coverage once expected `Chrome / com.android.chrome` after Chrome/launcher/Monitor rows existed. The 2026-05-02 correction supersedes this with foreground truth: Woong Monitor is shown when Woong is foreground.
- [x] Removed MainActivity's self-package Current Focus injection into DashboardFragment.
- [x] Superseded historical behavior: Dashboard skipped Monitor self rows and Nexus Launcher noise when selecting the Current Focus display candidate. Current behavior keeps launcher/SystemUI as noise but no longer treats Woong Monitor as noise when it is foreground.
- [x] Validation passed: focused Current Focus tests and full Android Gradle unit/build.
- [x] Emulator screenshot captured: `artifacts/android-check/latest/android-current-focus-after-chrome-dashboard.png`.

## 2026-05-01 Android Room-Backed Charts, Outbox Idempotency, And Screenshot Reliability

- [x] App Detail now renders Room-backed hourly usage chart data for the selected package.
- [x] Report now renders Room-backed recent-7-days daily trend chart data.
- [x] Chart formatter tests cover hour labels, day labels, and minute axes so charts avoid meaningless defaults.
- [x] Sync outbox duplicate enqueue now uses Room `IGNORE`, so a previously synced focus-session outbox item is not reset to pending.
- [x] Usage sessionization now closes a still-foreground open span at the collection window end, which keeps the current app visible even when Android does not emit a pause event before collection.
- [x] Android UI screenshot automation now captures `14-app-detail.png`.
- [x] Android UI screenshot automation accepts `-DeviceSerial`, allowing deterministic emulator targeting such as `emulator-5554`.
- [x] Latest screenshot evidence: `artifacts/android-ui-snapshots/20260501-191127/`.
- [x] Validation passed: `android\\gradlew.bat testDebugUnitTest assembleDebug assembleDebugAndroidTest --no-daemon --stacktrace`.
- [x] Full solution validation passed: `dotnet test` 491 passed / 6 skipped, `dotnet build` 0 warnings / 0 errors.
- [x] Coverage refreshed: line 88.0%, branch 69.5%, report at `artifacts/coverage/SummaryGithub.md`.
- [ ] Remaining Android gap: wire Settings sync controls to real opt-in state and a local-only/manual sync result.
- [ ] Remaining Android gap: add period-filter behavior for Dashboard/Sessions/Report buttons beyond the current default views.

## 2026-05-01 Android Settings Sync Opt-In And Sessions Period Filters

- [x] Settings Sync switch now reads and writes `SharedPreferencesAndroidSyncSettings`; sync remains off by default.
- [x] Manual Sync remains local-only when sync is off and shows a clear skipped status instead of uploading.
- [x] Sessions screen Today/1h/6h/24h/7d buttons now reload persisted Room sessions for the selected range.
- [x] Added RED/GREEN tests for Settings sync opt-in/manual skipped behavior and Room-backed session period filtering.
- [x] Latest screenshot evidence after installing the new APK: `artifacts/android-ui-snapshots/20260501-192752/`.
- [x] Validation passed: `android\\gradlew.bat testDebugUnitTest assembleDebug assembleDebugAndroidTest --no-daemon --stacktrace`.
- [x] Android UI snapshot automation passed on `emulator-5554`.
- [x] Full solution validation passed: `dotnet test` 491 passed / 6 skipped, `dotnet build` 0 warnings / 0 errors.
- [x] Coverage refreshed: line 88.0%, branch 69.5%, report at `artifacts/coverage/SummaryGithub.md`.
- [ ] Remaining Android gap: Dashboard period buttons still need full period reload behavior.
- [ ] Remaining Android gap: Report 30d/90d/custom filters still need real range behavior.
- [ ] Remaining Android gap: manual sync-on should eventually enqueue a configured server sync once endpoint/device configuration UI exists.

## 2026-05-01 Android Dashboard Period Filters And Room-Backed Charts

- [x] Dashboard Today/1h/6h/24h/7d buttons now reload Room-backed data.
- [x] Dashboard rolling windows are based on UTC `startedAtUtcMillis`/`endedAtUtcMillis`, not only `localDate`.
- [x] Dashboard top-app list now displays the top 3 app usage slices for the selected period.
- [x] Dashboard hourly focus chart now receives Room-backed bar entries for the selected period.
- [x] Added RED/GREEN tests for Dashboard last-hour repository behavior and MainActivity period-button behavior.
- [x] Latest screenshot evidence after installing the new APK: `artifacts/android-ui-snapshots/20260501-195308/`.
- [x] Validation passed: `android\\gradlew.bat testDebugUnitTest assembleDebug assembleDebugAndroidTest --no-daemon --stacktrace`.
- [x] Android UI snapshot automation passed on `emulator-5554`.
- [x] Full solution validation passed: `dotnet test` 491 passed / 6 skipped, `dotnet build` 0 warnings / 0 errors.
- [x] Coverage refreshed: line 88.0%, branch 69.5%, report at `artifacts/coverage/SummaryGithub.md`.
- [ ] Remaining Android gap: Report 30d/90d/custom filters still need real range behavior.
- [ ] Remaining Android gap: UsageStats collection needs anchored lookback/clamping for apps resumed before the collection start.
- [ ] Remaining Android visual gap: Dashboard chart styling is functional but still needs polish against the supplied reference.

## 2026-05-01 Android UsageStats Anchored Lookback And Report Ranges

- [x] UsageSessionizer now clamps sessions to `collectionStartUtcMillis`/`collectionEndUtcMillis`.
- [x] AndroidUsageCollectionRunner now queries an anchored lookback window and persists only the requested clipped interval.
- [x] This keeps current-focus collection robust when Chrome/another app was already foreground before the normal collection start.
- [x] Added `scripts/run-android-usage-current-focus-validation.ps1` as a no-wait emulator smoke for Chrome -> Woong Current Focus validation.
- [x] The validation script launches Chrome `about:blank`, returns to Woong, refuses to screenshot unless Woong is foreground, and captures only Woong UI artifacts.
- [x] Documented the script and its limitation in `docs/android-usage-current-focus-validation.md`: the exact anchored-boundary proof remains a JVM/test-hook responsibility.
- [x] Report 7d/30d/90d periods now aggregate from Room through a report-specific repository.
- [x] Report tab 7d/30d/90d buttons now reload total focus, daily average, date range, trend chart, and top apps.
- [x] Latest screenshot evidence after installing the new APK: `artifacts/android-ui-snapshots/20260501-201248/`.
- [x] Validation passed: `android\\gradlew.bat testDebugUnitTest assembleDebug assembleDebugAndroidTest --no-daemon --stacktrace`.
- [x] Android UI snapshot automation passed on `emulator-5554`.
- [x] Full solution validation passed: `dotnet test` 491 passed / 6 skipped, `dotnet build` 0 warnings / 0 errors.
- [x] Coverage refreshed: line 88.0%, branch 69.5%, report at `artifacts/coverage/SummaryGithub.md`.
- [x] Report custom range UI now exposes start/end `yyyy-MM-dd` inputs and applies an inclusive Room-backed date range.
- [x] Report custom range rejects invalid/reversed dates with a visible error and keeps the current summary.
- [x] Android UI screenshot automation now captures `15-report-custom-range.png`.
- [x] Latest custom-range screenshot evidence: `artifacts/android-ui-snapshots/20260501-203803/15-report-custom-range.png`.
- [x] Current-focus emulator validation passed: `artifacts/android-usage-current-focus/20260502-012243/`.
- [x] Report period buttons now expose selected state for 7d, 30d, 90d, and valid Custom ranges.
- [x] Latest selected Custom range screenshot evidence: `artifacts/android-ui-snapshots/20260501-210345/15-report-custom-range.png`.
- [x] Validation passed for Report selected-state polish: Android Gradle unit/build/androidTest APK, UI snapshot script on `emulator-5554`, full dotnet test/build, coverage line 88.0% / branch 69.5%.
- [x] Dashboard and Sessions period buttons now share selected/unselected styling with Report through `PeriodButtonStyler`.
- [x] Added RED/GREEN UI-state tests for Dashboard and Sessions selected-period behavior.
- [x] Android UI screenshot automation now captures `16-dashboard-1h-selected.png` and `17-sessions-6h-selected.png`.
- [x] Latest selected-period screenshot evidence: `artifacts/android-ui-snapshots/20260501-214011/`.
- [x] Validation passed for Dashboard/Sessions selected-state polish: Android Gradle unit/build/androidTest APK, UI snapshot script on `emulator-5554`, full dotnet test/build, coverage line 88.0% / branch 69.5%.
- [x] Dashboard, App Detail, and Report charts now share a readable MPAndroidChart visual contract through `DashboardChartConfigurator`.
- [x] Added RED/GREEN tests for muted chart axes/grids, disabled legends/highlights, branded bar/line datasets, and hidden raw value labels.
- [x] Latest chart screenshot evidence: `artifacts/android-ui-snapshots/20260501-221704/`.
- [x] Validation passed for chart visual polish: Android Gradle unit/build/androidTest APK, UI snapshot script on `emulator-5554`, full dotnet test/build, coverage line 88.0% / branch 69.5%.
- [x] Superseded historical behavior: Current Focus ignored common launcher/SystemUI noise after returning from Chrome and showed the latest meaningful external app. Current behavior still ignores launcher/SystemUI noise but shows Woong Monitor when Woong is the foreground app.
- [x] Current Focus session duration now uses the selected focus session duration instead of the selected-period total.
- [x] Added RED/GREEN test `dashboardCurrentFocusIgnoresAospLauncherAndSystemUiNoiseAfterReturningFromChrome`.
- [x] Latest current-focus UI snapshot evidence: `artifacts/android-ui-snapshots/20260501-224147/`.
- [x] Superseded historical evidence: Current-focus XML included Chrome/com.android.chrome at `artifacts/android-usage-current-focus/20260501-225045/`; the 2026-05-02 foreground validation now passes with Woong foreground evidence at `artifacts/android-usage-current-focus/20260502-012243/`.
- [x] Added Android GitHub Actions workflow `.github/workflows/android-ci.yml`.
- [x] Android CI runs unit tests, debug APK build, release APK build, and Android test APK build.
- [x] Android CI uploads debug/release/test APKs and unit test reports as GitHub Actions artifacts.
- [x] Added `scripts/validate-android-ci.ps1`; RED failed before workflow existed and GREEN passes after adding it.
- [x] Local equivalent CI command passed: `android\\gradlew.bat testDebugUnitTest assembleDebug assembleRelease assembleDebugAndroidTest --no-daemon --stacktrace`.
- [x] Current-focus validation screenshot timing/retry gap was resolved by the 2026-05-02 foreground validation pass.
- [ ] Remaining Android gap: add an optional test/debug hook if emulator validation must force explicit UsageStats collection `from/to` timestamps without relying on natural event timing.
- [x] Dashboard/Report top-app rows now reduce dead space and render proportional usage bars for ranked app lists.
- [x] Focused visual test: `ReportTopAppsVisualTest.reportTopAppsRenderProportionalUsageBars`.
- [ ] Remaining Android visual gap: improve Dashboard chart density so app labels remain readable when many apps exist.
