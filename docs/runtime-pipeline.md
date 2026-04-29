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

The browser-domain pipeline is metadata-only by default. It must not collect
keystroke contents, clipboard contents, page contents, form input, message
contents, passwords, screenshots, or full URLs unless a future explicit full
URL storage setting is enabled for URL storage only.

No fake domain should be inferred from a window title unless it is explicitly
marked low-confidence test/fallback data.

Missing browser-domain metadata is a capture-connection status, not a privacy
block. The UI should continue to show current app/process/window metadata as
soon as foreground capture starts. Browser domain should appear when the
extension/native messaging path, or the WPF app's metadata-only UI Automation
address-bar fallback, reports domain metadata. Full URL storage remains a
separate opt-in privacy setting.

Running the WPF app as Administrator is not enough to make domain capture work.
Elevation does not grant Chrome, Edge, Firefox, Brave, or other browsers
active-tab URL APIs and can still fail depending on browser UI, profile,
accessibility, and process boundaries. Production should use an explicit
browser extension/native messaging channel as the stable path, with the UI
Automation address-bar fallback documented as domain-only, status-aware, and
best-effort.

Chrome currently has the implemented native-messaging host and install script.
Edge and Brave require their own extension packaging plus browser-specific
native-messaging manifest/registry installers before they should be treated as
stable capture channels. Firefox requires a Firefox-specific extension and
native-messaging manifest path. Until those are present, non-Chrome domain
capture is limited to the best-effort domain-only UI Automation fallback.

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

Daily summary and date-range statistics use the requested user timezone when
assigning both focus sessions and web sessions to local dates. The server must
not rely only on a client-provided `FocusSession.LocalDate`, because integrated
Windows + Android summaries are user/timezone reports and a UTC-midnight
boundary can otherwise drop valid sessions from the requested day.

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

Closing the WPF window while tracking is still Running now executes the same
Stop flush path before the ticker is stopped and detached. This prevents the
currently open focus session from being lost when the user exits without
pressing Stop first; the session is persisted to Windows local SQLite and a
pending `focus_session` outbox row is queued.

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

Stop now explicitly flushes an open browser domain session. If Chrome remains
foreground on `github.com` and the user clicks Stop before another domain
change occurs, the coordinator completes the current `WebSession`, persists it
to Windows local SQLite in domain-only mode, queues a pending `web_session`
outbox row, and refreshes the WPF Web Focus summary plus Web Sessions grid.
Zero-duration browser sessions are discarded instead of being persisted.

Normal WPF product startup now defaults to visible auto-start tracking through
`WindowsAppOptions.AutoStartTracking`. `StartTrackingCommand` also performs an
immediate sync attempt so users see sync state right away. This does not enable
server upload by default: if sync is off, the visible status is
`Sync skipped. Enable sync to upload.` and data remains local.

The WPF `Sync Now` button is covered against real local outbox rows while sync
is off. After Start/Stop queues both `focus_session` and `web_session` rows,
clicking Sync Now leaves every row `Pending`, keeps `SyncedAtUtc` null and
`RetryCount` at zero, and shows the skipped/local-only status. This preserves
sync opt-in even when data is already waiting locally.

On startup/Start, the Windows tracker reads the current foreground app/window
metadata immediately and displays that current focus state. It does not treat
all running or background processes as focus time; only the foreground
app/window becomes the active FocusSession. Browser domain metadata is a
separate capture channel: show domain-only metadata immediately when capture is
available, and otherwise report the missing domain as a capture-connection
status rather than a privacy block.

The dashboard separates browser domain from browser capture status. Domain text
is reserved for metadata such as `github.com`; capture status reports whether
the domain came from extension/native messaging, the UI Automation address-bar
fallback, is unavailable, or failed while normal foreground tracking continues.

Chrome extension/native-message ingestion now has a local console host:
`tools/Woong.MonitorStack.ChromeNativeHost`. The host reads Chrome's
native-messaging stdin stream until EOF, sanitizes URL metadata with DomainOnly
storage, persists browser raw events and completed web sessions into the same
Windows local SQLite database, and enqueues `web_session` outbox rows. The
installer script `scripts/install-chrome-native-host.ps1` publishes the host and
registers the current-user Chrome native-messaging manifest. Foreground
app/window focus still comes from the WPF tracking pipeline; extension tab
events provide browser-domain metadata.

When either Chrome native messaging or the address-bar fallback reports a
domain, the dashboard should display that domain immediately. A missing domain
means the capture channel is unavailable, not yet installed, not connected, or
not currently reporting; it does not mean that privacy settings are blocking
domain-only metadata.

`SampleDashboard` acceptance mode is intentionally not a tracking pipeline. It
injects deterministic read-only dashboard rows for visual review and beginner
verification, while leaving the temp SQLite `focus_session`, `web_session`, and
`sync_outbox` tables empty. Use `TrackingPipeline` or RealStart acceptance when
the goal is to prove Start/Poll/Stop persistence.

Chrome native messaging acceptance is also isolated from the user's normal
runtime state. The acceptance script launches Chrome for Testing with a
temporary `--user-data-dir` profile, registers only the scoped HKCU test host
`com.woong.monitorstack.chrome_test`, writes to an artifact SQLite DB, and sets
`WOONG_MONITOR_REQUIRE_EXPLICIT_DB=1` so the host cannot silently fall back to
the user's real local DB during acceptance. Cleanup stops only Chrome processes
whose command line contains the temporary profile path, and it refuses cleanup
entirely if the path is not under the `woong-chrome-native-*` acceptance temp
root. The current acceptance uses extension/native messaging to write
`github.example` and `chatgpt.example` domain-only web sessions plus outbox
rows into temp SQLite, proving the browser metadata path without Chrome
address-bar scraping.
