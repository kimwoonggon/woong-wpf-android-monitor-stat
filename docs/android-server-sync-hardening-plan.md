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

- Android auth-required repair prompting and token refresh/re-registration
  behavior after `401/403`.
- Device revocation and cross-device management behavior.
- Registration policy/user auth decision: who may register a device, whether
  first registration requires a user/session token, and how re-registration is
  authorized.
- Visible Android registration/repair UI now exists in Settings; production
  polish, real identity policy, and automatic auth repair still remain.
- Production endpoint discovery/policy: approved server base URL source,
  production-vs-local environment labeling, loopback-only HTTP exceptions, and
  release behavior when no production endpoint is configured.
- Android Play signing/publishing policy: real `ANDROID_KEYSTORE_*` secrets,
  versioning, Play Console track, artifact retention, and release approval
  requirements if distribution moves beyond internal CI artifacts.

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
token in Android Keystore-backed storage and attach it to upload requests. This
is still not release-complete until token refresh/re-registration, auth repair
prompting, and production endpoint policy are finished.

- Define whether Android uses a device token, user auth token, or both, and
  whether a user/session token is mandatory before first registration or
  re-registration.
- Use the server-issued device token as the current upload authorization
  contract. Upload requests send it as `X-Device-Token`; payloads must not carry
  auth material.
- Store tokens in Android-appropriate secure storage; the current implementation
  keeps ciphertext/IV outside ordinary `woong_monitor_settings` preferences and
  uses Android Keystore AES-GCM for the default runtime token store.
- Send auth material through headers, not payload fields. Android focus and
  location upload calls now send `X-Device-Token`; future upload types must
  follow the same contract.
- Do not log tokens, full auth headers, or complete configured base URLs with
  secrets.
- Define token rotation, invalid-token handling, and sign-out/disconnect
  behavior.

### Secure Token Storage TDD Plan

Current inspected Android shape:

- `android/app/gradle/libs.versions.toml` has no AndroidX Security/Crypto,
  DataStore, Tink, or other secure-storage dependency.
- `SharedPreferencesAndroidSyncSettings` stores sync opt-in, server URL,
  device ID, stable device key, and `device_token` in the normal
  `woong_monitor_settings` SharedPreferences file.
- Existing tests cover observable behavior through `AndroidSyncSettings`,
  `persistRegisteredDevice`, Settings Register/Repair, Manual Sync gating, and
  `AndroidSyncWorker` missing-token failures.

Preferred implementation direction:

- Keep nonsecret sync state in `SharedPreferencesAndroidSyncSettings`.
- Move `deviceToken` behind a small token-store abstraction used by
  `SharedPreferencesAndroidSyncSettings` or its successor:
  `readDeviceToken`, `writeDeviceToken`, `clearDeviceToken`.
- Default production token store should be Android Keystore-backed. Avoid a
  broad new dependency unless it is explicitly accepted after validation.
- Candidate dependency: `androidx.security:security-crypto:1.1.0` provides
  `EncryptedSharedPreferences`, but AndroidX release notes mark these crypto
  APIs deprecated in favor of platform APIs/direct Android Keystore use. Treat
  this as a dependency risk, not the automatic choice.
- Safer long-term option: store an AES-GCM key in Android Keystore and encrypt
  only the device-token value into a private app file or dedicated preferences
  file. This avoids storing plaintext tokens and avoids depending on deprecated
  AndroidX crypto APIs, at the cost of a little more local code and
  instrumentation coverage.

Vertical TDD slices:

1. RED: update `SharedPreferencesAndroidSyncSettingsTest` to prove
   `persistRegisteredDevice` does not write `device-token-secret` into
   `woong_monitor_settings` while `deviceToken()` still returns the token after
   reloading settings. GREEN: introduce an injectable token store and keep a
   fake/in-memory test store for JVM tests.
2. RED: prove `clearSyncConfiguration` clears the secure token store as well as
   ordinary sync fields. GREEN: route token removal through the token store.
3. RED: add a Robolectric or instrumentation contract proving existing
   Settings Register/Repair and Manual Sync behavior still works through the
   public UI/state: registration persists a token, Manual Sync can run after
   registration, missing token still shows registration required. GREEN:
   preserve the `AndroidSyncSettings.deviceToken()` interface.
4. RED: add a focused Android instrumentation test for the production token
   store proving process-recreated settings can read the token and that the
   plaintext token is absent from the normal preferences XML. GREEN: implement
   Keystore-backed storage. If emulator Keystore behavior is flaky, keep this
   as `connectedDebugAndroidTest` evidence and do not claim release readiness
   from Robolectric alone.
