# Woong Monitor Stack Agent Rules

This repository builds a Windows + Android + Server usage measurement system.
All agents must follow the PRD in `docs/prd.md` and keep privacy boundaries strict.

## Default Skills And Priority

These installed skills are mandatory default guidance for every relevant task.
If they are not auto-loaded in the current session, read their local `SKILL.md`
files before working. Do not skip them silently:

- `C:\Users\gerard\.codex\skills\tdd`
- `C:\Users\gerard\.codex\skills\wpf-best-practices`
- `C:\Users\gerard\.codex\skills\find-skills`
- `C:\Users\gerard\.codex\skills\android-kotlin`
- `C:\Users\gerard\.codex\skills\android-device-automation`
- `C:\Users\gerard\.codex\skills\wpf-mvvm-generator`

The PRD wins over generic skill guidance. In particular, Android MVP is
Kotlin + XML/View based; do not switch it to Compose because a generic Android
skill suggests Compose.

## Non-Negotiable Product Rules

- This is a productivity statistics tool, not a covert monitoring tool.
- Never collect global keystroke contents, passwords, messages, form input, or
  Android global touch coordinates.
- Collection must be visible to the user, permissions must be explicit, and
  sync must be opt-in.
- Store all persisted instants in UTC. Convert to the display timezone only at
  presentation boundaries.
- Windows SQLite and Android Room store only local device data.
- Only the ASP.NET Core server/PostgreSQL database integrates Windows + Android
  data.
- Local databases never read or write each other directly; clients sync through
  API DTO contracts.

## TDD Rules

- Use red-green-refactor for every feature or bug fix.
- Work in vertical slices: one failing behavior test, minimal implementation,
  then the next behavior.
- Tests must verify observable behavior through public interfaces, not private
  implementation details or internal call order.
- Do not loosen tests to force a pass. If a test is wrong, compare it against
  the PRD before changing it.
- Do not move to build/run verification until relevant tests pass.

## Always-On Work Loop

- Keep `total_todolist.md` as the source-of-truth checklist for the whole PRD.
- Before starting a feature, add or update the relevant TODO entries.
- Finish the TODO slice you start before moving to the next slice.
- Run relevant tests, then run the relevant build command.
- If tests/build pass and the feature is healthy, commit the finished slice.
- Push successful commits to `origin`.
- After push, write a concise state summary so a future context reset can resume
  without rediscovery.
- Continue with the next highest-priority TODO until the full checklist is done.
- Do not skip privacy, test, build, or documentation TODOs to move faster.

## Platform Rules

- Windows: C# WPF, MVVM, CommunityToolkit.Mvvm preferred, SQLite local storage,
  LiveCharts2 first for charts, xUnit for tests, STA/FlaUI only where UI tests
  need them.
- Android: Kotlin, XML/View UI, AppCompatActivity or Fragment, ConstraintLayout,
  RecyclerView, ViewBinding, Room, WorkManager, UsageStatsManager, Espresso/UI
  Automator for UI tests.
- Server: ASP.NET Core Web API, EF Core PostgreSQL, WebApplicationFactory
  integration tests, BackgroundService for the initial daily summary job.
- Before introducing unfamiliar frameworks or tools, use `npx skills find ...`
  and verify the result against the PRD.
- Chrome Extension + Native Messaging is a dedicated Windows web-tracking
  milestone. Extension work must stay explicit/user-visible and must not collect
  passwords, message contents, form inputs, or typed text.
- Server integration tests that validate PostgreSQL-specific indexes,
  relational constraints, or idempotency must use a relational provider. Prefer
  PostgreSQL/Testcontainers with an explicit reset strategy; do not treat EF
  InMemory as proof of PostgreSQL relational behavior.

## Agent Ownership

- Main agent owns integration, final decisions, and conflict resolution.
- Architecture Agent owns common domain models, DTO contracts, DB/API contract
  docs, sync protocol docs, and milestone docs.
- Windows Agent owns future `src/Woong.MonitorStack.Windows*` projects and
  matching Windows tests.
- Windows Agent also owns future `extensions/chrome/` and native-messaging host
  receiver work unless a narrower owner is documented.
- Android Agent owns future `android/` projects and Android tests.
- Server Agent owns future `src/Woong.MonitorStack.Server*` projects and
  matching server tests.
- QA/Test Agent owns test matrix, validation commands, smoke checklists, and
  failure analysis.

Workers are not alone in the codebase. They must not revert or overwrite edits
made by others, and must report changed paths plus verification commands.

## Current Verification Commands

Use these commands until project structure changes:

```powershell
dotnet restore tests\Woong.MonitorStack.Domain.Tests\Woong.MonitorStack.Domain.Tests.csproj --configfile NuGet.config
dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal
dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal
```

`-maxcpucount:1` is intentional for now. On this Windows environment with
.NET SDK 10.0.201, solution-level parallel build previously returned exit code
1 without emitted errors.
