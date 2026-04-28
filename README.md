# Woong Monitor Stack

Windows + Android + Server usage measurement system for local productivity
statistics and integrated daily summaries.

This project follows the PRD in `docs/prd.md`. It is a visible productivity
tool, not covert monitoring software.

## Privacy Boundaries

- Do not collect global keystroke contents.
- Do not collect passwords, messages, or form input.
- Do not collect Android global touch coordinates.
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
android/
  app/                                       Kotlin XML/View Android app
docs/
  prd.md
  contracts.md
  hardening.md
  performance-checks.md
  release-checklist.md
```

## Windows And Server Verification

Use workspace-local .NET/NuGet locations on this machine:

```powershell
$env:DOTNET_CLI_HOME='D:\woong-monitor-stack\.dotnet'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:NUGET_PACKAGES='D:\woong-monitor-stack\.nuget\packages'
$env:APPDATA='D:\woong-monitor-stack\.appdata'
```

Restore, test, and build:

```powershell
dotnet restore tests\Woong.MonitorStack.Domain.Tests\Woong.MonitorStack.Domain.Tests.csproj --configfile NuGet.config
dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal
dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal
```

`-maxcpucount:1` is intentional for now because this Windows environment has
shown solution-level parallel build instability with .NET SDK 10.0.201.

Run the Windows collector smoke:

```powershell
dotnet run --project tools\Woong.MonitorStack.Windows.Smoke\Woong.MonitorStack.Windows.Smoke.csproj --no-build
```

## Android Verification

Use Gradle Wrapper from the `android` directory:

```powershell
$env:ANDROID_HOME='C:\Users\gerard\AppData\Local\Android\Sdk'
$env:ANDROID_SDK_ROOT='C:\Users\gerard\AppData\Local\Android\Sdk'
$env:GRADLE_USER_HOME='D:\woong-monitor-stack\.gradle-user'

cd D:\woong-monitor-stack\android
.\gradlew.bat testDebugUnitTest --no-daemon --stacktrace
.\gradlew.bat assembleDebug --no-daemon --stacktrace
.\gradlew.bat assembleDebugAndroidTest --no-daemon --stacktrace
```

With an emulator or device connected:

```powershell
.\gradlew.bat connectedDebugAndroidTest --no-daemon --stacktrace
```

## Current MVP Coverage

- Common domain and API DTO contracts.
- Windows foreground collector, idle detection, SQLite repositories, outbox
  sync worker, WPF dashboard state, and WPF shell.
- Android UsageStats sessionization, Room storage, WorkManager collection/sync,
  XML dashboard/settings/summary screens, and morning summary worker.
- Server device registration, raw/focus/web upload endpoints, idempotency, date
  range statistics, persisted daily summaries, and app-family aggregation.
- Hardening docs for migration policy, raw-event retention, sync failure UI,
  permission guidance, and resource smoke checks.

## Working Rules

Agents must follow `AGENTS.md` and `total_todolist.md`:

- TDD red-green-refactor for feature work.
- One behavior at a time.
- Run relevant tests and builds before commit.
- Commit and push successful slices to `origin/main`.
