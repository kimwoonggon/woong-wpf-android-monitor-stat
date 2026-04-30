# WPF UI Acceptance Checklist

Updated: 2026-04-30

WPF UI acceptance must verify semantic behavior, not only capture screenshots.
Screenshots are supporting evidence after FlaUI semantic checks.

## Pass/Fail Standard

The acceptance criterion is actual behavior from a launched WPF process. A UI
check is only considered passing when automation can invoke the relevant
control and observe the resulting state, data, persistence, or sync effect.
XAML structure tests and screenshots are useful regression evidence, but they
do not by themselves prove the feature works.

Examples:

- Start/Stop must be clicked through FlaUI and change visible tracking state.
- SampleDashboard mode must show deterministic app/domain/summary data without
  starting tracking or writing focus/web/outbox rows to SQLite.
- TrackingPipeline mode must generate fake foreground/browser activity and show
  the expected apps/domains in the dashboard.
- Stop must flush sessions into the temp SQLite database.
- Sync Now must call the fake sync path and update visible sync status.
- Screenshots must be captured after those semantic checks, not instead of
  them.

## Required AutomationIds

- `StartTrackingButton`
- `StopTrackingButton`
- `SyncNowButton`
- `TrackingStatusText`
- `CurrentAppNameText`
- `CurrentProcessNameText`
- `CurrentWindowTitleText`
- `CurrentSessionDurationText`
- `LastPersistedSessionText`
- `LastSyncStatusText`
- `CurrentActivityPanel`
- `HeaderStatusBar`
- `ControlBar`
- `WindowTitleVisibleCheckBox`
- `AppSessionsTab`
- `RecentAppSessionsList`
- `WebSessionsTab`
- `RecentWebSessionsList`
- `LiveEventsTab`
- `LiveEventsList`
- `SettingsPanel`
- `SummaryCardsContainer`
- `ChartArea`
- `SettingsTab`

## Semantic Checks

- Start button is visible and enabled.
- Stop button exists.
- Sync Now button exists.
- Start can be invoked.
- `TrackingStatusText` changes to `Running`.
- Current app name is populated.
- Current window title is populated or privacy-masked.
- TrackingPipeline mode shows Visual Studio Code, Chrome, Notepad, and File
  Explorer focus changes.
- TrackingPipeline mode shows same-window Chrome navigation through
  `youtube.com`, `github.com`, and `chatgpt.com`.
- TrackingPipeline mode shows a second Chrome process/window on
  `learn.microsoft.com`.
- TrackingPipeline mode proves temp SQLite `focus_session`, `web_session`, and
  `sync_outbox` rows are written after Start, Poll, Stop, and Sync.
- TrackingPipeline mode proves Web Sessions and Web Focus dashboard surfaces
  refresh from SQLite before Stop.
- TrackingPipeline mode proves default browser privacy stores domains only and
  does not store full URLs, page titles, or page content.
- Recent app sessions list contains expected app sessions.
- Recent web sessions list contains expected web sessions.
- Summary cards show expected durations.
- Live event log shows focus/web activity plus runtime semantics: Tracking
  started, FocusSession closed/started, WebSession closed/started,
  persisted/outbox events, sync skipped, and Tracking stopped.
- Settings contains privacy controls.
- Window and page titles are privacy-masked unless the explicit title setting
  allows them.
- Previously hidden titles are not retained and revealed later just because the
  setting changes.
- Stop changes `TrackingStatusText` to `Stopped`.
- Sync Now updates `LastSyncStatusText`.
- At 1024x768, Header, Control Bar, Current Focus, App Sessions, Web Sessions,
  Live Events, and Settings remain reachable through stable AutomationIds.

## Required Screenshots

