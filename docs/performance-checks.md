# Performance Checks

Date: 2026-04-28

This document records the Milestone 12 CPU, memory, and battery-impact smoke
checks. These are not benchmark claims. They are release-candidate sanity checks
that the current MVP does not show obvious runaway resource use.

## Windows Collector Smoke

Command target:

```powershell
tools\Woong.MonitorStack.Windows.Smoke\bin\Debug\net10.0\Woong.MonitorStack.Windows.Smoke.exe
```

Method:

- Ran the Windows smoke executable five times.
- Captured exit code, wall time, process CPU time, and polled working set peak.
- The smoke executable performs one foreground-window capture and one idle
  calculation using the real Windows readers.

Results:

| Run | Exit | Wall ms | CPU ms | Peak working set MB |
| --- | ---: | ---: | ---: | ---: |
| 1 | 0 | 114.70 | 109.38 | 4.88 |
| 2 | 0 | 76.71 | 46.88 | 17.80 |
| 3 | 0 | 57.60 | 46.88 | 17.66 |
| 4 | 0 | 54.00 | 31.25 | 16.48 |
| 5 | 0 | 62.25 | 46.88 | 17.96 |

Result: acceptable for the current one-shot collector smoke. Warm runs stayed
under 80 ms wall time and under 18 MB peak working set.

## Android Dashboard Smoke

Environment:

- AVD: `Medium_Phone`
- Device model: `sdk_gphone16k_x86_64`
- Android release: 17
- SDK: 37
- ABI: x86_64

Method:

- Installed `android/app/build/outputs/apk/debug/app-debug.apk`.
- Reset batterystats.
- Launched the app through the exported launcher with:

```powershell
adb -s emulator-5554 shell monkey -p com.woong.monitorstack 1
```

- Waited 30 seconds with the app in foreground.
- Collected `top`, `dumpsys meminfo`, and package-scoped `dumpsys batterystats`.

Idle-after-launch results:

| Metric | Value |
| --- | ---: |
| App PID | 16353 |
| `top` CPU | 0.0% |
| `top` RES | 109 MB |
| Total PSS | 56,917 KB |
| Total RSS | 109,880 KB |
| Total swap PSS | 2,353 KB |
| Views | 11 |
| Activities | 1 |

Batterystats reset-window notes:

- UID `u0a250` was foreground for about 30.6 seconds.
- CPU attribution for UID `u0a250` was reported as `0.0000540` in the reset
  window.
- No recurring app-specific jobs were observed in the package-scoped output.
- Only a transient `*launch*` wakelock appeared for the app UID.

Result: acceptable for the current Android dashboard smoke. The app returned to
0.0% CPU while idle after launch. Memory use is reasonable for a debug build
with AppCompat, Room, WorkManager, Retrofit, and chart dependencies loaded.

## Follow-Up

- Repeat Android measurements on a real device before a production release.
- Add longer-running Windows collector profiling once the tracker runs as a
  continuous background service instead of one-shot smoke.
- Add Android WorkManager collection/sync profiling once periodic workers are
  configured from the UI.
