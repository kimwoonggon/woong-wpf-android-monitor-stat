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

1. Server mixed-batch upload behavior is the highest-value independent queue.
   `server_check_todo.md` still lists open web, raw-event, and location
   mixed-batch slices. Focus mixed-batch is already recorded as covered in the
   active server checklist/resume flow.
2. Server invalid input/date-range edges remain actionable. Open slices:
   malformed dates, missing query values, invalid timezone ids, `from > to`,
   and date-range local-midnight split allocation.
3. PostgreSQL/Testcontainers validation remains externally blocked. Do not ask
   workers to close concurrency idempotency, migration application, or legacy
   web-session backfill verification without a real PostgreSQL fixture.
4. Android physical-device resource measurement remains externally blocked.
   Emulator resource evidence is PASS, but PRD-grade physical-device evidence
   still needs an attached device.
5. Android checklist has current UI screenshot evidence but does not yet call
   out the latest resource-measurement artifact `20260430-174552`. Treat this
   as a documentation breadcrumb gap, not a product/test failure.
6. WPF checklist is aligned with the latest same-window browser, Chrome native,
   privacy, and acceptance artifacts. No new WPF implementation slice should be
   queued until the current main-agent/browser-domain work lands.

## Recommended Delegation

- Server agent: next independent slice should be mixed-batch web upload
  coverage: existing duplicate, accepted new row, and missing focus parent in
  one request. It does not overlap Android/WPF files.
- Server agent after that: mixed-batch raw event upload, then unregistered
  location upload, then mixed-batch location upload.
- Android agent: only queue documentation/evidence refresh for the latest
  resource-measurement artifact or physical-device measurement when a device is
  available. Avoid Android feature work unless main agent explicitly opens it.
- WPF agent: safe queued task is docs/evidence review of WPF acceptance reports
  only. Avoid Chrome acceptance script/tests and browser-domain implementation
  while main integration is active there.
- Main integration: own cross-slice validation, `total_todolist.md`, final
  resume-state summary, and conflict resolution for server checklist edits
  already in progress.

## Conflict Watch

- Active dirty files include server tests/code, `server_check_todo.md`,
  `docs/resume-state.md`, and `total_todolist.md`. QA agents should not edit
  those while the main/server agents are working unless explicitly asked.
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
