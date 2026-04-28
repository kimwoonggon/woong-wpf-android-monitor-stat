# Production Migration Review

Updated: 2026-04-28

## Generated Migration

- Migration: `20260428131352_InitialCreate`
- Context: `MonitorDbContext`
- Provider: Npgsql / PostgreSQL
- Local tool: `dotnet-ef` 10.0.4 in `dotnet-tools.json`

## Tables

- `devices`
- `focus_sessions`
- `web_sessions`
- `raw_events`
- `daily_summaries`

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
- Before production deployment, run the migration against a real PostgreSQL
  test database or PostgreSQL/Testcontainers environment when Docker is
  available.
