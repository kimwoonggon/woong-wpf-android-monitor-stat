# Resume State

Updated: 2026-04-28

## Last Completed Slice

Bootstrap plus Milestone 1 common domain/contracts foundation.

## Completed

- Added root `AGENTS.md` with mandatory installed skill usage and always-on
  todo/test/build/commit/push workflow.
- Added `total_todolist.md` as the full PRD checklist.
- Added common domain models: `Device`, `Platform`, `AppFamily`,
  `PlatformApp`, `FocusSession`, `WebSession`, `DeviceStateSession`,
  `DailySummary`.
- Added contract DTOs for device registration and focus/web/raw upload batches.
- Added `DomainNormalizer` and `DailySummaryCalculator`.
- Added `docs/contracts.md` for time/date, device, upload idempotency, and web
  domain policy.
- Verified `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`.
- Pushed initial commit `fdf703c` to `origin/main`.

## Next Highest Priority

Continue Milestone 1 hardening before Windows Collector MVP:

1. Add stronger tests for upload batch request validation and raw event
   idempotency fields.
2. Add app-family summary tests so top apps can group `chrome.exe` and
   `com.android.chrome` under one family later.
3. Decide whether contracts remain in `Woong.MonitorStack.Domain.Contracts` or
   move to a separate `Woong.MonitorStack.Contracts` project before clients and
   server are created.