5. RED: add migration behavior for any existing plaintext `device_token`: on
   first read or registration repair, copy it into secure storage and remove it
   from `woong_monitor_settings`; never log the token during migration. GREEN:
   implement one-time migration and cleanup.

Test strategy and validation commands:

- JVM/Robolectric behavior gate:
  `cd android; .\gradlew.bat testDebugUnitTest --tests com.woong.monitorstack.settings.* --tests com.woong.monitorstack.sync.AndroidSyncWorkerTest --no-daemon --stacktrace`
- Connected/emulator secure-store gate:
  `cd android; .\gradlew.bat connectedDebugAndroidTest --no-daemon --stacktrace`
- Architecture/docs guardrail: add or update a script/architecture test only
  if it can verify the dependency decision without coupling to private
  implementation details.

Acceptance for closing this blocker:

- No new registration stores `deviceToken` in `woong_monitor_settings`.
- Existing plaintext `device_token` values are migrated or cleared safely.
- `AndroidSyncWorker` and upload clients still receive the server-issued token
  only through the settings/token-store interface.
- Clear/disconnect removes the token from secure storage.
- Tests prove behavior without logging or exposing token values.
- Dependency decision is documented: direct Android Keystore preferred;
  AndroidX `security-crypto` only accepted with an explicit deprecation-risk
  note and version pin.

### Server Auth Follow-Up Plan

The current server shape issues a device token during registration and enforces
`X-Device-Token` on focus-session, web-session, raw-event, and location-context
uploads. Remaining server-side hardening should be split into small slices:

1. Token rotation and revocation:
   - Server token rotation is implemented at
     `POST /api/devices/{deviceId}/token/rotate`: current token required, old
     token invalidated, new token returned, and existing focus/web/raw/location
     rows preserved.
   - Server token revocation is implemented at
     `POST /api/devices/{deviceId}/token/revoke`: current token required, the
     token hash is cleared, future upload/rotation/revoke attempts with the old
     token return `401 Unauthorized`, and existing stored rows are preserved.
   - Server revoked-device repair is implemented through registration: the same
     authenticated owner re-registering the same platform/device key receives
     the same device id with a newly issued token, the old token remains
     unauthorized, and existing rows stay attached to the device. A different
     authenticated user registering the same platform/device key receives a
     separate device and cannot recover the revoked owner's token.
   - Remaining: add cross-device management semantics after user-auth policy is
     decided.

2. Registration policy and user auth:
   - Current strict-mode behavior is option-gated by
     `DeviceRegistrationAuth:RequireAuthenticatedUser`. When enabled,
     registration requires `X-Woong-User-Id`; that authenticated user id
     overrides the payload `userId`.
   - The current release policy has two named modes. Dev/MVP payload mode keeps
     payload `userId` registration available for local/internal compatibility.
     Production strict-auth mode must set
     `DeviceRegistrationAuth:RequireAuthenticatedUser=true`, must use a real
     user/session provider, and must not ship with the `X-Woong-User-Id` header
     stub as the production identity provider.
  - Startup validation now fails production strict-auth startup when
    `DeviceRegistrationAuth:UserIdentityProviderMode=HeaderStub`. HeaderStub
    is the dev/MVP compatibility provider mode and is not a public production
    identity provider.
  - Startup validation also fails any non-`HeaderStub`
    `UserIdentityProviderMode` until the mode is wired to a concrete
    `IRegistrationUserIdentitySource`. Future provider names such as `Oidc`
    must not silently continue using the header stub.
  - `ClaimsPrincipal` mode is the current production-safe server boundary after
    upstream authentication middleware. Production strict-auth startup requires
    `DeviceRegistrationAuth:RequiredAuthenticationScheme`, and a runtime guard
    fails startup when that named scheme has no registered authentication
    handler. This keeps public release blocked until the provider is actually
    wired, not merely described in config.
  - The real user/session provider selection must remain an open release
    blocker until the provider, provisioning path, token/session validation,
     and operational ownership are documented and validated.
   - Token-protected upload/rotation/revoke endpoints use this response policy:
     missing, malformed, revoked, or wrong device tokens return
     `401 Unauthorized`; a valid device token presented with a different
     authenticated user returns `403 Forbidden`; `404 Not Found` is reserved
     for post-auth missing resources such as a race after authorization.
   - Decide whether production registration is allowed with device credentials
     only or must require a real user auth/session token.
   - Define who owns a device record and how a user can revoke or replace an
     Android device.
   - Keep first registration explicit and visible to the client user.
   - Existing tests cover unauthenticated strict-mode registration rejection,
     authenticated-user override, separate devices for separate users with the
     same device key, token theft prevention through payload `userId`, and
     strict-mode `403 Forbidden` for valid-token cross-user upload attempts.

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
- Document production endpoint discovery/configuration, local developer
  labeling, and release behavior when the production endpoint is unset.
