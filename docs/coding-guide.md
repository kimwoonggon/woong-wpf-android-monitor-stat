# Coding Guide

Updated: 2026-04-29

This guide condenses the current project constraints from `AGENTS.md`,
`docs/prd.md`, `total_todolist.md`, architecture docs, and the installed skills.
Use it as the day-to-day coding guide for Woong Monitor Stack changes.

## Source Of Truth

Read these first when starting a slice:

- `AGENTS.md`: mandatory agent workflow, default skills, privacy boundaries,
  platform rules, and verification commands.
- `docs/prd.md`: product scope and architecture intent.
- `total_todolist.md`: executable checklist and completion state.
- `docs/architecture/reference-rules.md`: project dependency direction.
- `docs/contracts.md`: time/date, upload, idempotency, and DTO contract rules.

If documents conflict, stop, report the conflict, and update the documents
consistently before continuing implementation.

## Mandatory Skill Usage

Use the installed skills as default guidance for relevant work. If the runtime
does not auto-load them, read their `SKILL.md` files directly:

- `tdd`: feature and bug work must follow red-green-refactor.
- `wpf-best-practices`: WPF, XAML, MVVM, DI, and desktop integration work.
- `find-skills`: before adding unfamiliar frameworks or tooling.
- `android-kotlin`: Android Kotlin guidance, filtered through this PRD.
- `android-device-automation`: Android device or emulator visual automation.
- `wpf-mvvm-generator`: WPF MVVM scaffolding when a new MVVM feature is needed.

The PRD wins over generic skill advice. For example, Android MVP is Kotlin with
XML/View UI, not Compose, even when a generic Android skill shows Compose
examples.

## Non-Negotiable Privacy Rules

This product is a visible productivity statistics tool. Do not implement covert
monitoring behavior.

- Measure metadata only: which apps, windows, and sites were used for how long.
- Do not collect global keystroke contents.
- Do not capture passwords, message contents, form input, or typed text.
- Do not capture clipboard contents, browser page contents, screen recordings,
  or periodic user activity screenshots.
- Do not collect Android global touch coordinates from other apps.
- Collection must be visible in UI and based on explicit user permissions.
- Sync must stay opt-in.
- Raw events are for debugging/replay and must follow the retention policy.
- Persist instants in UTC and convert to display timezones at explicit
  presentation or domain boundaries.

## Data Boundary Rules

Keep device-local stores and integrated stores separate:

- Windows SQLite stores Windows-only local data.
- Android Room/SQLite stores Android-only local data.
- ASP.NET Core/PostgreSQL is the only integrated Windows + Android database.
- Windows and Android local databases must never read or write each other.
- Clients sync only through API DTO contracts.
- Use outbox sync for client uploads: collect locally, enqueue outbox, upload,
  then mark synced on success or duplicate-safe acceptance.

## TDD Workflow

Every feature or bug fix uses vertical red-green-refactor slices:

1. Confirm the observable behavior.
2. Add or update the relevant TODO entry.
3. Write one failing behavior test through a public interface.
4. Run the focused test and see it fail for the expected reason.
5. Implement the minimum production change.
6. Run the focused test until it passes.
7. Refactor only while green.
8. Run the broader relevant test set.
9. Run the relevant build.
10. Update docs and `total_todolist.md`.
11. Commit and push the completed slice.

Do not write all tests first and then all implementation. Do not test private
methods, internal call order, or implementation shape unless there is a
documented seam. Do not weaken assertions just to pass.

## Architecture Boundaries

The intended dependency direction is:

```text
Domain
  -> used by Windows, Windows.Presentation, Windows.App, Server

Windows.Presentation
  -> Domain only, with the documented LiveCharts presentation adapter exception

Windows
  -> Domain only

Windows.App
  -> Domain, Windows.Presentation, Windows

Server
  -> Domain only
```

Follow these boundaries:

- `Woong.MonitorStack.Domain` remains OS-neutral and dependency-light. No WPF,
  ASP.NET Core, EF Core, LiveCharts, Windows infrastructure, or platform APIs.
- `Woong.MonitorStack.Windows.Presentation` contains ViewModels, mappers,
  formatters, row models, and chart data. No WPF `Window` or control types,
  `System.Windows`, filesystem, HTTP clients, database providers, user32, or
  server references.
- `Woong.MonitorStack.Windows` contains Windows-specific tracking, Chrome
  native messaging, SQLite local storage, sync, and OS wrappers. No XAML UI,
  no `Windows.App`, no server references.
- `Woong.MonitorStack.Windows.App` is the WPF composition root. Keep
  code-behind thin and use Generic Host plus DI.
- `Woong.MonitorStack.Server` contains ASP.NET Core and PostgreSQL integration.
  It must not depend on Windows projects.

Architecture tests in `tests/Woong.MonitorStack.Architecture.Tests` enforce
these rules. Add tests before adding new dependency exceptions.

## C# And .NET Style

- Use nullable reference types and keep null behavior explicit.
- Prefer file-scoped namespaces.
- Use PascalCase for public types/members and camelCase for locals/parameters.
- Prefer immutable value objects or records for domain facts.
- Keep domain logic dependency-free and deterministic.
- Inject dependencies through constructors; avoid service locators.
- Avoid `.Result`, `.Wait()`, and synchronous dispatcher blocking in WPF paths.
- Do not add NuGet packages without checking existing conventions and, when
  unfamiliar, using skill discovery first.
- Keep generated files, `bin`, `obj`, coverage, and UI snapshot artifacts out
  of commits.

## WPF Rules

For detailed WPF/C# layering and MVVM placement rules, read
`docs/wpf-csharp-coding-guide.md`.

