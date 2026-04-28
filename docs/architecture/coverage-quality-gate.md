# Coverage Quality Gate

Coverage is a quality signal, not a target to game. Tests should describe
observable behavior through public interfaces. Do not add meaningless tests only
to increase a percentage.

## Commands

Run coverage and generate a report with:

```powershell
.\scripts\test-coverage.ps1
```

Equivalent direct commands:

```powershell
dotnet test Woong.MonitorStack.sln --collect:"XPlat Code Coverage" --settings coverage.runsettings --results-directory artifacts/TestResults
dotnet tool restore
dotnet tool run reportgenerator -reports:"artifacts/TestResults/**/coverage.cobertura.xml" -targetdir:"artifacts/coverage" -reporttypes:"Html;MarkdownSummaryGithub;Cobertura"
```

The report is written to:

- `artifacts/coverage/index.html`
- `artifacts/coverage/SummaryGithub.md`
- `artifacts/coverage/Cobertura.xml`

These folders are intentionally ignored by Git.

## Initial Expectations

- Domain should stay high because it is pure logic. Initial target: at least
  80% line coverage.
- Windows.Presentation should stay high because ViewModels/mappers are
  deterministic. Initial target: at least 75% line coverage.
- Windows.App is a WPF composition root. Keep smoke/build coverage only; do not
  force high WPF UI coverage.
- Windows infrastructure should test deterministic wrappers, repositories,
  sync logic, and mappers. Avoid brittle real-OS tests for user32, registry, or
  last-input APIs.
- Server should cover endpoint contracts, validation, idempotency, relational
  persistence behavior, and summary aggregation.

Strict thresholds are not enabled yet. Raise them gradually once CI runtime and
flaky-test behavior are stable.

## Current Snapshot

Collected on 2026-04-29 with `coverage.runsettings` and ReportGenerator:

| Assembly | Line coverage | Notes |
| --- | ---: | --- |
| Overall | 92.1% | 2,017 / 2,189 coverable lines |
| Woong.MonitorStack.Domain | 88.7% | Above initial 80% target |
| Woong.MonitorStack.Windows.Presentation | 97.6% | Above initial 75% target |
| Woong.MonitorStack.Windows | 91.2% | OS wrappers intentionally remain low |
| Woong.MonitorStack.Windows.App | 85.0% | Increased by app-hosted tracking coordinator, SQLite dashboard tests, and env-safe RealStart options |
| Woong.MonitorStack.Server | 96.0% | EF generated migrations excluded |

## Known Gaps

- `Woong.MonitorStack.Windows.App.App` is not unit-tested directly because WPF
  `Application` lifecycle tests are brittle. The DI registration and
  `MainWindow` construction path are covered separately.
- `EmptyDashboardDataSource` has no behavior beyond returning empty collections.
  It is a temporary adapter registered through DI.
- `WindowsRegistryWriter`, `WindowsForegroundWindowReader`, and
  `WindowsLastInputReader` are real Windows API wrappers. They should remain
  covered by smoke/manual checks or focused integration tests, not ordinary unit
  tests.
- `SystemClock` and `SystemDashboardClock` are thin time adapters. Core
  time-dependent behavior is tested with fake clocks.
- Domain classes with lower coverage (`Device`, `DeviceStateSession`,
  `LocalDateCalculator`, `TimeBucket`) should receive behavior tests when their
  rules grow. Do not add shallow constructor-only tests just to move the number.
