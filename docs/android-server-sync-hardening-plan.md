# Android Server Sync Hardening Plan

Updated: 2026-05-02

This plan defines what must be true before Android server sync is treated as a
production-ready path. It is intentionally a requirements and acceptance plan,
not an implementation change.

## Current Shape

Existing Android sync names and responsibilities:

- `SharedPreferencesAndroidSyncSettings` stores sync opt-in, server base URL,
  device ID, and the current device-token value. Sync remains off by default.
  It can persist a registered device ID/token pair and clear sync
  configuration.
- `SettingsFragment` exposes server URL/device ID fields and Manual Sync, and
  rejects invalid server URLs before enqueueing sync work.
- `AndroidSyncWorker` gates sync by opt-in and requires `KEY_BASE_URL`,
  `KEY_DEVICE_ID`, and a positive pending limit.
- `AndroidSyncRunnerFactory` creates `AndroidRoomSyncRunner` only when worker
  input has a nonblank device ID and base URL.
- `AndroidOutboxSyncProcessor` uploads pending `focus_session` rows and
  opt-in location-context rows, marks `Accepted` and `Duplicate` as synced, and
  marks item-level `Error` or missing results as failed.
- `AndroidSyncClient` can register a device through `/api/devices/register`
  and posts focus sessions to `/api/focus-sessions/upload` and location
  contexts to `/api/location-contexts/upload`. Upload requests send the
  device-token auth header from Android.
- `SyncContracts.kt` maps Android UsageStats sessions to the shared
  focus-session upload contract and includes Android device-registration DTOs.

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

### Release Blocker Snapshot

Before public Android/server sync release, these items remain blockers:

- Secure Android token storage; SharedPreferences-backed token persistence is
  not release-complete.
- Token rotation/revocation and invalid-token recovery behavior.
- Registration policy/user auth decision for first registration and
  re-registration.
- Visible Android registration/repair UI now exists in Settings; production
  polish, real identity policy, and automatic auth repair still remain.
- Production endpoint discovery/policy, including production copy/configuration
  and local developer labeling.
- Android Play signing and publishing requirements if distribution moves beyond
  internal CI artifacts.

### Device Registration

Before production upload, Android needs a release-complete explicit device
registration flow. The MVP Settings flow can register or repair a device and
persist the server-issued device ID/token pair. Remaining requirements:

- Define how Android obtains or creates a stable local `deviceKey`.
- Improve the visible Settings registration state/copy and handle repair after
  auth-required worker results.
- Make registration idempotent by stable user/platform/device key, matching
  existing server idempotency policy.
- Expose registration state in Settings without implying sync is enabled.
- Never upload outbox rows until sync is enabled and the device is registered.

### Auth And Token Policy

Production sync must not rely only on user-entered `deviceId`. Server-side
token issuance/enforcement is active, and Android can persist the server-issued
token and attach it to upload requests. This is still not release-complete until
secure token storage, token refresh/re-registration, auth repair prompting, and
production endpoint policy are finished.

- Define whether Android uses a device token, user auth token, or both.
- Use the server-issued device token as the current upload authorization
  contract. Upload requests send it as `X-Device-Token`; payloads must not carry
  auth material.
- Store tokens in an Android-appropriate secure store before production use;
  SharedPreferences token persistence is not release-complete secure storage.
- Send auth material through headers, not payload fields. Android focus and
  location upload calls now send `X-Device-Token`; future upload types must
  follow the same contract.
- Do not log tokens, full auth headers, or complete configured base URLs with
  secrets.
- Define token rotation, invalid-token handling, and sign-out/disconnect
  behavior.

### Server Auth Follow-Up Plan

The current server shape issues a device token during registration and enforces
`X-Device-Token` on focus-session, web-session, raw-event, and location-context
uploads. Remaining server-side hardening should be split into small slices:

1. Token rotation and revocation:
   - Add an explicit server operation to rotate a device token.
   - Persist enough verifier metadata to invalidate old tokens without storing
     plaintext tokens.
   - Add tests proving old tokens fail, the new token works, and no existing
     focus/web/raw/location rows are deleted or rewritten.

2. Registration policy and user auth:
   - Decide whether registration is allowed with device credentials only or
     must require a user auth/session token.
   - Keep first registration explicit and visible to the client user.
   - Add tests for unauthenticated registration rejection once user auth exists,
     while preserving idempotent registration for the same user/platform/device
     key.

3. Endpoint filter cleanup:
   - Replace repeated minimal-API auth checks with a shared endpoint filter or
     helper only after current endpoint behavior is covered.
   - Preserve public behavior: missing, invalid, and mismatched tokens return
     `401 Unauthorized` and persist no rows.
   - Keep registration and read-only summary endpoints out of upload token
     enforcement unless a later auth policy explicitly changes them.

4. PostgreSQL validation:
   - Run the token-verifier migration and upload auth tests against
     PostgreSQL/Testcontainers when Docker is available.
   - Verify `DeviceTokenSalt` and `DeviceTokenHash` are present on migrated
     `devices` rows and that upload foreign-key/idempotency behavior still
     matches SQLite/InMemory test expectations.
   - Treat skipped PostgreSQL tests as an open production-readiness check, not
     as proof of provider-specific behavior.

### Server Base URL Validation

Settings now performs server URL validation before Manual Sync can enqueue
work:

- Reject blank values before worker enqueue.
- Require `https://` outside explicit local loopback development endpoints.
- Reject URLs with embedded credentials.
- Surface a user-readable validation error in Settings.
- Keep the current missing-config worker failure for defensive safety.

Remaining URL hardening:

- Normalize trailing slashes consistently.
- Document production endpoint discovery/configuration and local developer
  labeling.

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

- Release-complete server auth/device registration from Android.
- Secure token storage or token rotation.
- Production endpoint discovery.
- Play Store release signing or publishing.
- Required connected/emulator CI for `connectedDebugAndroidTest`; the current
  workflow is manual/optional evidence.
- New Android browser domain tracking.
- Any collection of page content, typed text, screenshots, or other-app UI.

## Test-First Acceptance Checklist

Future implementation should proceed by vertical TDD slices.

- [x] `SharedPreferencesAndroidSyncSettings` or successor keeps sync off by
  default and stores no auth token until registration/auth succeeds.
- [x] Settings rejects invalid production base URLs before enqueueing
  `AndroidSyncWorker`.
- [ ] Local developer mode allows only explicit loopback HTTP endpoints; visible
  nonproduction labeling in Settings remains open.
- [x] Manual Sync with sync off remains local-only and enqueues no worker.
- [x] Manual Sync with sync on but no registered device shows registration
  required and enqueues no worker.
- [x] Successful visible device registration persists a server-issued device ID
  and token without uploading existing outbox rows until sync is enabled.
- [x] `AndroidSyncWorker` sends auth/device token headers through
  `AndroidSyncClient` after registration.
- [x] Auth failure marks sync as configuration/auth required, not as a generic
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