- `01-startup.png`
- `02-after-start.png`
- `03-after-generated-activity.png`
- `03a-during-chrome-youtube-window.png`
- `03b-during-chrome-github-same-window.png`
- `03c-during-chrome-chatgpt-same-window.png`
- `03d-during-chrome-second-process-docs.png`
- `03e-during-notepad-switch.png`
- `03f-during-explorer-switch.png`
- `04-after-stop.png`
- `05-after-sync.png`
- `06-settings.png`
- `current-activity.png`
- `summary-cards.png`
- `recent-sessions.png`
- `recent-web-sessions.png`
- `live-events.png`
- `chart-area.png`, when visible
- `viewport-1024-header.png`
- `viewport-1024-control-bar.png`
- `viewport-1024-current-focus.png`
- `viewport-1024-settings.png`

## Required Artifacts

- `report.md`
- `manifest.json`
- `visual-review-prompt.md`
- `real-start-report.md` beside the RealStart acceptance temp DB
- `real-start-manifest.json` beside the RealStart acceptance temp DB

The report must include:

- PASS/FAIL/WARN table.
- Expected values.
- Actual values.
- Screenshot list.
- Skipped screenshots with reason.
- TrackingPipeline grouped evidence for Start -> Poll -> SQLite
  `focus_session`/`web_session` -> `sync_outbox` -> dashboard refresh.
- TrackingPipeline grouped browser privacy evidence for same-window Chrome
  domain changes, second Chrome window/process evidence, and arbitrary app
  switches.
- RealStart local DB evidence for `focus_session` persistence, `sync_outbox`
  queueing, readable app/process text, and server sync remaining disabled
  unless explicitly allowed.
- Next recommended fixes.

## Required Scripts

- `scripts/run-wpf-ui-acceptance.ps1`
- `scripts/run-wpf-real-start-acceptance.ps1`

The RealStart script must warn:

```text
This will observe foreground window metadata for local testing.
It will not record keystrokes.
It will not capture screen contents.
It will use a temp DB unless configured otherwise.
```

Real server sync must remain disabled unless `--AllowServerSync` is explicitly
provided.

## Visual Review Prompt

The snapshot package should generate
`artifacts/ui-snapshots/latest/visual-review-prompt.md`. It should ask a human
or GPT reviewer to check:

- Whether current activity is readable.
- Whether Start/Stop state is clear.
- Whether expected app names appear.
- Whether expected domains appear.
- Whether summary values match expected data.
- Whether lists are clipped.
- Whether chart area is visible.
- Whether settings/privacy controls are readable.
- Whether content is overlapped or offscreen.

No OpenAI API call is required. Automated GPT visual review is optional,
disabled by default, and may only run when `OPENAI_API_KEY` exists.

## Current Status

Milestone 21 added the baseline current-activity UI and unit/WPF tests for the
Start, Stop, Sync Now, title privacy, and fake coordinator behaviors. Full
semantic FlaUI acceptance, fake generated activity content, richer screenshots,
chart-area visibility handling, and better vertical space for App Sessions,
Web Sessions, and Live Event Log remain Milestone 25 work.

Milestone 22 added `scripts/run-wpf-real-start-acceptance.ps1` and the
`Woong.MonitorStack.Windows.RealStartAcceptance` tool. This local-only check
uses real Windows foreground readers, a temp SQLite DB, and FlaUI Start/Stop
clicks to prove that at least one focus session and one outbox item are
persisted without uploading to a server. Per the latest product priority, the
cramped lower App Sessions/Web Sessions area remains deferred while non-UI
tracking and browser-domain work continues.

Milestone 25 must use semantic FlaUI behavior checks as the automated gate. The
snapshot package remains visual evidence for humans, not the primary pass/fail
signal.

Current in-process WPF App coverage now includes the first semantic tracking
pipeline proof: the actual Start/Stop buttons are invoked through UI Automation
peers, fake foreground activity is persisted into a temp SQLite database,
outbox rows are queued, and the dashboard renders the persisted session back
from the SQLite-backed data source. The remaining Milestone 25 work is to lift
the same standard into a local FlaUI-launched acceptance tool with reports and
screenshots.

Additional semantic coverage now proves:

- Running tracking is polled through `PollTrackingCommand`, and the WPF
  `DispatcherTimer` path advances `CurrentSessionDurationText` beyond zero.
- Polling after a foreground change can refresh the current dashboard period
  when a closed focus session has been persisted.
