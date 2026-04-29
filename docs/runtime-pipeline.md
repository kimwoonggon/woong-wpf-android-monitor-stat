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
- Current browser domain, when metadata is available.
- Current session duration.
- Tracking status.
- Last persisted session.
- Last poll time.
- Last DB write time.
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
  -> Dashboard shows domain-only metadata immediately when capture reports it
  -> URL/domain changes close previous WebSession and start a new one
```

No fake domain should be inferred from a window title unless it is explicitly
marked low-confidence test/fallback data.

Missing browser-domain metadata is a capture-connection status, not a privacy
block. The UI should continue to show current app/process/window metadata as
soon as foreground capture starts. Browser domain should appear when the
extension/native messaging path, or the WPF app's metadata-only UI Automation
address-bar fallback, reports domain metadata. Full URL storage remains a
separate opt-in privacy setting.

Running the WPF app as Administrator is not enough to make domain capture work.
Elevation does not grant Chrome, Edge, Firefox, or Brave active-tab URL APIs.
Production should use an explicit browser extension/native messaging channel as
the stable path, with the UI Automation address-bar fallback documented as
domain-only, status-aware, and best-effort.

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
- No tracking starts automatically. The local snapshot tool enforces this with
  `WOONG_MONITOR_AUTO_START_TRACKING=0` so EmptyData remains a pure empty-state
  baseline even though the normal product startup can auto-start tracking.

### B. SampleDashboard

- Deterministic dashboard data is injected.
- UI shows expected app names, domains, durations, charts, and lists.

### C. TrackingPipeline

- Fake foreground/browser readers generate deterministic events.
- Temporary SQLite DB is used.
- The WPF app auto-starts tracking or FlaUI clicks Start if it has not started
  yet.
- Sessions are persisted to SQLite and shown in the dashboard.
- Expected content includes Visual Studio Code, Chrome, `github.com`, and
  `chatgpt.com`.
- Stop flushes sessions.
- Start immediately attempts sync, but sync remains off/local-only by default
  and reports a skipped upload. Sync Now uses a fake sync client only after the
  acceptance flow explicitly enables sync.

### D. RealStart Local Validation

- Real Windows readers are used.
- A temp/local DB is used.
- The app may auto-start; otherwise FlaUI clicks Start. The flow waits a short
  interval, then clicks Stop.
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

RealStart now also verifies that the latest persisted focus session appears in
the WPF `RecentAppSessionsList` after Stop. The check reads the process/app name
from the temp SQLite `focus_session` table and searches the visible WPF
automation tree, avoiding a hard-coded process name because real foreground
metadata is environment-dependent.

`WindowsTrackingDashboardCoordinator` now returns runtime evidence timestamps
in each `DashboardTrackingSnapshot`. `LastPollAtUtc` is set on Start/Poll/Stop
from the same clock used by the polling pipeline. `LastDbWriteAtUtc` is set
when a focus session is persisted to SQLite and an outbox row is queued. The
WPF Current Focus panel formats those timestamps in the display timezone.

The WPF product UI now keeps the last persisted session and last DB write time
visible until a newer persistence event replaces them. A later poll without a
closed session must not reset the display to "No session persisted"; this is
covered by `UpdateCurrentActivity_WhenLaterPollHasNoPersistedSession_KeepsLastPersistedSession`.

The tracking snapshot also carries a web-persistence refresh signal. When a
browser domain change persists only a `web_session` and no focus session closes,
`DashboardTrackingSnapshot.HasPersistedWebSession` tells the WPF dashboard to
reload the SQLite-backed summary, app/web rows, charts, and live events. This
keeps browser-domain totals current without waiting for Stop or a later app
switch.

The real WPF coordinator now supports an optional privacy-safe browser reader.
When a browser snapshot is available, the coordinator sanitizes it with
DomainOnly storage by default, feeds it through `BrowserWebSessionizer`, writes
completed `web_session` rows to Windows local SQLite, and queues pending
`web_session` outbox rows. The UI path is covered through a MainWindow
Start/tick test: Chrome remains foreground, the domain changes from
`github.com` to `chatgpt.com`, and the dashboard reloads `github.com` from
SQLite before Stop is clicked.

Normal WPF product startup now defaults to visible auto-start tracking through
`WindowsAppOptions.AutoStartTracking`. `StartTrackingCommand` also performs an
immediate sync attempt so users see sync state right away. This does not enable
server upload by default: if sync is off, the visible status is
`Sync skipped. Enable sync to upload.` and data remains local.

On startup/Start, the Windows tracker reads the current foreground app/window
metadata immediately and displays that current focus state. It does not treat
all running or background processes as focus time; only the foreground
app/window becomes the active FocusSession. Browser domain metadata is a
separate capture channel: show domain-only metadata immediately when capture is
available, and otherwise report the missing domain as a capture-connection
status rather than a privacy block.
