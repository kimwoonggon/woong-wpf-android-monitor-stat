# Android App Usage Contract Decision

Updated: 2026-04-29

## Decision

For MVP, Android app usage sync remains represented by the shared
`FocusSession` upload contract. Do not add a separate server
`AppUsageSession` upload contract yet.

Android may keep Room-specific local entities and DAO names that fit the local
implementation, but the server integration fact remains:

```text
Android UsageStats session
  -> local Room focus/app session row
  -> sync_outbox focus_session row
  -> FocusSessionUploadItem
  -> server focus_sessions
  -> daily summary aggregation
```

The Android collection runner now writes the local Room focus session first,
then enqueues a `focus_session` outbox payload for later opt-in sync. The sync
worker still suppresses upload while the persisted sync opt-in setting is off.

## Required Mapping

Android clients should map UsageStats-derived sessions into
`FocusSessionUploadItem` as follows:

- `clientSessionId`: stable Android-generated session id for idempotency.
- `platformAppKey`: Android package name such as `com.android.chrome`.
- `startedAtUtc` / `endedAtUtc` / `durationMs`: UsageStats-derived interval.
- `localDate`: local date derived from `startedAtUtc` and `timezoneId`.
- `timezoneId`: device timezone.
- `isIdle`: usually `false` unless Android later has an explicit inactivity
  state source.
- `source`: `android_usage_stats`.
- `processId`, `processName`, `processPath`, `windowHandle`, `windowTitle`:
  `null`, because these are Windows process/window metadata fields.

## Rationale

`FocusSession` is the shared fact meaning "the user spent time in a specific
app on a specific device." That fits both Windows foreground-window sessions
and Android UsageStats app intervals.

Keeping one server upload contract:

- keeps Windows and Android daily summaries on one aggregation path;
- avoids duplicated server tables for the same product concept;
- preserves local DB separation because Android Room still stays Android-only;
- keeps server idempotency consistent through `deviceId + clientSessionId`;
- lets app-family mapping roll up `chrome.exe` and `com.android.chrome` under
  the same family label.

## Deferred Alternative

A dedicated `AppUsageSession` server contract can be reconsidered later only if
Android gains semantics that no longer fit `FocusSession`, such as richer
UsageEvents diagnostics, permission-state intervals, or platform-specific
quality/confidence fields that would pollute the shared focus session contract.

If that happens, it should be introduced as a new TDD slice with explicit
server migration, DTO tests, Android sync tests, and daily-summary aggregation
tests.
