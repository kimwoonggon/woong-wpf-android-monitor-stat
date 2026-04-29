# Android UI Screenshot Testing

Updated: 2026-04-29

Android screenshot testing is local evidence for this app's own UI. It is not
product telemetry and must not capture other apps as usage data.

## Scope

Allowed screenshot targets:

- Woong Monitor Stack Dashboard.
- Sessions screen.
- Settings screen.
- Daily Summary screen.
- Usage Access permission handoff screen only as a manual/test artifact.

Not allowed:

- Screenshots of other apps as product telemetry.
- Background screen capture.
- Android touch-coordinate monitoring.
- Text input or screen-content extraction from other apps.

## Required Android UX Coverage

- Usage Access permission explanation.
- Open Usage Access settings button.
- Re-check permission after returning.
- No-permission state.
- Collection status.
- Last sync status.
- Local data status.
- Dashboard cards/charts/recent sessions with seeded sample usage.
- Settings privacy boundaries and sync opt-in state.

## Test Strategy

- JVM/Robolectric tests for ViewModels, repositories, Room DAO, WorkManager
  workers, and sync logic.
- Espresso tests for XML/View UI surfaces.
- UI Automator smoke for Usage Access Settings navigation.
- Optional Midscene/android-device-automation flow for visual review when model
  configuration and a device/emulator are available.

## Local Script

`scripts/run-android-ui-snapshots.ps1` is the repo-level entry point. It writes
local artifacts under `artifacts/android-ui-snapshots/<timestamp>/`, updates
`artifacts/android-ui-snapshots/latest/`, and always emits:

- `report.md`
- `manifest.json`
- `visual-review-prompt.md`

Run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/run-android-ui-snapshots.ps1
```

If no emulator or physical device is connected, the script exits successfully
with `Status: BLOCKED` and a clear reason. This is intentional: unavailable
device evidence should be explicit, not a silent failure.

Current behavior:

- Builds can be skipped with `-SkipBuild` for fast device-availability checks.
- The script checks `adb devices -l`.
- When no device is connected, it records the blocked state for dashboard,
  settings, sessions, and daily summary capture.
- When a device is connected and `-SkipBuild` is not used, it builds and
  installs the debug APK and debug androidTest APK before launching screens.
- Before capturing screenshots, it runs
  `com.woong.monitorstack.snapshots.SnapshotSeedTest` through instrumentation
  to seed deterministic Room focus sessions for Chrome, YouTube, Slack, and an
  idle interval. This keeps sample dashboard/session evidence local and
  test-only.
- When a device is connected, it launches Dashboard, Settings, Sessions, and
  Daily Summary activities and captures local PNG screenshots into the artifact
  folder.
- It does not use Midscene unless a future explicit visual-review slice adds
  model configuration and device steps.

Future connected-device improvements:

- State whether it used an emulator or physical device.
- Add optional Midscene visual review when model environment variables are
  configured.

## Current Gaps

- This environment currently has no connected Android device or emulator, so
  the connected-device capture path is covered with fake-adb tests and remains
  blocked for real visual evidence.
- Real seeded screenshots still require an attached emulator or physical
  device.
- Optional Midscene/android-device-automation requires model environment
  variables and a connected device/emulator.
- Physical-device resource measurement remains blocked until a device is
  connected.
