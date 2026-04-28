# Hardening Notes

Updated: 2026-04-28

## Database Migration Review

Current persistence surfaces:

- Windows local SQLite is schema-created by repository code and stores only
  Windows device data.
- Android Room is at database version 2 with the v1 to v2 migration adding
  `sync_outbox`.
- Server PostgreSQL is represented by EF Core entities in
  `MonitorDbContext`; production EF migrations have not been generated yet.

Release-candidate requirement:

- Generate and review EF Core migrations before production database use.
- Initial production migration review is documented in
  `docs/production-migrations.md`.
- Server relational behavior must be tested with a relational provider. See
  `docs/server-test-db-strategy.md` for the current SQLite fallback and the
  PostgreSQL/Testcontainers target strategy.
- Keep local Windows SQLite, Android Room, and server PostgreSQL migrations
  separate; no local database may depend on another device database.
- Verify unique indexes remain in place for idempotency:
  `(UserId, Platform, DeviceKey)`, `(DeviceId, ClientSessionId)`,
  duplicate-safe web session key, `(DeviceId, ClientEventId)`, and
  `(UserId, SummaryDate, TimezoneId)`.
- Treat `daily_summaries` as a derived table. It may be rebuilt from
  `focus_sessions` and `web_sessions`.

## Raw Event Retention Policy

`raw_events` exist for debugging, replay, and sync analysis. They are not a
long-term behavior archive.

MVP policy:

- Default retention target: 30 days.
- Keep derived `focus_sessions`, `web_sessions`, and `daily_summaries` after
  raw event expiry.
- Never store key contents, message contents, form input, passwords, or Android
  global touch coordinates in raw event payloads.
- Apply retention on the server first; client-side retention should be added
  before release-candidate packaging.
- Retention deletes must be explicit scheduled maintenance, not hidden
  collection behavior.

## Android Local Metadata Backup

The Android manifest sets `android:allowBackup="false"` for MVP. Local Room
usage metadata and sync outbox rows should stay local unless the user explicitly
opts into server sync.

## Android Notification Permission

`POST_NOTIFICATIONS` is declared so the morning summary worker can eventually
post notifications on Android 13+. Settings now shows a notification
permission explanation and an explicit request button. The request is made only
on Android 13+ when the permission has not already been granted.
