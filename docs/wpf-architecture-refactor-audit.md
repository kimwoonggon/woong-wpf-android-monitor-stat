# WPF Architecture Refactor Audit

Date: 2026-05-03
Role: WPF architecture/test audit subagent

## Scope

Reviewed `src/Woong.MonitorStack.Windows*`, `tests/Woong.MonitorStack.Windows*`, `tests/Woong.MonitorStack.Windows.Presentation.Tests`, `tests/Woong.MonitorStack.Windows.App.Tests`, `docs/architecture/reference-rules.md`, `docs/resume-state.md`, and `total_todolist.md`.

Read first: `AGENTS.md`, `docs/prd.md`, `total_todolist.md`. Also loaded the mandatory WPF/TDD guidance used for this audit: `C:\Users\gerard\.codex\skills\tdd\SKILL.md`, `C:\Users\gerard\.codex\skills\wpf-best-practices\SKILL.md`, `C:\Users\gerard\.codex\skills\wpf-mvvm-generator\SKILL.md`, and `C:\Users\gerard\.codex\skills\find-skills\SKILL.md`.

## Summary

The recent lifecycle/DI refactor is directionally good: tracking lifecycle orchestration now lives mostly in `MainWindowLifecycleCoordinator`, the WPF app has behavior tests for Start/Stop/Poll persistence, and solution verification is green. The remaining high-priority gaps are not broad rewrites; they are a few sharp TDD slices around sync execution, SQLite/outbox atomicity, architecture guards, and brittle WPF tests.

Privacy posture is still mostly sound in the reviewed WPF/Windows paths: persisted focus sessions clear window titles, web persistence stores domain-only by default, and dashboard UI tests assert sensitive URL/title text is not shown. The main risks are reliability and architectural drift rather than covert data collection.

## High-Priority Findings

### P1: WPF Sync Now is still a status stub, not an executable sync path

Evidence:

- `src/Woong.MonitorStack.Windows.App/Dashboard/WindowsTrackingDashboardCoordinator.cs:156` returns `"Sync requested..."` when sync is enabled, but does not invoke `WindowsSyncWorker`.
- `src/Woong.MonitorStack.Windows.App/WindowsAppServiceCollectionExtensions.cs:147` through `:159` registers `WindowsTrackingDashboardCoordinator`, repositories, and dashboard adapters, but no `WindowsSyncWorker`, `IWindowsSyncApiClient`, or checkpoint store for the WPF composition path.
- `src/Woong.MonitorStack.Windows.Presentation/Dashboard/DashboardViewModel.cs:402` through `:415` exposes Sync Now and updates the status label from the coordinator result, so the UI can imply sync happened when rows are only still pending.
- `tests/Woong.MonitorStack.Windows.App.Tests/MainWindowTrackingPipelineTests.cs:810` through `:875` covers the sync-off path only. `tests/Woong.MonitorStack.Windows.Tests/Sync/WindowsSyncWorkerTests.cs:10` through `:109` covers the worker in isolation, not the WPF button or DI path.

Risk: after local rows are queued, a user with sync enabled can press Sync Now and see a requested status without any outbox rows being uploaded, marked synced, or marked failed.

Next TDD slice:

1. Add a failing WPF app/composition behavior test with sync enabled, a fake `IWindowsSyncApiClient`, pending focus/web outbox rows, and a Sync Now command/button invocation. Assert rows become `Synced`, checkpoint/status text is updated, and no URL/title/window-title content leaks.
2. Add the failure variant where upload returns an error or throws; assert rows become `Failed` with retry metadata and the UI reports failure.
3. Introduce a small app-facing sync port or inject `WindowsSyncWorker` into `WindowsTrackingDashboardCoordinator`, then register the sync client/checkpoint implementation in `WindowsAppServiceCollectionExtensions`.

### P1: SQLite session persistence and outbox enqueue are not atomic

Evidence:

- `src/Woong.MonitorStack.Windows/Storage/WindowsFocusSessionPersistenceService.cs:30` through `:38` saves the focus session and then separately adds the outbox row.
- `src/Woong.MonitorStack.Windows/Storage/WindowsWebSessionPersistenceService.cs:36` through `:45` does the same for web sessions.
- The repository calls open their own connections, so a process crash or exception between the local insert and outbox insert can leave a local session that will never sync.
- `src/Woong.MonitorStack.Windows.Presentation/Dashboard/DashboardViewModel.cs:651` through `:668` always logs "Outbox row created" whenever the snapshot says a session/web session was persisted, but the persistence result does not distinguish inserted, duplicate, or failed outbox writes.

