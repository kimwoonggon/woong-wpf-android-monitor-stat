# Android Emulator Stability

This note records the stable local emulator path for Android QA on Windows.

## Symptom

When Chrome or Chromium is opened inside the emulator, the host console can show
messages like:

```text
ERROR | bad color buffer handle
Critical: UpdateLayeredWindowIndirect failed
```

These messages come from the Android Emulator host renderer. They are not
evidence by themselves that Woong Monitor crashed.

## Current Finding

The `Medium_Phone` AVD in this workspace was configured with:

- Play Store system image;
- host GPU auto mode;
- 2048 MB RAM.

During Chrome app-switch QA, logcat showed low-memory pressure and process
restarts. The visible symptom was that Woong Monitor appeared to stop and then
opened from splash again after returning from another app.

The latest local evidence is under:

```text
artifacts/android-performance-manual/latest/
```

Important files:

- `report.md`
- `logcat-after-app-switch.txt`
- `woong-process-lifecycle-summary.txt`
- `gfxinfo.txt`
- `meminfo.txt`

## Stable Launch Command

Use the stable launcher for Chrome/app-switch QA:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\start-android-emulator-stable.ps1 -AvdName Medium_Phone -Restart
```

The script starts the emulator with:

```text
-gpu swiftshader_indirect -memory 4096 -no-snapshot-load -no-boot-anim
```

This avoids the Windows host GPU path and gives the emulator more memory for
Chrome plus Woong Monitor.

## If It Still Fails

If Chrome still makes the emulator unstable:

1. Create a non-Play-Store Google APIs AVD with at least 4 GB RAM.
2. Keep the same `swiftshader_indirect` launch mode.
3. Capture `logcat`, `dumpsys meminfo`, and `dumpsys gfxinfo` again before
   changing app code.

Do not treat emulator low-memory process death as proof that the production
Android app has a Java crash. Confirm with `adb logcat -b crash` and app process
lifecycle evidence.
