# Android UsageStats Current Focus Validation

Updated: 2026-05-03

This local emulator check validates that returning from Chrome refreshes the
Woong Dashboard Current Focus card to the app that is actually foreground:
Woong Monitor itself. Chrome can still be persisted as prior app-usage history,
but it must not be shown as the current focus while the Woong app is open.

It is not product telemetry.

## Privacy Boundary

The validation script:

- launches Chrome with `about:blank`;
- does not screenshot Chrome or any external app;
- returns to Woong Monitor Stack before taking any screenshot;
- captures only Woong UI after confirming Woong is foreground;
- optionally inspects Woong's own UI hierarchy after returning, expecting
  `Woong Monitor / com.woong.monitorstack` as current focus;
- does not collect typed text, form input, passwords, messages, clipboard data,
  touch coordinates, or external app page contents.

## Command

Dry run, no device required:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/run-android-usage-current-focus-validation.ps1 -DryRun -SkipBuild
```

Emulator run with build/install:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/run-android-usage-current-focus-validation.ps1 -DeviceSerial emulator-5554
```

Fast run when the debug APK is already installed:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/run-android-usage-current-focus-validation.ps1 -DeviceSerial emulator-5554 -SkipBuild
```

For the stronger app-switch QA path that also proves Chrome was collected as
the latest external app in Room-backed Dashboard state, run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/run-android-app-switch-qa.ps1 -DeviceSerial emulator-5554 -ChromeForegroundSeconds 3
```

That runner executes the instrumentation test
`dashboardAfterChromeReturnShowsWoongAsCurrentAndChromeAsLatestExternal` after
UsageStats collection. It pulls `dashboard-current-focus-evidence.json`,
`dashboard-current-focus-after-chrome-return.png`, and
`dashboard-current-focus-after-chrome-return.xml`; the screenshot and hierarchy
are captured only from Woong Monitor after the app is foreground again.

## Artifacts

Each run writes:

- `report.md`
- `manifest.json`
- `foreground-window.txt`
- `current-focus-after-chrome.png` when a device run reaches Woong foreground
- `current-focus-after-chrome.xml` when a device run reaches Woong foreground

The app-switch QA runner additionally writes:

- `dashboard-current-focus-evidence.json`
- `dashboard-current-focus-after-chrome-return.png`
- `dashboard-current-focus-after-chrome-return.xml`

Artifacts are written to:

```text
artifacts/android-usage-current-focus/<timestamp>/
artifacts/android-usage-current-focus/latest/
```

## Limitations

This is a no-wait emulator smoke check. It verifies that launching Chrome and
returning to Woong can update the Dashboard Current Focus card quickly to the
actual foreground app, while capturing only Woong UI.

It does not by itself prove the exact resume-before-collection-start boundary,
because ADB cannot force Android `UsageEvents` timestamps or WorkManager input
data for the production collector without a test/debug hook. The anchored
lookback and collection-window clamping behavior should remain covered by JVM
tests for `UsageSessionizer` and `AndroidUsageCollectionRunner`.
