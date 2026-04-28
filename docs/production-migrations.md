# Production Migration Review

Updated: 2026-04-29

## Generated Migration

- Migration: `20260428131352_InitialCreate`
- Migration: `20260428165251_AddFocusSessionWindowMetadata`
- Context: `MonitorDbContext`
- Provider: Npgsql / PostgreSQL
- Local tool: `dotnet-ef` 10.0.4 in `dotnet-tools.json`

## Tables

- `devices`
- `focus_sessions`
- `web_sessions`
- `raw_events`
- `daily_summaries`

## Schema Restoration Migration

`20260428165251_AddFocusSessionWindowMetadata` updates the integrated server
schema for Original Intent Restoration:

- Adds nullable focus-session process/window columns: `ProcessId`,
  `ProcessName`, `ProcessPath`, `WindowHandle`, and `WindowTitle`.
- Changes `web_sessions.Url` and `web_sessions.PageTitle` to nullable so
  domain-only browser privacy mode can sync without storing full URLs or tab
  titles.
- Adds nullable browser capture provenance columns:
  `CaptureMethod`, `CaptureConfidence`, and `IsPrivateOrUnknown`.

## Idempotency Indexes

- `devices`: unique `(UserId, Platform, DeviceKey)`
- `focus_sessions`: unique `(DeviceId, ClientSessionId)`
- `raw_events`: unique `(DeviceId, ClientEventId)`
- `web_sessions`: unique `(DeviceId, FocusSessionId, StartedAtUtc, EndedAtUtc, Url)`
- `daily_summaries`: unique `(UserId, SummaryDate, TimezoneId)`

## Review Notes

- Persisted instants map to PostgreSQL `timestamp with time zone`.
- Local dates map to PostgreSQL `date`.
- `daily_summaries` is still a derived table and can be rebuilt from source
  sessions.
- The migration intentionally belongs only to the server PostgreSQL store. It
  must not be reused by Windows SQLite or Android Room.
- Windows local SQLite handles its own local `focus_session` schema evolution
  inside `SqliteFocusSessionRepository.Initialize`; Android Room migrations
  remain under the Android project.
- Before production deployment, run the migration against a real PostgreSQL
  test database or PostgreSQL/Testcontainers environment when Docker is
  available.
