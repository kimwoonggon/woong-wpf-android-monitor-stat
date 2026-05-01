# Android Server Sync Hardening Plan

Updated: 2026-05-02

This plan defines what must be true before Android server sync is treated as a
production-ready path. It is intentionally a requirements and acceptance plan,
not an implementation change.

## Current Shape

Existing Android sync names and responsibilities:

- `SharedPreferencesAndroidSyncSettings` stores sync opt-in, server base URL,
  and device ID. Sync remains off by default.
- `SettingsFragment` exposes server URL/device ID fields and Manual Sync.
- `AndroidSyncWorker` gates sync by opt-in and requires `KEY_BASE_URL`,
  `KEY_DEVICE_ID`, and a positive pending limit.
- `AndroidSyncRunnerFactory` creates `AndroidRoomSyncRunner` only when worker
  input has a nonblank device ID and base URL.
- `AndroidOutboxSyncProcessor` uploads pending `focus_session` rows and
  opt-in location-context rows, marks `Accepted` and `Duplicate` as synced, and
  marks item-level `Error` or missing results as failed.
- `AndroidSyncClient` posts focus sessions to
  `/api/focus-sessions/upload` and location contexts to
  `/api/location-contexts/upload`.
- `SyncContracts.kt` maps Android UsageStats sessions to the shared
  focus-session upload contract.

The shared contract decision remains: Android UsageStats app intervals sync as
`FocusSession` uploads, with `source = android_usage_stats`, Android package
name as `platformAppKey`, stable `clientSessionId`, UTC instants, local date,
timezone ID, and Android-specific omission of Windows process/window fields.

## Privacy Boundary

Android server sync must stay visible, explicit, and opt-in.

Allowed sync data:

- Android package/app usage metadata.
- Focus session start/end UTC instants, duration, local date, timezone, idle
  flag when known, and source.
- Optional location context only when the location feature and required Android
  permissions are enabled.
- Client-generated IDs needed for idempotency and retry.

Forbidden sync data:

- Typed text, passwords, messages, form input, clipboard contents, or global
  touch coordinates.
- Browser page contents, page titles, URL paths, screenshots, screen
  recordings, or other-app UI dumps.
- Android browser domain tracking unless a future explicit safe Android API
  scope is approved.

Sync opt-in controls server upload only. Local UsageStats collection and Room
persistence remain local device behavior and must not silently imply server
sync consent.

## Remaining Hardening Requirements

### Device Registration

Before production upload, Android needs an explicit device registration flow:

- Define how Android obtains or creates a stable local `deviceKey`.
- Register the Android device through the server device-registration endpoint.
- Persist the server-issued `deviceId` only after a successful registration or
  user-approved configuration step.
- Make registration idempotent by stable user/platform/device key, matching
  existing server idempotency policy.
- Expose registration state in Settings without implying sync is enabled.
- Never upload outbox rows until sync is enabled and the device is registered.

### Auth And Token Policy

Production sync must not rely only on user-entered `deviceId`.

- Define whether Android uses a device token, user auth token, or both.
- Treat current blank token behavior as a placeholder only.
- Store tokens in an Android-appropriate secure store before production use.
- Send auth material through headers, not payload fields.
- Do not log tokens, full auth headers, or complete configured base URLs with
  secrets.
- Define token rotation, invalid-token handling, and sign-out/disconnect
  behavior.

### Server Base URL Validation

Current settings trim server base URL. Production sync needs stricter checks:

- Reject blank values before worker enqueue.
- Require `https://` for production builds unless an explicit local developer
  mode allows HTTP loopback.
- Reject URLs with embedded credentials.
- Normalize trailing slashes consistently.
- Surface a user-readable validation error in Settings.
- Keep the current missing-config worker failure for defensive safety.

### Retry And Backoff

WorkManager and outbox retries must be explicit:

- Define retryable statuses: network `IOException`, HTTP 408/429/5xx, and
  transient TLS/connectivity failures.
