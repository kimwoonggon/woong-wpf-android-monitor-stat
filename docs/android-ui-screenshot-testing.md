# Android UI Screenshot Testing

Updated: 2026-04-30

Android screenshot testing is local evidence for this app's own UI. It is not
product telemetry and must not capture other apps as usage data.

## Scope

Allowed screenshot targets:

- Woong Monitor Stack main shell.
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
- Optional location context status, including a safe default where latitude and
  longitude are hidden because location capture is off.
- Dashboard cards/charts/recent sessions with seeded sample usage.
- Settings privacy boundaries and sync opt-in state.
- Settings location controls when implemented: location context off by default,
  approximate mode preferred, and precise latitude/longitude explicit opt-in.

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
- When a device is connected, it runs
  `com.woong.monitorstack.snapshots.SnapshotCaptureTest` through
  instrumentation. This keeps Dashboard, Settings, Sessions, and Daily Summary
  activities `exported=false` in the production manifest while still allowing
  local test screenshots.
- The instrumentation flow also captures feature-by-feature review shots:
  `01-dashboard-overview.png`, `02-dashboard-summary-location.png`,
  `03-dashboard-charts.png`, `04-dashboard-recent-sessions.png`,
  `05-settings-privacy-sync.png`, `06-settings-location-permission.png`,
  `07-sessions-list.png`, `08-daily-summary.png`, `09-main-shell.png`,
  `10-main-shell-sessions.png`, `11-main-shell-settings.png`, and
  `12-main-shell-report.png`.
- The instrumentation test captures local PNG screenshots into the app's
  external files directory, then the script pulls them into the artifact folder.
- It does not use Midscene unless a future explicit visual-review slice adds
  model configuration and device steps.
- The launcher `MainActivity` now uses a `MaterialToolbar`,
  `FragmentContainerView`, and `BottomNavigationView` shell. Existing Activity
  screenshots remain because they are still the Room-backed runtime surfaces
  while fragment ViewModels are wired incrementally.

Latest emulator evidence:

- `powershell -ExecutionPolicy Bypass -File scripts/run-android-ui-snapshots.ps1`
  passed on `Medium_Phone`.
- Artifact: `artifacts/android-ui-snapshots/20260430-133732`.
- Latest SettingsFragment runtime-wiring evidence:
  `artifacts/android-ui-snapshots/20260430-140141`.
- Latest ReportFragment runtime-wiring evidence:
  `artifacts/android-ui-snapshots/20260430-142758`.
- Latest compact toolbar evidence:
  `artifacts/android-ui-snapshots/20260430-143723`.
- Latest compact bottom-navigation label evidence:
  `artifacts/android-ui-snapshots/20260430-151005`; `09-main-shell.png` shows
  Dashboard, Sessions, Report, and Settings labels visible above the Android
  system-navigation area while keeping the custom overlay label row removed.
- Latest Current Focus wireframe-parity evidence:
  `artifacts/android-ui-snapshots/20260430-152733`; `09-main-shell.png` shows
  the launcher Dashboard Current Focus card as a compact horizontal app
  identity and timing row using seeded local test data.
- Captured dashboard, settings, sessions, daily summary, and the numbered
  feature screenshots listed above.
- Dashboard location card and Settings location section are visible in the
  captured screenshots.
- Dashboard, Sessions, Settings, and Daily Summary now avoid status-bar overlap
  through `fitsSystemWindows`, which is visible in the latest screenshots.
- Sessions rows now use a structured local row layout with package, local time
  range, duration, and active/idle state instead of plain debug text.
- `09-main-shell.png` captures the real launcher shell with top app bar,
  fragment dashboard content, and Material bottom navigation.
- The fragment dashboard summary tiles use distinct metric labels instead of
  repeating the same placeholder title.
- The fragment dashboard now reads seeded local Room data through
  `DashboardViewModel`/`RoomDashboardRepository`, so `09-main-shell.png` shows
  the seeded `com.android.chrome` current focus and non-zero Active/Screen/Idle
  totals.
- `10-main-shell-sessions.png` captures the real launcher shell after selecting
  Sessions; the list is loaded from seeded Room focus sessions through
  `SessionsFragment` and `RoomSessionsRepository`.
- The launcher shell is back on the compact 72dp Material bottom navigation
  shape from the user-provided XML skeleton; the oversized temporary overlay
  label row was removed.
- `09-main-shell.png` shows the fragment Dashboard with period filters directly
  after the summary cards, then the optional Room-backed location context card.
- `02-dashboard-summary-location.png` shows labeled latitude, longitude,
  accuracy, and captured-at values from seeded local test data.
- `03-dashboard-charts.png` now captures the actual chart section after nested
  scroll-coordinate repair. Its axes are human-readable: hour labels such as
  `09`, `10`, `11`, minute labels such as `0m` to `60m`, and app labels such as
  `Chrome`, `YouTube`, and `Slack`.
- Dashboard and Sessions rows now show user-facing app names first while
  preserving package names as secondary metadata. The row height was increased
  so app name, package, time range, state, and duration remain readable.
- `11-main-shell-settings.png` captures the real launcher shell after selecting
  Settings; it shows runtime privacy boundaries, Usage Access action, local-only
  sync status, notification permission action, and optional location context
  controls.
- `06-settings-location-permission.png` now scrolls to the actual Location
  context card and shows location off by default, precise latitude/longitude
  opt-in disabled, and the location permission action disabled until opt-in.
- `12-main-shell-report.png` captures the real launcher shell after selecting
  Report; it shows Room-backed Recent 7 days Active Focus, Daily Avg, Top App,
  and top-app rows from seeded local focus-session data.
- The launcher toolbar contract is now compact and test-guarded: 56dp app bar
  height, 16sp title text, and fixed 16dp title insets.
- The launcher bottom navigation is now compact and test-guarded: 96dp
  navigation height, 48dp system-navigation reserve, 144dp content reserve,
  18dp icons, 10sp labels, and no temporary overlay label row.

Future connected-device improvements:

- Add optional Midscene visual review when model environment variables are
  configured.
- Resource measurement is tracked separately in
  `docs/android-resource-measurement.md`; repeat that run on a physical device
  when hardware is connected.

## Current Gaps

- Emulator-backed screenshot evidence is complete for the current environment.
- The fragment shell can still receive future visual tightening from human/GPT
  review, but the compact toolbar/header contract is now covered by tests.
- Optional Midscene/android-device-automation requires model environment
  variables and a connected device/emulator.
- Physical-device resource measurement remains blocked until a device is
  connected.
