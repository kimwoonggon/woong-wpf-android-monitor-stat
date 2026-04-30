# QA Coordination Gap Snapshot

Updated: 2026-04-30

Scope: read-only audit of `docs/prd.md`, platform checklists, latest artifacts,
and `docs/resume-state.md`. This file is QA-owned coordination guidance and
does not replace `total_todolist.md`.

## Latest Evidence Observed

- Android UI snapshots: `artifacts/android-ui-snapshots/20260430-174439`
  (`latest/report.md` status PASS).
- Android resource measurements:
  `artifacts/android-resource-measurements/20260430-174552`
  (`latest/manifest.json` status PASS).
- WPF UI acceptance: `artifacts/wpf-ui-acceptance/20260430-170819`
  (`latest/report.md` status PASS).
- Chrome native acceptance:
  `artifacts/chrome-native-acceptance/20260430-172235`
  (`latest/report.md` status PASS, deterministic allowed origin recorded).
- Coverage artifacts were refreshed at `artifacts/coverage/` during the latest
  server verification window.

## PRD-To-Checklist Gaps

1. Server mixed-batch upload behavior is covered for focus, web, raw-event, and
   location contexts. HTTP invalid input/date-range behavior and date-range
   local-midnight split allocation are now covered in the active main-agent
   flow.
2. Remaining server gaps are mostly externally blocked PostgreSQL/Testcontainers
   checks plus optional raw-event privacy guard work if raw events expand beyond
   metadata/debug diagnostics.
3. PostgreSQL/Testcontainers validation remains externally blocked. Do not ask
   workers to close concurrency idempotency, migration application, or legacy
   web-session backfill verification without a real PostgreSQL fixture.
4. Android physical-device resource measurement remains externally blocked.
   Emulator resource evidence is PASS, but PRD-grade physical-device evidence
   still needs an attached device.
5. Android checklist has current UI screenshot evidence but does not yet call
   out the latest resource-measurement artifact `20260430-174552`. Treat this
   as a documentation breadcrumb gap, not a product/test failure.
6. WPF checklist is aligned with the broad same-window browser, Chrome native,
   privacy, and acceptance artifacts. Commit `ddfc617` adds a narrower
   origin-only UI Automation fallback behavior, but `docs/resume-state.md` does
   not yet include a verification breadcrumb for that commit.

## Latest Commit Audit

- `ddfc617 Limit browser fallback URL metadata`: implementation and focused
  test changed WPF UI Automation address-bar fallback from full URL persistence
  to origin-only URL metadata. Checklist rows W29/W31 cover the general privacy
  surface, but resume-state does not record the focused verification for this
  slice. Recommended main-agent action: add the verification breadcrumb when
  finalizing integration.
- `43d3d0b Handle web upload mixed-batch duplicates`: resume-state and
  `server_check_todo.md` are current. Web mixed-batch is closed with focused
  server test, full solution test/build, and coverage evidence.
- raw-event mixed-batch work: resume-state and `server_check_todo.md` are
  current. Raw-event mixed-batch closed with focused server tests, full solution
  test/build, and coverage evidence before the next server location slice.
- `6f6239a Handle location upload mixed-batch duplicates`: resume-state and
  `server_check_todo.md` are current. Location unknown-device and mixed-batch
  coverage closed with focused server tests, full solution test/build, and
  coverage evidence.
- date-range invalid input work: resume-state and `server_check_todo.md` are
  current in the active main-agent flow. Malformed dates, missing query values,
  invalid timezone ids, and `from > to` now return controlled HTTP 400
  responses before commit.
- date-range local-midnight split work: `server_check_todo.md` is current in
  the active main-agent flow. Range statistics now include only the portion of
  focus active, focus idle, and web sessions that falls inside the requested
  local date range before commit.
- `eb3cfdf Add QA coordination gap snapshot`: superseded by this refresh for
  server delegation order after web mixed-batch completion.

## Recommended Delegation

- Server agent: next independent slice should be raw-event payload privacy guard
  coverage only if raw events expand beyond metadata/debug diagnostics.
- Server agent after that: PostgreSQL/Testcontainers items remain blocked until
  a real fixture is available.
- Android agent: only queue documentation/evidence refresh for the latest
  resource-measurement artifact or physical-device measurement when a device is
  available. Avoid Android feature work unless main agent explicitly opens it.
- WPF agent: safe queued task is docs/evidence review of WPF acceptance reports
  or a resume-state verification note for `ddfc617`. Avoid Chrome acceptance
  script/tests and browser-domain implementation while main integration is
  active there.
- Main integration: own cross-slice validation, `total_todolist.md`, final
  resume-state summary, and conflict resolution for server checklist edits
  already in progress.

## Conflict Watch

- Current worktree audit after `6f6239a` shows active tracked edits in
  `tests/Woong.MonitorStack.Server.Tests/Summaries/DateRangeStatisticsApiTests.cs`
  and `src/Woong.MonitorStack.Server/Program.cs`, plus untracked visual/export
  artifacts. Do not assign another worker to date-range summary tests until
  those edits land; QA agents should still avoid `total_todolist.md`.
- Android and WPF checklist edits are low-conflict but should be batched
  sparingly; prefer this QA snapshot for coordination notes.
- Untracked visual/export artifacts are present and should not be removed by QA.

## Validation Commands Before Final Completion

```powershell
dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal
dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal
powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1
```

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1
powershell -ExecutionPolicy Bypass -File scripts\run-chrome-native-message-acceptance.ps1 -DryRun
```

Run from `android/`:

```powershell
.\gradlew.bat testDebugUnitTest assembleDebug assembleDebugAndroidTest --no-daemon --stacktrace
```

Run from repository root with an emulator/device connected:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-android-ui-snapshots.ps1
powershell -ExecutionPolicy Bypass -File scripts\run-android-resource-measurement.ps1
```
