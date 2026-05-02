# Woong Monitor Stack

Windows + Android + Server usage measurement system for local productivity
statistics and integrated daily summaries.

This project follows `docs/prd.md`. It is a visible productivity statistics
tool, not covert monitoring software.

## Quick Start: WPF App

Run the Windows dashboard from the repository root:

```powershell
dotnet run --project src\Woong.MonitorStack.Windows.App\Woong.MonitorStack.Windows.App.csproj
```

Release build and run:

```powershell
dotnet build Woong.MonitorStack.sln -c Release --no-restore -m:1 -v minimal
dotnet run --configuration Release --project src\Woong.MonitorStack.Windows.App\Woong.MonitorStack.Windows.App.csproj
```

You do not need to start SQLite. The WPF app uses a local SQLite file and
creates or updates it automatically:

```text
%LOCALAPPDATA%\WoongMonitorStack\windows-local.db
```

Runtime tracking/log events are also written locally so foreground/browser
polling failures can be diagnosed after the fact:

```text
%LOCALAPPDATA%\WoongMonitorStack\logs\windows-runtime.log
```

PostgreSQL and the ASP.NET Core server are only needed for server/integrated
sync scenarios. The WPF app can track locally and show the local dashboard
without them.

Inside the app, open **Settings** to manage the SQLite file:

- **Create / switch DB** creates a new local database file or switches to a
  selected path.
- **Load existing DB** opens an existing `.db` file and refreshes the dashboard
  from it.
- **Delete local DB** deletes the current local database and recreates an empty
  one after confirmation.
- **Runtime log** shows the local log path used for tracking start/stop, poll,
  persistence, sync-skip, and recoverable error events.
- Clicking the window X minimizes the app to the Windows taskbar so tracking can
  continue. Use **Settings -> Exit app** when you explicitly want to shut down.

## Privacy Boundaries

- Measures metadata: foreground app/window/site usage duration.
- Does not collect global keystroke contents.
- Does not collect passwords, messages, form input, clipboard contents, browser
  page contents, or user screen contents.
- Does not collect Android global touch coordinates.
- Collection state must be visible to the user.
- Sync is opt-in.
- Persisted instants are stored in UTC and converted at display boundaries.
- Windows SQLite and Android Room stay device-local.
- Only the ASP.NET Core server/PostgreSQL layer integrates Windows and Android
  data through API DTO contracts.

## Repository Layout

```text
src/
  Woong.MonitorStack.Domain/                 common domain and DTO contracts
  Woong.MonitorStack.Windows/                collector, SQLite, sync logic
  Woong.MonitorStack.Windows.Presentation/   WPF MVVM dashboard state
  Woong.MonitorStack.Windows.App/            WPF application shell
  Woong.MonitorStack.Server/                 ASP.NET Core API and EF model
tests/
  Woong.MonitorStack.*.Tests/                xUnit unit/component/integration tests
tools/
  Woong.MonitorStack.Windows.Smoke/          Windows foreground collector smoke
  Woong.MonitorStack.Windows.UiSnapshots/    local WPF UI snapshot tool
  Woong.MonitorStack.Windows.RealStartAcceptance/
  Woong.MonitorStack.ChromeNativeHost/       Chrome extension native host
android/
  app/                                       Kotlin XML/View Android app
extensions/
  chrome/                                    Chrome extension for browser metadata
scripts/
  *.ps1                                      local QA and acceptance helpers
docs/
  prd.md                                    durable product source of truth
  server-test-db-strategy.md
  wpf-ui-acceptance-checklist.md
  android-resource-measurement.md
```

## Prerequisites

Recommended environment:

- Windows 10 or later.
- .NET SDK 10.0.x.
- Android Studio / Android SDK with emulator support.
- JDK 17 for Android Gradle.
- Docker Desktop for PostgreSQL/Testcontainers validation.
- Chrome or Chromium for Chrome extension/native messaging acceptance.

Useful local environment variables from the repository root:

