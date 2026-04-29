# Original Product Intent Audit

Updated: 2026-04-29

## Product Intent

Woong Monitor Stack is a Windows + Android daily electronic-device usage
measurement system. It measures metadata about which apps, windows, browser
domains, and mobile apps were used for how long.

It is not an input-monitoring, screen-recording, or covert surveillance app.

The intended product covers:

- Windows foreground process/window tracking.
- Process and window switch logging by time.
- Browser app usage plus browser web/domain sessions when feasible.
- Windows local SQLite storage and outbox sync.
- WPF dashboard with real-time and period statistics.
- Android app usage tracking through UsageStatsManager.
- Android local Room storage and WorkManager sync.
- ASP.NET Core/PostgreSQL integrated storage.
- Windows + Android integrated daily summaries.
- Unit, component, integration, UI, and screenshot/visual review support.

## Current Windows/WPF State

Already present:

- Real foreground window reader using `GetForegroundWindow`,
  `GetWindowThreadProcessId`, process metadata, and window title.
- Last-input idle reader using `GetLastInputInfo`.
- Focus sessionizer and tracking poller primitives.
- SQLite repositories for focus sessions, web sessions, browser raw events, and
  sync outbox.
- Windows sync worker and HTTP sync client.
- Chrome extension/native messaging parser, receiver, ingestion flow, and web
  sessionizer primitives.
- WPF dashboard shell with summary cards, period filters, charts, app sessions,
  web sessions, live events, and settings.
- FlaUI-based local screenshot tool.

Gaps to restore original intent:

- WPF app is still wired to `EmptyDashboardDataSource`, not real SQLite.
- WPF does not expose Start/Stop/Sync Now tracking controls.
- No visible current-activity panel exists for current app/process/window title,
  session duration, tracking status, last persisted session, or last sync
  status.
- `TrackingPoller` is not hosted by the WPF app and is not tied to SQLite plus
  outbox persistence.
- Browser native messaging lacks full host packaging/installation UX and WPF
  connection status.
- Browser web sessions need stronger correlation to the active browser focus
  session over time.
- Snapshot automation is currently screenshot-first and permissive; semantic
  FlaUI acceptance must become the primary UI gate.

## Current Android State

Already present:

- Kotlin + XML/View Android app with ViewBinding.
- Usage Access declaration and settings-navigation entry.
- UsageStatsManager event reader, usage sessionizer, and Room focus sessions.
- WorkManager collection/sync worker classes.
- Android sync outbox, sync client, and daily summary client.
- Dashboard, Sessions, Settings, and Daily Summary activities.
- MPAndroidChart dashboard chart surface.
- Unit, Robolectric, and Espresso tests.

Gaps to restore original intent:

- WorkManager jobs are implemented but not clearly scheduled from app UX/startup.
- Collected app usage sessions do not clearly enqueue sync outbox rows.
- Sync opt-in is visible copy, but needs persisted enforcement.
- Sessions screen still needs real Room-backed data display.
- Android screenshot/device automation support is not documented or scripted.
- UI Automator coverage for Usage Access settings navigation is missing.
- Notification runtime permission UX remains incomplete.
- `android:allowBackup` must be reviewed for local usage metadata.

## Current Server/Schema State

Already present:

- ASP.NET Core Web API.
- EF Core relational schema for devices, focus sessions, web sessions, device
  state sessions, raw events, daily summaries, app families, and app-family
  mappings.
- Device registration, focus/web/raw upload endpoints, date-range statistics,
  and daily summary query.
- Idempotency tests for key upload flows.
- Web session DTO/entity idempotency through `deviceId + clientSessionId`,
  including domain-only retries where `Url` is null.
- Server app family mapper for known Windows/Android app keys.

Gaps to restore original intent:

- Android app usage may remain represented as focus sessions, but the contract
  must document that mapping clearly or add a dedicated app usage DTO/table.
- PostgreSQL/Testcontainers remains the preferred final provider-specific test
  target when Docker is available; current relational idempotency coverage uses
  SQLite fallback plus Npgsql model tests.

## Explicitly Reopened Work

The project is no longer considered complete after this audit. The executable
checklist has been reopened in `total_todolist.md` under the Original Intent
Restoration milestones.

The highest-priority next implementation slice is:

1. Add WPF Start/Stop/Sync UI tests for the required AutomationIds.
2. Add minimal presentation state and XAML controls.
3. Then wire fake tracking pipeline acceptance before real-start validation.