- A fake browser pipeline can create `github.com` and `chatgpt.com` web
  sessions, persist them in SQLite, and show them in the WPF Web Sessions tab.
- The minimum-size WPF window exposes scrolling so the lower dashboard tabs are
  still reachable.

`scripts/run-wpf-ui-acceptance.ps1` now composes the local RealStart semantic
check with TrackingPipeline snapshot evidence. It builds the WPF app and tools,
launches the app through FlaUI, invokes Start/Stop, verifies temp SQLite
`focus_session` and `sync_outbox` rows through the RealStart tool, then launches
the app again in `WOONG_MONITOR_ACCEPTANCE_MODE=TrackingPipeline`.

TrackingPipeline mode uses fake metadata-only activity: Code.exe, one Chrome
HWND/PID navigating through `youtube.com`, `github.com`, and `chatgpt.com`, a
second Chrome process/window on `learn.microsoft.com`, Notepad, and File
Explorer. The tool verifies Running/Stopped status, persisted SQLite-backed app
and web sessions, `focus_session`/`web_session`/`sync_outbox` row counts,
domain-only browser privacy evidence, live event rows, summary duration, and
fake opt-in Sync Now behavior. It writes
`artifacts/wpf-ui-acceptance/<timestamp>/report.md`, detailed snapshot
`report.md`, `manifest.json`, and `visual-review-prompt.md`.

This composed local script was verified with `-Seconds 2`; it is the current
beginner-friendly command for proving the runtime pipeline works before visual
review. The solution also has 175 passing .NET tests and current line coverage
of 92.2% overall. `ChartArea` can still be reported as WARN when it is below
the current scroll viewport; this is acceptable for the current functional gate
because required app/web/session content is semantically checked.

Still to do: add richer EmptyData and SampleDashboard acceptance modes so a
beginner can separately verify no-data and deterministic sample dashboard
states without relying on the TrackingPipeline scenario.

## 2026-04-29 Product UI Goal Slice

The WPF dashboard has been moved closer to the provided product UI goal image:

- Header now separates the title/subtitle from runtime process text.
- Header exposes tracking, sync, and privacy badges.
- Control Bar uses readable product labels: Start Tracking, Stop Tracking,
  Refresh, Sync Now, Today, 1h, 6h, 24h, and Custom.
- Current Focus exposes current domain, last poll, last DB write, and last
  persisted session fields.
- Summary cards now distinguish Active Focus, Foreground, Idle, and Web Focus.
- App Sessions, Web Sessions, and Live Event Log grids expose the required
  product columns with minimum widths.
- Settings shows privacy, sync, and runtime sections with safe defaults.

New/updated tests verify these WPF UI semantics through public window/ViewModel
behavior. The latest local WPF acceptance run passed at:

`artifacts/wpf-ui-acceptance/20260429-095154`

Coverage after the first product UI slice: overall line coverage 92.2%,
Windows.Presentation 95.7%, and Windows.App 86.3%.

The follow-up chart slice bound LiveCharts X/Y axes from tested presentation
data. Hour labels now use `HH` labels, Y labels format milliseconds as minutes
such as `0m`, `10m`, and `60m`, and chart data exposes `No data for selected
period` for empty inputs. The latest local WPF acceptance run passed at:

`artifacts/wpf-ui-acceptance/20260429-100159`

Coverage after the chart slice: overall line coverage 92.3%,
Windows.Presentation 96.6%, and Windows.App 86.3%.

The next empty-state UI slice added visible `No data for selected period`
overlays for hourly activity, app usage, and domain usage chart panels. The
latest local WPF acceptance run passed at:

`artifacts/wpf-ui-acceptance/20260429-100746`

Coverage remains 92.3% overall, with Windows.Presentation at 96.6% and
Windows.App at 86.3%.

The chart details tab-switch slice added semantic coverage for app/domain chart
`상세보기` actions. App details now selects App Sessions, domain details now
selects Web Sessions, and the `DashboardTabs` selected value follows
`DashboardViewModel.SelectedDetailsTab`. The latest local WPF acceptance run
passed at:

`artifacts/wpf-ui-acceptance/20260429-114947`

Coverage remains 92.3% overall.

The DetailsTabsPanel extraction slice moved App/Web/Live/Settings tab content
into `Views/DetailsTabsPanel.xaml` while preserving all existing tab/list/settings
AutomationIds and `SelectedDetailsTab` binding. The latest local WPF acceptance
run passed at:

`artifacts/wpf-ui-acceptance/20260429-121155`

Coverage remains 92.3% overall.

The SettingsPanel extraction slice moved the Settings tab body into
`Views/SettingsPanel.xaml` while preserving privacy-safe defaults, sync opt-in
behavior, runtime/storage disabled actions, and inherited dashboard
`DataContext`. The latest local WPF acceptance run passed at:

`artifacts/wpf-ui-acceptance/20260429-122321`

Coverage is 92.4% overall.

The StatusBadge extraction slice moved header badges into
`Controls/StatusBadge.xaml` while preserving `TrackingStatusBadge`,
`SyncStatusBadge`, and `PrivacyStatusBadge`. The latest local WPF acceptance run
passed at:

`artifacts/wpf-ui-acceptance/20260429-123308`

Coverage is 92.3% overall.

The DetailRow extraction slice moved repeated Current Focus label/value rows
into `Controls/DetailRow.xaml` while preserving key Current Focus value
AutomationIds. The latest local WPF acceptance run passed at:

`artifacts/wpf-ui-acceptance/20260429-124239`

Coverage is 92.0% overall.

The SectionCard extraction slice added `Controls/SectionCard.xaml` and moved
the chart area's reusable card surface onto it while preserving the chart
AutomationIds, empty states, and app/domain `상세보기` tab-switch commands. The
latest local WPF acceptance run passed at:

`artifacts/wpf-ui-acceptance/20260429-125444`

Coverage is 92.1% overall. The acceptance script still reports a non-fatal
clean-close warning from FlaUI/process ownership; semantic checks and
SQLite/outbox verification passed.

The button style dictionary slice added `Styles/Buttons.xaml`, merged it from
`App.xaml`, and replaced duplicate local button style definitions in
`ControlBar` and `SettingsPanel`. Button readability, Start/Stop/Refresh/Sync,
period controls, settings storage buttons, and semantic pipeline acceptance
remained green. The latest local WPF acceptance run passed at:

`artifacts/wpf-ui-acceptance/20260429-130753`

Coverage remains 92.1% overall.

The root scrolling alignment slice changed `DashboardView` to use vertical-only
root scrolling while keeping horizontal scrolling inside the wide App/Web/Live
DataGrids. Minimum-size tab reachability remains covered. The latest local WPF
acceptance run passed at:

`artifacts/wpf-ui-acceptance/20260429-131541`

Coverage remains 92.1% overall.

The cards style dictionary slice added `Styles/Cards.xaml` and moved duplicated
card border shape into shared dashboard-card and compact-surface styles.
MetricCard, SectionCard, CurrentFocusPanel, DetailsTabsPanel, and ControlBar
continue to pass behavior/layout tests. The latest local WPF acceptance run
passed at:

`artifacts/wpf-ui-acceptance/20260429-132227`

Coverage remains 92.1% overall.

The colors style dictionary slice added `Styles/Colors.xaml` and wired the core
background/surface/border/text brushes into the app shell and card styles. The
latest local WPF acceptance run passed at:

`artifacts/wpf-ui-acceptance/20260429-132937`

Coverage remains 92.1% overall.

The typography style dictionary slice added `Styles/Typography.xaml` and wired
shared text styles into the header, reusable section card, detail row, and
metric card controls. The latest local WPF acceptance run passed at:

`artifacts/wpf-ui-acceptance/20260429-134019`

Coverage remains 92.1% overall.

The DataGrid style dictionary slice added `Styles/DataGrid.xaml` and moved
shared read-only grid behavior into `SessionDataGridStyle`. App/Web/Live grids
still keep explicit column widths and their own horizontal scrolling. The
latest local WPF acceptance run passed at:

