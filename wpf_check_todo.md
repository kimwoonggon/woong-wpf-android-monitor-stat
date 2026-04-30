# WPF Check TODO

Updated: 2026-04-30

This checklist is the Windows/WPF feature verification map for Woong Monitor
Stack. The Windows app measures metadata only: foreground app/window intervals,
browser domain metadata when explicitly available, local SQLite persistence,
outbox/sync state, and dashboard summaries. It must not collect typed text,
passwords, form contents, clipboard contents, screen recordings, page contents,
private messages, or hidden screenshots of user activity.

## Evidence Folders

Primary WPF acceptance artifacts:

```text
artifacts/wpf-ui-acceptance/latest/
artifacts/ui-snapshots/latest/
```

Future consolidated WPF check package target:

```text
artifacts/wpf-check/latest/
```

Each checked feature should have:

- a behavior/unit/component/integration/UI test proving the feature;
- a local acceptance artifact or screenshot when the feature is visual;
- a note in `report.md` when the feature is not directly visual.

## Feature Checklist

| ID | Feature | Primary verification | Visual/evidence target |
|---|---|---|---|
| W01 | WPF composition root and DI host | App startup/architecture tests | App starts through `MainWindow` resolved from host |
| W02 | Layer/reference boundaries | Architecture tests | `docs/architecture/reference-rules.md` |
| W03 | Thin `MainWindow` shell | WPF App architecture tests | `MainWindow` hosts `DashboardView` only |
| W04 | MVVM presentation state | Presentation ViewModel tests | ViewModel properties/commands, no code-behind business logic |
| W05 | Foreground window reader | Windows collector tests and smoke | HWND, PID, process, path, title metadata |
| W06 | Idle detection | Unit tests | Last input threshold marks idle metadata only |
| W07 | Focus sessionizer | Unit/component tests | app/window switch closes previous session and starts new one |
| W08 | Tracking coordinator Start/Stop | WPF App tests | Start/Stop buttons update tracking status |
| W09 | Poll ticker loop | WPF App tests with fake ticker | tick calls poll and advances current duration |
| W10 | Poll-time persistence | WPF App SQLite temp DB tests | foreground change persists closed session before Stop |
| W11 | Stop flush behavior | WPF App tests | open focus/web session flushed on Stop |
| W12 | Windows local SQLite schema | SQLite repository/component tests | `focus_session`, `web_session`, `sync_outbox`, raw/state/settings |
| W13 | FocusSession persistence | SQLite tests and WPF acceptance DB checks | persisted focus rows with UTC duration/local date |
| W14 | Sync outbox creation | Component tests | pending outbox row for persisted session/web event |
| W15 | Sync opt-in default off | ViewModel/sync worker tests | Sync Now shows local-only/skipped state |
| W16 | Sync worker success/failure | Fake API tests | success marks synced, failure increments retry |
| W17 | SQLite-backed dashboard refresh | Dashboard data source tests | refresh reads temp SQLite, not fake production data |
| W18 | Period filters | DashboardViewModel tests | Today, 1h, 6h, 24h, Custom ranges |
| W19 | Header status badges | Layout/UI tests | Tracking, Sync, Privacy badges visible and non-overlapping |
| W20 | Control bar | Layout/command tests | Start, Stop, Refresh, Sync Now, Today, 1h, 6h, 24h, Custom |
| W21 | Current Focus panel | ViewModel/UI tests | app, process, title/privacy, domain, duration, poll, DB write |
| W22 | Summary cards | ViewModel/UI tests | Active Focus, Foreground, Idle, Web Focus from SQLite |
| W23 | Charts | chart mapper tests and snapshots | hour labels, minute axis, app/domain duration labels, empty state |
| W24 | App Sessions grid | XAML/layout/acceptance tests | readable columns, persisted VS Code/Chrome rows |
| W25 | Web Sessions grid | XAML/layout/acceptance tests | domain-only rows, no full URL by default |
| W26 | Live Event Log | ViewModel/acceptance tests | tracking/focus/web/outbox/sync/stop events |
| W27 | Settings tab | ViewModel/XAML tests | privacy-safe defaults, sync off, runtime/storage controls |
| W28 | Browser process classification | browser tests | Chrome, Edge, Firefox, Brave recognized |
| W29 | Browser URL/domain sanitizer | unit tests | domain-only default, full URL only on explicit opt-in |
| W30 | Web sessionizer | unit/component tests | domain changes close/start web sessions; unavailable URL creates no web row |
| W31 | Browser activity reader fallback | focused tests/smoke | metadata-only address-bar fallback when available |
| W32 | Chrome extension/native messaging | native host/manifest/acceptance tests | extension -> host -> SQLite domain events |
| W33 | Native host registry safety | script tests | HKCU-only, scoped host key, backup/restore/cleanup |
| W34 | WPF semantic acceptance | `scripts/run-wpf-ui-acceptance.ps1` | fake VS Code/Chrome/github.com/chatgpt.com, SQLite DB checks |
| W35 | WPF UI snapshots | `scripts/run-ui-snapshots.ps1` and FlaUI tool | startup, refresh, period, settings, section screenshots |
| W36 | RealStart local validation | `scripts/run-wpf-real-start-acceptance.ps1` | temp DB, visible warning, at least one real foreground session |
| W37 | Responsive layout | UI snapshot/acceptance tests | 1920, 1366, 1024 screenshots and no clipped grids/tabs |
| W38 | Reusable WPF components | architecture/XAML tests | DashboardView, HeaderStatusBar, ControlBar, CurrentFocusPanel, SummaryCardsPanel, ChartsPanel, DetailsTabsPanel, SettingsPanel |
| W39 | Reusable controls/styles | architecture/XAML tests | StatusBadge, MetricCard, SectionCard, DetailRow, EmptyState, shared styles |
| W40 | Coverage collection | coverage script | Coverlet/ReportGenerator output under `artifacts/coverage/` |
| W41 | Privacy forbidden scopes absent | architecture/source guard tests | no keylogging, typed text, clipboard, page content, screenshots-as-telemetry |
| W42 | UI screenshot privacy boundary | tool/docs tests | screenshots are of this app UI only, local developer artifacts |
| W43 | Windows smoke tool | `tools/Woong.MonitorStack.Windows.Smoke` | real foreground metadata only |
| W44 | Docs/resume/TODO hygiene | doc checks/manual review | docs updated after each slice |