```powershell
$env:DOTNET_CLI_HOME='D:\woong-monitor-stack\.dotnet'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:NUGET_PACKAGES='D:\woong-monitor-stack\.nuget\packages'
$env:APPDATA='D:\woong-monitor-stack\.appdata'
$env:GRADLE_USER_HOME='D:\woong-monitor-stack\.gradle-user'
$env:ANDROID_HOME="$env:LOCALAPPDATA\Android\Sdk"
$env:ANDROID_SDK_ROOT="$env:LOCALAPPDATA\Android\Sdk"
```

## Restore And Build

Run from the repository root:

```powershell
dotnet restore Woong.MonitorStack.sln --configfile NuGet.config
dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal
```

`-maxcpucount:1` is intentional on this Windows workspace because parallel
solution builds previously failed without useful diagnostics.

## WPF App

### Run The WPF Dashboard

Run from the repository root:

```powershell
dotnet run --project src\Woong.MonitorStack.Windows.App\Woong.MonitorStack.Windows.App.csproj
```

Release mode:

```powershell
dotnet build Woong.MonitorStack.sln -c Release --no-restore -m:1 -v minimal
dotnet run --configuration Release --project src\Woong.MonitorStack.Windows.App\Woong.MonitorStack.Windows.App.csproj
```

By default the WPF app uses:

```text
%LOCALAPPDATA%\WoongMonitorStack\windows-local.db
```

The runtime log is:

```text
%LOCALAPPDATA%\WoongMonitorStack\logs\windows-runtime.log
```

The log is for this app's own tracking pipeline events and exceptions. It does
not record keystrokes, typed text, page contents, screenshots, passwords, form
input, or clipboard contents.

The main window is explicitly shown in the Windows taskbar. Clicking X minimizes
the window to the taskbar instead of exiting. Use **Settings -> Exit app** for a
real app shutdown.

To run safely with a temporary SQLite DB:

```powershell
$env:WOONG_MONITOR_LOCAL_DB='D:\woong-monitor-stack\artifacts\manual\windows-local.db'
dotnet run --project src\Woong.MonitorStack.Windows.App\Woong.MonitorStack.Windows.App.csproj
Remove-Item Env:\WOONG_MONITOR_LOCAL_DB
```

Useful WPF environment variables:

```powershell
$env:WOONG_MONITOR_LOCAL_DB='D:\path\to\windows-local.db'
$env:WOONG_MONITOR_DEVICE_ID='windows-dev-machine'
$env:WOONG_MONITOR_AUTO_START_TRACKING='1'      # default is true
$env:WOONG_MONITOR_ACCEPTANCE_MODE='TrackingPipeline'
```

Supported `WOONG_MONITOR_ACCEPTANCE_MODE` values:

- `None`
- `SampleDashboard`
- `TrackingPipeline`

### WPF Smoke And Acceptance

Foreground collector smoke:

```powershell
dotnet build tools\Woong.MonitorStack.Windows.Smoke\Woong.MonitorStack.Windows.Smoke.csproj
dotnet run --project tools\Woong.MonitorStack.Windows.Smoke\Woong.MonitorStack.Windows.Smoke.csproj --no-build
```

Local WPF UI acceptance:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1
```

Real-start acceptance with local temp DB and no real server sync:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-wpf-real-start-acceptance.ps1
```

UI screenshot package:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-ui-snapshots.ps1
```

Consolidated WPF check package:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-wpf-check-package.ps1
```

WPF artifacts are written under ignored `artifacts/` folders such as:

- `artifacts/wpf-ui-acceptance/`
- `artifacts/ui-snapshots/`
- `artifacts/wpf-check/`

### Windows CI/CD And MSIX

GitHub Actions workflow:

```text
.github/workflows/windows-wpf-ci.yml
```

It restores, builds, tests, publishes the Windows app, packages a signed MSIX,
and uploads installable artifacts. If `WINDOWS_MSIX_CERTIFICATE_BASE64` and
`WINDOWS_MSIX_CERTIFICATE_PASSWORD` repository secrets are configured, CI signs
with that stable release signing certificate. If either secret is missing, CI
falls back to an ephemeral test certificate for local verification artifacts.

