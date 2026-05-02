# Release Candidate Checklist

Date: 2026-05-02

Use this checklist for the Milestone 12 release-candidate pass.

## Required Validation

- [x] `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- [x] `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- [x] `dotnet run --project tools\Woong.MonitorStack.Windows.Smoke\Woong.MonitorStack.Windows.Smoke.csproj --no-build`
- [x] `.\gradlew.bat testDebugUnitTest --no-daemon --stacktrace`
- [x] `.\gradlew.bat assembleDebug --no-daemon --stacktrace`
- [x] `.\gradlew.bat assembleDebugAndroidTest --no-daemon --stacktrace`
- [x] `.\gradlew.bat connectedDebugAndroidTest --no-daemon --stacktrace`

## Coordinator Release Validation Commands

Use these commands for the next release validation pass. They are listed as
operator checks, not proof that the current workspace has already passed them.

### Android Emulator CI

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\validate-android-emulator-workflow.ps1 -WorkflowPath .github\workflows\android-emulator-manual.yml
dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter AndroidManualEmulatorWorkflowTests -v minimal
```

The GitHub Actions workflow `.github/workflows/android-emulator-manual.yml` is
manual-only (`workflow_dispatch`). Run it only when connected-test emulator
evidence is needed and runner capacity is acceptable.

### Server Migration Bundle

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\build-server-migration-bundle.ps1 -Help
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\build-server-migration-bundle.ps1 -Configuration Release -OutputPath artifacts\server-migrations\dry-run-validation.exe -DryRun
dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter ServerProductionMigrationRunbookTests -v minimal
```

Build a real migration bundle only for a release operator review:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build-server-migration-bundle.ps1 `
  -Configuration Release `
  -OutputPath artifacts\server-migrations\woong-server-migrations.exe
