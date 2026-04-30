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

You do not need to start SQLite. The WPF app uses a local SQLite file and
creates or updates it automatically:

```text
%LOCALAPPDATA%\WoongMonitorStack\windows-local.db
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

By default the WPF app uses:

```text
%LOCALAPPDATA%\WoongMonitorStack\windows-local.db
```

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

## Android App

The Android MVP is Kotlin + XML/View based. Do not migrate it to Compose for
MVP work.

### Start An Emulator

From PowerShell:

```powershell
$env:ANDROID_HOME="$env:LOCALAPPDATA\Android\Sdk"
$env:ANDROID_SDK_ROOT="$env:LOCALAPPDATA\Android\Sdk"

& "$env:ANDROID_HOME\emulator\emulator.exe" -list-avds
& "$env:ANDROID_HOME\emulator\emulator.exe" -avd Medium_Phone
```

In a second terminal, wait for the device:

```powershell
& "$env:ANDROID_HOME\platform-tools\adb.exe" devices -l
& "$env:ANDROID_HOME\platform-tools\adb.exe" wait-for-device
```

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

### Local PostgreSQL Connection

The server reads `ConnectionStrings:MonitorDb`. The default fallback is:

```text
Host=localhost;Database=woong_monitor;Username=postgres;Password=postgres
```

To set it explicitly in PowerShell:

```powershell
$env:ConnectionStrings__MonitorDb='Host=localhost;Database=woong_monitor;Username=postgres;Password=postgres'
```

Apply EF Core migrations to a local PostgreSQL database:

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

Development OpenAPI is exposed by ASP.NET Core when the environment is
Development.

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