Local MSIX packaging:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\package-windows-msix.ps1
```

Signed local test MSIX packaging:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\package-windows-msix.ps1 -CreateTestCertificate
```

GitHub Actions uploads `woong-monitor-windows-msix` with:

- `WoongMonitorStack.Windows.msix`
- `certificates\WoongMonitorStack.Windows.Signing.cer` for release-secret builds,
  or `certificates\WoongMonitorStack.Windows.TestSigning.cer` for fallback test
  certificate builds
- `install-windows-msix.ps1`
- `Install-WoongMonitorStack.Windows.cmd`
- `README.md`

Recommended signed install for the CI/local test certificate:

1. Extract the `woong-monitor-windows-msix` artifact.
2. Right-click `Install-WoongMonitorStack.Windows.cmd`.
3. Choose **Run as administrator**.

Manual install from an elevated PowerShell prompt:

```powershell
powershell -ExecutionPolicy Bypass -File artifacts\windows-msix\install-windows-msix.ps1 `
  -PackagePath artifacts\windows-msix\WoongMonitorStack.Windows.msix `
  -CertificatePath artifacts\windows-msix\certificates\WoongMonitorStack.Windows.TestSigning.cer `
  -TrustCertificate `
  -TrustScope LocalMachine
```

If Windows reports `0x800B010A` or says the publisher certificate cannot be
verified, the public certificate from that exact artifact is not trusted in
`Cert:\LocalMachine\TrustedPeople` yet, or a `.cer` from a different CI run was
used. Use the `.cer` shipped beside the `.msix`; the fallback ephemeral test
certificate changes on every CI run. Double-clicking the `.msix` before that
trust step is expected to fail for self-signed CI artifacts.

Remove local test certificate trust after verification:

```powershell
$thumbprint = (Get-AuthenticodeSignature .\WoongMonitorStack.Windows.msix).SignerCertificate.Thumbprint
certutil -delstore TrustedPeople $thumbprint
```

Run that cleanup from an elevated PowerShell prompt if you trusted the
certificate with `-TrustScope LocalMachine`.

More details: `docs/windows-release-msix.md`.

Tag-based Windows releases use `.github/workflows/windows-wpf-release.yml`.
Push a `v*` tag after configuring `WINDOWS_MSIX_CERTIFICATE_BASE64` and
`WINDOWS_MSIX_CERTIFICATE_PASSWORD`; the release workflow signs with that stable
certificate and attaches the zipped MSIX bundle to the GitHub Release.

### Android CI/CD And APK Artifacts

GitHub Actions workflow:

```text
.github/workflows/android-ci.yml
.github/workflows/android-release.yml
```

It runs on pushes and pull requests that touch Android files, plus manual
`workflow_dispatch`. The workflow sets up Java 17, Android SDK components,
Gradle, then runs:

```bash
./gradlew testDebugUnitTest assembleDebug assembleRelease assembleDebugAndroidTest --no-daemon --stacktrace
```

On success, Android CI uploads:

- `woong-monitor-android-debug-apk`
- `woong-monitor-android-release-apk`
- `woong-monitor-android-test-apk`
- `woong-monitor-android-unit-test-report`

The upload steps use `if: always()` with missing files ignored, so unit test
reports and any APKs produced before a failure are still preserved for triage.
Push/PR Android CI does not run emulator or `connectedDebugAndroidTest`; manual
connected-test evidence lives in the separate emulator workflow below.

The Android CI release APK artifact is not a Play Store release. It is a
CI-built unsigned package artifact for local verification only.

The Android release workflow runs from `android/` and uses the checked-in
Gradle wrapper. It builds unit-test/debug/release/androidTest outputs without
requiring connected emulator/device tests in that workflow, but tag-based
release publishing requires `ANDROID_KEYSTORE_BASE64`,
`ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_ALIAS`, and `ANDROID_KEY_PASSWORD`.
If any signing secret is missing, the Android release workflow fails before
publishing. The release archive contains the signed release APK only; unsigned,
debug, and androidTest APKs remain CI artifacts from the non-release Android CI
workflow and must not be treated as Play Store-ready packages.
The release archive also contains `release-readiness.json`, a provenance
manifest that records `versionCode`, `versionName`, the signed APK SHA-256,
whether a production sync endpoint was configured, that sync default opt-in
remains false, that Play publishing is manual, and that an emulator evidence
path is required before any public release promotion. Set repository variable
`ANDROID_EMULATOR_EVIDENCE_PATH` to the accepted emulator evidence folder before
tagging; the manifest records both `emulatorEvidenceStatus` and
`emulatorEvidencePath` so operators can see whether concrete evidence was
provided.