```

### Windows Release

```powershell
dotnet restore Woong.MonitorStack.sln --configfile NuGet.config
dotnet build Woong.MonitorStack.sln -c Release --no-restore -m:1 -v minimal
dotnet test Woong.MonitorStack.sln -c Release --no-restore -m:1 -v minimal
powershell -ExecutionPolicy Bypass -File scripts\package-windows-msix.ps1 -CreateTestCertificate
```

For install validation of a local test MSIX, run the generated installer from an
elevated prompt and use the certificate emitted beside that exact MSIX artifact.

## Manual Smoke

- [x] WPF app opens Dashboard.
- [x] WPF Today, 1h, 6h, and 24h filters can be selected.
- [x] WPF Settings shows visible collection state, opt-in sync state, and sync
  failure status.
- [x] Windows smoke captures foreground window metadata without collecting key
  contents or form input.
- [x] Android app opens Dashboard on emulator/device.
- [x] Android Dashboard shows Today, Yesterday, and 7 days filters.
- [x] Android Settings shows Usage Access guidance, privacy boundary text,
  local-only sync status, and retryable sync failure status.
- [x] Android Usage Access Settings action opens the platform settings screen.
- [x] Android previous-day summary screen opens.

## Privacy And Data Boundaries

- [x] No global keystroke contents are collected.
- [x] No passwords, messages, or form input are collected.
- [x] No Android global touch coordinates are collected.
- [x] Collection is visible to the user.
- [x] Sync remains opt-in.
- [x] Windows SQLite stores only Windows local data.
- [x] Android Room stores only Android local data.
- [x] Server/PostgreSQL is the only cross-device integration layer.
- [x] Raw-event retention policy is documented in `docs/hardening.md`.
- [x] Server raw-event retention config defines production alerting thresholds
  for repeated retention failures and unusually high delete counts while
  keeping Development alerting disabled.

## Validation Evidence

- .NET test/build matrix completed with 0 failures and 0 build warnings.
- Windows smoke captured foreground metadata for `cmd.exe` with idle state
  `False`, without key contents, messages, or form input.
- Android unit tests, debug APK, androidTest APK, and connected tests completed
  on emulator `emulator-5554`.
- Android Usage Access action was manually verified by installing
  `app-debug.apk`, launching `com.woong.monitorstack/.MainActivity`, tapping
  `usageAccessSettingsButton`, and confirming current focus moved to
  `com.android.settings/com.android.settings.spa.SpaActivity`.

## Known Follow-Ups

- [x] Complete dedicated Milestone 4.5: Windows Chrome Extension + Native
  Messaging.
- [x] Generate production EF Core migrations before production PostgreSQL use.
- [x] Accept emulator-backed Android resource measurements as the current
  completion baseline.
  Fresh evidence: `artifacts/android-resource-measurements/20260430-223105`.
- [x] Defer physical-device Android resource measurements to optional future
  hardening because no real Android device is available in this workspace.
- [x] Add longer-running Windows collector profiling once continuous background
  tracking is enabled.

## Public Android/Server Sync Release Blockers

These are still open for any public Android/server sync release and must not be
closed until the owning implementation agents report verified completion:

- [x] Secure Android device-token storage; runtime token storage uses Android
  Keystore-backed AES-GCM and ordinary `woong_monitor_settings` no longer stores
  plaintext `device_token`.
- [x] Server token rotation; current token is required, old token is
  invalidated, new token works, and existing sync rows are preserved.
- [ ] Device revocation and Android invalid-token recovery policy.
  Server-side revoked-device repair semantics are covered: the same
  authenticated owner can re-register the same platform/device key to recover
  the same device id with a new token while old tokens remain unauthorized and
  existing rows are preserved; another authenticated user receives a separate
  device. This remains open until Android invalid-token recovery and public
  cross-device management policy are release-complete.
- [ ] User-auth/registration policy: decide who may register or re-register an
  Android device, whether first registration requires a user/session token, and
  how revocation or replacement works.
  Current server behavior has two explicitly separate modes:
  dev/MVP payload mode and production strict-auth mode. Dev/MVP payload mode
  allows `/api/devices/register` to use the payload `userId` for local and
  internal compatibility only. Production strict-auth mode must set
  `DeviceRegistrationAuth:RequireAuthenticatedUser=true`, must use a real
  user/session provider, and must not treat the current `X-Woong-User-Id`
  header stub as the production identity provider. Startup validation blocks
  production strict-auth startup when
  `DeviceRegistrationAuth:UserIdentityProviderMode=HeaderStub`, which is the
  dev/MVP compatibility provider mode. Startup validation also rejects any
  non-`HeaderStub` `UserIdentityProviderMode` until server code wires that mode
  to a concrete `IRegistrationUserIdentitySource`, so setting `Oidc` or another
  future provider name cannot silently keep using the header stub. A concrete
  production-safe server adapter now exists at
  `DeviceRegistrationAuth:UserIdentityProviderMode=ClaimsPrincipal`; it reads
  the stable user id from the configured authenticated `HttpContext.User`
  claim, defaults to claim type `sub`, ignores the header stub in claims mode,
  and returns no user when that claim is absent. Production strict-auth startup
  now also requires `DeviceRegistrationAuth:RequiredAuthenticationScheme` when
  `ClaimsPrincipal` mode is selected, keeping the mode tied to an explicit
  upstream authentication boundary. A runtime guard also fails production
  strict-auth startup when the configured
  `DeviceRegistrationAuth:RequiredAuthenticationScheme` does not have a
  registered authentication handler behind upstream authentication middleware.
  Until real upstream authentication middleware/provider selection is
  configured and validated,
  this item must remain an open release blocker and the release checklist must
  not be treated as complete.
  Existing policy coverage proves strict mode rejects missing authenticated
  users, authenticated identity overrides payload `userId`, the same device key
  is scoped per authenticated user, payload `userId` cannot steal another
  user's device token, and a valid device token used by another authenticated
  user returns `403 Forbidden`.
- [ ] Production user/session provider selected and validated.
  This must stay unchecked until the release owner selects the real provider
  (for example OIDC, first-party account session, or another approved
  production identity boundary), documents provisioning/rotation/incident
  ownership, configures `ClaimsPrincipal` mode with
  `RequiredAuthenticationScheme` behind real authentication middleware or
  another approved non-`HeaderStub` provider backed by server code, proves the
  named scheme has a registered authentication handler through the runtime
  guard, and reruns strict-auth registration plus token-protected endpoint
  tests with production-equivalent configuration.
- [ ] Production endpoint discovery/policy: approved server base URL source,
  release behavior when unset, local-dev endpoint labeling, and loopback-only
  HTTP exceptions.
  Release builds must not silently fall back to a local, blank, or example
  endpoint. If the production endpoint is unset, sync must remain disabled.
  Release builds may accept a user-entered endpoint only as explicit
  advanced/manual configuration, not as the default production path.
  Android production endpoint source is `BuildConfig.PRODUCTION_SYNC_BASE_URL`,
  populated by Gradle property `woongProductionSyncBaseUrl` or environment
  variable `WOONG_ANDROID_PRODUCTION_SYNC_BASE_URL`. If unset or invalid, sync
  remains disabled/fail-closed; loopback HTTP remains local-development only.
- [ ] Android Play signing/publishing: configure real `ANDROID_KEYSTORE_*`
  secrets, versioning, Play Console track, artifact retention, and release
  approval requirements before distribution beyond internal CI artifacts.
  Tag-based Android release publishing must fail when signing secrets are
  missing and must publish only the signed release APK, never debug,
  androidTest, or unsigned APKs as release assets.
  Required secrets: `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASSWORD`,
  `ANDROID_KEY_ALIAS`, and `ANDROID_KEY_PASSWORD`.
  Before tagging, confirm `versionCode`, `versionName`, approved Play Console
  track, and release approver.
  After the tag release, download the GitHub Release archive, verify
  `woong-monitor-android-release-signed.apk` is the only release APK, compare
  `release-readiness.json` with the artifact and release decision, upload the
  signed APK to the approved Play Console track, and record the GitHub tag,
  artifact name, Play track, release approver, emulator evidence status, and
  emulator evidence path. Set `ANDROID_EMULATOR_EVIDENCE_PATH` before tagging so
  the readiness manifest records the accepted emulator evidence folder.
  The readiness manifest must record `versionCode`, `versionName`, the signed
  APK SHA-256, whether a production sync endpoint was configured, that sync
  default opt-in remains false, that Play publishing is manual, and that
  emulator evidence is required before public promotion.
  unsigned, debug, and androidTest APKs are internal validation artifacts only.
