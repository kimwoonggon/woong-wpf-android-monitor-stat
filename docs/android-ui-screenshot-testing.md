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

## Required Future Tooling

Add a local-only Android screenshot script that:

- Builds the debug APK.
- Installs or launches the app on a connected emulator/device.
- Seeds deterministic sample data when possible.
- Captures dashboard/settings/sessions screenshots.
- Writes artifacts under `artifacts/android-ui-snapshots/<timestamp>/`.
- Updates `artifacts/android-ui-snapshots/latest/`.
- Writes `report.md`, `manifest.json`, and `visual-review-prompt.md`.

The script must clearly state whether it used an emulator or physical device.

## Current Gaps

- No repo-level Android screenshot script exists yet.
- UI Automator dependency/tests are missing.
- WorkManager scheduling needs explicit UX/startup wiring.
- Collected sessions need a clear path into sync outbox.
- Sync opt-in must be enforced in persisted settings, not only displayed.
- Physical-device resource measurement remains blocked until a device is
  connected.