Android Play publishing remains an operator-controlled step. Before creating an
`android-v*` release tag, configure the signing secrets
`ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_ALIAS`, and
`ANDROID_KEY_PASSWORD`, confirm the app `versionCode` and `versionName`, and
choose the approved Play Console track. After the tag release finishes, download
the GitHub Release archive, verify `woong-monitor-android-release-signed.apk`
is the only release APK, compare `release-readiness.json` against the signed APK
SHA-256 and endpoint/sync policy, upload the signed APK to the approved Play
Console track, and record the GitHub tag, artifact name, Play track, release
approver, emulator evidence status, and emulator evidence path. Unsigned, debug,
and androidTest APKs are internal validation artifacts only.

Android/server sync is also not a public release path until these policies are
closed: production endpoint discovery/configuration, local-development endpoint
labeling, user-auth/device-registration policy, secure device-token storage,
token rotation/revocation, and Android Play signing/publishing approval. Current
APK artifacts are for internal validation.

For Android/server sync, release builds must not silently fall back to a local,
blank, or example endpoint. If a production endpoint is unset, sync remains
disabled. Release builds may accept user-entered endpoints only as explicit
advanced/manual configuration for internal operators or testers; local HTTP is
limited to labeled nonproduction loopback endpoints.

The release-managed Android sync endpoint is `BuildConfig.PRODUCTION_SYNC_BASE_URL`.
Set it with Gradle property `woongProductionSyncBaseUrl` or environment variable
`WOONG_ANDROID_PRODUCTION_SYNC_BASE_URL` during the release build. If it is
unset, blank, local/example, or otherwise invalid, Android resolves no
production endpoint and sync remains disabled/fail-closed until an operator or
tester explicitly enters an advanced endpoint.

Local release workflow contract validation:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-android-release-workflow.ps1
dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~AndroidReleaseWorkflow" -maxcpucount:1 -v minimal
```

Manual emulator-backed connected tests use a separate workflow so they do not
run on every push:

```text
.github/workflows/android-emulator-manual.yml
```

It is `workflow_dispatch`-only and should be run only when emulator evidence is
needed and runner capacity is acceptable. Local contract validation:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\validate-android-emulator-workflow.ps1 -WorkflowPath .github\workflows\android-emulator-manual.yml
dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter AndroidManualEmulatorWorkflowTests -v minimal
```

## Android App

The Android MVP is Kotlin + XML/View based. Do not migrate it to Compose for
MVP work.

### Start An Emulator

From PowerShell, first point commands at the Android SDK:

```powershell
$env:ANDROID_HOME="$env:LOCALAPPDATA\Android\Sdk"
$env:ANDROID_SDK_ROOT="$env:LOCALAPPDATA\Android\Sdk"
```

List existing Android Virtual Devices:

```powershell
& "$env:ANDROID_HOME\emulator\emulator.exe" -list-avds
```

This workspace currently uses `Medium_Phone`. If that AVD does not exist on a
fresh machine, install a system image and create it:

```powershell
& "$env:ANDROID_HOME\cmdline-tools\latest\bin\sdkmanager.bat" "platform-tools" "emulator" "platforms;android-36" "system-images;android-36;google_apis;x86_64"
& "$env:ANDROID_HOME\cmdline-tools\latest\bin\avdmanager.bat" create avd -n Medium_Phone -k "system-images;android-36;google_apis;x86_64" -d "pixel_5"
```

For normal lightweight checks, start the emulator in its own terminal and leave
that terminal open:

```powershell
& "$env:ANDROID_HOME\emulator\emulator.exe" -avd Medium_Phone
```

