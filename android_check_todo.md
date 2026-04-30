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

## Current External Blocker

Physical-device resource measurement remains open until a real Android device
is connected. Emulator PASS evidence must not be used to close that physical
device TODO.

## Usage Access Onboarding Gate 2026-04-30

- [x] MainActivity shows permission onboarding when Usage Access is missing.
- [x] MainActivity shows Dashboard when Usage Access is granted.
- [x] Permission onboarding button opens Android Usage Access Settings.
- [x] Android UI snapshot package now includes `13-permission-onboarding.png`.
- [x] Latest emulator evidence: `artifacts/android-ui-snapshots/20260430-171242`.
