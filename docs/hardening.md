# Hardening Notes

Updated: 2026-04-29

## Database Migration Review

Current persistence surfaces:

- Windows local SQLite is schema-created by repository code and stores only
  Windows device data.
- Android Room is at database version 2 with the v1 to v2 migration adding
  `sync_outbox`.
- Server PostgreSQL is represented by EF Core entities in
  `MonitorDbContext`; production EF migrations are generated and reviewed in
  `docs/production-migrations.md`.

Release-candidate requirement:

- Review EF Core migrations before production database use.
- Production migration review is documented in `docs/production-migrations.md`.
- Server relational behavior must be tested with a relational provider. See
  `docs/server-test-db-strategy.md` for the current SQLite fallback and the
  PostgreSQL/Testcontainers target strategy.
- Keep local Windows SQLite, Android Room, and server PostgreSQL migrations
  separate; no local database may depend on another device database.
- Verify unique indexes remain in place for idempotency:
  `(UserId, Platform, DeviceKey)`, `(DeviceId, ClientSessionId)`,
  `(DeviceId, ClientSessionId)` for web sessions, `(DeviceId, ClientEventId)`, and
  `(UserId, SummaryDate, TimezoneId)`.
- Verify required server relationships remain in place:
  focus/web/raw/device-state rows reference `devices`, and web-session rows
  reference their focus session through `(DeviceId, FocusSessionId)`.
- Treat `daily_summaries` as a derived table. It may be rebuilt from
  `focus_sessions` and `web_sessions`.

## Raw Event Retention Policy

`raw_events` exist for debugging, replay, and sync analysis. They are not a
long-term behavior archive.

MVP policy:

- Default retention target: 30 days.
- Server production defaults in `appsettings.json` enable
  `RawEventRetention` with `RetentionDays = 30` and a daily
  `Interval = 1.00:00:00`.
- Development config keeps `RawEventRetention:Enabled = false` so local runs do
  not prune debugging rows unexpectedly; production deployment should either
  accept the default or set an explicit environment-specific value.
- Keep derived `focus_sessions`, `web_sessions`, and `daily_summaries` after
  raw event expiry.
- Never store key contents, message contents, form input, passwords, or Android
  global touch coordinates in raw event payloads.
- Apply retention on the server first; client-side retention should be added
  before release-candidate packaging.
- Retention deletes must be explicit scheduled maintenance, not hidden
  collection behavior.

Windows local browser raw events now have a concrete client-side retention
policy:

- `BrowserRawEventRetentionPolicy.Default` keeps 30 days.
- `SqliteBrowserRawEventRepository.DeleteOlderThan(...)` removes only rows
  older than the cutoff and keeps boundary/newer events.
- `ChromeNativeMessageIngestionFlow` can receive a
  `BrowserRawEventRetentionService`; when configured, each browser native
  message ingestion prunes expired local raw browser events using the observed
  event time.

This retention path applies only to local browser raw events. Derived
`web_session` rows remain available for dashboard and sync behavior.

Server retention observability:

- Expected startup/interval logs are `Raw event retention run starting.`,
  `Raw event retention run skipped because retention is disabled.`, and
  `Raw event retention run completed. Deleted {DeletedCount} rows older than
  {CutoffUtc}.`
- Failure logs use `Raw event retention run failed.` at error level and should
  be investigated because raw events are intended to be short-lived debug/sync
  artifacts.
- Operators should watch for repeated failures, unexpectedly high
  `DeletedCount`, or no completion/skip logs after service startup.
- Configure per environment with `RawEventRetention:Enabled`,
  `RawEventRetention:RetentionDays`, `RawEventRetention:Interval`,
  `RawEventRetention:FailureAlertEnabled`,
  `RawEventRetention:FailureAlertAfterConsecutiveFailures`, and
  `RawEventRetention:HighDeleteCountAlertThreshold`.
  Production defaults enable daily 30-day retention; local Development defaults
  keep retention disabled to avoid surprising debug data deletion.
- Production defaults enable retention failure alert policy after 3 consecutive
  failures and flag unusually large single-run deletes at 10,000 raw-event
  rows. Development keeps retention failure alerting disabled while retaining
  the same threshold values for local validation.
- Runtime alert delivery is wired through `IRawEventRetentionAlertSink`; the
  default server sink logs warning-level operational alert metadata only:
  alert kind, run status, cutoff, deleted row count, exception type, and
  exception message.
- Do not log raw-event payload contents while investigating retention. The
  useful operational facts are run status, cutoff, deleted row count, and
  exception type/message.

## Android Local Metadata Backup

The Android manifest sets `android:allowBackup="false"` for MVP. Local Room
usage metadata and sync outbox rows should stay local unless the user explicitly
opts into server sync.

## Android Notification Permission

`POST_NOTIFICATIONS` is declared so the morning summary worker can eventually
post notifications on Android 13+. Settings now shows a notification
permission explanation and an explicit request button. The request is made only
on Android 13+ when the permission has not already been granted.

## Privacy Boundary Architecture Tests

`tests/Woong.MonitorStack.Architecture.Tests` includes privacy guardrails for
the metadata-only product boundary:

- Android manifest checks reject invasive permissions such as Accessibility
  service binding, overlay, camera/microphone, SMS, contacts, broad storage,
  notification listener, input method, and query-all-packages permissions.
- Product source scans reject keylogging hooks, raw keyboard-state APIs,
  clipboard APIs, screen capture APIs, Android Accessibility/text-input hooks,
  and Chrome page-content/history/cookie/capture APIs.
- Chrome extension checks reject content scripts, browser debugger/history/
  cookie/download/clipboard permissions, and tab/desktop capture APIs.
- WPF snapshot tooling checks require screenshots to come from the target app
  window or automation elements, not desktop-wide capture.