For Chrome/app-switch QA, use the stable launcher instead:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\start-android-emulator-stable.ps1 -AvdName Medium_Phone -Restart
```

This launches the AVD with 4 GB RAM, no boot animation, and no snapshot
restore. The default GPU mode is `auto` because the current
`Medium_Phone` Play Store Android 37 image boots reliably that way. If a
different AVD supports software rendering, you can try:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\start-android-emulator-stable.ps1 -AvdName Medium_Phone -Restart -GpuMode swiftshader_indirect
```

Use this stable launcher when the emulator prints Chromium renderer messages
such as `bad color buffer handle` or `UpdateLayeredWindowIndirect failed`, or
when Chrome causes Woong Monitor to relaunch from splash because the emulator is
under memory pressure.

In a second terminal, wait for the device and confirm Android has finished
booting:

```powershell
& "$env:ANDROID_HOME\platform-tools\adb.exe" devices -l
& "$env:ANDROID_HOME\platform-tools\adb.exe" wait-for-device
& "$env:ANDROID_HOME\platform-tools\adb.exe" shell getprop sys.boot_completed
```

`sys.boot_completed` should print `1`. If it prints nothing, wait a little and
run the same command again.

### Build, Install, And Launch

Run Gradle commands from `android/`:

```powershell
cd D:\woong-monitor-stack\android
.\gradlew.bat testDebugUnitTest --no-daemon --stacktrace
.\gradlew.bat assembleDebug --no-daemon --stacktrace
.\gradlew.bat assembleDebugAndroidTest --no-daemon --stacktrace
```

Install and launch:

```powershell
& "$env:ANDROID_HOME\platform-tools\adb.exe" install -r app\build\outputs\apk\debug\app-debug.apk
& "$env:ANDROID_HOME\platform-tools\adb.exe" shell monkey -p com.woong.monitorstack -c android.intent.category.LAUNCHER 1
```

The launcher activity is:

```text
com.woong.monitorstack/.MainActivity
```

Take a quick manual screenshot after launch:

```powershell
New-Item -ItemType Directory -Force artifacts\android-check\manual
& "$env:ANDROID_HOME\platform-tools\adb.exe" shell screencap -p /sdcard/woong-dashboard.png
& "$env:ANDROID_HOME\platform-tools\adb.exe" pull /sdcard/woong-dashboard.png artifacts\android-check\manual\dashboard.png
& "$env:ANDROID_HOME\platform-tools\adb.exe" shell rm /sdcard/woong-dashboard.png
```

Use the `screencap` + `pull` flow on PowerShell. Raw `exec-out screencap -p >
file.png` can corrupt PNG bytes when PowerShell treats redirected output as
text.

If the app needs Usage Access permission, open the Android settings handoff:

```powershell
& "$env:ANDROID_HOME\platform-tools\adb.exe" shell am start -a android.settings.USAGE_ACCESS_SETTINGS
```

### Android Instrumentation And Emulator QA

With an emulator or device connected:

```powershell
cd D:\woong-monitor-stack\android
.\gradlew.bat connectedDebugAndroidTest --no-daemon --stacktrace
```

Android UI screenshots from the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-android-ui-snapshots.ps1
```

If more than one emulator/device is connected, pin the target explicitly:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-android-ui-snapshots.ps1 -DeviceSerial emulator-5554
```

The screenshot script uses instrumentation to seed Room data and capture
Dashboard, Sessions, App Detail, Report, Report custom range, Settings, and permission screens from
inside the app. It does not use blind coordinate taps or screenshot other apps.

Current-focus emulator smoke after opening Chrome and returning to Woong:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-android-usage-current-focus-validation.ps1 -DeviceSerial emulator-5554
```

That script captures only Woong Monitor UI after confirming Woong is foreground.
It does not screenshot Chrome or inspect Chrome page contents.

Package-scoped resource measurement:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-android-resource-measurement.ps1
```

Short emulator resource smoke:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-android-resource-measurement.ps1 -DurationSeconds 3
```

Optional physical-device-only resource evidence:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-android-resource-measurement.ps1 -RequirePhysicalDevice
```

