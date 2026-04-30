# Completion Audit

Date: 2026-04-30

This audit checks the implemented repository against `docs/prd.md`,
`total_todolist.md`, and the metadata-only privacy boundary.

## Source Of Truth Check

- `docs/prd.md` remains the durable product/source-of-truth document.
- `total_todolist.md` remains the executable checklist derived from the PRD.
- No direct conflict was found between `docs/prd.md` and the current
  `total_todolist.md`.
- Physical-device-bound Android resource measurements have been reclassified as
  optional future hardening because a real Android device is not available.
  Emulator-backed Android UI screenshots and package-scoped resource
  measurements are the current acceptance baseline.

## Optional External Hardening

The following items can add confidence later, but they are not required to
complete the current project state:

- Repeat Android resource measurements on a physical Android device for
  battery, thermal, OEM background policy, and real hardware variability.

The current blocker readiness check can be rerun with:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\check-external-blockers.ps1
```

Latest result:

- Status: PASS for Docker readiness and BLOCKED only for optional physical
  Android-device readiness.
- Artifact: `artifacts/external-blockers/20260430-185208`.
- Android: only `emulator-5554` is connected; no physical Android device was
  reported by `adb devices -l`.
- Docker: Docker daemon is reachable by `docker ps`.

Latest emulator-backed Android UI evidence:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-android-ui-snapshots.ps1
```

Result:

- Status: PASS on `emulator-5554` / `Medium_Phone` emulator.
- Artifact: `artifacts/android-ui-snapshots/20260430-222943`.
- Captured dashboard, settings, sessions, daily summary, launcher shell,
  current-focus, chart, location, and report screenshots.
- Dashboard location card, Settings location section, compact bottom
  navigation labels, and current-focus wireframe parity are visible in the
  captured screenshots.

Latest emulator-backed Android resource evidence:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-android-resource-measurement.ps1
```

Result:

- Status: PASS on `emulator-5554` / `Medium_Phone` emulator.
- Artifact: `artifacts/android-resource-measurements/20260430-223105`.
- Captured package-scoped process, memory, and graphics frame diagnostics.
- The script does not capture screenshots, typed text, clipboard data,
  passwords, forms, messages, other-app content, or touch coordinates.

## Safety Boundary Search

The product remains a metadata measurement app. It measures app/window/site
usage duration and never captures user content.

Allowed and implemented scopes:

- Windows foreground app/window metadata.
- Browser domain/full URL only through documented privacy settings, with
  domain-only storage by default.
- Android app usage duration through UsageStatsManager and explicit Usage
  Access.
- Optional Android location context metadata, off by default, local-first, with
  precise latitude/longitude requiring separate opt-in and foreground
  permission.
- Local UI screenshots of this app's own dashboard/testing surfaces only.

Forbidden scopes remain absent from product implementation:

- Keylogging or typed text capture.
- Passwords, forms, messages, page contents, clipboard contents, screenshots of
  user activity, or screen recording.
- Covert/background surveillance.
- Android global touch-coordinate or text-input tracking.

## Database Separation

- Windows uses local SQLite repositories for Windows local focus/web/session
  data and outbox rows.
- Android uses Room/SQLite for Android local usage/location/session data and
  outbox rows.
- ASP.NET Core/PostgreSQL is the only integrated Windows + Android database.
- Architecture tests preserve Domain, Windows, Presentation, WPF App, and Server
  dependency direction so local clients do not depend on server internals or
  each other's local databases.

## Integrated Daily Summary

Server tests prove the integrated summary path:

- Windows and Android devices can register for the same user.
- Windows and Android focus sessions upload through API DTO contracts.
- Idle time is excluded from active time.
- App family mapping combines Windows `chrome.exe` and Android
  `com.android.chrome` into `Chrome`.
- Web sessions aggregate top domains.
- Other users' sessions are excluded.

Representative test:

- `DailySummaryApi_WhenWindowsAndAndroidClientsUploadSessions_ReturnsIntegratedSummary`

## Latest Validation Matrix

.NET:

```powershell
dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal
dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal
powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1
```

Results:

- .NET tests passed: 437 total, with 6 explicit PostgreSQL/Testcontainers
  tests skipped by default unless `WOONG_MONITOR_RUN_POSTGRES_TESTS=1`.
- .NET build passed with 0 warnings and 0 errors.
- Coverage report generation passed.
- Overall line coverage: 90.1% (3965/4398).
- Overall branch coverage: 70.6% (570/807).

Android:

```powershell
.\gradlew.bat testDebugUnitTest assembleDebug assembleDebugAndroidTest --no-daemon --stacktrace
powershell -ExecutionPolicy Bypass -File scripts\run-android-ui-snapshots.ps1
powershell -ExecutionPolicy Bypass -File scripts\run-android-resource-measurement.ps1
```

Result:

- Android unit tests, debug build, and debug androidTest build passed.
- Android UI snapshot script passed on `Medium_Phone` and generated
  `artifacts/android-ui-snapshots/20260430-153654`.
- Android launcher now uses `MainActivity` as the fragment shell and exposes
  Dashboard, Sessions, Report, and Settings through bottom navigation.
- Android resource measurement passed on `Medium_Phone` and generated
  `artifacts/android-resource-measurements/20260430-153804`.
- A verification rerun with a temp output root also passed after installing the
  debug APK before launch; `-SkipBuild` requires the app to already be
  installed.
- The resource script now checks launcher availability before `monkey` launch
  and writes a clear `BLOCKED` report when `-SkipBuild` is used before the app
  is installed.

WPF semantic acceptance:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1
```

Result:

- Passed.
- Artifact: `artifacts/wpf-ui-acceptance/20260430-153850`.
- RealStart used a temp SQLite DB, server sync stayed disabled, focus sessions
  persisted, and sync outbox rows were queued.

Chrome native messaging sandbox safety:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-chrome-native-message-acceptance.ps1 -CleanupOnly -DryRun
```

Result:

- Passed.
- Dry-run cleanup touched no registry values.
- The scoped HKCU test host key was the only target:
  `HKCU\Software\Google\Chrome\NativeMessagingHosts\com.woong.monitorstack.chrome_test`.
- Cleanup remained sandboxed to a temp Chrome profile and did not touch user
  Chrome windows/profiles.

Server PostgreSQL/Testcontainers:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-server-postgres-validation.ps1
```

Result:

- Passed.
- Artifact: `artifacts/server-postgres-validation/20260430-190958`.
- EF Core migrations applied through Npgsql against a Testcontainers PostgreSQL
  instance.
- The legacy `web_sessions.ClientSessionId` backfill was verified before the
  required unique index was applied.
- Concurrent duplicate focus/web/raw/location uploads were verified as
  race-safe idempotent operations.

## Completion Status

Windows, Android local build, Android emulator connected tests/screenshots,
server, architecture, privacy guardrails, coverage, WPF semantic acceptance,
Chrome native messaging sandbox, and docs are complete for the currently
available environment.

Physical-device Android resource measurement is deferred optional hardening.
The current Android completion baseline is emulator-backed UI screenshot
automation plus package-scoped resource measurement.
