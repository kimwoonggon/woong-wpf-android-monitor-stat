# Original Product Intent Audit

Updated: 2026-04-30

## Product Intent

Woong Monitor Stack is a Windows + Android daily electronic-device usage
measurement system. It measures metadata about which apps, windows, browser
domains, and mobile apps were used for how long.

It is not an input-monitoring, screen-recording, or covert surveillance app.

The implemented product covers:

- Windows foreground process/window tracking.
- Process and window switch logging by time.
- Browser app usage plus privacy-aware browser web/domain sessions.
- Windows local SQLite storage and outbox sync.
- WPF dashboard with real-time and period statistics.
- Android app usage tracking through UsageStatsManager.
- Android local Room storage, location-context metadata storage, and WorkManager
  collection/sync scheduling.
- ASP.NET Core/PostgreSQL integrated storage.
- Windows + Android integrated daily summaries.
- Unit, component, integration, UI, and screenshot/visual review support.

## Current Windows/WPF State

Implemented:

- Real foreground window reader using `GetForegroundWindow`,
  `GetWindowThreadProcessId`, process metadata, and privacy-aware window title
  handling.
- Last-input idle reader using `GetLastInputInfo`.
- Focus sessionizer and tracking poller primitives.
- SQLite repositories for focus sessions, web sessions, browser raw events, and
  sync outbox.
- Windows focus/web persistence services that write privacy-safe local rows and
  queue outbox payloads.
- Windows sync worker and HTTP sync client.
- Chrome extension/native messaging parser, receiver, ingestion flow, and web
  sessionizer primitives.
- WPF dashboard shell with Start/Stop/Refresh/Sync, current focus panel,
  summary cards, period filters, charts, app sessions, web sessions, live
  events, settings, and safe privacy/sync defaults.
- Generic Host + DI startup with a dedicated startup service so code-behind
  remains host lifecycle glue.
- FlaUI-based local screenshot/semantic acceptance tooling.

Known environment-bound follow-up:

- Chrome native messaging full headed Chrome acceptance requires a local Chrome
  for Testing or compatible Chrome setup. Registry cleanup and sandbox behavior
  are covered with safe HKCU dry-run tests/scripts.

## Current Android State

Implemented:

- Kotlin + XML/View Android app with ViewBinding.
- Usage Access declaration, onboarding copy, and settings-navigation entry.
- UsageStatsManager event reader, usage sessionizer, and Room focus sessions.
- WorkManager collection and sync workers.
- Android sync outbox, sync client, and daily summary client.
- Dashboard, Sessions, Settings, and Daily Summary activities.
- MPAndroidChart dashboard chart surface.
- Optional location context metadata: off by default, foreground permission
  only, approximate preferred, precise latitude/longitude requires separate
  opt-in, local-first storage, and sync opt-in upload gate.
- Local collection runner for location context snapshots and outbox rows.
- Unit, Robolectric, Espresso/androidTest build, UI Automator smoke coverage,
  and local screenshot script with emulator-backed screenshot artifacts.

Known environment-bound follow-up:

- Physical-device Android resource measurements require an attached physical
  device. Emulator-backed dashboard/settings/sessions/daily-summary screenshots
  are captured in `artifacts/android-ui-snapshots/20260430-091721`.
- Hardware-backed location reader remains a future explicit opt-in slice; the
  current default runtime reader is no-op and does not access GPS hardware.

## Current Server/Schema State

Implemented:

- ASP.NET Core Web API.
- EF Core relational schema for devices, focus sessions, web sessions, device
  state sessions, raw events, location contexts, daily summaries, app families,
  and app-family mappings.
- Server relationship constraints from focus/web/raw/device-state/location rows
  to devices, plus web-session linkage to focus sessions through
  `(DeviceId, FocusSessionId) -> (DeviceId, ClientSessionId)`.
- Device registration, focus/web/raw/location upload endpoints, date-range
  statistics, and daily summary query.
- Idempotency tests for key upload flows.
- Web session DTO/entity idempotency through `deviceId + clientSessionId`,
  including domain-only retries where `Url` is null.
- Server app family mapper for known Windows/Android app keys.
- Integrated daily summary runtime test proving Windows + Android uploads for
  the same user aggregate into one summary.

## Privacy Boundary

Forbidden behavior remains out of scope and unimplemented:

- Keylogging.
- Typed text, password, form, message, clipboard, or browser page-content
  capture.
- Screen recording or periodic screenshots of user activity.
- Covert tracking without visible UI status.
- Android global touch coordinate or text input tracking.

The project measures metadata only: app/window/domain/package/location-context
metadata and durations, under explicit local-first and opt-in sync rules.

## Completion Status

Original product intent is restored for the available Windows development
environment. Android emulator evidence is now captured. The only open
environment-bound item is physical-device Android resource measurement.