`-RequirePhysicalDevice` intentionally writes `Status: BLOCKED` when only an
emulator is connected. The current completion baseline accepts emulator-backed
UI screenshots and package-scoped resource measurements; physical-device
measurement is optional future hardening for battery, thermal, and real
hardware variability.

Android artifacts are written under ignored folders:

- `artifacts/android-ui-snapshots/`
- `artifacts/android-usage-current-focus/`
- `artifacts/android-resource-measurements/`
- `artifacts/android-check/`

### Android Permissions

The app uses `UsageStatsManager`, so manual Usage Access approval is required
for real app-usage collection:

```powershell
& "$env:ANDROID_HOME\platform-tools\adb.exe" shell am start -a android.settings.USAGE_ACCESS_SETTINGS
```

Location context is opt-in. Precise latitude/longitude requires separate
foreground location permission and explicit user setting.

## ASP.NET Core Server And PostgreSQL

### Local PostgreSQL With Docker

For local development, run PostgreSQL through Docker Compose from the repository
root. This keeps the integrated Blazor dashboard and server API on PostgreSQL
without requiring a manually installed database:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\start-server-postgres.ps1
```

The script:

- creates `.env` from `.env.example` when missing;
- starts `woong-monitor-postgres` from `docker-compose.yml`;
- maps host port `55432` to container port `5432`;
- applies EF Core migrations unless `-SkipMigrations` is passed;
- prints the server and dashboard commands.

Use dry-run mode to see the exact commands without touching Docker:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\start-server-postgres.ps1 -DryRun
```

Stop the local database:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\stop-server-postgres.ps1
```

Delete the local dev PostgreSQL volume only when you intentionally want to reset
all server data:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\stop-server-postgres.ps1 -RemoveVolumes
```

The development connection string is:

```text
Host=localhost;Port=55432;Database=woong_monitor;Username=woong;Password=woong_dev_password
```

To set it explicitly in PowerShell:

```powershell
$env:ConnectionStrings__MonitorDb='Host=localhost;Port=55432;Database=woong_monitor;Username=woong;Password=woong_dev_password'
```

Apply EF Core migrations manually if you skipped the script migration step:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update `
  --project src\Woong.MonitorStack.Server\Woong.MonitorStack.Server.csproj `
  --startup-project src\Woong.MonitorStack.Server\Woong.MonitorStack.Server.csproj
```

Run the API server:

```powershell
dotnet run --project src\Woong.MonitorStack.Server\Woong.MonitorStack.Server.csproj
```

Open the integrated Blazor dashboard:

```text
http://localhost:5000/dashboard?userId=user-1&from=2026-04-30&to=2026-04-30&timezoneId=UTC
```

### Local WPF + Android Emulator To Blazor Dashboard

For the normal local workflow, you do not need production deployment or Play
Store signing. Use the WPF app and Android emulator app, then run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-local-integrated-dashboard.ps1
```

That command:

- starts Docker PostgreSQL;
- starts the ASP.NET Core/Blazor server on `http://127.0.0.1:5087`;
- reads WPF SQLite from `%LOCALAPPDATA%\WoongMonitorStack\windows-local.db`;
- pulls Android emulator Room `woong-monitor.db` with `adb`;
- uploads both local metadata sets through server API DTOs;
- opens the Blazor dashboard for `local-user`.

If you only want to see what it will do:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-local-integrated-dashboard.ps1 -DryRun
```

More details are in `docs/local-integrated-dashboard.md`.

Development OpenAPI is exposed by ASP.NET Core when the environment is
Development.

### Production Migration Bundle Validation

Production migrations should be applied deliberately by an operator. The helper
script builds a reviewed EF Core migration bundle; it does not apply migrations
and does not accept production connection strings.

Safe local validation:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\build-server-migration-bundle.ps1 -Help
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\build-server-migration-bundle.ps1 -Configuration Release -OutputPath artifacts\server-migrations\dry-run-validation.exe -DryRun
dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter ServerProductionMigrationRunbookTests -v minimal
```