- Use WPF + MVVM with CommunityToolkit.Mvvm by default.
- `App.xaml.cs` owns Generic Host lifecycle and DI registration.
- `MainWindow.xaml.cs` should only initialize the view and assign DataContext.
- Bind UI behavior to ViewModel properties and commands; avoid code-behind
  click logic.
- Keep WPF controls and `System.Windows` out of Presentation.
- Use stable `AutomationProperties.AutomationId` values for UI automation.
- Use LiveCharts2 as the dashboard chart default. The current LiveCharts mapper
  in Presentation is a documented exception; move it to `Windows.App` if it
  becomes WPF-specific.
- Verify meaningful XAML changes with WPF app tests and, when visual layout
  changes, the local UI snapshot script.

## Windows Infrastructure Rules

- Wrap user32, registry, last-input, and foreground-window APIs behind
  interfaces.
- Test deterministic wrappers with fakes. Keep real OS checks in smoke or
  manual validation.
- Foreground tracking must record process/window metadata without collecting
  typed content.
- Chrome Extension + Native Messaging is explicit and user-visible. It may
  collect active tab URL/title/domain, but not passwords, messages, form input,
  or typed text.
- Browser events should flow through raw event storage and then derived
  `web_session` creation.

## Android Rules

- Follow the existing Gradle/version style under `android/`.
- Use Kotlin + XML/View UI with ViewBinding, AppCompat/Activity or Fragment,
  ConstraintLayout, RecyclerView, Room, WorkManager, and UsageStatsManager.
- Do not introduce Compose for MVP unless the PRD is updated.
- Usage collection must use user-granted Usage Access and visible permission
  guidance.
- Use WorkManager for periodic collection/sync. Use a foreground service only
  after an explicit, user-visible requirement is added.
- Run Gradle commands from the directory containing `gradlew.bat`.
- Prefer Robolectric/Room/WorkManager tests for component behavior and
  Espresso/UI Automator for UI or settings-navigation flows.

## Server Rules

- Server is ASP.NET Core Web API with EF Core PostgreSQL.
- Server PostgreSQL is the only integrated database.
- Idempotency is mandatory:
  - `(UserId, Platform, DeviceKey)` for device registration.
  - `(DeviceId, ClientSessionId)` for focus sessions.
  - Duplicate-safe web session key.
  - `(DeviceId, ClientEventId)` for raw events.
  - `(UserId, SummaryDate, TimezoneId)` for daily summaries.
- Use WebApplicationFactory for API tests.
- Do not validate PostgreSQL-specific relational behavior with EF InMemory.
  Prefer PostgreSQL/Testcontainers when Docker is available; current fallback is
  SQLite in-memory relational testing with an explicit reset strategy.
- Treat daily summaries as derived data rebuildable from focus and web
  sessions.

## Testing Guide

Use the project taxonomy:

- Unit tests: pure logic, no real OS, DB, network, or UI.
- Component tests: one component with near-real infrastructure, such as SQLite,
  Room, WorkManager, or ViewModel plus fake repository.
- Integration tests: multiple components together, such as API -> DB or
  outbox -> server upload.
- UI tests: actual UI interaction, WPF smoke/FlaUI snapshots, Espresso, or UI
  Automator.

Prefer fake clocks, fake data sources, and deterministic inputs. Avoid brittle
real OS/filesystem/network work unless the test is explicitly component,
integration, smoke, or manual validation.

## Verification Commands

Use the current .NET commands unless the project structure changes:

```powershell
dotnet restore Woong.MonitorStack.sln
dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal
dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1 -v minimal
```

`-maxcpucount:1` is intentional for now because this environment previously had
solution-level parallel-build issues.

Coverage:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1
```

WPF local UI snapshots:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-ui-snapshots.ps1
```

Android:

```powershell
cd android
.\gradlew.bat testDebugUnitTest --no-daemon --stacktrace
.\gradlew.bat assembleDebug --no-daemon --stacktrace
.\gradlew.bat assembleDebugAndroidTest --no-daemon --stacktrace
.\gradlew.bat connectedDebugAndroidTest --no-daemon --stacktrace
```

Run connected Android tests only when an emulator or physical device is
available. The only currently documented external blocker is repeating Android
resource measurements on a physical device.

## Documentation And TODO Rules

- Update docs in the same slice as code when behavior, architecture, contracts,
  commands, or validation changes.
- Keep `total_todolist.md` current before and after work.
- A TODO is complete only when relevant tests pass, build succeeds, docs are
  updated, privacy boundaries are respected, and the slice is committed and
  pushed.
- Update `docs/resume-state.md` after pushes so a context reset can resume
  without rediscovery.
- Keep local artifacts out of Git: coverage reports, TestResults, UI snapshots,
  Android build output, and .NET `bin/obj`.

## Commit And Push Discipline

Before committing:

- Check `git status --short`.
- Review the diff and ensure no generated artifacts are staged.
- Run the focused tests, then the relevant solution/platform build.
- For WPF UI or visual changes, run WPF app tests and UI snapshot automation.
- For Android UI/usage changes, run JVM tests, assemble, and instrumentation
  compile or connected tests when available.

Commit after each completed vertical slice and push to `origin/main` when
credentials/network are available. If push fails due to auth or network, leave
the local commit and write a concise resume summary.

## Stop Conditions

Stop and report before proceeding when:

- `docs/prd.md`, `AGENTS.md`, `total_todolist.md`, or architecture docs
  conflict.
- A requested feature would violate privacy boundaries.
- A new dependency would break the documented architecture direction.
- A required external device/service is unavailable and no faithful local
  substitute exists.
- Tests fail for reasons that cannot be safely fixed in the current slice.