- Define nonretryable statuses: auth failure, invalid device registration,
  malformed payload, forbidden endpoint, and privacy/config violations.
- Use bounded exponential backoff for scheduled sync work.
- Keep per-item retry count/error state in the outbox.
- Preserve current behavior that item-level `Accepted` and `Duplicate` both
  mark local rows synced.
- Ensure one failed item does not mark unrelated accepted items failed.

### Idempotency

Production sync must be retry-safe:

- `focus_session` uploads use `deviceId + clientSessionId`.
- Location-context uploads use `deviceId + clientContextId`.
- Duplicates returned by the server must be treated as successful completion.
- Outbox enqueue must not reset already-synced duplicate rows back to pending.
- Server and Android tests must cover repeated worker runs with the same client
  IDs.

### TLS And Network Configuration

Production sync expectations:

- HTTPS is required outside local developer mode.
- Android network security config must not allow broad cleartext traffic.
- Certificate errors must fail closed and never be silently bypassed.
- Local emulator/dev endpoints must be explicitly documented as nonproduction.
- Timeout policy should be finite and tested.

### What Is Intentionally Not Done Yet

This hardening slice does not implement:

- Real server auth/device registration from Android.
- Token storage or token rotation.
- Production endpoint discovery.
- Play Store release signing or publishing.
- A connected/emulator CI workflow for `connectedDebugAndroidTest`.
- New Android browser domain tracking.
- Any collection of page content, typed text, screenshots, or other-app UI.

## Test-First Acceptance Checklist

Future implementation should proceed by vertical TDD slices.

- [ ] `SharedPreferencesAndroidSyncSettings` or successor keeps sync off by
  default and stores no auth token until registration/auth succeeds.
- [ ] Settings rejects invalid production base URLs before enqueueing
  `AndroidSyncWorker`.
- [ ] Local developer mode, if added, allows only explicit loopback HTTP
  endpoints and is visibly labeled nonproduction.
- [ ] Manual Sync with sync off remains local-only and enqueues no worker.
- [ ] Manual Sync with sync on but no registered device shows registration
  required and enqueues no worker.
- [ ] Successful device registration persists a server-issued device ID and
  token without uploading existing outbox rows until sync is enabled.
- [ ] `AndroidSyncWorker` sends auth/device token headers through
  `AndroidSyncClient` after registration.
- [ ] Auth failure marks sync as configuration/auth required, not as a generic
  retry loop.
- [ ] Retryable network failures return WorkManager retry and preserve pending
  outbox rows.
- [ ] Nonretryable validation/auth failures do not spin indefinitely.
- [ ] Repeated focus-session uploads with the same `clientSessionId` are
  treated as idempotent success when the server returns `Duplicate`.
- [ ] Repeated location-context uploads with the same `clientContextId` are
  treated as idempotent success when the server returns `Duplicate`.
- [ ] Outbox duplicate enqueue never resets a synced row to pending.
- [ ] Android sync payload tests prove no forbidden browser URL/path, page
  title, typed text, clipboard, screenshot, or touch-coordinate fields exist.
- [ ] Server integration tests cover Android focus-session upload with
  Windows-only fields omitted.
- [ ] Settings shows sync/registration/auth state without making sync appear
  enabled by default.
- [ ] Production network policy tests reject broad cleartext traffic.

## Suggested Validation Commands

Android focused tests:

```powershell
cd android
.\gradlew.bat testDebugUnitTest --tests com.woong.monitorstack.sync.* --tests com.woong.monitorstack.settings.* --no-daemon --stacktrace
```

Android full local build gate:

```powershell
cd android
.\gradlew.bat testDebugUnitTest assembleDebug assembleDebugAndroidTest --no-daemon --stacktrace
```

Server/API contract checks:

```powershell
dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal
dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal
```

Docs check for this plan:

```powershell
git diff --check -- docs/android-server-sync-hardening-plan.md
```
