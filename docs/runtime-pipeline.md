# Runtime Pipeline

Updated: 2026-04-29

This document describes the target runtime behavior. It separates real
collection, fake acceptance pipelines, and server sync boundaries.

## Windows Runtime Pipeline

```text
User clicks Start
  -> WPF tracking coordinator starts a visible tracking session
  -> Foreground reader captures current window metadata
  -> Idle reader captures last input timestamp
  -> FocusSessionizer closes/starts focus sessions on app/window/idle changes
  -> Closed focus sessions are persisted to Windows local SQLite with safe
     process/window metadata
  -> Outbox rows are enqueued for sync with the same metadata
  -> Browser reader/sessionizer creates web sessions when URL/domain is known
  -> Dashboard reads current state plus SQLite-backed period statistics
  -> User clicks Stop
  -> Current sessions are flushed to SQLite/outbox
```

Required current activity UI:

- Start button.
- Stop button.
- Sync Now button.
- Current app name.
- Current process name.
- Current window title, privacy-aware.
- Current session duration.
- Tracking status.
- Last persisted session.
- Last sync status.

The WPF app must make collection visible. No Windows tracking should run as a
hidden background behavior in MVP.

Focus sessions now carry nullable process/window metadata through the local and
server sync path: process id, process name, executable path, window handle, and
window title. Window title remains nullable and should be sent only when the
user's privacy setting allows it.

## Browser Runtime Pipeline

Browser tracking is layered on top of normal browser focus sessions:

```text
Browser process focused
  -> FocusSession records chrome.exe/msedge.exe/firefox.exe/brave.exe duration
  -> Browser reader attempts URL/title/domain capture
  -> If URL/domain unavailable, keep only FocusSession
  -> If domain available, create WebSession linked to FocusSession
  -> URL/domain changes close previous WebSession and start a new one
```

No fake domain should be inferred from a window title unless it is explicitly
marked low-confidence test/fallback data.

## Android Runtime Pipeline

```text
User grants Usage Access
  -> User-visible collection setting is enabled
  -> WorkManager periodically runs usage collection only while both are true
  -> UsageStatsManager UsageEvents are sessionized
  -> App usage sessions are stored in Room
  -> Local sync outbox rows are created for retry-safe future upload
  -> WorkManager sync uploads outbox rows to the server only when sync is opted in
  -> Android dashboard reads local Room data
  -> Daily summary screen can query integrated server summary
```

Android must not use global touch tracking, text input capture, screen-content
reading, or hidden always-on monitoring.

## Server Runtime Pipeline

```text
Client registers device
  -> Uploads focus/web/raw sessions through idempotent DTOs
  -> PostgreSQL stores integrated data for all devices
  -> Daily aggregation builds user/timezone local-date summaries
  -> Windows and Android query integrated summaries by date or date range
```

Server PostgreSQL is the only integrated store. Windows SQLite and Android Room
remain local device stores.

## Acceptance Modes

### A. EmptyData

- App starts without crashing.
- Empty dashboard states are visible.
- No tracking starts automatically.

### B. SampleDashboard

- Deterministic dashboard data is injected.
- UI shows expected app names, domains, durations, charts, and lists.

### C. TrackingPipeline

- Fake foreground/browser readers generate deterministic events.
- Temporary SQLite DB is used.
- FlaUI clicks Start.
- Sessions are persisted to SQLite and shown in the dashboard.
- Expected content includes Visual Studio Code, Chrome, `github.com`, and
  `chatgpt.com`.
- Stop flushes sessions.
- Sync Now uses a fake sync client.

### D. RealStart Local Validation

- Real Windows readers are used.
- A temp/local DB is used.
- FlaUI clicks Start, waits a short interval, then clicks Stop.
- At least one real focus session is persisted.
- UI shows a recent app session.
- No real server upload occurs unless an explicit `--AllowServerSync` flag is
  provided.

The RealStart script must warn that it observes foreground window metadata,
does not record keystrokes, does not capture screen contents, and uses a temp
DB unless configured otherwise.

## Current Windows Implementation Status

Milestone 22 now wires the WPF composition root to a real app-hosted tracking
coordinator. The coordinator creates a fresh `TrackingPoller` for each Start,
persists closed focus sessions to Windows local SQLite, queues `focus_session`
outbox rows, and lets the dashboard read local SQLite data through
`SqliteDashboardDataSource`.

`scripts/run-wpf-real-start-acceptance.ps1` now provides the RealStart local
validation path. It prints the required privacy warning, uses
`WOONG_MONITOR_LOCAL_DB` to force the app onto a temp SQLite DB, and keeps real
server sync disabled unless `--AllowServerSync` is explicitly passed.

The latest local RealStart run launched the WPF app with real Windows readers,
clicked Start/Stop through FlaUI, and verified that the temp DB contained one
`focus_session` row and one `sync_outbox` row.