Risk: this breaks the outbox reliability contract. It can also make the dashboard runtime log overstate what actually happened.

Next TDD slice:

1. Red: add behavior tests for focus and web persistence where outbox insertion fails after the session insert attempt. Assert either both writes roll back or the result explicitly reports partial failure.
2. Green: introduce a SQLite unit-of-work/repository method that writes session plus outbox row in one transaction.
3. Refactor persistence results to include inserted/duplicate/outbox status, then update dashboard runtime events to log only observed outcomes.

### P1: Sync worker does not handle thrown upload failures

Evidence:

- `src/Woong.MonitorStack.Windows/Sync/WindowsSyncWorker.cs:39` through `:52` awaits `_apiClient.UploadAsync` and marks failed only when a result is returned. A thrown network/client exception exits the method before `MarkFailed`.
- `src/Woong.MonitorStack.Windows/Sync/WindowsSyncWorker.cs:59` through `:60` retries every pending or failed item with no backoff/attempt policy.
- Existing sync tests cover accepted, returned error, duplicate, and checkpoint behavior at `tests/Woong.MonitorStack.Windows.Tests/Sync/WindowsSyncWorkerTests.cs:10` through `:109`; they do not cover thrown exceptions, partial failure across multiple rows, cancellation, or retry policy.

Risk: transient network exceptions can leave the same row `Pending` forever with no retry count/error trail, making sync failures harder to diagnose.

Next TDD slice:

1. Red: add `ProcessPendingAsync_WhenApiThrows_MarksItemFailedAndContinuesWithNextItem`.
2. Red: add cancellation behavior separately so cooperative cancellation does not get converted into a failed row.
3. Green: catch non-cancellation exceptions per item, call `MarkFailed`, continue processing, and add a minimal retry/backoff policy aligned with the PRD.

### P2: Architecture rules document a stricter boundary than tests enforce

Evidence:

- `docs/architecture/reference-rules.md:12` through `:17` says `Windows.Presentation -> Domain only` and `Windows -> Domain only`.
- `docs/architecture/reference-rules.md:86` through `:106` says Windows infrastructure should avoid referencing `Woong.MonitorStack.Windows.Presentation`.
- `tests/Woong.MonitorStack.Architecture.Tests/ProjectReferenceRulesTests.cs:50` through `:54` forbids Windows infrastructure from referencing App and Server, but not Presentation.
- `tests/Woong.MonitorStack.Architecture.Tests/ProjectReferenceRulesTests.cs:123` through `:136` also omits `Woong.MonitorStack.Windows.Presentation` from the dependency guard.

Risk: a future infrastructure-to-ViewModel reference would pass architecture tests while violating the documented direction.

Next TDD slice:

1. Red: add `Woong.MonitorStack.Windows.Presentation` to the forbidden project/reference list for `Woong.MonitorStack.Windows`.
2. Red: add a dependency-level assertion that Windows infrastructure does not depend on Presentation namespaces.
3. Green: keep current production references unchanged, then run the architecture suite.

### P2: Coordinator StartTracking is not guarded against duplicate starts

Evidence:

- `src/Woong.MonitorStack.Windows.App/Dashboard/WindowsTrackingDashboardCoordinator.cs:78` through `:96` always creates a new `TrackingPoller` and sets `_isRunning = true`.
- `src/Woong.MonitorStack.Windows.App/Dashboard/WindowsTrackingDashboardCoordinator.cs:99` through `:128` has an explicit not-running guard for Stop, and `:130` through `:154` has one for Poll.
- `tests/Woong.MonitorStack.Windows.App.Tests/WindowsTrackingDashboardCoordinatorTests.cs:497` through `:530` covers start after stop, but there is no direct public behavior test for start while already running.

Risk: the ViewModel normally prevents a second start through command `CanExecute`, but the coordinator is a public state machine. A second call can replace the active poller and lose the open session boundary.

Next TDD slice:

1. Red: add a coordinator test for `StartTracking` while already running. Decide the expected behavior: no-op current snapshot, or persist/close the prior session before restart.
2. Green: implement the explicit guard in the coordinator, not only in the ViewModel command layer.

### P2: Web session duplicate protection is not enforced by SQLite schema

Evidence:

