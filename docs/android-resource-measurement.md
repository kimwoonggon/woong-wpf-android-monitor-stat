# Android Resource Measurement

Updated: 2026-04-30

Android resource measurement is local QA evidence for the Woong Monitor Stack
Android app. It verifies that the app can launch through the normal Android
launcher entry point and that package-scoped memory/frame diagnostics can be
captured on an emulator or physical device.

This is not product telemetry.

## Privacy Boundary

The script:

- launches only `com.woong.monitorstack`;
- collects package-scoped `pidof`, `dumpsys meminfo`, and `dumpsys gfxinfo`
  output;
- writes local artifacts under `artifacts/android-resource-measurements/`;
- does not capture screenshots;
- does not record typed text, passwords, form input, messages, clipboard data,
  other-app content, or touch coordinates.

## Command

Run from the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/run-android-resource-measurement.ps1
```

Useful shorter emulator smoke:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/run-android-resource-measurement.ps1 -DurationSeconds 3
```

The script builds and installs the debug APK unless `-SkipBuild` is provided.
It exits successfully with `Status: BLOCKED` when no emulator or physical device
is connected, so missing device evidence is explicit.

When `-SkipBuild` is used, the debug app must already be installed. The script
checks the package launcher before invoking `monkey`; if the app is missing, it
writes a `BLOCKED` report that tells the user to rerun without `-SkipBuild`
instead of surfacing a raw ADB launch error.

## Artifacts

Each run writes:

- `report.md`
- `manifest.json`
- `process.txt`
- `meminfo.txt`
- `gfxinfo.txt`

Artifacts are written to:

```text
artifacts/android-resource-measurements/<timestamp>/
artifacts/android-resource-measurements/latest/
```

## Latest Emulator Evidence

- Command:
  `powershell -ExecutionPolicy Bypass -File scripts/run-android-resource-measurement.ps1 -SkipBuild -DurationSeconds 3`
- Result: PASS on `emulator-5554` / `Medium_Phone` emulator.
- Artifact: `artifacts/android-resource-measurements/20260430-184442`.
- Captured package-scoped process, memory, and graphics frame diagnostics.

## Remaining Gap

Physical-device resource measurement is still open. Emulator evidence must not
be used to mark the physical-device TODO complete.