- Decide whether release builds allow user-entered endpoints or require a
  pinned/managed endpoint policy.

### Production Endpoint Policy

Release builds must not silently fall back to a local, blank, or example
endpoint. If the production endpoint is unset, Android/server sync remains
disabled, Settings must surface configuration-required status, and queued
workers must fail closed without upload attempts.

Approved endpoint sources, in priority order:

1. A release-managed production endpoint supplied by signed build/release
   configuration. Android reads this from `BuildConfig.PRODUCTION_SYNC_BASE_URL`,
   which is populated from Gradle property `woongProductionSyncBaseUrl` or
   environment variable `WOONG_ANDROID_PRODUCTION_SYNC_BASE_URL`.
2. An explicit advanced/manual configuration path for internal operators or
   testers.
3. Local developer loopback endpoints for development builds and emulator
   validation only.

Release builds may accept user-entered endpoints only as an explicit
advanced/manual configuration path. They must not make arbitrary endpoint entry
look like the default production flow, and they must clearly distinguish
operator/test configuration from the normal local-only sync-off state.

Local developer HTTP endpoints are limited to loopback hosts: `localhost`,
`127.0.0.1`, Android emulator host alias `10.0.2.2`, and `::1`. Any local HTTP
endpoint must be labeled nonproduction.
Non-loopback production sync endpoints require HTTPS, no embedded credentials,
and no broad cleartext network policy.

Current implementation:

- `PRODUCTION_SYNC_BASE_URL` defaults to blank at build time.
- When the production endpoint is blank, local/example, HTTP loopback, or
  otherwise invalid, Android resolves no production endpoint and sync remains
  disabled/fail-closed unless the user has explicitly entered an advanced
  endpoint.
- User-entered advanced endpoints remain explicit and are still validated:
  HTTPS is required outside loopback local development and embedded credentials
  are rejected.
- `localhost`, `127.0.0.1`, `10.0.2.2`, and `::1` HTTP endpoints remain
  local-development only and show visible nonproduction Settings labeling.

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
- Android network security config denies broad cleartext traffic by default and
  allows only explicit local loopback HTTP hosts for developer use.
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
- [x] Local developer mode allows only explicit loopback HTTP endpoints and
  visible nonproduction labeling in Settings.
- [x] Manual Sync with sync off remains local-only and enqueues no worker.
- [x] Manual Sync with sync on but no registered device shows registration
  required and enqueues no worker.
- [x] Successful visible device registration persists a server-issued device ID
  and token without uploading existing outbox rows until sync is enabled.
- [x] `AndroidSyncWorker` sends auth/device token headers through
  `AndroidSyncClient` after registration.
- [x] Auth failure marks sync as configuration/auth required, not as a generic
  retry loop.
- [x] Disconnect revoke `401/403` marks Android sync as auth-required repair
  without clearing local registration or pending outbox rows.
- [x] Device tokens are stored in Android Keystore-backed secure storage, not
  plaintext `woong_monitor_settings` SharedPreferences.
- [x] Existing plaintext `device_token` values are migrated or removed without
  logging token contents.
- [x] Retryable network failures return WorkManager retry and preserve pending
  outbox rows.
- [x] Nonretryable validation/auth failures do not spin indefinitely.
- [x] Repeated focus-session uploads with the same `clientSessionId` are
  treated as idempotent success when the server returns `Duplicate`.
- [x] Repeated location-context uploads with the same `clientContextId` are
  treated as idempotent success when the server returns `Duplicate`.
- [x] Outbox duplicate enqueue never resets a synced row to pending.
- [x] Android sync payload tests prove no forbidden browser URL/path, page
  title, typed text, clipboard, screenshot, or touch-coordinate fields exist.
- [x] Server integration tests cover Android focus-session upload with
  Windows-only fields omitted.
- [x] Settings shows sync/registration/auth state without making sync appear
  enabled by default.
- [x] Production network policy tests reject broad cleartext traffic.

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
