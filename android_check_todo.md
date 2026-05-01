# Android Check TODO

Updated: 2026-04-30

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
artifacts/android-ui-snapshots/20260501-192752/
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
- [x] Current Focus now displays the latest meaningful tracked external session from Room, not the Monitor app process itself, so returning from Chrome shows Chrome as the current/latest tracked app.
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
- [x] Dashboard Current Focus now reports the latest meaningful tracked external app from Room, such as `Chrome / com.android.chrome`, while persisted Room sessions still drive totals and recent session rows.
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

- [x] Added Robolectric behavior coverage that returns to the Monitor dashboard after Chrome/launcher/Monitor usage rows exist and expects `Chrome / com.android.chrome` in Current Focus.
- [x] Removed MainActivity's self-package Current Focus injection into DashboardFragment.
- [x] Dashboard now skips Monitor self rows and Nexus Launcher noise when selecting the Current Focus display candidate.
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