`artifacts/wpf-ui-acceptance/20260429-134913`

Coverage remains 92.1% overall.

The tabs style dictionary slice added `Styles/Tabs.xaml` and wired
`DashboardTabs` to shared TabControl/TabItem styles without changing selection
binding or tab headers. The latest local WPF acceptance run passed at:

`artifacts/wpf-ui-acceptance/20260429-135614`

Coverage remains 92.1% overall.

The Domain Focus chart mismatch slice replaced the earlier `PieChart` with a
Cartesian/ranking chart backed by `DashboardLiveChartsData`, matching the App
Focus chart shape and keeping domain labels readable. The latest local WPF
acceptance run passed at:

`artifacts/wpf-ui-acceptance/20260429-140652`

Coverage remains 92.1% overall.

The Settings privacy coverage slice made safety controls explicit: page title
capture is off/disabled, full URL capture remains off/disabled, domain-only
browser storage is on, the sync endpoint is disabled until sync opt-in, and
clear local data is disabled until a guarded flow exists. The latest local WPF
acceptance run passed at:

`artifacts/wpf-ui-acceptance/20260429-141606`

Coverage remains 92.1% overall.

The Details pagination slice moved App Sessions, Web Sessions, and Live Event
Log grids onto visible paged collections. The details footer exposes 10/25/50
rows-per-page plus previous/next commands, and semantic tests verify the footer
bindings through public ViewModel state and XAML AutomationIds. The latest
local WPF acceptance run passed at:

`artifacts/wpf-ui-acceptance/20260429-143059`

Coverage after this slice is 91.9% overall.

The viewport-aware acceptance slice added `--viewport-widths` support to the
local UI snapshot tool and now captures dashboard plus section screenshots at
1920, 1366, and 1024 widths. The manifest records `viewportWidths` and skipped
screenshot reasons. The latest local WPF acceptance run passed at:

`artifacts/wpf-ui-acceptance/20260429-144429`

Coverage after this slice is 91.9% overall. The run generated all viewport
dashboard, summary, chart, app-session, web-session, and live-event screenshots
with no skipped screenshots.

Remaining UI acceptance gap: strict visual regression and CI execution remain
future work. The semantic pipeline gate remains the primary automated evidence.

The live browser persistence slice added a UI-surface regression check for the
Start/tick path: with Chrome foreground, the fake browser reader changes from
`github.com` to `chatgpt.com`, the completed `github.com` WebSession is written
to SQLite, a pending `web_session` outbox row is created, and the Web Sessions
grid refreshes before Stop. The latest local WPF acceptance run passed at:

`artifacts/wpf-ui-acceptance/20260429-151739`

Coverage after this slice is 92.2% overall.

The RealStart acceptance tool now verifies more than SQLite row counts: after
Stop, it reads the latest persisted focus-session process/app name from the
temp DB and confirms that text appears in the WPF `RecentAppSessionsList`.
The latest combined local WPF acceptance run passed at:

`artifacts/wpf-ui-acceptance/20260429-152658`

Coverage remains 92.2% overall.

The auto-start and sync-at-start slice updated acceptance semantics for the
normal product startup. RealStart and TrackingPipeline now accept an app that is
already `Running` when automation attaches, instead of requiring the Start
button to be initially enabled. EmptyData mode explicitly disables auto-start
with `WOONG_MONITOR_AUTO_START_TRACKING=0`. TrackingPipeline still proves
Code.exe, chrome.exe, `github.com`, and `chatgpt.com` appear in SQLite-backed
UI surfaces, and Start-triggered sync remains local-only until sync is enabled.
The latest combined local WPF acceptance run passed at:

`artifacts/wpf-ui-acceptance/20260429-154548`

Coverage after this slice is 92.0% overall.

The TrackingPipeline SQLite evidence slice made the UI snapshot report
semantically prove local persistence, not only visible text and screenshots.
TrackingPipeline now writes DB checks into the PASS/FAIL/WARN table and a
dedicated `## SQLite Evidence` section. The latest local acceptance run
recorded `focus_session=2`, `web_session=2`, and `sync_outbox=4`, and the
manifest includes the same `databaseEvidence` object.

