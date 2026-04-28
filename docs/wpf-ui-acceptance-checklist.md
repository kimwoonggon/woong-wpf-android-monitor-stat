# WPF UI Acceptance Checklist

Updated: 2026-04-29

WPF UI acceptance must verify semantic behavior, not only capture screenshots.
Screenshots are supporting evidence after FlaUI semantic checks.

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
- `WindowTitleVisibleCheckBox`
- `RecentAppSessionsList`
- `RecentWebSessionsList`
- `LiveEventsList`
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
- TrackingPipeline mode shows Visual Studio Code.
- TrackingPipeline mode shows Chrome.
- TrackingPipeline mode shows `github.com`.
- TrackingPipeline mode shows `chatgpt.com`.
- Recent app sessions list contains expected app sessions.
- Recent web sessions list contains expected web sessions.
- Summary cards show expected durations.
- Live event log shows `FocusChanged` and `BrowserVisit`.
- Settings contains privacy controls.
- Window and page titles are privacy-masked unless the explicit title setting
  allows them.
- Previously hidden titles are not retained and revealed later just because the
  setting changes.
- Stop changes `TrackingStatusText` to `Stopped`.
- Sync Now updates `LastSyncStatusText`.

## Required Screenshots

- `01-startup.png`
- `02-after-start.png`
- `03-after-generated-activity.png`
- `04-after-stop.png`
- `05-after-sync.png`
- `06-settings.png`
- `current-activity.png`
- `summary-cards.png`
- `recent-sessions.png`
- `recent-web-sessions.png`
- `live-events.png`
- `chart-area.png`, when visible

## Required Artifacts

- `report.md`
- `manifest.json`
- `visual-review-prompt.md`

The report must include:

- PASS/FAIL/WARN table.
- Expected values.
- Actual values.
- Screenshot list.
- Skipped screenshots with reason.
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
