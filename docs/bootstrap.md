# Repository Bootstrap

Updated: 2026-04-28

## Current Structure

```text
D:\woong-monitor-stack
  Woong.MonitorStack.sln
  NuGet.config
  docs/
    prd.md
    bootstrap.md
  src/
    Woong.MonitorStack.Domain/
      Common/
        FocusSession.cs
        LocalDateCalculator.cs
        TimeBucket.cs
        TimeBucketAggregator.cs
        TimeRange.cs
  tests/
    Woong.MonitorStack.Domain.Tests/
      Common/
        FocusSessionTests.cs
        LocalDateCalculatorTests.cs
        TimeBucketAggregatorTests.cs
        TimeRangeTests.cs
```

## Project Decisions

* The repo was empty at bootstrap time; no existing Windows, Android, Server, Gradle, or test structure was present.
* The first implementation slice is a pure .NET domain library: `Woong.MonitorStack.Domain`.
* The first test project is `Woong.MonitorStack.Domain.Tests` with xUnit.
* The installed .NET 10 SDK template did not allow `net8.0`; projects currently target `net10.0`.
* `NuGet.config` is pinned to `nuget.org` so restore does not depend on user-level NuGet configuration.

## Verified Commands

Use a workspace-local CLI/cache location in this sandbox:

```powershell
$env:DOTNET_CLI_HOME='D:\woong-monitor-stack\.dotnet'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:NUGET_PACKAGES='D:\woong-monitor-stack\.nuget\packages'
$env:APPDATA='D:\woong-monitor-stack\.appdata'
```

Restore:

```powershell
dotnet restore tests\Woong.MonitorStack.Domain.Tests\Woong.MonitorStack.Domain.Tests.csproj --configfile NuGet.config
```

Test:

```powershell
dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal
```

Build:

```powershell
dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal
```

`-maxcpucount:1` is currently intentional. On this Windows sandbox with .NET SDK `10.0.201`, solution-level parallel build returned exit code `1` without emitted errors, while project-level builds and single-node solution builds passed.

## Verified Result

* `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
  * Passed: 8
  * Failed: 0
* `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
  * Warnings: 0
  * Errors: 0