`artifacts/wpf-ui-acceptance/20260429-155615`

Coverage after this slice remains 92.0% overall.

The EmptyData acceptance slice now proves the inverse baseline: the snapshot
tool launches with auto-start disabled, writes `empty-data.db`, and records
`focus_session=0`, `web_session=0`, and `sync_outbox=0` in the local report and
manifest. The same slice changed the Current Focus browser-domain empty state
to `No browser domain yet. Connect browser capture; app focus is tracked.` so a
missing domain does not look like a privacy failure.

`artifacts/ui-snapshots/latest` from run `20260429-160531`

The latest combined WPF UI acceptance run passed at:

`artifacts/wpf-ui-acceptance/20260429-161328`

Coverage after this slice remains 92.0% overall.

## 2026-04-30 Same-Window Browser Navigation Acceptance

The WPF TrackingPipeline acceptance now covers a Chrome same-window navigation path before switching to another Chrome process/window:

1. Start with VS Code foreground.
2. Switch to one Chrome HWND/PID on `youtube.com`.
3. Navigate the same Chrome HWND/PID to `github.com`.
4. Navigate the same Chrome HWND/PID to `chatgpt.com`.
5. Switch to a second Chrome process/window on `learn.microsoft.com`.
6. Switch to Notepad.
7. Switch to File Explorer.
8. Stop and flush.

Expected evidence:

- The same Chrome HWND/PID navigation persists `youtube.com`, `github.com`, and `chatgpt.com` as separate WebSessions without closing the Chrome FocusSession.
- The second Chrome process/window and arbitrary app switches close/persist FocusSessions.
- The default browser privacy path persists domains only and does not persist full URLs, page titles, or page content.
- The consolidated WPF check package under `artifacts/wpf-check/latest/`
  contains report/manifest pointers only; PNGs stay in the ignored
  WPF acceptance/snapshot artifact folders and are referenced from the package.

Latest local run passed at:

`artifacts/wpf-ui-acceptance/20260430-163246`

Coverage after this slice: line 91.9%, branch 70.8%.

## Same-Window Browser Navigation Regression Evidence 2026-04-30

Automated checks now cover the product-critical case where one Chrome window stays foreground while the active tab/domain changes repeatedly:

1. Start tracking on Chrome at `youtube.com`.
2. Poll after the same Chrome HWND/PID reports `github.com`.
3. Poll again after the same Chrome HWND/PID reports `chatgpt.com`.
4. Verify `youtube.com` and `github.com` are persisted to SQLite as WebSessions before Stop.
5. Verify the current domain shown in the WPF UI is `chatgpt.com`.
6. Verify Web Focus and Web Sessions grid refresh from SQLite before Stop.
7. Verify full URLs, query strings, and page content are not persisted in default domain-only mode.

Latest verification:

- Focused WPF tests passed: `PollOnce_WhenSameChromeWindowVisitsYoutubeGithubChatGpt` and `PollTick_WhenSameChromeWindowDomainChangesTwice`.
- Full `.NET` solution tests passed: 409 tests.
- WPF UI acceptance passed: `artifacts/wpf-ui-acceptance/20260430-165524`.

## 2026-05-01 Taskbar And Explicit Exit Contract

- The WPF MainWindow must appear in the Windows taskbar.
- Clicking the titlebar X must minimize Woong Monitor Stack to the taskbar and must not stop tracking or exit the process.
- Settings must expose an explicit **Exit app** action for real shutdown.
- Release validation commands:
  - `dotnet build Woong.MonitorStack.sln -c Release --no-restore -m:1 -v minimal`
  - `dotnet run --configuration Release --project src\Woong.MonitorStack.Windows.App\Woong.MonitorStack.Windows.App.csproj`
- Local MSIX package command:
  - `powershell -ExecutionPolicy Bypass -File scripts\package-windows-msix.ps1`
