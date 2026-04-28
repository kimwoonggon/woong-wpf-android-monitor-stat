# Release Candidate Checklist

Date: 2026-04-28

Use this checklist for the Milestone 12 release-candidate pass.

## Required Validation

- [ ] `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- [ ] `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- [ ] `dotnet run --project tools\Woong.MonitorStack.Windows.Smoke\Woong.MonitorStack.Windows.Smoke.csproj --no-build`
- [ ] `.\gradlew.bat testDebugUnitTest --no-daemon --stacktrace`
- [ ] `.\gradlew.bat assembleDebug --no-daemon --stacktrace`
- [ ] `.\gradlew.bat assembleDebugAndroidTest --no-daemon --stacktrace`
- [ ] `.\gradlew.bat connectedDebugAndroidTest --no-daemon --stacktrace`

## Manual Smoke

- [ ] WPF app opens Dashboard.
- [ ] WPF Today, 1h, 6h, and 24h filters can be selected.
- [ ] WPF Settings shows visible collection state, opt-in sync state, and sync
  failure status.
- [ ] Windows smoke captures foreground window metadata without collecting key
  contents or form input.
- [ ] Android app opens Dashboard on emulator/device.
- [ ] Android Dashboard shows Today, Yesterday, and 7 days filters.
- [ ] Android Settings shows Usage Access guidance, privacy boundary text,
  local-only sync status, and retryable sync failure status.
- [ ] Android Usage Access Settings action opens the platform settings screen.
- [ ] Android previous-day summary screen opens.

## Privacy And Data Boundaries

- [ ] No global keystroke contents are collected.
- [ ] No passwords, messages, or form input are collected.
- [ ] No Android global touch coordinates are collected.
- [ ] Collection is visible to the user.
- [ ] Sync remains opt-in.
- [ ] Windows SQLite stores only Windows local data.
- [ ] Android Room stores only Android local data.
- [ ] Server/PostgreSQL is the only cross-device integration layer.
- [ ] Raw-event retention policy is documented in `docs/hardening.md`.

## Known Follow-Ups

- [ ] Generate production EF Core migrations before production PostgreSQL use.
- [ ] Repeat Android resource measurements on a physical device.
- [ ] Add longer-running Windows collector profiling once continuous background
  tracking is enabled.
