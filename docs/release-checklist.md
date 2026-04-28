# Release Candidate Checklist

Date: 2026-04-28

Use this checklist for the Milestone 12 release-candidate pass.

## Required Validation

- [x] `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- [x] `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- [x] `dotnet run --project tools\Woong.MonitorStack.Windows.Smoke\Woong.MonitorStack.Windows.Smoke.csproj --no-build`
- [x] `.\gradlew.bat testDebugUnitTest --no-daemon --stacktrace`
- [x] `.\gradlew.bat assembleDebug --no-daemon --stacktrace`
- [x] `.\gradlew.bat assembleDebugAndroidTest --no-daemon --stacktrace`
- [x] `.\gradlew.bat connectedDebugAndroidTest --no-daemon --stacktrace`

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
- [ ] Repeat Android resource measurements on a physical device.
- [x] Add longer-running Windows collector profiling once continuous background
  tracking is enabled.