Build the bundle only when a release operator is ready to review it:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build-server-migration-bundle.ps1 `
  -Configuration Release `
  -OutputPath artifacts\server-migrations\woong-server-migrations.exe
```

More details: `docs/production-migrations.md`.

### PostgreSQL/Testcontainers Validation

Start Docker Desktop first. Then run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\check-external-blockers.ps1
powershell -ExecutionPolicy Bypass -File scripts\run-server-postgres-validation.ps1
```

The PostgreSQL validation script:

- starts PostgreSQL through Testcontainers;
- applies EF Core migrations through Npgsql;
- verifies legacy `web_sessions.ClientSessionId` backfill;
- verifies relational constraints and race-safe idempotency for duplicate
  focus/web/raw/location uploads;
- writes artifacts under `artifacts/server-postgres-validation/`.

The PostgreSQL tests are skipped during normal `dotnet test` unless explicitly
enabled:

```powershell
$env:WOONG_MONITOR_RUN_POSTGRES_TESTS='1'
dotnet test tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj `
  --filter "FullyQualifiedName~PostgresMonitorDbContextTests" `
  -maxcpucount:1 -v minimal
Remove-Item Env:\WOONG_MONITOR_RUN_POSTGRES_TESTS
```

## Chrome Extension And Native Messaging

Chrome/Edge/Firefox/Brave active tab URL/domain tracking is not done through
FlaUI address-bar scraping. The intended path is browser extension metadata to
C# native host to SQLite/dashboard.

Run the safe Chrome native messaging acceptance:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-chrome-native-message-acceptance.ps1
```

Registry safety notes:

- Test host name is `com.woong.monitorstack.chrome_test`.
- Tests use HKCU only.
- The script uses a temp Chrome profile and temp SQLite DB.
- Cleanup restores or removes only the scoped test host key.

Dry-run cleanup:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-chrome-native-message-acceptance.ps1 -CleanupOnly -DryRun
```

## Test Commands

### .NET

Restore:

```powershell
dotnet restore Woong.MonitorStack.sln --configfile NuGet.config
```

All standard .NET tests:

```powershell
dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal
```

Build:

```powershell
dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal
```

Coverage:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1
```

Coverage report output:

```text
artifacts/coverage/index.html
artifacts/coverage/SummaryGithub.md
artifacts/coverage/Cobertura.xml
```

### Android

Run from `android/`:

```powershell
.\gradlew.bat testDebugUnitTest --no-daemon --stacktrace
.\gradlew.bat assembleDebug --no-daemon --stacktrace
.\gradlew.bat assembleDebugAndroidTest --no-daemon --stacktrace
.\gradlew.bat connectedDebugAndroidTest --no-daemon --stacktrace
```

### Focused Checks

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-wpf-check-package.ps1
powershell -ExecutionPolicy Bypass -File scripts\run-android-ui-snapshots.ps1
powershell -ExecutionPolicy Bypass -File scripts\run-android-resource-measurement.ps1
powershell -ExecutionPolicy Bypass -File scripts\run-server-postgres-validation.ps1
powershell -ExecutionPolicy Bypass -File scripts\check-external-blockers.ps1
```

## Current MVP Coverage

- Common domain and API DTO contracts.
- Windows foreground collector, idle detection, SQLite repositories, outbox
  sync worker, WPF dashboard state, WPF shell, WPF acceptance/screenshot tools.
- Browser domain tracking through extension/native messaging acceptance and
  privacy-safe domain-only defaults.
- Android UsageStats sessionization, Room storage, WorkManager collection/sync,
  XML dashboard/settings/summary screens, optional location context metadata,
  and emulator screenshot/resource evidence.
- Server device registration, raw/focus/web/location upload endpoints,
  idempotency, PostgreSQL/Testcontainers migration validation, date range
  statistics, persisted daily summaries, and app-family aggregation.

## Working Rules

Agents must follow `AGENTS.md` and `total_todolist.md`:

- TDD red-green-refactor for feature work.
- One behavior at a time.
- Run relevant tests and builds before commit.
- Commit and push successful slices to `origin/main`.