- `src/Woong.MonitorStack.Windows/Storage/SqliteWebSessionRepository.cs:26` through `:43` creates the table and a non-unique focus-session index only.
- `src/Woong.MonitorStack.Windows/Storage/SqliteWebSessionRepository.cs:57` through `:88` uses `INSERT ... WHERE NOT EXISTS` for duplicate avoidance, but there is no unique index on `(focus_session_id, started_at_utc)`.
- `tests/Woong.MonitorStack.Windows.Tests/Storage/WindowsWebSessionPersistenceServiceTests.cs:76` through `:116` covers sequential duplicate saves, not competing repository instances or concurrent connections.

Risk: duplicate web rows can still appear under races or multiple process/connection paths, and the app currently relies on query-time checks rather than a relational invariant.

Next TDD slice:

1. Red: add a repository test that uses two repository instances/connections to attempt the same focus/start tuple.
2. Green: add a unique SQLite index on `(focus_session_id, started_at_utc)` and keep `INSERT OR IGNORE`/idempotent service behavior.

## Test Quality And MVVM Boundary Gaps

- `tests/Woong.MonitorStack.Windows.App.Tests/WpfTestHelpers.cs:41` through `:70` has the safer shared STA helper with `DispatcherSynchronizationContext` and preserved exception dispatch, but `tests/Woong.MonitorStack.Windows.App.Tests/MainWindowTrackingPipelineTests.cs:1126` through `:1149` and `tests/Woong.MonitorStack.Windows.App.Tests/WindowsAppCompositionTests.cs:354` through `:377` still carry local raw STA helpers. Consolidate them to avoid lost stack traces and dispatcher-context-only failures.
- `tests/Woong.MonitorStack.Windows.App.Tests/MainWindowUiExpectationTests.cs:46` through `:61` asserts exact button text with mojibake-like prefixes such as `"??Refresh"`, and `:147` through `:151` asserts mojibake chart labels. These tests are brittle and can preserve encoding corruption as the contract.
- `tests/Woong.MonitorStack.Windows.App.Tests/MainWindowUiExpectationTestHelpers.cs:109` through `:200` asserts many exact RGB/font setter values. Keep a few resource-token/automation contract checks, but move pixel-level style confidence to snapshots or narrower visual smoke tests.
- `src/Woong.MonitorStack.Windows.App/MainWindow.xaml.cs:15` through `:45` still has convenience constructors that directly construct `DispatcherTrackingTicker` and a noop tray lifecycle. The primary DI constructor is acceptable, but architecture tests only guard `App.xaml.cs` at `tests/Woong.MonitorStack.Architecture.Tests/ProjectReferenceRulesTests.cs:210` through `:223`.
- `src/Woong.MonitorStack.Windows.App/Views/ChartDetailsWindow.xaml.cs:8` through `:14` creates its ViewModel in code-behind, and `src/Woong.MonitorStack.Windows.App/Views/ChartDetailsWindowViewModel.cs:48` through `:52` reads `DateTimeOffset.UtcNow` directly. This is not a severe violation, but it is a good cleanup slice: move detail ViewModel/range logic toward Presentation or inject a clock/factory.

## Suggested Next TDD Order

1. WPF Sync Now enabled path: button/command -> worker -> outbox row synced/failed -> status text.
2. Sync worker thrown exception handling and cancellation behavior.
3. Atomic focus/web session plus outbox transaction.
4. Architecture guard: Windows infrastructure must not reference Presentation.
5. Coordinator duplicate-start behavior.
6. SQLite unique index for web session idempotency across connections.
7. WPF test harness consolidation and brittle UI text/style cleanup.
8. Chart details ViewModel clock/factory cleanup.

## Commands Run

All verification commands exited `0`.

- `dotnet restore tests\Woong.MonitorStack.Domain.Tests\Woong.MonitorStack.Domain.Tests.csproj --configfile NuGet.config`: all projects up to date.
- `dotnet test tests\Woong.MonitorStack.Windows.Tests\Woong.MonitorStack.Windows.Tests.csproj --no-restore -maxcpucount:1 -v minimal`: 95 passed.
- `dotnet test tests\Woong.MonitorStack.Windows.Presentation.Tests\Woong.MonitorStack.Windows.Presentation.Tests.csproj --no-restore -maxcpucount:1 -v minimal`: 112 passed.
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`: 207 passed.
- `dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~ProjectReferenceRules" -maxcpucount:1 -v minimal`: 22 passed.
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`: 691 passed, 6 skipped, 0 failed.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`: 0 warnings, 0 errors.

Note: `rg.exe` returned "Access is denied" in this environment, so code search used PowerShell `Get-ChildItem`, `Get-Content`, and `Select-String`.

## Changed Paths

- `docs/wpf-architecture-refactor-audit.md`

No other files were intentionally edited. No commit or push was performed.