## Runtime Modes To Verify

| Mode | Purpose | Expected evidence |
|---|---|---|
| EmptyData | App opens without data and shows safe empty states | `artifacts/ui-snapshots/latest/report.md` |
| SampleDashboard | Deterministic non-tracking dashboard sample | `scripts/run-ui-snapshots.ps1 -Mode SampleDashboard` |
| TrackingPipeline | Fake foreground/browser readers with temp SQLite DB | WPF UI acceptance report and DB row checks |
| RealStart | Real Windows foreground reader, temp DB, no real server upload by default | `scripts/run-wpf-real-start-acceptance.ps1` |
| ChromeNativeAcceptance | Test Chrome profile, extension, native host, temp SQLite DB | `artifacts/chrome-native-acceptance/latest/report.md` |

## Required Validation Commands

Run from repository root:

```powershell
dotnet restore Woong.MonitorStack.sln --configfile NuGet.config
dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal
dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal
powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1
powershell -ExecutionPolicy Bypass -File scripts\run-ui-snapshots.ps1
powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1
powershell -ExecutionPolicy Bypass -File scripts\run-wpf-real-start-acceptance.ps1
dotnet run --project tools\Woong.MonitorStack.Windows.Smoke
```

Chrome native messaging acceptance is separate and must use a temp Chrome
profile, temp native host manifest, temp SQLite DB, and HKCU test-only registry
key:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-chrome-native-message-acceptance.ps1
```

## Visual Review Entry Points

Use these files when they exist:

- `artifacts/wpf-ui-acceptance/latest/report.md`
- `artifacts/wpf-ui-acceptance/latest/manifest.json`
- `artifacts/wpf-ui-acceptance/latest/visual-review-prompt.md`
- `artifacts/ui-snapshots/latest/report.md`
- `artifacts/ui-snapshots/latest/visual-review-prompt.md`

Important screenshots expected from WPF acceptance/snapshot tools:

- startup/dashboard
- after Start
- after generated activity
- after Stop
- after Sync Now while sync is off
- settings
- current activity
- summary cards
- charts
- App Sessions
- Web Sessions
- Live Event Log
- 1920 dashboard
- 1366 dashboard
- 1024 dashboard

## Privacy Boundary

Allowed:

- foreground app/process/window metadata;
- window title only when user setting allows;
- idle/active state from last input timestamp, not typed content;
- browser domain/full URL only under documented privacy settings;
- Chrome/Edge extension/native messaging active tab metadata when explicitly
  installed/enabled;
- local developer screenshots of this app's own UI only.

Forbidden:

- keylogging;
- recording typed characters;
- recording passwords, forms, messages, clipboard, or page contents;
- covert tracking;
- screen recording or periodic screenshots of user activity;
- using FlaUI to scrape Chrome URLs;
- uploading during acceptance without explicit opt-in.

## Current External Notes

- Administrator rights alone cannot reliably read browser active tab URLs.
  Reliable domain tracking requires browser extension + native messaging, with
  address-bar UI Automation only as an explicit best-effort fallback.
- WPF acceptance must use temp SQLite databases and must not touch the user's
  production/local DB.
- If a future consolidated `artifacts/wpf-check/latest/` package is added, it
  should aggregate the existing WPF acceptance, UI snapshot, Chrome native, and
  RealStart reports without committing PNG artifacts to git.

## WPF Same-Window Browser Navigation Evidence 2026-04-30

- [x] Add TDD coverage for same Chrome HWND/PID navigation: youtube.com -> github.com -> chatgpt.com persists WebSessions without closing the Chrome FocusSession.
- [x] Update TrackingPipeline acceptance to capture before/during/after PNGs for same-window Chrome navigation, second Chrome process/window, Notepad, File Explorer, Stop, Sync, and Settings.
- [x] Verify TrackingPipeline temp SQLite evidence: focus_session rows = 5, web_session rows = 4, sync_outbox rows = 9.
- [x] Verify default browser privacy evidence: domain rows exist for youtube.com, github.com, chatgpt.com, learn.microsoft.com; full URL/page title/page content are not stored.
- [x] Generate consolidated package at `artifacts/wpf-check/latest/`.

Key PNGs:

- `artifacts/wpf-check/latest/01-startup.png`
- `artifacts/wpf-check/latest/02-after-start.png`
- `artifacts/wpf-check/latest/03a-during-chrome-youtube-window.png`
- `artifacts/wpf-check/latest/03b-during-chrome-github-same-window.png`
- `artifacts/wpf-check/latest/03c-during-chrome-chatgpt-same-window.png`
- `artifacts/wpf-check/latest/03d-during-chrome-second-process-docs.png`
- `artifacts/wpf-check/latest/03e-during-notepad-switch.png`
- `artifacts/wpf-check/latest/03f-during-explorer-switch.png`
- `artifacts/wpf-check/latest/04-after-stop.png`
- `artifacts/wpf-check/latest/05-after-sync.png`
- `artifacts/wpf-check/latest/06-settings.png`
- `artifacts/wpf-check/latest/app-sessions-window-titles-visible.png`

## WPF Same-Window Browser Navigation Regression Tests 2026-04-30

- [x] Add coordinator regression test for same Chrome HWND/PID navigation: youtube.com -> github.com -> chatgpt.com.
- [x] Add MainWindow vertical regression test for two same-window browser domain changes before Stop.
- [x] Verify prior domains are persisted as SQLite WebSessions while the Chrome FocusSession remains open.
- [x] Verify Web Sessions grid and Web Focus summary refresh from SQLite before Stop.
- [x] Verify domain-only privacy: URL path/query data is not stored in SQLite or outbox payloads.
- [x] Verify full solution tests, build, coverage, and WPF UI acceptance.
- [x] Latest WPF acceptance artifact: `artifacts/wpf-ui-acceptance/20260430-165524`.

## Chrome Native Messaging Cleanup Failure Evidence 2026-04-30

- [x] Chrome native acceptance reports sandbox Chrome process cleanup failures.
- [x] Chrome native acceptance reports temp profile cleanup failures.
- [x] Chrome native acceptance reports temp work root cleanup failures.
- [x] `manifest.json` includes `cleanupFailures` and refreshed `nativeMessagingSafetyEvidence` after cleanup.
- [x] Latest cleanup-only dry-run: `artifacts/chrome-native-acceptance/20260430-170711`.
