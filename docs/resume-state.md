# Resume State

Updated: 2026-04-30
## 2026-04-30 Android Emulator Completion Baseline

- Reclassified physical Android device resource measurement as optional future hardening because a real Android device is not available in this workspace.
- Accepted emulator-backed Android UI screenshots and package-scoped resource measurements as the current completion baseline.
- Re-ran `scripts\run-android-ui-snapshots.ps1` on `emulator-5554`; artifact: `artifacts/android-ui-snapshots/20260430-222943`.
- Re-ran `scripts\run-android-resource-measurement.ps1 -DurationSeconds 3` on `emulator-5554`; artifact: `artifacts/android-resource-measurements/20260430-223105`.
- Updated Android/checklist/completion docs so physical-device measurement is optional future hardening, not an open blocker.

## 2026-04-30 Final Validation And Completion Audit Refresh

- Historical note: this validation was superseded by the later 2026-04-30 Cross-Slice Verification below, which passed 411 .NET tests after the WPF repeated-browser, Chrome cleanup, Server raw-event, and Android onboarding slices.
- Reran the final validation matrix after the WPF final-audit slice and Android current-focus wireframe slice were pushed.
- Historical note: this section predates the later emulator-baseline decision. Physical Android device resource measurement is now optional future hardening, not the only external hardware blocker.
- The connected Android target is currently `emulator-5554`; no physical Android device is connected.

Verified:

- `dotnet restore tests\Woong.MonitorStack.Domain.Tests\Woong.MonitorStack.Domain.Tests.csproj --configfile NuGet.config` passed.
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed 402 tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1` passed; latest report shows 91.7% line coverage and 70.7% branch coverage.
- `./gradlew.bat testDebugUnitTest assembleDebug assembleDebugAndroidTest --no-daemon --stacktrace` passed from `android/`.
- `powershell -ExecutionPolicy Bypass -File scripts\run-android-ui-snapshots.ps1` passed with artifact `artifacts/android-ui-snapshots/20260430-153654`.
- `powershell -ExecutionPolicy Bypass -File scripts\run-android-resource-measurement.ps1` passed on emulator with artifact `artifacts/android-resource-measurements/20260430-153804`; later policy accepts emulator resource evidence as the current completion baseline.
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1` passed with artifact `artifacts/wpf-ui-acceptance/20260430-153850`.

## 2026-04-30 Android Current Focus Wireframe Parity Slice

- Audited the launcher fragment UI against the user-provided Android XML skeleton direction and `artifacts/android-ui-flow/woong-monitor-android-ui-flow.figma-import.svg`.
- Picked the highest-impact feasible Android-only mismatch still present after the toolbar/bottom-nav slices: `DashboardFragment` rendered the Current Focus card as a vertical key/value stack while the wireframe shows a compact horizontal runtime card.
- Added RED architecture coverage requiring `fragment_dashboard.xml` to expose `currentFocusRuntimeRow`, app icon placeholder, identity column, and timing column in a horizontal Current Focus card.
- Updated `fragment_dashboard.xml` and `DashboardFragment.kt` so the card shows app icon, app/package identity, and right-aligned session/collection timing from Room-backed dashboard state. No tracking, sync, location, or privacy collection behavior changed.
- Latest emulator evidence: `artifacts/android-ui-snapshots/20260430-152733`; `09-main-shell.png` shows the compact Current Focus card with seeded local test data.

Verified:

- RED first: `dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~AndroidFragmentDashboard_CurrentFocusUsesCompactHorizontalRuntimeCard" -maxcpucount:1 -v minimal` failed on missing `currentFocusRuntimeRow`, then passed after the XML/Kotlin fix.
- `dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~Android" -maxcpucount:1 -v minimal` passed 34 Android architecture tests.
- `.\gradlew.bat testDebugUnitTest assembleDebug assembleDebugAndroidTest --no-daemon --stacktrace` passed from `android/`.
- `powershell -ExecutionPolicy Bypass -File scripts\run-android-ui-snapshots.ps1` passed on emulator `emulator-5554` with artifact `artifacts/android-ui-snapshots/20260430-152733`.

## 2026-04-30 WPF Acceptance Final-Audit Recommendation Evidence Slice

- Audited WPF acceptance/runtime evidence against `total_todolist.md`, `docs/wpf-ui-acceptance-checklist.md`, `docs/runtime-pipeline.md`, and current WPF scripts after Milestone 54.
- Found no remaining WPF implementation TODO, but the root WPF acceptance report did not directly satisfy the checklist requirement to include next recommended fixes/checks.
- Added RED WPF App script coverage requiring the root report to include `## Next Recommended WPF Checks` with a no-open-WPF-runtime-TODO statement, final WPF acceptance/test/build gate guidance, and Android physical-device resource measurement ownership guidance.
- Updated `scripts/run-wpf-ui-acceptance.ps1` so the root acceptance report carries that final-audit recommendation evidence.
- This is a WPF script/test/docs acceptance-evidence slice only. It does not change Android, Android docs/scripts, parked design refs, WPF XAML, SQLite schema, server code, or collected telemetry.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore --filter "FullyQualifiedName~UiAcceptanceScript_RootReportIncludesNextRecommendedWpfChecks" -maxcpucount:1 -v minimal` failed RED on missing root report next-recommended WPF checks, then passed after the report section was added.
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal` passed 136 WPF App tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1` passed with artifact `artifacts/wpf-ui-acceptance/20260430-152612`; root `report.md` includes `## Next Recommended WPF Checks`.
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` was attempted; Domain, Windows, Windows Presentation, WPF App, and Server tests passed, then an Android-owned architecture test failed outside this WPF slice: `AndroidWireframeLayoutTests.AndroidFragmentDashboard_CurrentFocusUsesCompactHorizontalRuntimeCard`.

## 2026-04-30 Android Bottom Navigation Label Visibility Slice

- Installed `mobile-android-design` into `C:\Users\gerard\.agents\skills\mobile-android-design` and read its guidance. The PRD still wins: Android MVP remains Kotlin + XML/View, so only Material/accessibility guidance was applied.
- Reworked the launcher shell bottom navigation from the too-tall 112dp temporary fix to a compact 96dp Material `BottomNavigationView` with a 48dp system-navigation reserve and 144dp fragment-content reserve.
- Kept the temporary overlay label row removed while preserving visible Dashboard, Sessions, Report, and Settings labels in emulator screenshots.
- Latest emulator evidence: `artifacts/android-ui-snapshots/20260430-151005`; `09-main-shell.png` shows readable bottom-navigation labels above the Android system-navigation area.

Verified:

- RED first: `dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~AndroidMainShell_UsesCompactWireframeBottomNavigation|FullyQualifiedName~AndroidMainShell_BottomNavigationKeepsWireframeHeightAndLabelsAboveSystemNavigation" -v minimal` failed on the previous oversized 112dp contract, then passed after the compact label-safe contract was implemented.
- Focused GREEN: `dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~AndroidMainShell_UsesCompactWireframeBottomNavigation|FullyQualifiedName~AndroidMainShell_BottomNavigationKeepsCompactLabelsAboveSystemNavigation" -v minimal` passed.
- `./gradlew.bat testDebugUnitTest assembleDebug assembleDebugAndroidTest --no-daemon --stacktrace` passed from `android/`.
- `powershell -ExecutionPolicy Bypass -File scripts\run-android-ui-snapshots.ps1` passed on the emulator with artifact `artifacts/android-ui-snapshots/20260430-151005`.
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed 400 tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1` passed; latest report shows 91.7% line coverage and 70.7% branch coverage.

## 2026-04-30 WPF Acceptance Snapshot Matrix Evidence Slice

- Audited WPF acceptance artifacts after Milestone 53 and found no remaining WPF implementation TODO, but the root run-configuration evidence did not expose the snapshot mode or viewport-width matrix used by the UI snapshot tool.
- Added RED WPF App script coverage requiring the root acceptance report and manifest run configuration to include `TrackingPipeline` snapshot mode and the `1920,1366,1024` viewport matrix.
- Updated `scripts/run-wpf-ui-acceptance.ps1` so snapshot mode and viewport widths are named once, passed to `Woong.MonitorStack.Windows.UiSnapshots`, and written into both root report and root manifest run configuration evidence.
- This is a WPF script/test/docs acceptance-evidence slice only. It does not change Android, Android scripts, Android docs, parked design refs, WPF XAML, SQLite schema, server code, or collected telemetry.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore --filter "FullyQualifiedName~UiAcceptanceScript_RootRunConfigurationIncludesSnapshotModeAndViewportWidths" -maxcpucount:1 -v minimal` failed RED on missing root snapshot mode/viewport run-configuration evidence, then passed after the report/manifest fields were added.
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal` passed 135 WPF App tests.
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed 399 solution tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1` passed with artifact `artifacts/wpf-ui-acceptance/20260430-144509`; root `report.md` and `manifest.json` include snapshot mode `TrackingPipeline` and viewport widths `1920,1366,1024`.

## 2026-04-30 Android Compact Toolbar Contract Slice

- Closed the remaining launcher toolbar/header TODO against the user-provided Android XML skeleton direction.
- Added RED architecture coverage requiring `activity_main.xml` and `styles.xml` to make the shell toolbar explicit and compact: 56dp `actionBarSize`, matching `topAppBar` minHeight, 16dp title insets, and a 16sp `WmsToolbarTitle`.
- Updated the Android theme and `MaterialToolbar` XML to satisfy that compact toolbar contract without changing privacy, sync, UsageStats, Room, or screenshot telemetry scope.

Verified:

- RED first: `dotnet test tests/Woong.MonitorStack.Architecture.Tests/Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~AndroidMainShell_UsesCompactReadableToolbarTitle" -v minimal` failed on missing compact toolbar contract, then passed after XML/style updates.
- `./gradlew.bat testDebugUnitTest assembleDebug assembleDebugAndroidTest --no-daemon --stacktrace` passed from `android/`.
- `powershell -ExecutionPolicy Bypass -File scripts/run-android-ui-snapshots.ps1` passed on the emulator with artifact `artifacts/android-ui-snapshots/20260430-143723`.
## 2026-04-30 WPF Acceptance Run Configuration Evidence Slice

- Audited WPF acceptance artifacts after Milestone 52 and found no remaining WPF implementation TODO, but the root acceptance report/manifest did not expose the run configuration as a grouped evidence object.
- Added RED WPF App script coverage requiring the root `report.md` and `manifest.json` to include acceptance seconds, server sync opt-in state, and app path run-configuration evidence.
- Updated `scripts/run-wpf-ui-acceptance.ps1` so the root report has a `## Run Configuration` section and the root manifest has a `runConfiguration` object with `seconds`, `allowServerSync`, and `appPath`.
- This is a WPF script/test/docs acceptance-evidence slice only. It does not change Android, Android scripts, Android docs, parked design refs, WPF XAML, SQLite schema, or server code.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore --filter "FullyQualifiedName~UiAcceptanceScript_RootArtifactsIncludeRunConfigurationEvidence" -maxcpucount:1 -v minimal` failed RED on missing root run-configuration evidence, then passed after the report/manifest fields were added.
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal` passed 134 WPF App tests.
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed 398 solution tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1` passed with artifact `artifacts/wpf-ui-acceptance/20260430-143517`; root `report.md` includes `## Run Configuration` and root `manifest.json` includes `runConfiguration` with `allowServerSync: false`.

## 2026-04-30 Android Report Fragment Runtime Wiring Slice

- Treated the user-provided Android XML wireframe skeleton as the launcher shell target and closed the remaining Report tab skeleton gap.
- Added RED Robolectric coverage requiring `MainActivity` Report navigation to load Recent 7 days summary cards and top-app rows from Room-backed focus sessions.
- Wired `ReportFragment` to `DashboardViewModel` and `RoomDashboardRepository` so it renders Active Focus, Daily Avg, Top App, and top-app rows from local Room data.
- Added RED architecture/snapshot coverage requiring `12-main-shell-report.png` in the connected-device screenshot contract.
- Latest emulator evidence: `artifacts/android-ui-snapshots/20260430-142758`; `12-main-shell-report.png` shows the launcher Report tab with Room-backed summary cards and top-app rows.

Verified:

- RED first: `./gradlew.bat testDebugUnitTest --tests "com.woong.monitorstack.MainActivityTest.reportTabLoadsRoomBackedSevenDaySummary" --no-daemon --stacktrace` failed on a zero Report total, then passed after `ReportFragment` loaded Room-backed summary data.
- RED first: `dotnet test tests/Woong.MonitorStack.Architecture.Tests/Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~AndroidUiSnapshotScript_DocumentsFeatureByFeatureScreenshots|FullyQualifiedName~AndroidUiSnapshotScript_WhenDeviceConnected_CapturesExpectedAppScreens" -v minimal` failed on missing `12-main-shell-report.png`, then passed.
- `./gradlew.bat testDebugUnitTest assembleDebug assembleDebugAndroidTest --no-daemon --stacktrace` passed from `android/`.
- `powershell -ExecutionPolicy Bypass -File scripts/run-android-ui-snapshots.ps1` passed on the emulator with artifact `artifacts/android-ui-snapshots/20260430-142758`.
## 2026-04-30 WPF Root Report Privacy Boundary Evidence Slice

- Audited WPF/browser/runtime acceptance after Milestone 51 and found no remaining WPF implementation TODO, but the human-readable root WPF acceptance report omitted two privacy/safety rows already present in the machine-readable root manifest.
- Added RED WPF App script coverage requiring the root `report.md` Privacy Boundary section to include server sync disabled unless explicitly allowed and temp SQLite-only acceptance evidence.
- Updated `scripts/run-wpf-ui-acceptance.ps1` so the root report now mirrors the manifest privacy boundary for no keystrokes, no product screen telemetry, local-only app UI screenshots, explicit server sync opt-in, and temp SQLite databases.
- This is a WPF script/test/docs acceptance-evidence slice only. It does not change Android, Android scripts, Android docs, parked design refs, WPF XAML, SQLite schema, or server code.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore --filter "FullyQualifiedName~UiAcceptanceScript_RootReportIncludesCompletePrivacyBoundaryEvidence" -maxcpucount:1 -v minimal` failed RED on the missing root report privacy rows, then passed after the report lines were added.
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal` passed 133 WPF App tests.
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed 397 solution tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1` passed with artifact `artifacts/wpf-ui-acceptance/20260430-141948`; root `report.md` includes explicit server sync opt-in and temp SQLite privacy boundary rows.

## 2026-04-30 WPF Acceptance Root Manifest Privacy Boundary Slice

- Audited WPF acceptance evidence after Milestone 50 and found the root manifest summarized child artifacts but did not carry the privacy boundary that the root report states.
- Added RED WPF App script coverage requiring the root `manifest.json` generated by `scripts/run-wpf-ui-acceptance.ps1` to include machine-readable privacy boundary evidence.
- Updated the WPF UI acceptance script so the root manifest includes `privacyBoundary` rows for no keystrokes, no product screen telemetry, screenshots as local app-UI artifacts, sync disabled unless explicitly allowed, and temp SQLite databases only.
- This is a WPF script/test/docs audit-evidence slice only. It does not change Android, Android scripts, Android docs, parked design refs, WPF XAML, SQLite schema, or server code.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore --filter "FullyQualifiedName~UiAcceptanceScript_RootManifestIncludesPrivacyBoundaryEvidence" -maxcpucount:1 -v minimal` failed RED on missing root manifest `privacyBoundary`, then passed after the manifest evidence rows were added.
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal` passed 132 WPF App tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed 396 solution tests.
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1` passed with artifact `artifacts/wpf-ui-acceptance/20260430-141235`; root `manifest.json` includes the `privacyBoundary` rows.

## 2026-04-30 Android Settings Fragment Runtime Wiring Slice

- Treated the user-provided Android XML wireframe skeleton as the target shape for the launcher Settings tab, not a placeholder include stack.
- Added RED Robolectric coverage requiring `MainActivity` Settings navigation to expose runtime Usage Access, privacy boundary, local-only sync, notification, and optional location controls.
- Replaced `fragment_settings.xml` placeholder includes with runtime cards and wired `SettingsFragment` to `UsageAccessSettingsIntentFactory`, `NotificationPermissionController`, `SharedPreferencesAndroidLocationSettings`, and `LocationPermissionController`.
- Added RED architecture/snapshot coverage requiring a dedicated `11-main-shell-settings.png` shell screenshot and requiring `06-settings-location-permission.png` to scroll to the actual Location context card.
- Latest emulator evidence: `artifacts/android-ui-snapshots/20260430-140141`; `11-main-shell-settings.png` shows the launcher Settings tab, and `06-settings-location-permission.png` shows location context off by default plus disabled precise latitude/longitude and permission controls.

Verified:

- RED first: `./gradlew.bat testDebugUnitTest --tests "com.woong.monitorstack.MainActivityTest.settingsTabShowsRuntimePrivacySyncAndLocationControls" --no-daemon --stacktrace` failed on missing SettingsFragment runtime controls, then passed.
- RED first: `dotnet test tests/Woong.MonitorStack.Architecture.Tests/Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~AndroidUiSnapshotScript_DocumentsFeatureByFeatureScreenshots|FullyQualifiedName~AndroidUiSnapshotScript_WhenDeviceConnected_CapturesExpectedAppScreens" -v minimal` failed on missing `11-main-shell-settings.png`, then passed.
- RED first: `dotnet test tests/Woong.MonitorStack.Architecture.Tests/Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~AndroidSnapshotCapture_ScrollsSettingsLocationSectionForLocationScreenshot" -v minimal` failed on missing settings location scroll evidence, then passed.
- `./gradlew.bat testDebugUnitTest assembleDebug assembleDebugAndroidTest --no-daemon --stacktrace` passed from `android/`.
- `powershell -ExecutionPolicy Bypass -File scripts/run-android-ui-snapshots.ps1` passed on the emulator with artifact `artifacts/android-ui-snapshots/20260430-140141`.
## 2026-04-30 WPF Acceptance Root Manifest Slice

- Audited WPF acceptance evidence after Milestone 49 and found no incomplete WPF runtime TODO, but the composed WPF acceptance run still lacked a machine-readable root manifest tying RealStart and TrackingPipeline child artifacts together.
- Added RED WPF App script coverage requiring `scripts/run-wpf-ui-acceptance.ps1` to write a root `manifest.json` with RealStart report/manifest paths, `realStartEvidence`/`realStartSafetyEvidence` names, and TrackingPipeline snapshot report/manifest/prompt paths.
- Updated the WPF UI acceptance script to write that root `manifest.json` beside the root `report.md` before copying the run to `artifacts/wpf-ui-acceptance/latest`.
- This is a WPF script/test/docs audit-evidence slice only. It does not change Android, Android scripts, Android docs, parked design refs, WPF XAML, SQLite schema, or server code.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore --filter "FullyQualifiedName~UiAcceptanceScript_RootManifestSummarizesChildEvidenceArtifacts" -maxcpucount:1 -v minimal` failed RED on missing root `manifest.json`, then passed after the composed manifest writer was added.
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal` passed 131 WPF App tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed 394 solution tests.
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1` passed with artifact `artifacts/wpf-ui-acceptance/20260430-134900`; root `manifest.json` summarizes RealStart and TrackingPipeline child evidence artifacts.

## 2026-04-30 Android Chart Axis And App Label Slice

- Treated the user-provided Android XML wireframe skeleton as the visual target and fixed the latest mismatch where charts and session rows looked like debug/package-name output.
- Added RED Android repository tests requiring Room-backed dashboard and sessions rows to expose user-facing app labels such as `Chrome` and `Slack` while preserving package names as secondary metadata.
- Added RED architecture tests requiring the shared focus-session row to include a primary `sessionAppNameText` and enough row height for app name, package, time range, state, and duration.
- Added `AppDisplayNameFormatter` and wired Dashboard/Sessions repositories plus Activity/Fragment adapters so app labels are primary and package names remain visible metadata.
- Added `DashboardChartConfigurator` and tests so MPAndroidChart hour/minute/app axes use `09`, `10`, `60m`, `Chrome`, `YouTube`, and `Slack` instead of decimal placeholder labels.
- Repaired Android snapshot scrolling with descendant coordinate mapping so `03-dashboard-charts.png` captures the actual chart section.
- Latest emulator evidence: `artifacts/android-ui-snapshots/20260430-133732`; charts show human-readable axes, Dashboard/Sessions rows show app labels first, and row state text is no longer clipped.

Verified:

- RED first: `.\gradlew.bat testDebugUnitTest --tests "com.woong.monitorstack.dashboard.RoomDashboardRepositoryTest" --tests "com.woong.monitorstack.sessions.RoomSessionsRepositoryTest" --no-daemon --stacktrace` failed on missing `topAppName`/`appName`, then passed.
- RED first: `dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~AndroidFocusSessionRowLayout_SeparatesPackageTimeDurationAndState" -maxcpucount:1 -v minimal` failed on missing `sessionAppNameText`, then passed.
- RED first: `dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~AndroidFocusSessionRowLayout_UsesReadableHeightForAppPackageTimeAndState" -maxcpucount:1 -v minimal` failed on missing readable row height, then passed.
- `.\gradlew.bat testDebugUnitTest assembleDebug assembleDebugAndroidTest --no-daemon --stacktrace` passed from `android/`.
- `powershell -ExecutionPolicy Bypass -File scripts\run-android-ui-snapshots.ps1` passed on the emulator with artifact `artifacts/android-ui-snapshots/20260430-133732`.
- Full Android verification passed: `.\gradlew.bat testDebugUnitTest assembleDebug assembleDebugAndroidTest --no-daemon --stacktrace`.
- Full `.NET` verification passed: `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed 393 tests.
- Full `.NET` build passed: `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` with 0 warnings and 0 errors.
- Coverage passed: `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`; latest report shows 91.7% line coverage and 70.7% branch coverage.
## 2026-04-30 WPF Acceptance Root Report RealStart Links Slice

- Audited WPF acceptance evidence after Milestone 48 and found no incomplete WPF runtime TODO, but the composed root WPF acceptance report did not link the deeper RealStart report/manifest evidence.
- Added RED WPF App script coverage requiring `scripts/run-wpf-ui-acceptance.ps1` to expose `real-start-report.md`, `real-start-manifest.json`, `realStartEvidence`, and `realStartSafetyEvidence` in the root report.
- Updated the WPF UI acceptance script so `report.md` includes a `## RealStart Evidence Artifacts` section pointing to the generated RealStart report and manifest.
- This is a WPF script/test/docs audit-evidence slice only. It does not change Android, Android scripts, Android docs, parked design refs, WPF XAML, SQLite schema, or server code.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore --filter "FullyQualifiedName~UiAcceptanceScript_RootReportLinksRealStartEvidenceArtifacts" -maxcpucount:1 -v minimal` failed RED on missing RealStart report/manifest links, then passed after the root report section was added.
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal` passed 130 WPF App tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed 391 solution tests.
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1` passed with artifact `artifacts/wpf-ui-acceptance/20260430-131811`; root `report.md` links the RealStart report/manifest and names `realStartEvidence` plus `realStartSafetyEvidence`.

## 2026-04-30 Android Compact Shell And Location Context Slice

- Treated the user-provided Android XML wireframe skeleton as the immediate launcher-shell target after screenshot review showed the prior shell fix had made bottom navigation too large.
- Added RED architecture coverage requiring `activity_main.xml` to use compact 72dp `BottomNavigationView` structure and to remove the temporary oversized overlay label row.
- Corrected `activity_main.xml` and `WmsBottomNavLabel` back toward the skeleton: `FragmentContainerView` reserves 72dp, the Material bottom navigation is 72dp, and labels come from the navigation component instead of a custom overlay row.
- Added RED architecture coverage requiring `DashboardFragment` to expose local Room-backed optional location context with latitude/longitude, labeled coordinate strings, and explicit ordering so period filters remain directly after summary cards.
- Updated `DashboardFragment` and `DashboardActivity` to render location context as labeled `Latitude`, `Longitude`, `Accuracy`, and `Captured` rows while preserving the safe off-by-default text in XML defaults.
- Moved the optional fragment `Location context` card after the period filter so the dashboard follows the user-provided skeleton flow: status/current focus, summary cards, period filters, optional location context, charts, lists.
- Latest emulator evidence: `artifacts/android-ui-snapshots/20260430-130812`; `09-main-shell.png` shows compact shell plus period filters before location context, and `02-dashboard-summary-location.png` shows labeled latitude/longitude values from seeded local test data.
- Remaining Android UI gaps: chart axes still show decimal placeholder-style labels in MPAndroidChart screenshots, Sessions rows still prioritize package names, and Report/Settings fragments still need runtime wiring.

Verified:

- RED first: `dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~AndroidMainShell_UsesCompactWireframeBottomNavigation" -maxcpucount:1 -v minimal` failed on the old oversized shell, then passed.
- RED first: `dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~AndroidFragmentDashboard_ShowsOptionalLocationContextFromRoomState" -maxcpucount:1 -v minimal` failed on missing labeled coordinate resources, then passed.
- RED first: `dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~AndroidFragmentDashboard_KeepsPeriodFiltersBeforeOptionalLocationContext" -maxcpucount:1 -v minimal` failed on old location-before-period ordering, then passed.
- `.\gradlew.bat testDebugUnitTest assembleDebug assembleDebugAndroidTest --no-daemon --stacktrace` passed from `android/`.
- `powershell -ExecutionPolicy Bypass -File scripts\run-android-ui-snapshots.ps1` passed on the emulator with artifact `artifacts/android-ui-snapshots/20260430-130812`.
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed 390 solution tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1` passed; coverage summary reports 91.7% line coverage and 70.7% branch coverage.
## 2026-04-30 WPF RealStart Safety Evidence Slice

- Audited WPF/browser/runtime acceptance gaps after the Chrome sandbox evidence slice and found RealStart artifacts proved persistence but not the safety boundary as a grouped report/manifest section.
- Added RED WPF App/tool coverage requiring RealStart `real-start-report.md` and `real-start-manifest.json` to include dedicated safety evidence.
- Updated `Woong.MonitorStack.Windows.RealStartAcceptance` with `realStartSafetyEvidence` rows for the explicit local SQLite DB, test-only device id, server sync opt-in state, and launched-process-scoped cleanup.
- This is a WPF tool/test/docs acceptance evidence slice only. It does not change Android, Android scripts, Android docs, parked design refs, WPF XAML, SQLite schema, or server code.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore --filter "FullyQualifiedName~RealStartAcceptanceTool_ReportAndManifestIncludeSafetyEvidence" -maxcpucount:1 -v minimal` failed RED on missing `BuildRealStartSafetyEvidence`, then passed after grouped RealStart safety evidence was added.
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal` passed 129 WPF App tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed 389 solution tests.
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1` passed with artifact `artifacts/wpf-ui-acceptance/20260430-130120`; `real-start-report.md` and `real-start-manifest.json` include `realStartSafetyEvidence` rows as Pass.
## 2026-04-30 Chrome Native Messaging Sandbox Evidence Slice

- Audited Chrome/native messaging acceptance safety coverage and found the sandbox behavior was implemented but not exposed as grouped report/manifest evidence.
- Added RED Windows browser script coverage requiring `nativeMessagingSafetyEvidence` rows in `scripts/run-chrome-native-message-acceptance.ps1`.
- Updated the Chrome native messaging acceptance script so `report.md` includes `## Sandbox Safety Evidence` and `manifest.json` includes `nativeMessagingSafetyEvidence` rows.
- Evidence rows cover the sandboxed Chrome profile, user Chrome preservation, scoped HKCU test host, temp acceptance DB, and cleanup restore/remove behavior.
- This is a Windows browser/native messaging script/test/docs slice only. It does not change Android, Android scripts, Android docs, parked design refs, WPF UI XAML, SQLite schema, or server code.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.Tests\Woong.MonitorStack.Windows.Tests.csproj --no-restore --filter "FullyQualifiedName~AcceptanceScript_ReportAndManifestIncludeGroupedSandboxSafetyEvidence" -maxcpucount:1 -v minimal` failed RED on missing `New-NativeMessagingSafetyEvidence`, then passed after grouped safety evidence was added.
- `dotnet test tests\Woong.MonitorStack.Windows.Tests\Woong.MonitorStack.Windows.Tests.csproj --no-restore --filter "FullyQualifiedName~ChromeNativeMessagingAcceptanceScriptTests" -maxcpucount:1 -v minimal` passed 13 browser acceptance script tests.
- `powershell -ExecutionPolicy Bypass -File scripts\run-chrome-native-message-acceptance.ps1 -CleanupOnly -DryRun` passed and generated `artifacts/chrome-native-acceptance/20260430-125155` with `nativeMessagingSafetyEvidence` in report and manifest.
- `dotnet test tests\Woong.MonitorStack.Windows.Tests\Woong.MonitorStack.Windows.Tests.csproj --no-restore -maxcpucount:1 -v minimal` passed 89 Windows tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed 388 solution tests.
## 2026-04-30 Android Main Shell Bottom Navigation Readability Slice

- Added RED architecture coverage requiring the launcher shell to reserve a larger bottom-navigation area above system navigation and expose visible Dashboard/Sessions/Report/Settings labels.
- Updated `activity_main.xml` bottom-navigation sizing, fragment bottom margin, item icon/text appearance settings, and an explicit label row so emulator screenshots no longer show icon-only navigation.
- Added `WmsBottomNavLabel` styling with readable 14sp primary text.
- Latest emulator evidence: `artifacts/android-ui-snapshots/20260430-124333`; `10-main-shell-sessions.png` now shows visible Dashboard, Sessions, Report, and Settings labels.

Verified:

- `dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~AndroidMainShell_KeepsBottomNavigationReadableAboveSystemNavigation" -maxcpucount:1 -v minimal` failed RED first, then passed after shell layout/style updates.
- `.\gradlew.bat testDebugUnitTest assembleDebug assembleDebugAndroidTest --no-daemon --stacktrace` passed from `android/`.
- `powershell -ExecutionPolicy Bypass -File scripts\run-android-ui-snapshots.ps1` passed on the emulator with artifact `artifacts/android-ui-snapshots/20260430-124333`.
## 2026-04-30 WPF RealStart Local DB Evidence Slice

- Audited WPF runtime acceptance evidence after Milestone 44 and found the
  RealStart path proved local DB persistence/outbox behavior only through
  console output.
- Added RED WPF App/tool coverage requiring durable RealStart acceptance
  artifacts.
- Updated `Woong.MonitorStack.Windows.RealStartAcceptance` to write
  `real-start-report.md` and `real-start-manifest.json` beside the temp
  acceptance DB.
- The artifacts include `realStartEvidence` rows for `focus_session`
  persistence, `sync_outbox` queueing, readable latest app/process text, and
  server sync remaining disabled unless explicitly allowed.
- This is a WPF tool/test/docs acceptance evidence slice only. It does not
  change Android, Android scripts, Android docs, parked design refs, product
  telemetry collection policy, SQLite schema, or server code.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore --filter "FullyQualifiedName~RealStartAcceptanceTool_WritesLocalDbPersistenceEvidenceArtifacts" -maxcpucount:1 -v minimal` failed RED on missing `real-start-report.md`, then passed after the RealStart artifact writer was added.
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal` passed 128 WPF App tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed 386 solution tests.
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1` passed with artifact `artifacts/wpf-ui-acceptance/20260430-123830`; `real-start-report.md` and `real-start-manifest.json` include all RealStart local DB evidence rows as Pass.
## 2026-04-30 Android Sessions Fragment Room Wiring Slice

- Added RED architecture coverage requiring `SessionsFragment` to use the
  local Room-facing `RoomSessionsRepository`, expose an empty state, and submit
  rows to a RecyclerView.
- Updated `SessionsFragment` so the launcher shell Sessions tab reads persisted
  Room focus sessions through `MonitorDatabase.getInstance(...).focusSessionDao()`.
- Added `emptySessionsText` to `fragment_sessions.xml` for a safe no-data
  state.
- Extended Android screenshot automation and script evidence with
  `10-main-shell-sessions.png`, captured after selecting the Sessions bottom
  navigation item.
- Updated Android UI docs to record that the user-provided XML wireframe
  skeleton is the target shape, not merely a loose reference. Current shell
  gaps remain: toolbar/header sizing, bottom-navigation readability, app-label
  display, Report fragment wiring, and Settings fragment wiring.

Verified:

- `dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~AndroidSessionsFragment_UsesRoomRepositoryAndEmptyState" -maxcpucount:1 -v minimal` failed RED first, then passed after implementation.
- `dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~AndroidUiSnapshotScript_DocumentsFeatureByFeatureScreenshots|FullyQualifiedName~AndroidUiSnapshotScript_WhenDeviceConnected_CapturesExpectedAppScreens" -maxcpucount:1 -v minimal` failed RED first, then passed after adding `10-main-shell-sessions.png`.
- `.\gradlew.bat testDebugUnitTest assembleDebug assembleDebugAndroidTest --no-daemon --stacktrace` passed from `android/`.
- `powershell -ExecutionPolicy Bypass -File scripts\run-android-ui-snapshots.ps1` passed on the emulator with artifact `artifacts/android-ui-snapshots/20260430-121642`.
## 2026-04-30 WPF Minimum Size Reachability Evidence Slice

- Added RED WPF App acceptance-tool coverage requiring grouped minimum-size reachability evidence in both `report.md` and `manifest.json`.
- Updated `Woong.MonitorStack.Windows.UiSnapshots` so the 1024x768 viewport records `minimumSizeReachabilityEvidence` rows for Header, ControlBar, CurrentFocus, App Sessions, Web Sessions, Live Events, and Settings using stable AutomationIds and screenshot references.
- Updated the WPF UI acceptance checklist with the required minimum-size AutomationIds, semantic check, and supporting screenshots.
- This is a WPF App/tool/docs acceptance evidence slice only. It does not change Android, Android scripts, Android docs, parked design refs, product telemetry collection policy, SQLite schema, or server code.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore --filter "FullyQualifiedName~UiSnapshotsTool_ReportAndManifestIncludeMinimumSizeReachabilityEvidence" -maxcpucount:1 -v minimal` failed RED on missing `## Minimum Size Reachability Evidence`, then passed after the snapshot tool update.
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal` passed 127 WPF App tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1` passed with artifact `artifacts/wpf-ui-acceptance/20260430-122205`; the latest snapshot report/manifest include grouped minimum-size reachability evidence and all 1024x768 rows are Pass.
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed 384 solution tests, including `WpfUiAcceptanceScriptTests.UiSnapshotsTool_ReportAndManifestIncludeMinimumSizeReachabilityEvidence`.
## 2026-04-30 WPF Browser Domain Privacy Evidence Slice

- Added RED WPF App acceptance-tool coverage requiring grouped browser-domain privacy evidence in both `report.md` and `manifest.json`.
- Updated `Woong.MonitorStack.Windows.UiSnapshots` so TrackingPipeline acceptance records `browserDomainPrivacyEvidence` rows proving `github.com` and `chatgpt.com` domain-only web sessions persist while full URL values, page title values, and content-like storage columns remain absent.
- Kept existing generic checks, SQLite evidence, and `databaseEvidence` manifest shape for backward compatibility.
- This is a WPF App/tool acceptance evidence slice only. It does not change Android, Android scripts, Android docs, parked design refs, product telemetry collection policy, SQLite schema, or server code.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore --filter "FullyQualifiedName~UiSnapshotsTool_ReportAndManifestIncludeGroupedBrowserDomainPrivacyEvidence" -maxcpucount:1 -v minimal` failed RED on missing `## Browser Domain Privacy Evidence`, then passed after the snapshot tool update.
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore --filter "FullyQualifiedName~TrackingPipelineMode_StartPollStop_PersistsFocusWebSessionsAndFakeSyncsOutbox" -maxcpucount:1 -v minimal` failed RED on full URL/page title persistence in fake TrackingPipeline web sessions, then passed after domain-only fake WebSessions were used.
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal` passed 126 WPF App tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1` passed with artifact `artifacts/wpf-ui-acceptance/20260430-121336`; the latest snapshot report/manifest include grouped browser-domain privacy evidence and all privacy rows are Pass.
## 2026-04-30 Android Dashboard Fragment Room Wiring Slice

- Added RED architecture tests proving `fragment_dashboard.xml` does not hardcode fake runtime data such as `com.android.chrome`, `00:12:31`, or `09:25`.
- Added RED architecture tests requiring bindable fragment Dashboard value IDs plus `DashboardFragment` use of `MonitorDatabase`, `RoomDashboardRepository`, and `DashboardViewModel`.
- Updated `DashboardFragment` to load local Room data through the existing Dashboard repository/ViewModel and render current app/package, session duration, last collected/DB-write text, Active/Screen/Idle totals, and recent sessions.
- Latest connected-emulator screenshot artifact: `artifacts/android-ui-snapshots/20260430-120317`; `09-main-shell.png` now shows seeded Room-backed values including `com.android.chrome`, `2h 0m`, `2h 10m`, and `10m`.
- Remaining Android shell gap: Sessions, Report, and Settings fragments still need runtime wiring equivalent to the existing Activity screens.

Verified:

- `dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~AndroidFragmentDashboard_DoesNotHardcodeFakeRuntimeData|FullyQualifiedName~AndroidFragmentDashboard_ExposesRoomBackedValueIds" -maxcpucount:1 -v minimal` failed RED then passed.
- `.\gradlew.bat testDebugUnitTest --tests "com.woong.monitorstack.MainActivityTest" --no-daemon --stacktrace` passed.
- `.\gradlew.bat testDebugUnitTest assembleDebug assembleDebugAndroidTest --no-daemon --stacktrace` passed from `android/`.
- `powershell -ExecutionPolicy Bypass -File scripts\run-android-ui-snapshots.ps1` passed on the emulator and generated `09-main-shell.png` with Room-backed data.
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed 381 `.NET` tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1` passed; latest coverage summary reports 91.7% line coverage and 70.7% branch coverage.

## 2026-04-30 WPF SQLite Runtime Evidence Slice

- Audited WPF acceptance runtime proof after control action evidence and found SQLite persistence/outbox counts were available only as generic DB evidence.
- Added RED WPF App acceptance-tool coverage requiring grouped SQLite runtime evidence in both `report.md` and `manifest.json`.
- Updated `Woong.MonitorStack.Windows.UiSnapshots` so `report.md` includes `## SQLite Runtime Evidence` and `manifest.json` includes `sqliteRuntimeEvidence` rows for `focus_session`, `web_session`, and `sync_outbox` with expected row count, actual row count, and status.
- Preserved existing `databaseEvidence` and generic PASS/FAIL checks for backward compatibility.
- This is a WPF App/tool acceptance evidence slice only. It does not change Android, Android scripts, Android docs, parked design refs, product telemetry collection, SQLite schema, or server code.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore --filter "FullyQualifiedName~UiSnapshotsTool_ReportAndManifestIncludeGroupedSqliteRuntimeEvidence" -maxcpucount:1 -v minimal` failed RED on missing `## SQLite Runtime Evidence`, then passed after the snapshot tool update.
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal` passed 125 WPF App tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1` passed with artifact `artifacts/wpf-ui-acceptance/20260430-120341`; the latest snapshot report/manifest include grouped SQLite runtime evidence.

## 2026-04-30 Android Main Shell Wireframe Correction Slice

- Added RED architecture and Robolectric tests proving `MainActivity` is a real XML/View shell instead of redirecting immediately to `DashboardActivity`.
- Added `activity_main.xml` with `MaterialToolbar`, `FragmentContainerView`, and `BottomNavigationView`, plus menu/color resources.
- Added fragment XML skeletons for Splash, Permission onboarding, Dashboard, Sessions, App detail, Report, and Settings from the provided Android wireframe direction.
- Added minimal XML/View fragments for Dashboard, Sessions, Report, Settings, Splash, and Permission onboarding while keeping existing Activity screens available as the current Room-backed runtime surfaces.
- Fixed Android summary metric cards to use rounded Material card containers and fixed fragment Dashboard summary cards so they show distinct Active Focus, Screen On, Idle time, and local-only sync labels.
- Extended Android screenshot automation to capture `09-main-shell.png`; latest connected-emulator artifact is `artifacts/android-ui-snapshots/20260430-115054`.
- Remaining Android UI gap: wire the new fragment shell screens to the same Room-backed ViewModels/repositories as the existing Activity screens.

Verified:

- `dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~AndroidMainShell_UsesFragmentContainerAndMaterialBottomNavigation|FullyQualifiedName~AndroidFragmentWireframeLayouts_ExistForProductFlow|FullyQualifiedName~AndroidSummaryMetricCards_UseMaterialCardContainers" -maxcpucount:1 -v minimal` passed.
- `dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~AndroidMainShell_UsesCompactReadableToolbarTitle|FullyQualifiedName~AndroidFragmentDashboard_SummaryCardsUseDistinctMetrics" -maxcpucount:1 -v minimal` passed after RED.
- `.\gradlew.bat testDebugUnitTest assembleDebug assembleDebugAndroidTest --no-daemon --stacktrace` passed from `android/`.
- `powershell -ExecutionPolicy Bypass -File scripts\run-android-ui-snapshots.ps1` passed on the emulator and produced `09-main-shell.png`.
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed 378 `.NET` tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1` passed; latest coverage summary reports 91.7% line coverage and 70.7% branch coverage.


## 2026-04-30 Android Session Row And System Bar Polish Slice

- Added RED Android repository coverage requiring session rows to expose local time range and active/idle state in addition to package and duration.
- Added RED architecture coverage requiring a reusable `item_focus_session.xml` row and requiring primary Activity layouts to avoid status-bar overlap through `fitsSystemWindows`.
- Added `item_focus_session.xml` and wired both `SessionsActivity` and Dashboard recent sessions to the structured row layout.
- Updated `RoomSessionsRepository` so `SessionRow` includes `timeRangeText` and `stateText` based on persisted Room session fields and timezone.
- Latest connected-emulator screenshot artifact: `artifacts/android-ui-snapshots/20260430-112341`; the title no longer overlaps the status bar and Sessions rows show package, time range, duration, and state.

Verified:

- `dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~AndroidFocusSessionRowLayout|FullyQualifiedName~AndroidPrimaryActivityLayouts_AvoidSystemBarOverlap" -maxcpucount:1 -v minimal` passed.
- `.\gradlew.bat testDebugUnitTest --tests "com.woong.monitorstack.sessions.RoomSessionsRepositoryTest" --no-daemon --stacktrace` passed after RED compile failure and implementation.
- `.\gradlew.bat testDebugUnitTest assembleDebug assembleDebugAndroidTest --no-daemon --stacktrace` passed from `android/`.
- `powershell -ExecutionPolicy Bypass -File scripts\run-android-ui-snapshots.ps1` passed on `emulator-5554` and generated the latest numbered screenshots.
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed 371 `.NET` tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1` passed; latest coverage summary reports 91.7% line coverage and 70.7% branch coverage.

## 2026-04-30 Android Wireframe XML Alignment Slice

- Added RED architecture tests proving Android Dashboard, Settings, Sessions, and Daily Summary layouts follow the wireframe card/chip/scroll structure and that period buttons use readable mixed-case content-width styling.
- Preserved the existing Activity/ViewBinding runtime contract instead of switching navigation to fragments in this slice.
- Added shared `wms_*` color tokens, `WmsCard`, status chip, section title, key/value, and period button styles plus status-chip/icon placeholder drawables.
- Reworked Dashboard and Settings XML into product-card layouts, then followed up with Sessions and Daily Summary card screens after screenshots showed the old debug-like list.
- Extended Android screenshot automation to capture numbered feature screenshots for dashboard overview, summary/location, charts, recent sessions, settings privacy/sync, settings location permission, sessions list, and daily summary.
- Latest connected-emulator screenshot artifact: `artifacts/android-ui-snapshots/20260430-110459`.
- Remaining Android UI polish: summary tiles should become reusable rounded card items, and session rows should move from plain text to structured app/package/time/duration rows.

Verified:

- `dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~AndroidWireframeLayoutTests|FullyQualifiedName~AndroidUiSnapshotScriptTests" -maxcpucount:1 -v minimal` passed.
- `.\gradlew.bat testDebugUnitTest assembleDebug assembleDebugAndroidTest --no-daemon --stacktrace` passed from `android/`.
- `powershell -ExecutionPolicy Bypass -File scripts\run-android-ui-snapshots.ps1` passed on `emulator-5554` and generated the numbered feature screenshots.
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed 368 `.NET` tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1` passed; latest coverage summary reports 91.7% line coverage and 70.7% branch coverage.

## 2026-04-30 WPF Control Action Evidence Slice

- Audited WPF acceptance runtime evidence after section screenshot grouping and found Start/Stop/Sync action results were only present as generic checks.
- Added RED WPF App acceptance-tool coverage requiring grouped `controlActionEvidence` in `report.md` and `manifest.json`.
- Updated `Woong.MonitorStack.Windows.UiSnapshots` to record Start tracking, Stop tracking, Sync local-only, and Sync enabled action results with AutomationIds and statuses while preserving generic `checks`.
- Verified focused test, WPF App tests, solution build, and WPF UI acceptance at `artifacts/wpf-ui-acceptance/20260430-114045`; latest report/manifest include grouped control action evidence.
- This is a WPF App/tool runtime acceptance evidence slice only. It does not change Android, Android scripts, Android docs, parked design refs, product telemetry collection, SQLite schema, or server code.

## 2026-04-30 WPF Section Screenshot Evidence Slice

- Audited the remaining WPF acceptance screenshot artifact shape and found section crops were only represented as generic screenshot filenames/skipped strings.
- Added RED WPF App acceptance-tool coverage requiring report and manifest grouped section screenshot evidence for Current activity, Summary cards, Sessions, Web sessions, Live events, Chart area, and Settings.
- Updated `Woong.MonitorStack.Windows.UiSnapshots` so `report.md` includes `## Section Screenshot Evidence`, and `manifest.json` includes `sectionScreenshotEvidence` rows with section, automationId, screenshot, skippedReason, and status.
- Kept the existing generic `screenshots`, `skippedScreenshots`, and `skippedScreenshotReasons` manifest arrays for backward compatibility.
- Verified focused test, WPF App tests, solution build, and WPF UI acceptance at `artifacts/wpf-ui-acceptance/20260430-113257`; latest report/manifest include grouped section screenshot evidence.
- This is a WPF App/tool acceptance evidence slice only. It does not change Android, Android scripts, Android docs, parked design refs, product telemetry collection, SQLite schema, or server code.

## 2026-04-30 WPF Current Focus Manifest Evidence Slice

- Added RED WPF App acceptance-tool coverage requiring `manifest.json` to include a dedicated machine-readable `currentFocusRuntimeEvidence` array separate from generic `checks`.
- Updated `Woong.MonitorStack.Windows.UiSnapshots` so manifest rows include `field`, `readableName`, `automationId`, `runtimeValue`, and `status` for Current app, Current process, Current window title, Current browser domain, Current session duration, Last poll time, Last DB write time, Last persisted session, and Sync state.
- Kept the existing generic `checks` manifest array for backward compatibility.
- Verified WPF App tests, solution build, and WPF UI acceptance at `artifacts/wpf-ui-acceptance/20260430-111517`; the latest manifest includes `currentFocusRuntimeEvidence` and `checks`.
- This is a WPF App/tool manifest evidence slice only. It does not change Android, Android scripts, Android docs, parked design refs, product telemetry collection, SQLite schema, or server code.

## 2026-04-30 WPF Current Focus Report Table Slice

- Added RED WPF App acceptance-tool coverage requiring the snapshot report to include a human-readable Current Focus runtime semantic evidence table.
- Updated `Woong.MonitorStack.Windows.UiSnapshots` so `report.md` now has `## Current Focus Runtime Semantic Evidence` with Field, AutomationId, Readable Name, Runtime Value, and Status columns.
- The table is populated from the same Current Focus semantic checks that feed the manifest/report check list, keeping automation IDs, readable names, and runtime values together for manual review.
- Verified WPF App tests, solution build, and WPF UI acceptance at `artifacts/wpf-ui-acceptance/20260430-105239`.
- This is a WPF App/tool report evidence slice only. It does not change Android, Android scripts, Android docs, product telemetry collection, SQLite schema, or server code.

## 2026-04-30 WPF Current Focus Acceptance Semantic Evidence Slice

- Added RED WPF App acceptance-tool coverage requiring `Woong.MonitorStack.Windows.UiSnapshots` to emit Current Focus semantic checks into the shared report/manifest check list.
- Added readable-name and runtime-status evidence for Current app, Current process, Current window title, Current browser domain, Current session duration, Last poll time, Last DB write time, Last persisted session, and Sync state.
- The snapshot tool reuses the existing `AutomationProperties.ItemStatus`/text reader path, so accessibility names stay readable while runtime values remain visible to acceptance artifacts.
- Verified WPF App tests, solution build, and WPF UI acceptance at `artifacts/wpf-ui-acceptance/20260430-104509`.
- This is a WPF App/tool acceptance evidence slice only. It does not change product telemetry collection, SQLite schema, Android, Android scripts, Android docs, or server code.

## 2026-04-30 WPF Acceptance Header Badge Evidence Slice

- Added RED WPF App acceptance-tool coverage requiring the snapshot tool to emit Header Tracking, Sync, and Privacy badge semantic-name checks into the shared report/manifest check list.
- Updated `Woong.MonitorStack.Windows.UiSnapshots` to verify Header badge readable names before each mode-specific scenario. The checks now appear in both `report.md` and `manifest.json` through the existing `context.Results` artifact pipeline.
- Repaired RealStart and UiSnapshots runtime value reads after semantic names were added to Current Focus fields: status value checks now read `AutomationProperties.ItemStatus` first, while readable names remain available for accessibility selectors.
- Added `AutomationProperties.ItemStatus` bindings to Current Focus value TextBlocks so acceptance tools can read runtime values without weakening the semantic `AutomationProperties.Name` labels.
- This is a WPF App/tool accessibility slice. It does not change product telemetry collection, SQLite schema, Android, Android scripts, or server code.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore --filter "FullyQualifiedName~UiSnapshotsTool_ReportAndManifestIncludeHeaderBadgeSemanticEvidence" -maxcpucount:1 -v minimal` failed RED on missing `VerifyHeaderBadgeSemanticNames`, then passed after the snapshot tool update.
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal` passed 119 WPF App tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1` passed with artifact `artifacts/wpf-ui-acceptance/20260430-100126`. The latest UI snapshot `report.md` and `manifest.json` include Header Tracking/Sync/Privacy badge readable-name checks.

## 2026-04-30 WPF Header StatusBadge Accessibility Slice

- Added RED WPF App accessibility coverage for Header Tracking, Sync, and
  Privacy status badges so each badge exposes a readable
  `AutomationProperties.Name` that matches its visible state text.
- Updated reusable `StatusBadge` so the control-level accessible name follows
  the bound `Text` value, including dynamic status text changes.
- This is a WPF App-only accessibility slice. It does not change tracking,
  persistence, sync upload behavior, Android, scripts, or server code.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore --filter "FullyQualifiedName~HeaderStatusBar_StatusBadgesExposeReadableNamesMatchingStateText" -maxcpucount:1 -v minimal` failed RED on empty `TrackingStatusBadge` automation name, then passed after the `StatusBadge` metadata update.
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal` passed 118 WPF App tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
## 2026-04-30 WPF Current Focus Runtime Accessibility Slice

- Added RED WPF App accessibility coverage for Current Focus runtime values so
  tracking state, app/process/window, browser domain, duration, persistence,
  poll, browser capture, DB write, and sync status fields have both stable
  AutomationIds and readable semantic names.
- Updated the reusable `DetailRow` value TextBlock to use its visible label as
  `AutomationProperties.Name` and added explicit names for Last DB write and
  Sync state text in `CurrentFocusPanel`.
- Added a MainWindow semantic test that selects Start, Sync Now, and Stop by
  stable AutomationIds/readable names and verifies Current Focus state remains
  selectable through the runtime transitions.
- This is a WPF App-only UI accessibility slice. It does not change tracking
  persistence, SQLite schema, sync upload behavior, Android, scripts, or server
  code.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore --filter "FullyQualifiedName~CurrentFocusPanel_RuntimeStateValuesExposeReadableAutomationNames" -maxcpucount:1 -v minimal` failed RED on empty `TrackingStatusText` automation name, then passed after the XAML/control metadata update.
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore --filter "FullyQualifiedName~CurrentFocusPanelAccessibilityTests" -maxcpucount:1 -v minimal` passed 2 tests.
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal` passed 117 WPF App tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
## 2026-04-30 Android Launcher And Resource Measurement Slice

- Added RED Robolectric coverage proving the Android launcher `MainActivity` immediately opens `DashboardActivity` and finishes, so a normal app launch lands on the real dashboard instead of the old permission placeholder.
- Simplified `MainActivity` into a launcher router and added `DashboardActivity.createIntent(...)` while keeping `DashboardActivity` `exported=false`.
- Added `scripts/run-android-resource-measurement.ps1` for local package-scoped process, memory, and graphics diagnostics. The script writes `report.md`, `manifest.json`, `process.txt`, `meminfo.txt`, and `gfxinfo.txt` under `artifacts/android-resource-measurements/`.
- Added architecture tests for the script contract, no-device blocked artifacts, fake connected-device behavior, and the privacy boundary that it does not capture screenshots or typed/user content.
- Ran the script on the `Medium_Phone` emulator: `artifacts/android-resource-measurements/20260430-093728`.
- Historical note: physical-device resource measurement was open in this older slice. Current policy accepts emulator evidence as the completion baseline and treats physical-device measurement as optional future hardening.

Verified:

- `./gradlew.bat testDebugUnitTest --tests "com.woong.monitorstack.MainActivityTest" --no-daemon --stacktrace` passed focused launcher RED/GREEN coverage.
- `./gradlew.bat testDebugUnitTest assembleDebug connectedDebugAndroidTest --no-daemon --stacktrace` passed from `android/`.
- `dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~AndroidResourceMeasurementScriptTests" -maxcpucount:1 -v minimal` passed 3 script contract tests.
- `powershell -ExecutionPolicy Bypass -File scripts\run-android-resource-measurement.ps1 -OutputRoot <temp> -DurationSeconds 1` passed on the `Medium_Phone` emulator after installing the debug APK before launch.
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed 358 total `.NET` tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors after rerunning sequentially; an earlier parallel test/build attempt hit a transient file lock.
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1` passed with line coverage 91.7% and branch coverage 70.7%.
## 2026-04-30 WPF Control Bar Accessibility Polish Slice

- Added RED WPF App accessibility coverage for Control Bar button semantic
  names so icon-prefixed visual labels do not become noisy assistive names.
- Added explicit `AutomationProperties.Name` values for Start Tracking, Stop
  Tracking, Refresh, Sync Now, Today, 1h, 6h, 24h, and Custom period buttons.
- This is a WPF App-only accessibility slice. It does not change tracking,
  SQLite persistence, sync behavior, browser capture, Android, or server code.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore --filter "FullyQualifiedName~ControlBar_ButtonsExposeReadableAutomationNames" -maxcpucount:1 -v minimal` failed RED on empty `StartTrackingButton` automation name, then passed after XAML names were added.
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal` passed 115 WPF App tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.

## 2026-04-29 WPF App Root Style Merge Slice

- Added RED architecture tests proving that `App.xaml` merges every shared
  style dictionary under `Styles/` and that `MainWindow.xaml` does not duplicate
  application-level style dictionaries.
- Fixed the resource composition by adding `Styles/Inputs.xaml` to
  `App.xaml` and removing the duplicate `Colors.xaml` merge from
  `MainWindow.xaml`.
- This keeps `MainWindow` as a thin WPF shell while preserving shared Settings
  input/checkbox styles at the application root.
- Updated the WPF background expectation to inject test-local resources into
  the Window under test rather than creating a process-wide WPF `Application`,
  keeping STA UI tests isolated.

Verified so far:

- `dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~AppResources_MergeEverySharedStyleDictionaryAtApplicationRoot|FullyQualifiedName~MainWindow_DoesNotDuplicateApplicationLevelStyleDictionaries" -v minimal`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-233728`.

Coverage after this slice: overall line coverage 91.3%.

## 2026-04-29 Android Optional Location Context Plan Slice

- Added `docs/android-ui-plan.md` with Android XML/View screen structure plus
  optional latitude/longitude location context.
- The plan treats ?꾨룄/寃쎈룄 as sensitive metadata: off by default, Android
  location permission required, explicit in-app opt-in required, approximate
  mode preferred, and precise coordinates a separate choice.
- Updated `docs/prd.md`, `docs/privacy-boundaries.md`,
  `docs/android-ui-screenshot-testing.md`, and `total_todolist.md` so the new
  location UI request does not conflict with the metadata-only privacy model.
- Added an architecture documentation guardrail test so the Android UI plan
  keeps latitude/longitude opt-in, permission-gated, and non-inferred from
  other app content.

Verified so far:

- `dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter AndroidUiPlan_DocumentsLatitudeLongitudeAsExplicitOptInMetadata -v minimal`
- `dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `.\gradlew.bat testDebugUnitTest assembleDebug --no-daemon --stacktrace` from `android/`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

## Last Completed Slice

Chrome acceptance sandbox guard follow-up. RED test
`AcceptanceScript_RefusesToCleanupNonTempChromeProfiles` now requires the
native-message acceptance cleanup path to reject ordinary Chrome profile paths
before process enumeration. `run-chrome-native-message-acceptance.ps1` now
verifies the cleanup profile is under the acceptance temp root
`woong-chrome-native-*`, so user Chrome windows cannot be closed by a mistaken
profile path. Focused Chrome acceptance script tests and dry-run acceptance
passed without launching Chrome or changing HKCU values. Full `.NET` tests
passed (289), full `.NET` build passed with 0 warnings/errors, and coverage
generation completed with 91.3% line coverage.

Server WebSession idempotency hardening slice. RED tests first required
`WebSessionUploadItem.clientSessionId`, a server unique index on `(DeviceId,
ClientSessionId)`, relational duplicate enforcement for domain-only web
sessions, and a migration that backfills legacy rows before enforcing the new
non-null unique key. The server upload service now detects duplicate web
uploads by `deviceId + clientSessionId` instead of nullable URL fields, Windows
outbox/native-host payloads include the same stable aggregate id as
`clientSessionId`, and production migration
`20260429101507_AddWebSessionClientSessionId` updates PostgreSQL schema safely.
Verification passed: domain tests (22), server tests (25), Chrome-native
focused tests (33), full `.NET` tests (277), full `.NET` build with 0 warnings
and 0 errors, and coverage generation with 91.2% line coverage.

Server relationship constraint follow-up slice. RED model tests first proved
that focus/web/raw/device-state sessions did not have required Device foreign
keys and WebSession did not have a composite relationship to its FocusSession.
`MonitorDbContext` now enforces required device FKs and
`web_sessions(DeviceId, FocusSessionId)` -> `focus_sessions(DeviceId,
ClientSessionId)` with restrict delete behavior. Migration
`20260429102602_AddServerSessionForeignKeys` applies the relationship
constraints, and relational SQLite tests prove missing parent rows are rejected.
Verification passed: focused server tests (29), full `.NET` tests (281), full
`.NET` build with 0 warnings and 0 errors, and coverage generation with 91.3%
line coverage.

Chrome native messaging acceptance safety hardening slice. RED/focused tests
now guard against closing the user's normal Chrome and against dangerous HKCU
native-messaging cleanup. The acceptance script launches Chrome with a temp
`--user-data-dir` profile, cleanup stops only Chrome processes whose command
line contains that temp profile, `HostName` values must be scoped and
non-blank before any registry path is built, and the native host can be forced
with `WOONG_MONITOR_REQUIRE_EXPLICIT_DB=1` so acceptance cannot silently fall
back to the user's real local DB. Added
`docs/chrome-native-messaging-acceptance.md`. Verified focused Chrome native
messaging script/registry/parser/receiver tests, the dry-run acceptance path,
full `.NET` tests (267), full `.NET` build with 0 warnings/errors, and coverage
generation with 91.2% line coverage.

Chrome native messaging acceptance completion slice. Chrome stable blocked
command-line unpacked extension loading, so the local acceptance path now uses
Chrome for Testing from the ignored `.cache/chrome-for-testing/` cache. The
script quotes host resolver rules, writes Chrome/native-host diagnostic logs to
artifacts, and enumerates SQLite JSON rows correctly in PowerShell. DomainOnly
browser storage now redacts both full URL and page title by default, preventing
early Chrome titles such as `github.example:port/github.html` from leaking
paths through outbox payloads. Full headed acceptance passed at
`artifacts/chrome-native-acceptance/20260429-185325`: `github.example` and
`chatgpt.example` arrived through Extension -> NativeHost -> temp SQLite, two
domain-only web sessions and outbox rows were written, cleanup removed only the
scoped HKCU test host key, and no user Chrome process was stopped. Verification
after this fix passed: full `.NET` tests (271), full `.NET` build with 0
warnings/errors, and coverage generation with 91.2% line coverage.

Follow-up sandbox hardening: the Chrome native-message acceptance script no
longer falls back to the user's installed Chrome by default. It now requires
Chrome for Testing from `.cache/chrome-for-testing/`, `-InstallChromeForTesting`,
or an explicit `-ChromePath`; installed Chrome fallback requires the explicit
`-AllowInstalledChromeFallback` switch for isolated manual debugging. Cleanup
still stops only Chrome processes whose command line contains the temp
`--user-data-dir` profile. Dry-run cleanup now passes `-DryRun` through to the
uninstall script, so dry-run acceptance reports registry cleanup without
changing HKCU values.

Verification after the sandbox follow-up passed on 2026-04-29: Chrome-native
focused tests (33), full `.NET` tests (273), full `.NET` build with 0 warnings
and 0 errors, coverage generation at 91.2% line coverage, dry-run acceptance,
and full Chrome for Testing acceptance at
`artifacts/chrome-native-acceptance/20260429-190639`. The scoped HKCU test key
was absent after cleanup.

Milestone 27 Android UI snapshot blocked-evidence slice. RED tests first
required a repo-level `scripts\run-android-ui-snapshots.ps1` contract and a
fake-ADB execution path that writes `report.md`, `manifest.json`, and
`visual-review-prompt.md` even when no Android device is connected. The script
now checks `adb devices -l`, writes artifacts under
`artifacts/android-ui-snapshots/<timestamp>/`, updates `latest/`, and exits
with `Status: BLOCKED` when no emulator/physical device is available. Android
unit tests, debug build, and androidTest APK build passed via Gradle. Full
`.NET` regression tests passed (249 tests), full `.NET` build passed with 0
warnings/errors, and coverage generation completed with 91.2% line coverage.
The local Android snapshot availability run wrote
`artifacts/android-ui-snapshots/latest/report.md` with blocked evidence because
no Android device/emulator was connected. Connected screenshot capture and
`connectedDebugAndroidTest` remain deferred until an emulator or physical device
is available.

Milestone 25 SampleDashboard acceptance slice. RED tests first required
`WindowsAppAcceptanceMode.SampleDashboard`, deterministic DI registration for a
sample dashboard data source, and snapshot-tool support for `--mode
SampleDashboard`. The WPF app now has a non-tracking sample dashboard mode that
shows deterministic Chrome, Code.exe, `github.com`, `chatgpt.com`, and
`docs.microsoft.com` data without writing focus/web/outbox rows to SQLite.
`scripts/run-ui-snapshots.ps1 -Mode SampleDashboard` produces screenshots,
`report.md`, `manifest.json`, and `visual-review-prompt.md`. Verification
passed: full `.NET` tests (247), full `.NET` build with 0 warnings/errors,
coverage generation with 91.2% line coverage, SampleDashboard snapshot
artifacts at `artifacts/ui-snapshots/latest`, WPF UI acceptance at
`artifacts/wpf-ui-acceptance/20260429-172417`, and the Windows smoke tool.

Milestone 30 Live Event Log runtime semantics slice. RED tests first required
the public `DashboardViewModel` Start/Poll/Stop/Sync commands to publish visible
runtime log rows for Tracking started, FocusSession closed/started,
FocusSession persisted, WebSession closed/started, WebSession persisted, outbox
row created, sync skipped, and Tracking stopped. The ViewModel now keeps
runtime event rows separate from SQLite-derived focus/web rows, then publishes a
combined Live Event Log without losing runtime evidence on dashboard refresh.
Verification passed: full `.NET` tests (244), full `.NET` build with 0
warnings/errors, coverage generation with 91.0% line coverage, WPF UI
acceptance at `artifacts/wpf-ui-acceptance/20260429-171411`, and the Windows
smoke tool.

Milestone 4.5/23 native browser host slice. RED tests first required a
`ChromeNativeMessageHostRunner`, a local Chrome native host install script, and
a persistent `chrome.runtime.connectNative` extension path. The Windows layer
now has a host runner that processes Chrome native messages until EOF, plus
`tools/Woong.MonitorStack.ChromeNativeHost`, a console native host so a real
extension can stream domain-only active-tab metadata into local SQLite and
outbox rows. The extension now keeps a persistent native port instead of
launching a one-shot host per message. Verification passed: full `.NET` tests
(241), full `.NET` build with 0 warnings/errors, coverage generation with 90.8%
line coverage, and WPF UI acceptance at
`artifacts/wpf-ui-acceptance/20260429-170610`. The Windows smoke tool reported
real Chrome foreground metadata without keystroke, screen, or page-content
capture.

Milestone 23/25 browser connection status UI slice. RED tests first required a
presentation-level `DashboardBrowserCaptureStatus`, a visible
`BrowserCaptureStatusText`, and a stable `BrowserCaptureStatusText`
AutomationId. The WPF Current Focus panel now keeps browser domain and browser
capture status separate: `github.com` remains the domain value, while the
status can say `Browser capture unavailable`, `Browser extension connected`,
`Domain from address bar fallback`, or `Browser capture error`. The coordinator
maps capture methods to the presentation status and catches browser reader
failures so app/window focus tracking continues even when browser metadata
capture fails. Verification passed: full `.NET` tests (238), full `.NET` build
with 0 warnings/errors, coverage generation with 90.6% line coverage, WPF UI
acceptance at `artifacts/wpf-ui-acceptance/20260429-164942`, and the Windows
smoke tool.

Milestone 23/25 browser-domain immediate capture slice. The WPF app now
registers a metadata-only UI Automation address-bar fallback through DI, wires
`WindowsTrackingDashboardCoordinator` with `SqliteWebSessionRepository`,
`IBrowserActivityReader`, and DomainOnly URL storage, and keeps extension/native
messaging documented as the more reliable browser-owned active-tab path. RED
tests first covered production browser reader DI, immediate Start snapshot
domain display, WPF `StartTrackingButton` domain display, and safe fallback
behavior when a foreground process is not a browser or no address bar URL is
available. This does not rely on Administrator rights, does not infer domains
from window titles, and still stores no full URLs by default. Verification
passed: full `.NET` tests (233), full `.NET` build with 0 warnings/errors,
coverage generation with 90.6% line coverage, WPF UI acceptance at
`artifacts/wpf-ui-acceptance/20260429-163949`, and the Windows smoke tool.

Milestone 25 WPF EmptyData evidence and browser-domain copy adjustment.
The in-progress EmptyData acceptance slice now proves the local snapshot tool
launches with auto-start disabled and records zero `focus_session`,
`web_session`, and `sync_outbox` rows in the temp DB. The browser-domain empty
state copy was also adjusted from a privacy-sounding message to
`No browser domain yet. Connect browser capture; app focus is tracked.` The
policy docs now clarify that Administrator rights are not a reliable way to
read active browser tab URLs; browser extension/native messaging or explicit
UI Automation fallback is the correct path. Verification passed: all `.NET`
tests (226), full `.NET` build, EmptyData snapshot flow, WPF acceptance at
`artifacts/wpf-ui-acceptance/20260429-161328`, and coverage generation with
overall line coverage 92.0%.

Previous completed slice: Milestone 25 WPF TrackingPipeline SQLite evidence.
Focused RED tests first required the UI snapshot tool to query the temp SQLite
DB and publish semantic DB evidence in both `report.md` and `manifest.json`.
`tools/Woong.MonitorStack.Windows.UiSnapshots` now uses `Microsoft.Data.Sqlite`
as a tool-only dependency to count `focus_session`, `web_session`, and
`sync_outbox` rows after the fake TrackingPipeline run. The latest acceptance
report shows `focus_session=2`, `web_session=2`, and `sync_outbox=4`.
Verification passed: all `.NET` tests (225), full `.NET` build, WPF acceptance
at `artifacts/wpf-ui-acceptance/20260429-155615`, and coverage generation with
overall line coverage 92.0%.

## Completed

- Added architecture/privacy boundary tests that fail if product code or
  manifests introduce keylogging hooks, raw keyboard state APIs, clipboard or
  screen capture APIs, Android Accessibility/text-input monitoring, or Chrome
  page-content/history/cookie/capture APIs.
- Added a WPF snapshot-tool privacy test proving the local screenshot helper
  captures only the target app window/elements rather than the desktop.
- Documented the automated privacy guardrails in `docs/privacy-boundaries.md`
  and `docs/hardening.md`.
- Added `MainWindowTrackingPipelineTests` for actual WPF Start/Stop behavior:
  UI Automation invokes the buttons, fake foreground changes are persisted to
  SQLite, outbox rows are queued, and dashboard cards/session rows are rendered
  from the SQLite-backed datasource.
- Disabled parallel execution in `Woong.MonitorStack.Windows.App.Tests` to make
  WPF XAML/STA window tests more deterministic.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal`, `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1 -v
  minimal`, and `scripts\test-coverage.ps1`; current .NET line coverage is
  92.9% overall, Domain 89.3%, Windows 92.5%, Windows.Presentation 97.6%,
  Windows.App 85.3%, and Server 96.3%.
- Verified `scripts/run-wpf-real-start-acceptance.ps1 -Seconds 2` exits
  successfully with a temp SQLite DB and no server sync.
- Added Windows local browser raw-event retention: a 30-day default
  `BrowserRawEventRetentionPolicy`, repository-level
  `DeleteOlderThan(...)`, and optional `ChromeNativeMessageIngestionFlow`
  pruning through `BrowserRawEventRetentionService`.
- Added `PollTrackingCommand` to the dashboard ViewModel and a WPF
  `DispatcherTimer` adapter in `MainWindow` so Running tracking continues to
  poll and advance the current session duration.
- Added semantic WPF runtime tests for poll-tick duration updates, foreground
  change refresh after persistence, fake browser github/chatgpt web sessions in
  the Web Sessions tab, and minimum-size scrolling for lower dashboard tabs.
- Verified the requested runtime confidence pass with
  `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
  (171 tests passed), `dotnet build Woong.MonitorStack.sln --no-restore
  -maxcpucount:1 -v minimal` (0 warnings/errors),
  `scripts/run-wpf-real-start-acceptance.ps1 -Seconds 2`, and
  `scripts\test-coverage.ps1`. Current .NET line coverage remains 92.9%
  overall; Domain 89.3%, Windows 92.5%, Windows.Presentation 97.6%,
  Windows.App 85.3%, Server 96.3%.
- Added `scripts/run-wpf-ui-acceptance.ps1`, which composes the RealStart
  semantic Start/Stop/temp-SQLite verification with local UI snapshot evidence
  under `artifacts/wpf-ui-acceptance/`.
- Added `WpfUiAcceptanceScriptTests` so the WPF App test suite guards the
  acceptance script's RealStart composition, temp SQLite focus/outbox checks,
  local artifact path, sync opt-in flag, and privacy warning text.
- Verified `scripts/run-wpf-ui-acceptance.ps1 -Seconds 2`; it launched the WPF
  app through FlaUI, invoked Start/Stop, persisted temp SQLite
  `focus_session` and `sync_outbox` rows, ran UI snapshots, and wrote
  `artifacts/wpf-ui-acceptance/latest/report.md`.
- Re-verified `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1
  -v minimal`; all 172 .NET tests passed.
- Re-verified `dotnet build Woong.MonitorStack.sln --no-restore
  -maxcpucount:1 -v minimal`; build passed with 0 warnings/errors.
- Re-verified `scripts\test-coverage.ps1`; current .NET line coverage is
  92.9% overall, Domain 89.3%, Windows 92.5%, Windows.Presentation 97.4%,
  Windows.App 86.6%, and Server 96.3%.
- Added `WindowsAppAcceptanceMode.TrackingPipeline` and an
  `AcceptanceTrackingDashboardCoordinator` for local-only WPF acceptance. It
  persists fake Code.exe and chrome.exe focus sessions to temp SQLite, creates
  linked `github.com` and `chatgpt.com` web sessions, queues focus/web outbox
  rows, and fake-syncs outbox rows only after the UI enables sync.
- Upgraded `tools/Woong.MonitorStack.Windows.UiSnapshots` with semantic FlaUI
  TrackingPipeline checks, required screenshot names, `manifest.json`, and a
  local `visual-review-prompt.md`.
- Hardened `scripts/run-wpf-ui-acceptance.ps1` so native `dotnet` exit codes
  fail the script, and split RealStart and TrackingPipeline temp SQLite DBs for
  deterministic fake-pipeline checks.
- Verified TrackingPipeline acceptance with
  `scripts/run-wpf-ui-acceptance.ps1 -Seconds 2`; latest detailed snapshot
  report is PASS and confirms Running/Stopped state, Code.exe, chrome.exe,
  `github.com`, `chatgpt.com`, summary `15m`, live Focus/Web events, and fake
  Sync Now status.
- Re-verified `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1
  -v minimal`; all 175 .NET tests passed.
- Re-verified `dotnet build Woong.MonitorStack.sln --no-restore
  -maxcpucount:1 -v minimal`; build passed with 0 warnings/errors.
- Re-verified `scripts\test-coverage.ps1`; current .NET line coverage is
  92.2% overall, Domain 89.3%, Windows 92.5%, Windows.Presentation 97.4%,
  Windows.App 85.7%, and Server 96.3%.
- Added a Windows coordinator opt-in enforcement test proving `SyncNow(false)`
  returns the skipped status and leaves queued outbox rows pending, unsynced,
  and retry count zero.
- Verified the existing Android `AndroidSyncWorkerTest` still proves disabled
  sync returns skipped success and does not invoke the sync runner.
- Added WPF Settings copy for browser URL/domain privacy:
  `Browser URL storage is domain-only by default. Full URLs require explicit
  future opt-in.`
- Re-verified `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1
  -v minimal`; all 176 .NET tests passed.
- Re-verified `dotnet build Woong.MonitorStack.sln --no-restore
  -maxcpucount:1 -v minimal`; build passed with 0 warnings/errors.
- Re-verified Android `testDebugUnitTest` and `assembleDebug` from `android/`.
- Re-verified `scripts/run-wpf-ui-acceptance.ps1 -Seconds 2`; the latest WPF
  acceptance report remains PASS.
- Re-verified `scripts\test-coverage.ps1`; current .NET line coverage is
  92.3% overall.
- Completed Milestone 21 with Presentation-first MVVM state and tests.
- Added `IDashboardTrackingCoordinator`, `NoopDashboardTrackingCoordinator`,
  `DashboardTrackingSnapshot`, structured `DashboardPersistedSessionSnapshot`,
  and `DashboardSyncResult`.
- Added WPF current activity panel and stable AutomationIds:
  `StartTrackingButton`, `StopTrackingButton`, `SyncNowButton`,
  `TrackingStatusText`, `CurrentAppNameText`, `CurrentProcessNameText`,
  `CurrentWindowTitleText`, `CurrentSessionDurationText`,
  `LastPersistedSessionText`, and `LastSyncStatusText`.
- Added `WindowTitleVisibleCheckBox`; window and web page titles are hidden by
  default and previously hidden raw titles are not retained for later reveal.
- Verified Start/Stop commands through a fake tracking coordinator and verified
  Sync Now receives the current sync opt-in state.
- Reduced the default WPF shell width to fit local snapshot automation better
  and added a UI expectation test for the default width ceiling.
- Verified `dotnet restore Woong.MonitorStack.sln`, `dotnet build
  Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`, and `dotnet
  test Woong.MonitorStack.sln --no-build -maxcpucount:1 -v minimal`; all 120
  .NET tests passed.
- Verified coverage generation with `scripts\test-coverage.ps1`; current
  overall line coverage is 91.9%, Domain 88.6%, Windows.Presentation 98.2%,
  Windows 91.1%, Windows.App 52.0%, and Server 96.0%.
- Verified local WPF UI snapshots with `scripts/run-ui-snapshots.ps1`;
  `artifacts/ui-snapshots/latest/report.md` reports PASS. `ChartArea` crop
  remains skipped when not visible in the first viewport and should be handled
  in Milestone 25 semantic acceptance.
- Deferred deeper WPF layout tuning to Milestone 25 after user feedback: the
  lower App Sessions, Web Sessions, and Live Event Log areas are more important
  than perfect first-viewport visibility of the current activity panel.
- Added `WindowsAppOptions` for typed Windows app composition settings:
  dashboard timezone, device id, local SQLite connection string, and idle
  threshold.
- Added `WindowsTrackingDashboardCoordinator`, which creates a fresh
  `TrackingPoller` per Start, persists closed focus sessions to
  `SqliteFocusSessionRepository`, enqueues `focus_session` upload payloads in
  `SqliteSyncOutboxRepository`, and keeps Stop->Start from extending a previous
  stopped session.
- Added `SqliteDashboardDataSource` so the WPF dashboard reads focus and linked
  web sessions from Windows local SQLite rather than `EmptyDashboardDataSource`.
- Updated WPF DI to register real Windows tracking readers, idle detection,
  sessionizer, tracking poller factory, SQLite repositories, dashboard data
  source, and tracking coordinator.
- Added app-level tests for DI registration, fake foreground change
  persistence/outbox enqueueing, Stop flush, Stop->Start session separation,
  and SQLite dashboard source focus/web reads.
- Added a Presentation test proving Stop refreshes the current dashboard period
  after flushed data becomes available.
- Verified `dotnet restore Woong.MonitorStack.sln`, `dotnet build
  Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`, and `dotnet
  test Woong.MonitorStack.sln --no-build -maxcpucount:1 -v minimal`; all 127
  .NET tests passed.
- Verified coverage generation with `scripts\test-coverage.ps1`; current
  overall line coverage is 92.0%, Domain 88.6%, Windows.Presentation 97.6%,
  Windows 91.1%, Windows.App 83.6%, and Server 96.0%.
- Verified WPF local snapshot smoke with `scripts/run-ui-snapshots.ps1`;
  `artifacts/ui-snapshots/latest/report.md` reports PASS.
- Added `WOONG_MONITOR_LOCAL_DB` and `WOONG_MONITOR_DEVICE_ID` app option
  overrides so local acceptance can force the WPF app onto a temp SQLite DB and
  stable test device id.
- Added `scripts/run-wpf-real-start-acceptance.ps1` with the required privacy
  warning: it observes foreground window metadata only, does not record
  keystrokes, does not capture screen contents, and uses a temp DB by default.
- Added `tools/Woong.MonitorStack.Windows.RealStartAcceptance`, which launches
  the WPF app, clicks Start/Stop through FlaUI, and verifies persisted
  `focus_session` plus `sync_outbox` rows.
- Verified RealStart locally with a temp DB containing one `focus_session` row
  and one `sync_outbox` row.
- Verified `dotnet restore Woong.MonitorStack.sln`, `dotnet build
  Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`, and `dotnet
  test Woong.MonitorStack.sln --no-build -maxcpucount:1 -v minimal`; all 129
  .NET tests passed.
- Verified coverage generation with `scripts\test-coverage.ps1`; current
  overall line coverage is 92.1%, Domain 88.6%, Windows.Presentation 97.6%,
  Windows 91.1%, Windows.App 85.0%, and Server 96.0%.
- Added `BrowserActivitySnapshot`, `CaptureMethod`, `CaptureConfidence`,
  `BrowserProcessClassification`, `IBrowserProcessClassifier`,
  `IBrowserActivityReader`, `IBrowserUrlSanitizer`, `IWebSessionizer`, and the
  initial `BrowserProcessClassifier`.
- Verified supported browser process classification for `chrome.exe`,
  `msedge.exe`, `firefox.exe`, `brave.exe`, uppercase variants, and process
  names reported without `.exe`.
- Verified non-browser process names do not classify as browsers.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal` and `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1
  -v minimal`; all 140 .NET tests passed.
- Verified coverage generation with `scripts\test-coverage.ps1`; current
  overall line coverage is 92.3%, Domain 88.6%, Windows.Presentation 97.6%,
  Windows 91.5%, Windows.App 85.0%, and Server 96.0%.
- Added `BrowserUrlSanitizer`, which clears URL/domain capture when browser
  URL storage is Off, stores registrable domain without full URL in DomainOnly
  mode, and strips URL fragments when FullUrl storage is explicitly enabled.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal` and `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1
  -v minimal`; all 143 .NET tests passed.
- Verified coverage generation with `scripts\test-coverage.ps1`; current
  overall line coverage is 92.0%, Domain 88.6%, Windows.Presentation 97.6%,
  Windows 91.0%, Windows.App 85.0%, and Server 96.0%.
- Updated `WebSession` and `WebSessionUploadItem` so `Url` and `PageTitle` are
  nullable while `Domain` remains required for persisted web sessions.
- Updated `SqliteWebSessionRepository` to write/read nullable URL and page
  title values for domain-only browser privacy mode.
- Updated `BrowserWebSessionizer` to accept sanitized
  `BrowserActivitySnapshot` inputs, create domain-only `WebSession` rows
  without a full URL, close a prior domain on domain change, and ignore
  snapshots with neither URL nor domain so they fall back to FocusSession-only
  tracking.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal` and `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1
  -v minimal`; all 145 .NET tests passed.
- Verified coverage generation with `scripts\test-coverage.ps1`; current
  overall line coverage is 92.1%, Domain 88.7%, Windows.Presentation 97.6%,
  Windows 91.2%, Windows.App 85.0%, and Server 96.0%.
- Added optional `CaptureMethod`, `CaptureConfidence`, and
  `IsPrivateOrUnknown` fields to shared `WebSession`, web-session upload
  contracts, and server web-session entity shape.
- Updated `SqliteWebSessionRepository` to create/read/write
  `capture_method`, `capture_confidence`, and `is_private_or_unknown` columns
  in Windows local SQLite.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal` and `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1
  -v minimal`; all 146 .NET tests passed.
- Verified coverage generation with `scripts\test-coverage.ps1`; current
  overall line coverage is 92.2%, Domain 88.8%, Windows.Presentation 97.6%,
  Windows 91.4%, Windows.App 85.0%, and Server 96.0%.
- Updated `ChromeNativeMessageIngestionFlow` so completed web sessions can
  enqueue `web_session` outbox items when configured with a device id and
  outbox repository.
- Verified the web-session upload payload includes device id, focus session id,
  domain, duration, nullable URL/title fields, and capture provenance.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal` and `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1
  -v minimal`; all 147 .NET tests passed.
- Verified coverage generation with `scripts\test-coverage.ps1`; current
  overall line coverage is 92.3%, Domain 88.8%, Windows.Presentation 97.6%,
  Windows 91.7%, Windows.App 85.0%, and Server 96.0%.
- Added nullable raw browser event records so browser raw events can preserve
  domain-only data without storing full URLs.
- Updated Chrome native-message ingestion to sanitize messages before raw-event
  persistence and before WebSession creation.
- Verified DomainOnly ingestion stores `Url = null` in both raw browser events
  and completed WebSessions while retaining the normalized domain.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal` and `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1
  -v minimal`; all 149 .NET tests passed.
- Verified coverage generation with `scripts\test-coverage.ps1`; current
  overall line coverage is 92.6%, Domain 88.8%, Windows.Presentation 97.6%,
  Windows 92.3%, Windows.App 85.0%, and Server 96.0%.
- Updated server EF web-session model configuration so URL and page title are
  optional, while capture method/confidence have bounded lengths.
- Verified duplicate web-session upload retries remain idempotent when URL and
  page title are null and capture provenance is present.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal` and `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1
  -v minimal`; all 148 .NET tests passed.
- Verified coverage generation with `scripts\test-coverage.ps1`; current
  overall line coverage is 92.3%, Domain 88.8%, Windows.Presentation 97.6%,
  Windows 91.7%, Windows.App 85.0%, and Server 96.0%.
- Added `NativeMessagingHostManifestGenerator`, which emits the Chrome native
  messaging host manifest JSON with the stable host name, description, host
  executable path, `stdio` transport type, and explicit allowed extension
  origins.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal` and `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1
  -v minimal`; all 150 .NET tests passed.
- Verified coverage generation with `scripts\test-coverage.ps1`; current
  overall line coverage is 92.7%, Domain 88.8%, Windows.Presentation 97.6%,
  Windows 92.4%, Windows.App 85.0%, and Server 96.0%.
- Added nullable process/window metadata fields to `FocusSession` and
  `FocusSessionUploadItem`: `ProcessId`, `ProcessName`, `ProcessPath`,
  `WindowHandle`, and `WindowTitle`.
- Updated `FocusSessionizer` to preserve safe process id/name/path and window
  handle from foreground snapshots while leaving `WindowTitle` null until a
  privacy setting explicitly allows it.
- Updated `SqliteFocusSessionRepository` to create and backfill local
  `focus_session` metadata columns and round-trip them in queries.
- Updated the WPF tracking coordinator's `focus_session` outbox payload to
  include the same metadata.
- Updated server `FocusSessionEntity`, EF mapping, and upload service so the
  integrated DB persists the metadata.
- Added server schema tests for required focus process/window fields and web
  nullable URL/title plus capture provenance fields.
- Generated server migration
  `20260428165251_AddFocusSessionWindowMetadata`, which also captures prior
  web-session nullable URL/title and capture provenance schema changes.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal` and `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1
  -v minimal`; all 154 .NET tests passed.
- Verified coverage generation with `scripts\test-coverage.ps1`; current
  overall line coverage is 92.8%, Domain 89.3%, Windows.Presentation 97.6%,
  Windows 92.5%, Windows.App 85.3%, and Server 96.1%.
- Added `DeviceStateSessionEntity`, `AppFamilyEntity`, and
  `AppFamilyMappingEntity` to the server integrated DB model.
- Added EF mappings for `device_state_sessions`, `app_families`, and
  `app_family_mappings`, including unique indexes for `(DeviceId,
  ClientSessionId)`, `Key`, and `(MappingType, MatchKey)`.
- Added relational SQLite fallback tests proving duplicate
  `device_state_sessions` and `app_family_mappings` rows are rejected by a real
  relational provider rather than EF InMemory.
- Generated server migration
  `20260428170042_AddDeviceStateAndAppFamilyTables`.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal` with 0 warnings/errors.
- The first full `dotnet test --no-build` run hit the known intermittent WPF
  XAML lazy-load failure in `MainWindowSmokeTests`; rerunning the WPF App test
  project passed, then rerunning full `dotnet test Woong.MonitorStack.sln
  --no-build -maxcpucount:1 -v minimal` passed all 158 tests.
- Verified coverage generation with `scripts\test-coverage.ps1`; current
  overall line coverage is 92.9%, Domain 89.3%, Windows.Presentation 97.6%,
  Windows 92.5%, Windows.App 85.3%, and Server 96.3%.
- Added `docs/android-app-usage-contract-decision.md`.
- Decided that Android app usage sync remains on `FocusSessionUploadItem` for
  MVP instead of introducing a separate `AppUsageSession` server contract.
- Documented the Android mapping: package name to `platformAppKey`,
  `source = android_usage_stats`, and null Windows process/window metadata.
- Added `docs/coding-guide.md` as the project-wide coding guide for future
  slices.
- Reopened `total_todolist.md` for Original Intent Restoration and changed the
  Final Definition of Done back to incomplete until the new restoration
  milestones are implemented and verified.
- Added `docs/original-product-intent-audit.md`, `docs/runtime-pipeline.md`,
  `docs/browser-tracking-policy.md`, `docs/privacy-boundaries.md`,
  `docs/wpf-ui-acceptance-checklist.md`,
  `docs/android-ui-screenshot-testing.md`, and
  `docs/future-macos-feasibility.md`.
- Updated `AGENTS.md` and `docs/coding-guide.md` to state the core rule:
  Woong Monitor Stack measures app/window/site usage metadata, not typed input,
  screen contents, clipboard contents, messages, forms, or covert activity.
- Used subagent audits for Windows/WPF/browser and Android. Key findings:
  Windows has foreground/idle/sessionizer/SQLite/browser primitives but lacks
  WPF Start/Stop/Sync UI, app-hosted tracking orchestration, SQLite-backed
  dashboard wiring, and semantic FlaUI acceptance. Android has UsageStats,
  Room, workers, dashboard, settings, and tests, but lacks WorkManager
  scheduling UX, collection-to-outbox wiring, persisted sync opt-in
  enforcement, UI Automator navigation, and Android screenshot tooling.
- Verified the planning/docs slice with `dotnet restore Woong.MonitorStack.sln`,
  `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`,
  and `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1 -v
  minimal`; all 110 .NET tests passed.
- Verified the coding-guide documentation slice with `dotnet restore
  Woong.MonitorStack.sln`, `dotnet build Woong.MonitorStack.sln --no-restore
  -maxcpucount:1 -v minimal`, and `dotnet test Woong.MonitorStack.sln
  --no-build -maxcpucount:1 -v minimal`; all 110 .NET tests passed.
- Added `docs/wpf-csharp-coding-guide.md` as a dedicated WPF/.NET layering
  guide that explains what belongs in Domain, Windows.Presentation, Windows
  infrastructure, Windows.App, and Server.
- Linked the WPF/C# guide from `docs/coding-guide.md`.
- Verified the WPF/C# guide documentation slice with `dotnet restore
  Woong.MonitorStack.sln`, `dotnet build Woong.MonitorStack.sln --no-restore
  -maxcpucount:1 -v minimal`, and `dotnet test Woong.MonitorStack.sln
  --no-build -maxcpucount:1 -v minimal`; all 110 .NET tests passed.
- Added `docs/architecture/reference-rules.md` with project dependency
  direction and the explicit LiveCharts Presentation adapter exception.
- Added `tests/Woong.MonitorStack.Architecture.Tests` with NetArchTest and
  csproj/source checks for forbidden project, package, WPF, server, and Windows
  dependencies.
- Removed the accidental `Windows.Presentation` reference to the Windows
  infrastructure project.
- Refactored WPF startup to `Microsoft.Extensions.Hosting`; `App.xaml.cs` owns
  host lifecycle and resolves `MainWindow` from DI.
- Added typed `DashboardOptions` and registered `MainWindow`,
  `DashboardViewModel`, `IDashboardDataSource`, and `IDashboardClock` through
  DI.
- Kept `MainWindow.xaml.cs` thin and added a DI composition smoke test.
- Verified `MainWindow.xaml` is readable WPF XAML and added the missing app
  usage chart binding alongside activity and domain charts.
- Expanded dashboard ViewModel tests for Today, rolling, and custom ranges,
  empty state, invalid timezone behavior, top domain, row ordering, chart mapper
  empty input, timezone labels, and retained LiveCharts mapper behavior.
- Added `coverage.runsettings`, `scripts\test-coverage.ps1`,
  `scripts/test-coverage.sh`, and ReportGenerator local tool registration.
- Added `docs/architecture/coverage-quality-gate.md`; current coverage snapshot
  is overall 91.7%, Domain 88.6%, Windows.Presentation 99.0%, Windows 91.1%,
  Windows.App 51.0%, Server 96.0%.
- Verified `dotnet restore`, `dotnet build --no-restore`,
  `dotnet test --no-build`, coverage collection/report generation, and Windows
  smoke tool.
- Added `docs/completion-audit.md` after checking PRD/TODO consistency,
  hidden TODO/FIXME markers, Android device availability, and the full
  validation matrix. Historical note: physical Android resource measurement was the open item then; it is now optional future hardening because emulator evidence is the accepted baseline.
- Added local WPF UI snapshot automation with FlaUI under
  `tools/Woong.MonitorStack.Windows.UiSnapshots`, stable MainWindow
  AutomationIds, `scripts/run-ui-snapshots.ps1`, and
  `docs/ui-snapshot-testing.md`.
- Verified `scripts/run-ui-snapshots.ps1` locally. It created
  `artifacts/ui-snapshots/latest/` with `01-startup.png`,
  `02-dashboard-after-refresh.png`, `03-dashboard-period-change.png`,
  `04-settings.png`, optional visible crops, and `report.md`.
- Added root `AGENTS.md` with mandatory installed skill usage and always-on
  todo/test/build/commit/push workflow.
- Added `total_todolist.md` as the full PRD checklist.
- Added common domain models: `Device`, `Platform`, `AppFamily`,
  `PlatformApp`, `FocusSession`, `WebSession`, `DeviceStateSession`,
  `DailySummary`.
- Added contract DTOs for device registration and focus/web/raw upload batches.
- Added `DomainNormalizer` and `DailySummaryCalculator`.
- Added app-family grouping support so platform app keys can roll up to a
  shared family label in daily top apps.
- Added explicit null guards for upload batch request item lists.
- Added Windows tracking project and tests.
- Added `FocusSessionizer` app-change, same-window, and idle-state behavior.
- Added pure `IdleDetector`.
- Added foreground window collector abstraction with fake-testable reader/clock.
- Added Windows `user32.dll` foreground window and last-input readers behind
  interfaces.
- Added `TrackingPoller` orchestration and Windows smoke console.
- Smoke run captured foreground window `Codex.exe` / title `Codex` on
  2026-04-28.
- Added `Microsoft.Data.Sqlite` local persistence.
- Added focus session repository with duplicate-safe insert and range query.
- Added web session repository linked by `focusSessionId`.
- Added sync outbox repository with pending, synced, failed, and retry count.
- Added WPF app project targeting `net10.0-windows`.
- Added WPF presentation project with CommunityToolkit.Mvvm.
- Added dashboard presentation test project.
- Added `DashboardViewModel` with today, 1h, 6h, 24h, and custom period
  selection.
- Added dashboard summary refresh behavior that reports active, idle, web, top
  app, and top domain totals through public ViewModel properties.
- Added `DashboardSummaryCard` models and tested the public summary-card
  collection.
- Added `SelectDashboardPeriodCommand` for XAML period filter buttons.
- Bound the WPF shell to `DashboardViewModel` with period buttons and summary
  cards.
- Added an empty dashboard data source placeholder for safe WPF startup before
  the local SQLite dashboard adapter is introduced.
- Added dependency-free dashboard chart points and mapper logic for hourly
  activity, app usage, and domain usage.
- Added ViewModel chart point publication for the dashboard.
- Added LiveCharts2 series mapping through `LiveChartsCore.SkiaSharpView`
  2.0.0.
- Added WPF LiveCharts2 controls through `LiveChartsCore.SkiaSharpView.WPF`
  2.0.0.
- Suppressed the known transitive `NU1701` warnings only in the WPF app project
  with an inline csproj explanation; final build reports 0 warnings.
- Added recent app session row models with timezone-adjusted start time,
  formatted duration, and idle flag.
- Bound the WPF shell to a read-only app sessions `DataGrid`.
- Added recent web session row models with domain, page title, local start
  time, and duration.
- Added live event row models derived from focus and web sessions until raw
  events are wired into the dashboard.
- Replaced the lower dashboard panel with tabs for App Sessions, Web Sessions,
  and Live Event Log.
- Added settings ViewModel defaults: collection visible and sync opt-out.
- Exposed settings through the dashboard ViewModel.
- Added WPF Settings tab bound to the privacy/sync settings state.
- Added WPF app smoke test project targeting `net10.0-windows`.
- Added STA-thread smoke test that constructs `MainWindow` with a
  `DashboardViewModel`.
- Verified 49 tests pass across domain, Windows, presentation, and WPF app
  tests.
- Added ASP.NET Core Web API project and server xUnit integration test project.
- Added `Microsoft.AspNetCore.Mvc.Testing` 10.0.1 for server API tests.
- Replaced the template weather endpoint with `POST /api/devices/register`.
- Added in-memory duplicate-safe device registration service as the first
  vertical API slice.
- Verified 50 tests pass across all current projects.
- Added `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.1 to the server.
- Added `MonitorDbContext` and `DeviceEntity`.
- Configured the server with a local `MonitorDb` PostgreSQL connection string.
- Added model metadata test for the unique `(UserId, Platform, DeviceKey)`
  device index.
- Verified 51 tests pass across all current projects.
- Added server test EF InMemory override for WebApplicationFactory.
- Moved device registration from singleton memory storage to scoped
  `MonitorDbContext` persistence.
- Preserved duplicate-safe registration behavior while proving a `DeviceEntity`
  row is persisted.
- Verified 52 tests pass across all current projects.
- Added `FocusSessionEntity` and unique `(DeviceId, ClientSessionId)` index.
- Added `POST /api/focus-sessions/upload`.
- Added EF-backed upload service returning `Accepted` and `Duplicate` item
  statuses.
- Verified duplicate retry does not insert a second focus session row.
- Verified 53 tests pass across all current projects.
- Added `WebSessionEntity` and unique duplicate-detection index for
  `(DeviceId, FocusSessionId, StartedAtUtc, EndedAtUtc, Url)`.
- Added `POST /api/web-sessions/upload`.
- Added EF-backed web session upload service returning `Accepted` and
  `Duplicate` item statuses.
- Verified duplicate retry does not insert a second web session row.
- Added `RawEventEntity` and unique `(DeviceId, ClientEventId)` index.
- Added `POST /api/raw-events/upload`.
- Added EF-backed raw event upload service returning `Accepted` and
  `Duplicate` item statuses.
- Verified duplicate retry does not insert a second raw event row.
- Added `GET /api/daily-summaries/{summaryDate}` with `userId` and
  `timezoneId` query parameters.
- Added EF-backed daily summary query service that combines all devices for a
  user, excludes idle focus sessions from active totals, reports idle totals
  separately, and groups web totals by requested timezone.
- Verified the summary API combines Windows + Android sessions for the same
  user and ignores another user's device data.
- Added `GET /api/statistics/range` with `userId`, inclusive `from`/`to`, and
  `timezoneId` query parameters.
- Added range aggregation response for total active, idle, web, top apps, and
  top domains.
- Verified the date-range API combines user devices across multiple local dates
  and excludes out-of-range plus other-user data.
- Added Windows sync API client abstraction.
- Added sync outbox repository interface and connected the SQLite outbox
  repository to it.
- Added `WindowsSyncWorker` for processing pending/failed outbox rows.
- Verified a fake API `Accepted` upload marks the outbox item synced.
- Added retry handling for server item-level `Error` upload results.
- Verified a fake API error marks the outbox item failed, increments retry
  count, and stores the server error message.
- Verified server `Duplicate` upload results are treated as synced so retrying
  an already accepted payload does not keep failing locally.
- Added HTTP Windows sync API client for focus, web, and raw outbox aggregate
  types.
- Added `WindowsSyncClientOptions` with a required device token placeholder.
- Verified the HTTP client posts the raw outbox payload to the matching server
  endpoint with the `X-Device-Token` header.
- Added sync checkpoint store abstraction.
- Verified the worker saves a checkpoint timestamp when at least one outbox
  item syncs successfully.
- Verified a real SQLite outbox row is uploaded through the HTTP sync client
  and then marked synced locally.
- Added Android project under `android/` using the local WoongViewActivity
  Gradle Wrapper/AGP 9.1 style because `WoongAndroidBasicProject` was not
  present in the workspace.
- Configured Kotlin source, XML/View UI, ViewBinding, AppCompat,
  ConstraintLayout, RecyclerView, Room runtime/ktx, WorkManager, Retrofit,
  Moshi, OkHttp, JUnit, Espresso, and AndroidX test dependencies.
- Added `MainActivity`, `activity_main.xml`, an empty JUnit smoke test, and an
  empty instrumentation smoke test.
- Verified `.\gradlew.bat testDebugUnitTest --no-daemon --stacktrace`.
- Verified `.\gradlew.bat assembleDebug --no-daemon --stacktrace`.
- Added `UsageAccessPermissionChecker` with a fake-reader JVM unit test.
- Added Android `AppOpsManager`-based usage access permission reader.
- Added Usage Access Settings intent factory and connected a ViewBinding button
  in `MainActivity` to launch Android settings.
- Verified the settings action factory with a JVM unit test.
- Added pure Kotlin usage event snapshot/session models.
- Added `UsageSessionizer` with resumed-to-paused session creation.
- Verified resumed/paused events create one app session with expected duration.
- Added configurable same-app merge gap to `UsageSessionizer`.
- Verified close same-app pause/resume pairs merge into one session.
- Verified a different app resume closes the previous active app session.
- Added KSP/Room compiler plus Robolectric/Room testing dependencies.
- Added Room `FocusSessionEntity`, `FocusSessionDao`, and `MonitorDatabase`.
- Verified Room in-memory DAO insert/query by local date range with a
  Robolectric component test.
- Added `UsageEventsReader`, Android `UsageStatsManager` event reader, and
  `UsageStatsCollector`.
- Verified the collector reads the requested UTC range and returns events sorted
  by timestamp.
- Added `CollectUsageWorker` with injectable collection runner for WorkManager
  testing.
- Added `AndroidUsageCollectionRunner` to collect UsageStats events, sessionize
  them, and store deterministic `focus_sessions` rows in Room.
- Added Room bulk insert support and a singleton `MonitorDatabase` opener for
  worker execution.
- Declared `PACKAGE_USAGE_STATS` in the Android manifest; permission remains
  explicit through the existing Usage Access settings flow.
- Verified worker behavior with a Robolectric WorkManager test.
- Verified `.\gradlew.bat testDebugUnitTest --no-daemon --stacktrace`.
- Verified `assembleDebug`; AGP reports the expected experimental
  `android.disallowKotlinSourceSets=false` warning required for KSP with
  built-in Kotlin.
- Added pure Kotlin dashboard state models: `DashboardPeriod`,
  `DashboardSnapshot`, `DashboardSessionRow`, and `DashboardUiState`.
- Added `DashboardRepository` and a dependency-light `DashboardViewModel`.
- Verified selecting a dashboard period loads a repository snapshot and exposes
  total active time, top app, idle time, and recent sessions through public
  ViewModel state.
- Added `RoomDashboardRepository` for Today/Yesterday/Recent 7 Days dashboard
  snapshots from local Android Room data.
- Verified Today aggregation excludes idle sessions from active total, tracks
  idle separately, picks the top active package, and formats recent session
  rows using the requested timezone.
- Added XML/ViewBinding `DashboardActivity`.
- Added dashboard XML surface with Today/Yesterday/Recent 7 Days filters,
  active/top-app/idle cards, Usage Access settings action, and recent sessions
  `RecyclerView`.
- Added Espresso smoke test for DashboardActivity display and verified the
  androidTest APK compiles.
- Bound DashboardActivity period buttons to the Room-backed dashboard
  repository through `DashboardViewModel`.
- Added visible value fields for active time, top app, and idle time.
- Added empty recent-session state text and covered it from the Espresso smoke
  surface.
- Added explicit Usage Access guidance text and covered it from the Espresso
  smoke surface.
- Added XML/ViewBinding `SessionsActivity` with a sessions `RecyclerView` and
  empty-state text.
- Added Espresso smoke test for SessionsActivity and verified the androidTest
  APK compiles.
- Checked installed `find-skills` guidance and ran
  `npx skills find android chart mpandroidchart`; no Android-specific
  MPAndroidChart skill was selected.
- Verified MPAndroidChart `v3.1.0` from the official GitHub release page and
  added JitPack repository/dependency configuration.
- Added `DashboardChartMapper` and chart data models that convert durations to
  minute-based MPAndroidChart `Entry`, `BarEntry`, and `PieEntry` values.
- Added MPAndroidChart `LineChart` and `BarChart` views to DashboardActivity.
- Configured chart no-data states and covered chart view presence from the
  Espresso smoke surface.
- Added selected-period label and Espresso click assertions for Today,
  Yesterday, and Recent 7 Days filters.
- Added XML/ViewBinding `SettingsActivity` with visible collection and opt-in
  sync defaults.
- Added Espresso smoke test for SettingsActivity privacy/sync defaults and
  verified the androidTest APK compiles.
- Extended dashboard snapshots and ViewModel state with chart data.
- Added Room-backed active-only chart aggregation for hourly activity and app
  usage.
- Bound non-empty chart data into DashboardActivity MPAndroidChart line/bar
  views.
- Added Android Room `sync_outbox` table, DAO, status enum, and v1-to-v2
  database migration.
- Verified pending outbox insert/query and synced transition with a Robolectric
  Room test.
- Added OkHttp/Moshi Android sync client for focus session upload.
- Added Moshi Kotlin dependency and numeric upload status adapter.
- Verified the Android sync client posts the contract path/payload and parses
  numeric upload status responses.
- Added `AndroidOutboxSyncProcessor` for focus-session outbox upload batches.
- Added `AndroidSyncApi` and `SyncOutboxStore` interfaces so the processor can
  be unit-tested without WorkManager or HTTP.
- Verified `Accepted` and `Duplicate` server item results mark outbox items as
  synced, while `Error` marks the item failed and retryable with the server
  error message.
- Added Room DAO `markFailed` behavior that increments retry count, stores the
  last error, and keeps the row in the pending query.
- Added `AndroidSyncRunner` and `AndroidRoomSyncRunner` to connect WorkManager
  execution to the Room outbox plus OkHttp sync client.
- Added `AndroidSyncWorker` with device/base URL input keys, pending limit,
  success output counts, and retry behavior when any item fails or sync
  configuration is missing.
- Verified sync worker success output and failed-item retry behavior with a
  Robolectric WorkManager test.
- Added `DailySummaryClient` for
  `GET /api/daily-summaries/{summaryDate}?userId=...&timezoneId=...`.
- Added Android summary response models for total active, idle, web, top apps,
  and top domains.
- Verified summary client path/query construction and Moshi response parsing
  against the server's daily summary contract.
- Added `DailySummaryViewModel` and repository abstraction for loading the
  previous local day and formatting active, idle, web, top app, and top domain
  summary state.
- Added XML/ViewBinding `DailySummaryActivity` registered in the manifest.
- Added summary Activity smoke coverage that verifies previous-day date and
  summary values render from intent-provided state.
- Added `MorningSummaryNotificationWorker` and notification runner seam for
  WorkManager-triggered morning summary notifications.
- Declared `POST_NOTIFICATIONS`; runtime permission UX remains a hardening
  concern before release.
- Verified the notification worker delegates title/text to the runner and
  returns success.
- Added server `DailySummaryEntity` and `daily_summaries` EF model mapping with
  a unique `(UserId, SummaryDate, TimezoneId)` index.
- Added `DailySummaryAggregationService` that generates and upserts persisted
  daily summaries from integrated focus/web session data.
- Verified persisted daily summary totals combine Windows + Android devices,
  exclude idle time from active totals, compute top apps/domains, and ignore
  another user's data.
- Added initial server `AppFamilyMapper` so known Windows/Android platform app
  keys such as `chrome.exe` and `com.android.chrome` roll up into `Chrome`.
- Updated daily summary and date-range top app aggregation to group by app
  family labels.
- Verified persisted and API summary top apps combine Windows + Android Chrome
  durations under one `Chrome` row.
- Added persisted summary coverage for a web session that crosses a UTC date
  boundary but belongs to the requested `Asia/Seoul` local date.
- Verified server daily aggregation includes web duration and top domain by
  requested user timezone, not raw UTC date.
- Added persisted summary rerun coverage proving the aggregation service
  updates a single `(UserId, SummaryDate, TimezoneId)` row instead of creating
  duplicates or inflating totals.
- Added Windows `HttpWindowsSummaryApiClient` for
  `GET /api/daily-summaries/{summaryDate}`.
- Verified Windows summary client path/query construction and deserialization
  of integrated summary totals/top apps/top domains.
- Android summary client verification was completed earlier in Milestone 10,
  so both Windows and Android clients now have summary query coverage.
- Added `docs/hardening.md` with DB migration review notes, server/local DB
  separation reminders, required idempotency indexes, raw event retention
  policy, and Android notification permission follow-up.
- Added Windows Settings sync failure status in the dashboard presentation
  model and WPF Settings tab.
- Added Android Settings permission guidance, explicit privacy boundary text,
  Usage Access action, local-only sync status, and retryable sync failure status.
- Verified Android Settings UI hardening with Robolectric and Espresso compile
  coverage.
- Added `docs/performance-checks.md` with Windows collector smoke CPU/memory
  measurements and Android emulator CPU/memory/batterystats smoke results.
- Added root `README.md` and `docs/release-checklist.md` for the final
  release-candidate pass.
- Verified `.\gradlew.bat testDebugUnitTest --no-daemon --stacktrace`.
- Verified `.\gradlew.bat assembleDebug --no-daemon --stacktrace`.
- Verified `.\gradlew.bat assembleDebugAndroidTest --no-daemon --stacktrace`.
- Verified `.\gradlew.bat testDebugUnitTest --no-daemon --stacktrace`.
- Verified `.\gradlew.bat assembleDebug --no-daemon --stacktrace`.
- Verified `.\gradlew.bat assembleDebugAndroidTest --no-daemon --stacktrace`.
- Verified `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`.
- Added `docs/contracts.md` for time/date, device, upload idempotency, and web
  domain policy.
- Verified `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`.
- Pushed latest completed slice to `origin/main`.
- Completed the full release-candidate validation matrix:
  `dotnet test`, `dotnet build`, Windows foreground smoke,
  `testDebugUnitTest`, `assembleDebug`, `assembleDebugAndroidTest`, and
  `connectedDebugAndroidTest`.
- Verified Android Usage Access settings navigation manually on
  `emulator-5554`; tapping `usageAccessSettingsButton` moved focus to
  `com.android.settings/com.android.settings.spa.SpaActivity`.
- Marked `docs/release-checklist.md` and `total_todolist.md` complete for the
  PRD/MVP release-candidate pass.
- Reopened project completion after user clarified that Chrome Extension +
  Native Messaging must be a dedicated milestone rather than an informal
  post-RC follow-up.
- Added Milestone 4.5 to `docs/prd.md` and `total_todolist.md`.
- Added `AGENTS.md` rules for Chrome extension privacy boundaries and
  PostgreSQL/Testcontainers-style relational server integration testing.
- Added Windows-side `ChromeTabChangedMessage` as the first native-message DTO
  seam.
- Verified extension URL payload domain extraction with a TDD test.
- Added `docs/chrome-native-messaging.md` with privacy boundaries and current
  Milestone 4.5 status.
- Added `extensions/chrome/` with an MV3 manifest and `background.js`.
- Verified the manifest declares the required service worker and permissions.
- Added Windows native messaging host registration abstraction and a
  Windows-only registry writer.
- Verified registration writes the expected Chrome HKCU native host key using a
  fake registry writer.
- Added `ChromeNativeMessageParser` for the extension `activeTabChanged` JSON
  payload.
- Verified parsed messages preserve browser family, tab/window ids, domain, and
  UTC timestamp.
- Added `ChromeNativeMessageReceiver` for Chrome's length-prefixed native
  messaging stream protocol.
- Verified the receiver reads a length-prefixed active tab message and returns a
  normalized tab DTO.
- Added `SqliteBrowserRawEventRepository` with a `browser_raw_event` local
  SQLite table.
- Verified browser raw event save/query round-trips Chrome active tab metadata.
- Added `BrowserWebSessionizer` to convert active tab messages into
  `WebSession` intervals.
- Verified active tab changes create a web session for the previous URL.
- Verified duplicate tab events do not inflate duration.
- Added `ChromeNativeMessageIngestionFlow` for receiver -> raw event repository
  -> web-session repository.
- Verified a Chrome active tab URL appears in the Windows local SQLite raw event
  table and that the previous tab is persisted as a `web_session`.
- Added `docs/server-test-db-strategy.md`.
- Added `RelationalTestDatabase` using SQLite in-memory as the current
  relational fallback because Docker/Testcontainers is unavailable here.
- Verified the server device unique index is enforced by a relational provider.
- Verified the relational reset strategy recreates an empty schema.
- Added local `dotnet-ef` 10.0.4 tool manifest and EF Core design package.
- Generated `20260428131352_InitialCreate` for server PostgreSQL schema.
- Added `docs/production-migrations.md` with migration review notes.
- Verified migration files define the core PostgreSQL tables.
- Added `tools/Woong.MonitorStack.Windows.Profile`.
- Ran a 30-second Windows collector polling profile: 59 polls, 406.25 ms CPU,
  23.80 MB peak working set.
- Checked `adb devices -l`; no physical Android device was connected, so
  physical-device resource measurement was blocked at that time; it is now optional future hardening.
- Verified latest .NET restore/test/build after migration and profiling changes.
- Verified Android `testDebugUnitTest` and `assembleDebug` after hardening
  changes.
- Added `AndroidSyncSettings` and
  `SharedPreferencesAndroidSyncSettings`; sync opt-in defaults to false and
  persists enabled state.
- Updated `AndroidSyncWorker` so disabled sync returns a skipped success result
  with zero upload counts and does not invoke the sync runner.
- Verified `.\gradlew.bat testDebugUnitTest --no-daemon --stacktrace` from
  `android/`.
- Verified `.\gradlew.bat assembleDebug --no-daemon --stacktrace` from
  `android/`.
- Verified `.NET` coverage generation with `scripts\test-coverage.ps1`;
  current overall line coverage is 92.9%, Domain 89.3%, Windows 92.5%,
  Windows.Presentation 97.6%, Windows.App 85.3%, and Server 96.3%.
  Android coverage collection is not configured yet, so Android validation for
  this slice is unit-test/build based.
- Recreated ignored `android/local.properties` locally to point Gradle at the
  installed Android SDK; this file remains untracked and must not be committed.
- Added `UsageSyncOutboxEnqueuer` and `FocusSessionSyncOutboxEnqueuer`.
- Updated `AndroidUsageCollectionRunner` to enqueue collected focus sessions
  into `sync_outbox` after local Room storage.
- Updated the Android UsageStats source value to `android_usage_stats` to match
  the shared contract decision.
- Added a `SyncOutboxWriter` interface implemented by `SyncOutboxDao`.
- Updated `scripts\test-coverage.ps1` to pass `-maxcpucount:1`, matching the
  repository's stable .NET validation convention after coverage collection hit
  the known intermittent WPF XAML lazy-load failure once.
- Verified `.\gradlew.bat testDebugUnitTest --no-daemon --stacktrace` from
  `android/`.
- Verified `.\gradlew.bat assembleDebug --no-daemon --stacktrace` from
  `android/`.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal`.
- Verified `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1 -v
  minimal`.
- Verified `.NET` coverage generation with `scripts\test-coverage.ps1`;
  current overall line coverage is 92.4%, Domain 89.3%, Windows 92.5%,
  Windows.Presentation 97.6%, Windows.App 79.9%, and Server 96.3%.
- Added `AndroidManifestPrivacyTest` to enforce
  `android:allowBackup="false"` for local usage metadata.
- Updated `AndroidManifest.xml` to disable app backup.
- Documented Android local metadata backup hardening in
  `docs/privacy-boundaries.md` and `docs/hardening.md`.
- Verified `.\gradlew.bat testDebugUnitTest --no-daemon --stacktrace` from
  `android/`.
- Verified `.\gradlew.bat assembleDebug --no-daemon --stacktrace` from
  `android/`.
- Added `AndroidUsageCollectionSettings` with a SharedPreferences
  implementation that defaults collection scheduling to disabled.
- Added `AndroidUsageCollectionScheduler`, `UsageCollectionWorkScheduler`, and
  `WorkManagerUsageCollectionWorkScheduler` for unique 15-minute
  `CollectUsageWorker` scheduling.
- Verified the scheduler cancels periodic work when collection is disabled,
  cancels when Usage Access is missing, and schedules only when both persisted
  collection setting and Usage Access permission allow it.
- Updated `docs/runtime-pipeline.md` so local outbox creation is separate from
  server upload opt-in: outbox rows are local retry metadata, and upload remains
  suppressed while sync opt-in is off.
- Verified `.\gradlew.bat testDebugUnitTest --no-daemon --stacktrace` from
  `android/`.
- Verified `.\gradlew.bat assembleDebug --no-daemon --stacktrace` from
  `android/`.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal`.
- Verified `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1 -v
  minimal`.
- Added `FocusSessionDao.queryRecent(limit)` and verified newest sessions are
  returned first.
- Added `RoomSessionsRepository`, `SessionRow`, and duration formatting for
  Android app usage rows.
- Updated `SessionsActivity` to load recent sessions from `MonitorDatabase` on
  a background thread and hide the empty state when rows exist.
- Verified `.\gradlew.bat testDebugUnitTest --no-daemon --stacktrace` from
  `android/`.
- Verified `.\gradlew.bat assembleDebug --no-daemon --stacktrace` from
  `android/`.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal`.
- Verified `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1 -v
  minimal`.
- Added `DailySummaryActivityLoader` and `DailySummaryActivityLoadRequest`.
- Updated `DailySummaryActivity` so `EXTRA_BASE_URL` + `EXTRA_USER_ID` triggers
  repository/client loading for the previous local day while preserving the
  existing intent-display fallback used by smoke tests.
- Verified `.\gradlew.bat testDebugUnitTest --no-daemon --stacktrace` from
  `android/`.
- Verified `.\gradlew.bat assembleDebug --no-daemon --stacktrace` from
  `android/`.
- Verified `.\gradlew.bat assembleDebugAndroidTest --no-daemon --stacktrace`
  from `android/`.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal`.
- Verified `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1 -v
  minimal`.
- Added `NotificationPermissionPolicy` and `NotificationPermissionController`
  for Android 13+ `POST_NOTIFICATIONS` runtime requests.
- Updated Android Settings XML and strings with notification permission
  guidance plus an explicit request button.
- Extended Settings Robolectric coverage for notification permission copy and
  button binding.
- Verified `.\gradlew.bat testDebugUnitTest --no-daemon --stacktrace` from
  `android/`.
- Verified `.\gradlew.bat assembleDebug --no-daemon --stacktrace` from
  `android/`.
- Verified `.\gradlew.bat assembleDebugAndroidTest --no-daemon --stacktrace`
  from `android/`.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal`.
- Verified `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1 -v
  minimal`.
- Added AndroidX UI Automator to the Gradle version catalog and androidTest
  dependencies.
- Added `UsageAccessSettingsNavigationTest`, which launches Settings, taps the
  Usage Access button, and waits for the system Settings package.
- Verified `.\gradlew.bat assembleDebugAndroidTest --no-daemon --stacktrace`
  from `android/`.
- Verified `.\gradlew.bat testDebugUnitTest --no-daemon --stacktrace` from
  `android/`.
- Verified `.\gradlew.bat assembleDebug --no-daemon --stacktrace` from
  `android/`.
- Checked `adb devices -l`; no device/emulator was attached, so
  `connectedDebugAndroidTest` remains blocked for this smoke.
- Verified `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v
  minimal`.
- Verified `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1 -v
  minimal`.

## 2026-04-29 WPF Product UI Goal Slice

- Added `docs/wpf-ui-plan.md` as the durable text version of the provided WPF
  UI goal image and instructions.
- Reopened `total_todolist.md` with Milestone 30 for the WPF product UI goal.
- Updated `MainWindow.xaml` with a larger product dashboard layout:
  header title/subtitle, tracking/sync/privacy badges, readable control bar,
  Current Focus panel, four metric cards, chart titles, wider App/Web/Live grids,
  and clearer Settings sections.
- Moved current process/top app text out of the Header.
- Added Current Focus fields for browser domain, last poll time, and last DB
  write time.
- Updated ViewModel display state for `TrackingBadgeText`, `SyncBadgeText`,
  `PrivacyBadgeText`, `CurrentBrowserDomainText`, `LastPollTimeText`, and
  `LastDbWriteTimeText`.
- Updated summary cards to the product metric vocabulary: Active Focus,
  Foreground, Idle, and Web Focus.
- Expanded dashboard row models so the UI can show process, start/end, state,
  window/source, URL mode, browser, confidence, app, and domain columns.
- Fixed a runtime UI bug: a later poll with no newly persisted session no
  longer clears the last persisted session text.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter MainWindowUiExpectationTests`
- `dotnet test tests\Woong.MonitorStack.Windows.Presentation.Tests\Woong.MonitorStack.Windows.Presentation.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter UpdateCurrentActivity_WhenLaterPollHasNoPersistedSession_KeepsLastPersistedSession`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1 -Seconds 2`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Current .NET test count is 177 passing tests. Coverage report generated at
`artifacts/coverage/SummaryGithub.md`; current overall line coverage is 92.2%,
Windows.Presentation is 95.7%, and Windows.App is 86.3%.

Latest WPF acceptance artifact:

`artifacts/wpf-ui-acceptance/20260429-095154`

Remaining Milestone 30 work:

- Add visible empty-state overlays for all chart panels.
- Add richer Live Event Log semantic tests for start/close/persist/outbox/sync
  event names.
- Add explicit Settings tests for full URL off, sync off, and folder command
  availability/disabled state.
- Capture explicit 1920/1366/1024 WPF snapshots.
- Update browser UI docs if new browser connection/status labels are added.

## Next Highest Priority

Continue with the next product/UI correctness gap from the read-only audits:
decide Details rows-per-page pagination for Milestone 31 versus a future slice.
If implementing now, add visible row collections and page commands in
Presentation before changing XAML. Acceptance viewport variants remain open.

## 2026-04-29 WPF Chart Axis Slice

- Updated `DashboardChartMapper` hour labels from `HH:mm` to compact `HH`
  labels for hourly Active Focus.
- Extended `DashboardLiveChartsData` with tested `XAxes`, `YAxes`, and
  `EmptyStateText`.
- Updated `DashboardLiveChartsMapper` so Cartesian charts expose category
  labels on X axes and minute labels on Y axes.
- Bound `HourlyActivityChart` and `AppUsageChart` X/Y axes in `MainWindow.xaml`.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.Presentation.Tests\Woong.MonitorStack.Windows.Presentation.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "DashboardChartMapperTests|DashboardLiveChartsMapperTests"`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1 -Seconds 2`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF acceptance artifact:

`artifacts/wpf-ui-acceptance/20260429-100159`

Current overall line coverage is 92.3%, Windows.Presentation is 96.6%, and
Windows.App is 86.3%.

## 2026-04-29 WPF Chart Empty-State Slice

- Added visible chart empty-state overlays for hourly activity, app usage, and
  domain usage panels.
- Added `MainWindow_WithEmptyData_ShowsReadableChartEmptyStates` to verify the
  WPF UI exposes `No data for selected period` instead of relying on broken
  empty chart axes.
- Kept the semantic WPF acceptance path passing after the chart UI change.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter MainWindow_WithEmptyData_ShowsReadableChartEmptyStates`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1 -Seconds 2`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF acceptance artifact:

`artifacts/wpf-ui-acceptance/20260429-100746`

Current overall line coverage is 92.3%, Windows.Presentation is 96.6%, and
Windows.App is 86.3%.

## 2026-04-29 WPF Componentization Guidance

The user provided a component architecture direction for the WPF dashboard:
`MainWindow -> DashboardView -> HeaderStatusBar / ControlBar /
CurrentFocusPanel / SummaryCardsPanel / ChartsPanel / DetailsTabsPanel`, plus
reusable controls (`StatusBadge`, `MetricCard`, `SectionCard`, `DetailRow`,
`EmptyState`) and style dictionaries. This guidance is saved in
`docs/wpf-ui-plan.md` and tracked as Milestone 31 in `total_todolist.md`.

The `DashboardView`, `HeaderStatusBar`, `ControlBar`, `CurrentFocusPanel`,
`SummaryCardsPanel`, reusable `MetricCard`, `ChartsPanel`, and reusable
`EmptyState` extractions are now complete, the runtime poll-tick persistence
quality gap is covered, and app/domain chart `?곸꽭蹂닿린` actions now switch to
the App Sessions and Web Sessions details tabs.

## 2026-04-29 WPF DashboardView Extraction Slice

- Extracted the existing vertical dashboard layout from `MainWindow.xaml` into
  `Views/DashboardView.xaml`.
- Added `Views/DashboardView.xaml.cs`.
- Kept `MainWindow.xaml` as a thin shell that hosts `DashboardView`.
- Updated `MainWindowUiExpectationTests` so
  `MainWindow_ExposesDashboardControlsAndCommandBindings` asserts the hosted
  `DashboardView` is present while preserving the dashboard control and command
  binding expectations.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter MainWindow_ExposesDashboardControlsAndCommandBindings`
  passed.
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
  passed all 26 Windows App tests.
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
  passed.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
  passed.
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1 -Seconds 2`
  passed and wrote WPF acceptance evidence at
  `artifacts/wpf-ui-acceptance/20260429-102017`.
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`
  completed successfully and generated the coverage report.

Remaining Milestone 31 componentization work stays open, including remaining
reusable controls, style dictionaries, `wpfelements.md` alignment decisions,
final componentization verification, and commit/push.

## 2026-04-29 WPF HeaderStatusBar Extraction Slice

- Added a RED componentization test,
  `DashboardView_HostsHeaderStatusBarAndPreservesHeaderContent`, proving the
  dashboard hosts a `HeaderStatusBar` while preserving header title/subtitle,
  tracking/sync/privacy badges, and the absence of current process text in the
  header.
- Extracted the header markup from `Views/DashboardView.xaml` into
  `Views/HeaderStatusBar.xaml`.
- Added `Views/HeaderStatusBar.xaml.cs`.
- Kept existing AutomationIds: `HeaderArea`, `TrackingStatusBadge`,
  `SyncStatusBadge`, and `PrivacyStatusBadge`.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter DashboardView_HostsHeaderStatusBarAndPreservesHeaderContent`
  failed before implementation and passed after extraction.
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
  passed all 27 Windows App tests.
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
  passed.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
  passed.
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1 -Seconds 2`
  passed and wrote WPF acceptance evidence at
  `artifacts/wpf-ui-acceptance/20260429-103315`.
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`
  completed successfully; overall line coverage is 92.3%.

ControlBar is the next completed componentization slice after HeaderStatusBar.

## 2026-04-29 WPF ControlBar Extraction Slice

- Added a RED componentization test,
  `DashboardView_HostsControlBarAndPreservesCommandBindings`, proving the
  dashboard hosts a `ControlBar` while preserving Start/Stop/Refresh/Sync
  command bindings, period/custom controls, readable control-bar expectations,
  and stable AutomationIds.
- Extracted the control-bar markup from `Views/DashboardView.xaml` into
  `Views/ControlBar.xaml`.
- Added `Views/ControlBar.xaml.cs`.
- Kept existing command bindings and AutomationIds intact through
  `<views:ControlBar>`.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter DashboardView_HostsControlBarAndPreservesCommandBindings`
  failed before implementation and passed after extraction.
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
  passed all 28 Windows App tests.
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
  passed.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
  passed.
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1 -Seconds 2`
  passed and wrote WPF acceptance evidence at
  `artifacts/wpf-ui-acceptance/20260429-104336`.
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`
  completed successfully; overall line coverage remains about 92.3%.

Runtime poll-tick persistence coverage is the next completed quality slice
after ControlBar extraction.

## 2026-04-29 Runtime Poll-Tick Persistence Gap

- Added the runtime confidence test
  `MainWindowTrackingPipelineTests.PollTick_WhenForegroundChanges_PersistsClosedSessionAndRefreshesDashboardBeforeStop`.
- It proves Start -> DispatcherTimer poll foreground change -> closed Code.exe
  session persisted to SQLite -> focus_session outbox queued -> dashboard
  summary/recent rows refreshed while `TrackingStatus` remains `Running`,
  before Stop.
- No production code changes were needed.

Verified:

- Focused
  `MainWindowTrackingPipelineTests.PollTick_WhenForegroundChanges_PersistsClosedSessionAndRefreshesDashboardBeforeStop`
  passed.
- All Windows App tests passed (29 tests).
- Full `dotnet test` passed.
- Full `dotnet build` passed.
- WPF acceptance passed at `artifacts/wpf-ui-acceptance/20260429-105310`.
- `scripts\test-coverage.ps1` generated the coverage report.

## 2026-04-29 WPF CurrentFocusPanel Extraction Slice

- Added the RED componentization test
  `DashboardView_HostsCurrentFocusPanelAndPreservesCurrentFocusBindings`.
- Extracted the current focus markup from `Views/DashboardView.xaml` into
  `Views/CurrentFocusPanel.xaml`.
- Added `Views/CurrentFocusPanel.xaml.cs`.
- Kept `DashboardView` hosting `<views:CurrentFocusPanel>`.
- Preserved `CurrentActivityPanel` and all child AutomationIds.

Verified:

- Focused
  `DashboardView_HostsCurrentFocusPanelAndPreservesCurrentFocusBindings`
  passed.
- All Windows App tests passed (30 tests).
- Full `dotnet test` passed.
- Full `dotnet build` passed.
- WPF acceptance passed at
  `artifacts/wpf-ui-acceptance/20260429-110649`.
- `scripts\test-coverage.ps1` generated the coverage report with overall line
  coverage 92.4%.

The next completed componentization slice is `SummaryCardsPanel` plus reusable
`MetricCard` extraction.

## 2026-04-29 WPF SummaryCardsPanel Extraction Slice

- Added the RED componentization test
  `DashboardView_HostsSummaryCardsPanelAndPreservesSummaryCardContent`.
- Added the reusable-control test
  `MetricCard_RendersLabelValueAndSubtitle`.
- Extracted the summary-card markup from `Views/DashboardView.xaml` into
  `Views/SummaryCardsPanel.xaml`.
- Added `Views/SummaryCardsPanel.xaml.cs`.
- Added reusable `Controls/MetricCard.xaml` and `Controls/MetricCard.xaml.cs`.
- Kept `DashboardView` hosting `<views:SummaryCardsPanel>`.
- Preserved `SummaryCardsContainer` AutomationId and summary card content.

Verified:

- Focused
  `DashboardView_HostsSummaryCardsPanelAndPreservesSummaryCardContent` and
  `MetricCard_RendersLabelValueAndSubtitle` passed.
- All Windows App tests passed (32 tests).
- Full `dotnet test` passed.
- Full `dotnet build` passed.
- WPF acceptance passed at
  `artifacts/wpf-ui-acceptance/20260429-112103`.
- `scripts\test-coverage.ps1` generated the coverage report with overall line
  coverage 92.3%.

Next highest priority is `ChartsPanel` plus reusable `EmptyState` extraction
while preserving chart AutomationIds, chart empty states, bindings, behavior
tests, and semantic WPF acceptance.

## 2026-04-29 WPF ChartsPanel Extraction Slice

- Added the RED componentization test
  `DashboardView_HostsChartsPanelAndPreservesChartContent`.
- Added the reusable-control test
  `EmptyState_RendersBoundTextWithTextAutomationId`.
- Extracted the chart markup from `Views/DashboardView.xaml` into
  `Views/ChartsPanel.xaml`.
- Added `Views/ChartsPanel.xaml.cs`.
- Added reusable `Controls/EmptyState.xaml` and `Controls/EmptyState.xaml.cs`.
- Kept `DashboardView` hosting `<views:ChartsPanel>`.
- Preserved `ChartArea`, chart AutomationIds, empty-state TextBlock
  AutomationIds, LiveCharts bindings, and Korean chart headings.

Verified:

- Focused `DashboardView_HostsChartsPanelAndPreservesChartContent` and
  `EmptyState_RendersBoundTextWithTextAutomationId` passed.
- All Windows App tests passed (34 tests).
- Full `dotnet test` passed.
- Full `dotnet build` passed.
- WPF acceptance passed at
  `artifacts/wpf-ui-acceptance/20260429-113407`.
- `scripts\test-coverage.ps1` generated the coverage report with overall line
  coverage 92.3%.

`wpfelements.md` audit notes still to resolve:

- Decide whether Domain Focus should become a Cartesian/ranking chart or keep
  the current PieChart as a documented exception.
- Decide whether Details rows-per-page pagination is part of Milestone 31 or a
  future slice.
- Make SettingsPanel explicit about Sync endpoint, page-title capture,
  domain-only browser storage, runtime controls, storage actions, and guarded
  Clear local data.
- Align root scrolling with vertical dashboard scrolling and keep horizontal
  scrolling inside wide grids/charts where needed.
- When style dictionaries land, replace hard-coded colors and duplicate local
  styles in extracted panels instead of adding empty dictionaries only.

## 2026-04-29 WPF Chart Details Tab-Switch Slice

- Added `DetailsTab` as the presentation-level details tab state enum.
- Added `DashboardViewModel.SelectedDetailsTab`.
- Added `ShowAppFocusDetailsCommand` and `ShowDomainFocusDetailsCommand`.
- Added app/domain chart `?곸꽭蹂닿린` buttons in `Views/ChartsPanel.xaml`.
- Bound `DashboardTabs.SelectedValue` to `SelectedDetailsTab` using tab `Tag`
  values so chart detail actions select App Sessions and Web Sessions without
  code-behind.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.Presentation.Tests\Woong.MonitorStack.Windows.Presentation.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1 -Seconds 2`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-114947`.

Coverage after this slice: overall line coverage 92.3%.

## 2026-04-29 WPF DetailsTabsPanel Extraction Slice

- Added the RED componentization test
  `DashboardView_HostsDetailsTabsPanelAndPreservesTabsBinding`.
- Extracted the details tab markup from `Views/DashboardView.xaml` into
  `Views/DetailsTabsPanel.xaml`.
- Added `Views/DetailsTabsPanel.xaml.cs`.
- Kept `DashboardView` hosting `<views:DetailsTabsPanel>`.
- Preserved `DashboardTabs`, App/Web/Live/Settings tab AutomationIds, App/Web
  DataGrid AutomationIds, Settings child AutomationIds, and the
  `SelectedDetailsTab` two-way binding.
- Narrowed `MainWindow_RefreshButtonRendersSummaryCardsAndChartSurface` from a
  whole-window visual-tree traversal to stable Header/Summary/Charts/Details
  AutomationId scopes after the WPF visual tree traversal hung around the
  LiveCharts surface. The test still verifies the same visible dashboard
  content and chart controls.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1 -Seconds 2`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-121155`.

Coverage after this slice: overall line coverage 92.3%.

## 2026-04-29 WPF SettingsPanel Extraction Slice

- Added the RED componentization test
  `DetailsTabsPanel_HostsSettingsPanelInsideSettingsTab`.
- Added Settings behavior tests for privacy-safe defaults, sync two-way binding,
  and runtime/storage disabled actions.
- Extracted the Settings tab content from `Views/DetailsTabsPanel.xaml` into
  `Views/SettingsPanel.xaml`.
- Added `Views/SettingsPanel.xaml.cs`.
- Preserved inherited dashboard `DataContext`, `SettingsTab`, all existing
  Settings child AutomationIds, safe privacy defaults, and sync opt-in behavior.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1 -Seconds 2`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-122321`.

Coverage after this slice: overall line coverage 92.4%.

## 2026-04-29 WPF StatusBadge Extraction Slice

- Added the RED reusable-control test
  `StatusBadge_RendersTextAndPreservesAutomationId`.
- Updated the header regression test to assert the three status badges are
  `StatusBadge` controls while preserving `HeaderArea`, badge AutomationIds,
  header title/subtitle, and no current-process leakage in the header.
- Added `Controls/StatusBadge.xaml` and `Controls/StatusBadge.xaml.cs`.
- Replaced raw header badge `Border`s in `Views/HeaderStatusBar.xaml` with
  `controls:StatusBadge`.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1 -Seconds 2`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-123308`.

Coverage after this slice: overall line coverage 92.3%.

## 2026-04-29 WPF DetailRow Extraction Slice

- Added the RED reusable-control test
  `DetailRow_RendersLabelAndValueWithStableValueAutomationId`.
- Added `Controls/DetailRow.xaml` and `Controls/DetailRow.xaml.cs`.
- Replaced repeated Current Focus label/value `StackPanel` markup with
  `controls:DetailRow`.
- Preserved key Current Focus value AutomationIds including
  `TrackingStatusText`, `CurrentAppNameText`, `CurrentProcessNameText`,
  `CurrentWindowTitleText`, `CurrentBrowserDomainText`,
  `CurrentSessionDurationText`, `LastPersistedSessionText`, and
  `LastPollTimeText`.
- Kept the two-value Last DB write / Sync state block explicit for now to avoid
  hiding behavior inside an over-generalized row control.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1 -Seconds 2`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-124239`.

Coverage after this slice: overall line coverage 92.0%.

## 2026-04-29 WPF SectionCard Extraction Slice

- Added the RED reusable-control test
  `SectionCard_RendersContentAndOptionalActionCommand`.
- Added `Controls/SectionCard.xaml` and `Controls/SectionCard.xaml.cs`.
- Implemented a WPF-only `CardContent` slot with optional title/action header
  so nested dashboard content remains in `Windows.App` and no WPF dependency is
  introduced into `Windows.Presentation`.
- Replaced the `ChartsPanel` outer card `Border` with `controls:SectionCard`
  while preserving `ChartArea`, chart AutomationIds, empty-state TextBlock
  AutomationIds, and app/domain `?곸꽭蹂닿린` tab-switch command behavior.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "SectionCard_RendersContentAndOptionalActionCommand|DashboardView_HostsChartsPanelAndPreservesChartContent|DashboardView_ChartDetailButtonsSelectExpectedDetailsTabs"`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1 -Seconds 2`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-125444`.

Coverage after this slice: overall line coverage 92.1%.

## 2026-04-29 WPF Button Style Dictionary Slice

- Added the RED style-resource test
  `ButtonStyleDictionary_DefinesReadableDashboardButtonStyles`.
- Added `Styles/Buttons.xaml` with shared `DashboardButtonStyle`,
  `PrimaryButtonStyle`, `DangerButtonStyle`, `SecondaryButtonStyle`, and
  `PeriodButtonStyle`.
- Merged the button dictionary from `App.xaml`.
- Replaced duplicate local `DashboardButtonStyle` definitions in
  `Views/ControlBar.xaml` and `Views/SettingsPanel.xaml` with the shared
  resource dictionary while preserving button AutomationIds and command
  bindings.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "ButtonStyleDictionary_DefinesReadableDashboardButtonStyles|DashboardView_HostsControlBarAndPreservesCommandBindings|SettingsPanel_PreservesRuntimeAndStorageActions|MainWindow_ExposesDashboardControlsAndCommandBindings|MainWindow_TabsExposeExpectedListsAndSettingsControls"`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1 -Seconds 2`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-130753`.

Coverage after this slice: overall line coverage 92.1%.

Next highest priority is the next narrow Milestone 31 slice. The read-only
audit recommends root scrolling alignment before broader style churn, then
`Colors.xaml`/`Cards.xaml`, then typography, data grid, and tabs. Keep all
WPF-only resources in `Windows.App`, preserve current semantic/UI acceptance
tests, and avoid adding WPF references to `Windows.Presentation`.

## 2026-04-29 WPF Root Scrolling Alignment Slice

- Added the RED layout test
  `DashboardView_UsesVerticalRootScrollAndKeepsGridHorizontalScroll`.
- Changed `Views/DashboardView.xaml` so the root dashboard `ScrollViewer` keeps
  vertical scrolling but disables root horizontal scrolling.
- Updated the minimum-size tracking pipeline UI test to expect the same
  vertical-only root rule while still proving the App Sessions grid keeps
  horizontal scrolling and the details tabs remain reachable.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "DashboardView_UsesVerticalRootScrollAndKeepsGridHorizontalScroll|MainWindow_AtMinimumSize_KeepsTabsReachableOrProvidesScrolling"`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1 -Seconds 2`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-131541`.

Coverage after this slice: overall line coverage 92.1%.

Next highest priority is `Cards.xaml` extraction. Keep the slice narrow:
resource test first, then move only duplicated card `Border` setters before
the broader color/typography migration.

## 2026-04-29 WPF Cards Style Dictionary Slice

- Added the RED resource test
  `CardStyleDictionary_DefinesReusableDashboardCardStyles`.
- Added `Styles/Cards.xaml` with `DashboardCardBorderStyle` and
  `CompactSurfaceBorderStyle`.
- Merged the cards dictionary from `App.xaml`.
- Replaced duplicated card border setters in `MetricCard`, `SectionCard`,
  `CurrentFocusPanel`, `DetailsTabsPanel`, and the compact `ControlBar`
  surface.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "CardStyleDictionary_DefinesReusableDashboardCardStyles|MetricCard_RendersLabelValueAndSubtitle|SectionCard_RendersContentAndOptionalActionCommand|DashboardView_HostsControlBarAndPreservesCommandBindings|DashboardView_HostsCurrentFocusPanelAndPreservesCurrentFocusBindings|DashboardView_HostsDetailsTabsPanelAndPreservesTabsBinding"`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1 -Seconds 2`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-132227`.

Coverage after this slice: overall line coverage 92.1%.

Next highest priority is `Colors.xaml` extraction. Keep it staged: shared
background/surface/border/text brushes first, then badge/status colors in a
separate follow-up if needed.

## 2026-04-29 WPF Colors Style Dictionary Slice

- Added the RED resource test
  `ColorStyleDictionary_DefinesCoreDashboardBrushes`.
- Added `Styles/Colors.xaml` with shared app background, surface, border,
  primary text, and muted text brushes.
- Merged the colors dictionary before button/card dictionaries in `App.xaml`.
- Updated `Styles/Cards.xaml` to consume `SurfaceBrush` and `BorderBrush`.
- Updated `MainWindow.xaml` content background to use `AppBackgroundBrush`.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "ColorStyleDictionary_DefinesCoreDashboardBrushes|CardStyleDictionary_DefinesReusableDashboardCardStyles|MainWindow_ExposesDashboardControlsAndCommandBindings|MetricCard_RendersLabelValueAndSubtitle|SectionCard_RendersContentAndOptionalActionCommand"`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1 -Seconds 2`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-132937`.

Coverage after this slice: overall line coverage 92.1%.

Next highest priority is typography style extraction. Keep it narrow by moving
common heading/subtitle/section/muted/body TextBlock setters first and leaving
badge/status colors for a separate slice.

## 2026-04-29 WPF Typography Style Dictionary Slice

- Added the RED resource test
  `TypographyStyleDictionary_DefinesDashboardTextStyles`; it failed first on
  the missing `Styles/Typography.xaml` resource.
- Added `Styles/Typography.xaml` with shared heading, subtitle, section title,
  body, muted, and metric-value `TextBlock` styles.
- Merged the typography dictionary from `App.xaml`.
- Updated `HeaderStatusBar`, `SectionCard`, `DetailRow`, and `MetricCard` to
  consume shared typography styles while preserving their bindings and
  AutomationIds.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "TypographyStyleDictionary_DefinesDashboardTextStyles|DashboardView_HostsHeaderStatusBarAndPreservesHeaderContent|DetailRow_RendersLabelAndValueWithStableValueAutomationId|MetricCard_RendersLabelValueAndSubtitle|SectionCard_RendersContentAndOptionalActionCommand"`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1 -Seconds 2`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-134019`.

Coverage after this slice: overall line coverage 92.1%.

Next highest priority is `DataGrid.xaml` extraction. Add a RED resource/style
test first, keep column MinWidth definitions in `DetailsTabsPanel.xaml`, and
preserve DataGrid-level horizontal scrolling for 1024px readability.

## 2026-04-29 WPF DataGrid Style Dictionary Slice

- Added the RED resource test
  `DataGridStyleDictionary_DefinesReadableSessionGridStyle`; it failed first on
  the missing `Styles/DataGrid.xaml` resource.
- Added `Styles/DataGrid.xaml` with shared read-only `SessionDataGridStyle`.
- Merged the DataGrid dictionary from `App.xaml`.
- Updated `DetailsTabsPanel` so App Sessions, Web Sessions, and Live Event Log
  use `SessionDataGridStyle`.
- Preserved explicit column MinWidth definitions in the view and preserved each
  DataGrid's horizontal scrollbar contract for 1024px readability.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "DataGridStyleDictionary_DefinesReadableSessionGridStyle|DashboardView_UsesVerticalRootScrollAndKeepsGridHorizontalScroll|MainWindow_TabsExposeExpectedListsAndSettingsControls"`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1 -Seconds 2`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-134913`.

Coverage after this slice: overall line coverage 92.1%.

Next highest priority is `Tabs.xaml` extraction. Add a RED resource/style test
first and keep `DashboardTabs` selected-value binding intact.

## 2026-04-29 WPF Tabs Style Dictionary Slice

- Added the RED resource test
  `TabsStyleDictionary_DefinesReadableDashboardTabsStyle`; it failed first on
  the missing `Styles/Tabs.xaml` resource.
- Added `Styles/Tabs.xaml` with shared `DashboardTabControlStyle` and
  `DashboardTabItemStyle`.
- Merged the tabs dictionary from `App.xaml`.
- Updated `DetailsTabsPanel` so `DashboardTabs` uses the shared tab styles.
- Preserved selected-value binding, four tab headers, tab reachability at
  minimum size, and existing DataGrid contracts.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "TabsStyleDictionary_DefinesReadableDashboardTabsStyle|MainWindow_TabsExposeExpectedListsAndSettingsControls|MainWindow_AtMinimumSize_KeepsTabsReachableOrProvidesScrolling"`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1 -Seconds 2`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-135614`.

Coverage after this slice: overall line coverage 92.1%.

Next highest priority is the Domain Focus chart mismatch. Convert the current
PieChart to the same Cartesian/ranking chart shape used for app focus, with
tests in Presentation and Windows.App.

## 2026-04-29 WPF Domain Focus Cartesian Chart Slice

- Added the RED WPF expectation that `DomainUsageChart` must be a
  `CartesianChart`; it failed first because the UI still rendered a `PieChart`.
- Replaced `DashboardViewModel.DomainUsageSeries` with
  `DashboardViewModel.DomainUsageChart` so the domain chart uses
  `DashboardLiveChartsData`.
- Removed the Presentation `BuildPieSeries` adapter and reused
  `BuildColumnChart("Domains", ...)` for domain focus.
- Updated `ChartsPanel.xaml` so `DomainUsageChart` is a Cartesian chart with
  bound series and axes.
- Updated Presentation and WPF tests to verify domain labels, series, and chart
  type.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.Presentation.Tests\Woong.MonitorStack.Windows.Presentation.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "BuildColumnChart_MapsDomainLabelsAndValues|SelectPeriod_PublishesLiveChartsSeries"`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "DashboardView_HostsChartsPanelAndPreservesChartContent|MainWindow_RefreshButtonRendersSummaryCardsAndChartSurface|MainWindow_WithEmptyData_ShowsReadableChartEmptyStates"`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1 -Seconds 2`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-140652`.

Coverage after this slice: overall line coverage 92.1%.

Next highest priority is Settings privacy/safety coverage. Add RED tests for
Capture page title off by default, domain-only browser storage on by default,
sync endpoint disabled/absent until opt-in, and guarded clear local data.

## 2026-04-29 WPF Settings Privacy Coverage Slice

- Added RED Settings tests that failed first because the ViewModel did not
  expose explicit safety properties for page-title capture, full URL capture,
  domain-only browser storage, sync endpoint text, and clear-local-data state.
- Added `DashboardSettingsViewModel` safe defaults:
  page-title capture off, full URL capture off, domain-only browser storage on,
  sync endpoint unconfigured, and clear local data disabled.
- Updated `SettingsPanel` to show page title capture, full URL capture,
  domain-only browser storage, sync endpoint, and clear local data controls.
- Kept risky controls disabled by default; sync endpoint only becomes editable
  after sync opt-in.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.Presentation.Tests\Woong.MonitorStack.Windows.Presentation.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter Constructor_DefaultsToVisibleCollectionAndSyncOptOut`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "SettingsPanel_PreservesPrivacyControlsAndSafeDefaults|SettingsPanel_PreservesSyncControlsAndTwoWayBinding|SettingsPanel_PreservesRuntimeAndStorageActions"`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1 -Seconds 2`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-141606`.

Coverage after this slice: overall line coverage 92.1%.

Next highest priority is the Details pagination decision. Decide whether rows
per page belongs in Milestone 31; if yes, start with ViewModel visible row
collections and page command behavior tests before XAML.

## 2026-04-29 WPF Details Pagination Slice

- Treated Details rows-per-page pagination as Milestone 31 scope.
- Added RED Presentation tests for default rows-per-page state and
  next/previous page behavior.
- Added visible App/Web/Live row collections, `RowsPerPageOptions`,
  `CurrentDetailsPage`, `DetailsPageText`, and pager commands to
  `DashboardViewModel`.
- Updated `DetailsTabsPanel` so App Sessions, Web Sessions, and Live Event Log
  bind to visible paged rows and expose a footer with rows-per-page,
  previous/next, and page status controls.
- Added a RED WPF test assertion that caught a missing `Buttons.xaml` merge in
  `DetailsTabsPanel`; fixed it by merging the shared button dictionary.
- Updated `AGENTS.md` to make completed subagent reassignment an explicit
  working rule.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.Presentation.Tests\Woong.MonitorStack.Windows.Presentation.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "DetailsTabs_DefaultRowsPerPageIsTen|DetailsTabs_NextAndPreviousPageUpdateVisibleRows"`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter MainWindow_TabsExposeExpectedListsAndSettingsControls`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1 -Seconds 2`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-143059`.

Coverage after this slice: overall line coverage 91.9%.

Next highest priority is viewport-aware WPF acceptance/snapshot automation:
add RED script/tool contract tests for 1920/1366/1024 screenshots, section
scroll/crops, manifest viewport metadata, and skipped screenshot reasons.

## 2026-04-29 WPF Viewport Acceptance Slice

- Added RED WPF App contract tests for viewport-aware UI acceptance. They
  failed first on missing `--viewport-widths`, required viewport screenshot
  names, section bring-into-view support, and manifest metadata.
- Added `--viewport-widths` support to
  `tools/Woong.MonitorStack.Windows.UiSnapshots`.
- Updated `scripts/run-wpf-ui-acceptance.ps1` and
  `scripts/run-ui-snapshots.ps1` to request the `1920,1366,1024` viewport
  matrix.
- The snapshot tool now captures viewport dashboard screenshots plus summary,
  chart, app-session, web-session, and live-event section screenshots for each
  viewport width.
- The snapshot tool records `viewportWidths` and `skippedScreenshotReasons` in
  `manifest.json`; the latest run had no skipped screenshots.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter WpfUiAcceptanceScriptTests`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1 -Seconds 2`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-144429`.

Coverage after this slice: overall line coverage 91.9%.

Next highest priority is the WPF runtime tracking pipeline gap audit returned
by the subagent: continue with the next TDD vertical slice that proves
Start/Stop/Poll persistence and dashboard refresh behavior through SQLite and
outbox.

## 2026-04-29 WPF Runtime Poll/DB Timestamp Slice

- Added RED WPF App coordinator test
  `PollOnce_WhenForegroundChanges_ReturnsLastPollAndLastDbWriteTimes`; it
  failed first because real coordinator snapshots did not populate
  `LastPollAtUtc`.
- Updated `WindowsTrackingDashboardCoordinator` so Start/Poll/Stop snapshots
  include the poll timestamp.
- Updated focus-session persistence so snapshots include `LastDbWriteAtUtc`
  when SQLite persistence and focus-session outbox enqueue happen.
- Documented the runtime timestamp evidence in `docs/runtime-pipeline.md`.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter PollOnce_WhenForegroundChanges_ReturnsLastPollAndLastDbWriteTimes`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter WindowsTrackingDashboardCoordinatorTests`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1 -Seconds 2`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-145334`.

Coverage after this slice: overall line coverage 92.0%.

Next highest priority is the web persistence refresh signal gap:
`DashboardViewModel.PollTrackingCommand` should refresh when a web session
persists even if no focus session closed.

## 2026-04-29 WPF Web Persistence Refresh Signal Slice

- Added RED Presentation test
  `PollTrackingCommand_WhenWebSessionPersistsWithoutFocusChange_RefreshesDashboard`.
- The test failed first at compile time because `DashboardTrackingSnapshot` did
  not expose a public web-persistence signal.
- Added `DashboardTrackingSnapshot.HasPersistedWebSession`.
- Updated `DashboardViewModel.PollTrackingCommand` so a web-only persistence
  signal refreshes SQLite-backed dashboard data even when no focus session
  closed.
- Documented the signal in `docs/runtime-pipeline.md`.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.Presentation.Tests\Woong.MonitorStack.Windows.Presentation.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter PollTrackingCommand_WhenWebSessionPersistsWithoutFocusChange_RefreshesDashboard`
- `dotnet test tests\Woong.MonitorStack.Windows.Presentation.Tests\Woong.MonitorStack.Windows.Presentation.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1 -Seconds 2`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-150004`.

Coverage after this slice: overall line coverage 92.0%.

Next highest priority is proving real coordinator browser-domain persistence:
add a RED `WindowsTrackingDashboardCoordinator` test that a domain change
persists a linked `web_session`, creates a pending outbox item, and surfaces
the refresh signal to the dashboard.

## 2026-04-29 WPF Browser Persistence Coordinator Slice

- Added RED coordinator test
  `PollOnce_WhenBrowserDomainChanges_PersistsCompletedWebSessionQueuesOutboxAndSignalsDashboardRefresh`.
- The test failed first because `WindowsTrackingDashboardCoordinator` had no
  browser-reader/web-repository constructor path.
- Added foreground snapshot propagation from `TrackingPoller` through
  `FocusSessionizerResult`.
- Added an optional browser reader path to `WindowsTrackingDashboardCoordinator`
  that sanitizes snapshots with DomainOnly storage by default, persists
  completed `web_session` rows to SQLite, enqueues `web_session` outbox rows,
  and returns `HasPersistedWebSession`.
- The test then caught an incorrect web upload `deviceId`; fixed the payload to
  use the current FocusSession device id.
- Added UI-surface test
  `PollTick_WhenBrowserDomainChanges_PersistsWebSessionAndRefreshesWebRowsBeforeStop`.
- That test exposed that `SqliteDashboardDataSource` only queried web sessions
  through persisted focus sessions, hiding completed web sessions for an open
  browser focus. Added `SqliteWebSessionRepository.QueryByRange` and made the
  dashboard read web sessions by their own time range.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter PollOnce_WhenBrowserDomainChanges_PersistsCompletedWebSessionQueuesOutboxAndSignalsDashboardRefresh`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter PollTick_WhenBrowserDomainChanges_PersistsWebSessionAndRefreshesWebRowsBeforeStop`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test tests\Woong.MonitorStack.Windows.Tests\Woong.MonitorStack.Windows.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "FullyQualifiedName~Browser|FullyQualifiedName~SqliteWebSession"`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1 -Seconds 2`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-151739`.

Coverage after this slice: overall line coverage 92.2%.

Next highest priority is to decide the next WPF runtime acceptance gap from the
subagent audit: RealStart evidence depth, semantic SQLite-backed UI acceptance,
or a small refactor to extract the coordinator browser persistence helper
without changing behavior.

## 2026-04-29 WPF RealStart UI Evidence Slice

- Added RED source-contract test
  `RealStartTool_VerifiesPersistedFocusSessionAppearsInRecentAppSessionsList`.
- The test failed first because the RealStart acceptance tool only counted
  SQLite `focus_session` and `sync_outbox` rows.
- Updated the RealStart tool so after Stop it reads the latest persisted
  process/app name from the temp SQLite DB and verifies that the WPF
  `RecentAppSessionsList` automation tree contains that value.
- Kept the check environment-safe: it does not require a specific process name,
  does not upload, and still prints the foreground metadata privacy warning.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter RealStartTool_VerifiesPersistedFocusSessionAppearsInRecentAppSessionsList`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-real-start-acceptance.ps1 -Seconds 2`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1 -Seconds 2`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-152658`.

Coverage after this slice: overall line coverage 92.2%.

Next highest priority from the subagent audits is TrackingPipeline semantic DB
evidence in the snapshot tool: prove it checks temp SQLite for focus_session,
web_session, and sync_outbox rows instead of relying on visible text and
screenshots alone.

## 2026-04-29 WPF Auto-Start And Sync-At-Start Slice

- Added RED Presentation tests proving `StartTrackingCommand` automatically
  requests sync both when sync is enabled and when sync is off.
- The sync-off path remains privacy/safety preserving: it calls the coordinator
  with sync disabled and surfaces `Sync skipped. Enable sync to upload.`
- Added RED WPF App test proving a `MainWindow` constructed with
  `AutoStartTracking` starts tracking on load, shows the current foreground app,
  attempts sync, and leaves the Start button disabled while Running.
- Added `WindowsAppOptions.AutoStartTracking`, controlled by
  `WOONG_MONITOR_AUTO_START_TRACKING` and defaulting on for normal WPF app
  startup. Tests can still use the manual `MainWindow(DashboardViewModel)`
  constructor.
- Updated the browser-domain fallback copy to
  `Browser domain not connected yet. Domain-only privacy is safe.` so a missing
  browser-domain connection does not look like a broken privacy-hidden field.
- Updated RealStart and UI snapshot acceptance tools to tolerate an
  already-running auto-started app instead of assuming `StartTrackingButton` is
  initially enabled.
- The UI snapshot tool explicitly disables auto-start in EmptyData mode and
  enables it in TrackingPipeline mode, preserving deterministic acceptance
  semantics.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.Presentation.Tests\Woong.MonitorStack.Windows.Presentation.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "StartTrackingCommand_WhenSyncIsEnabled_AutomaticallyRequestsSync|StartTrackingCommand_WhenSyncIsOff_AutomaticallyReportsLocalOnlySkippedStatus|UpdateCurrentActivity_WhenBrowserDomainMissing_ExplainsConnectionAndPrivacyState"`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "MainWindow_WhenAutoStartEnabled_StartsTrackingOnLoadedAndAttemptsSync|UiSnapshotsTool_ToleratesAutoStartedTrackingPipeline|RealStartTool_ToleratesAutoStartedTracking|UiSnapshotsTool_DoesNotRequireCodeAsInitialCurrentAppWhenAutoStartAlreadyAdvanced"`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-real-start-acceptance.ps1 -Seconds 2`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-154548`.

Coverage after this slice: overall line coverage 92.0%.

## 2026-04-29 WPF TrackingPipeline SQLite Evidence Slice

- Added RED source-contract tests requiring the local UI snapshot tool to query
  temp SQLite, write a `## SQLite Evidence` report section, and include a
  `databaseEvidence` object in `manifest.json`.
- Added `Microsoft.Data.Sqlite` to `tools/Woong.MonitorStack.Windows.UiSnapshots`
  as a tooling-only dependency. It is used only to inspect local acceptance DB
  artifacts and does not make SQLite a dependency of `Windows.App` or
  `Windows.Presentation`.
- `RunTrackingPipelineAcceptance` now counts `focus_session`, `web_session`,
  and `sync_outbox` rows after the fake pipeline run and adds them to the
  PASS/FAIL/WARN table.
- The latest WPF acceptance report recorded `focus_session=2`,
  `web_session=2`, and `sync_outbox=4`.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "UiSnapshotsTool_TrackingPipelineQueriesTempSqliteDatabase|UiSnapshotsTool_ReportIncludesTrackingPipelineSqliteEvidence|UiSnapshotsTool_ManifestIncludesTrackingPipelineSqliteEvidence"`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-155615`.

Coverage after this slice: overall line coverage 92.0%.

Next highest priority is Milestone 25 EmptyData mode acceptance: prove the
empty dashboard remains stopped, empty, and free of SQLite/outbox rows when the
snapshot tool launches with auto-start disabled.

## 2026-04-29 WPF EmptyData Acceptance And Browser Copy Slice

- Added RED source-contract test
  `UiSnapshotsTool_EmptyDataModeDisablesAutoStartAndVerifiesZeroSqliteRows`.
- EmptyData snapshot mode now always creates a temp `empty-data.db`, sets
  `WOONG_MONITOR_AUTO_START_TRACKING=0`, and records DB evidence with
  `focus_session=0`, `web_session=0`, and `sync_outbox=0`.
- The browser-domain empty state copy now says
  `No browser domain yet. Connect browser capture; app focus is tracked.`
  instead of implying privacy itself is the reason domains are missing.
- Documented that Administrator rights are not a reliable way to read active
  browser tab URLs. App/window focus metadata appears immediately on Start;
  browser-domain metadata requires browser integration or explicit fallback.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.Presentation.Tests\Woong.MonitorStack.Windows.Presentation.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter UpdateCurrentActivity_WhenBrowserDomainMissing_ExplainsCaptureConnectionAndAppFocusState`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "DashboardView_HostsCurrentFocusPanelAndPreservesCurrentFocusBindings|UiSnapshotsTool_EmptyDataModeDisablesAutoStartAndVerifiesZeroSqliteRows"`
- `powershell -ExecutionPolicy Bypass -File scripts\run-ui-snapshots.ps1`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest EmptyData snapshot artifact:
`artifacts/ui-snapshots/latest` from the 20260429-160531 run, with zero DB rows
in `report.md` and `manifest.json`.

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-161328`.

Coverage after this slice: overall line coverage 92.0%.

Next highest priority is Milestone 25 SampleDashboard mode acceptance: add a
deterministic, non-tracking dashboard sample mode for beginner visual review.

## 2026-04-29 WPF Compact Action Button Style Slice

- Added a RED WPF style expectation to require a reusable
  `CompactActionButtonStyle`.
- Added `CompactActionButtonStyle` to `Styles/Buttons.xaml` with compact,
  readable sizing for small chart/card action buttons.
- Replaced duplicate inline MinWidth, MinHeight, Padding, and FontSize setters
  in `ChartsPanel` and `SectionCard` with the shared style.
- Preserved chart detail tab-switch behavior and `SectionCard` action command
  behavior.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter ButtonStyleDictionary_DefinesReadableDashboardButtonStyles`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "SectionCard_RendersContentAndOptionalActionCommand|DashboardView_ChartDetailButtonsSelectExpectedDetailsTabs|ButtonStyleDictionary_DefinesReadableDashboardButtonStyles"`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-193853`.

Coverage after this slice: overall line coverage 91.3%.

## 2026-04-29 Completion Audit Refresh

- Refreshed `docs/completion-audit.md` for the current repository state after
  WPF runtime flush, sync-off, and Chrome cleanup-only sandbox hardening.
- Re-ran Android local unit/build/androidTest APK validation with Gradle
  wrapper; it succeeded, while connected device evidence remains blocked
  because `adb devices -l` reported no attached devices.
- Rechecked hidden work markers and forbidden tracking capability indicators.
  No forbidden product implementation was found; remaining matches are
  documentation/test policy text or benign metadata fields.

Verified:

- `adb devices -l`
- `.\gradlew.bat testDebugUnitTest assembleDebug assembleDebugAndroidTest --no-daemon --stacktrace`

## 2026-04-29 WPF Sync-Off Pending Outbox Slice

- Added WPF runtime integration coverage for Sync Now after local outbox rows
  exist. The test starts Chrome/github.com tracking, stops after seven minutes,
  verifies queued `focus_session` and `web_session` rows, clicks `Sync Now`
  while sync is still off, and proves rows remain `Pending` with null
  `SyncedAtUtc` and zero `RetryCount`.
- No production change was needed for this slice; the missing piece was
  integration evidence at the WPF button + temp SQLite boundary.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter MainWindow_SyncNowButton_WhenSyncOffAfterQueuedRows_LeavesOutboxPendingAndShowsSkippedStatus`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-222624`.

Coverage after this slice: overall line coverage 91.3%.

## 2026-04-29 Chrome Cleanup-Only Sandbox Slice

- Hardened `scripts/run-chrome-native-message-acceptance.ps1 -CleanupOnly` so
  it runs before Chrome for Testing resolution. Manual cleanup no longer
  depends on Chrome for Testing or installed Chrome discovery.
- Added a native-host cleanup guard so cleanup-only uninstall/restore runs once
  and the `finally` block does not remove a key that was just restored.
- Confirmed the cleanup-only dry run prints only the scoped HKCU test host key
  and only scans/stops Chrome processes tied to the generated temp profile
  sandbox.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.Tests\Woong.MonitorStack.Windows.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "AcceptanceScript_CleanupOnlyRunsBeforeChromeResolution|AcceptanceScript_CleanupOnlyDoesNotRunNativeHostCleanupTwice"`
- `powershell -ExecutionPolicy Bypass -File scripts\run-chrome-native-message-acceptance.ps1 -CleanupOnly -DryRun`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Coverage after this slice: overall line coverage 91.3%.

Next componentization work remains the larger hard-coded color/brush cleanup
and any child ViewModel/adaptor extraction that improves testability without
moving behavior into code-behind.

## 2026-04-29 WPF Typography Cleanup Slice

- Added a RED WPF assertion that `EmptyState` renders through a shared text
  style instead of direct foreground/font setters.
- Added `EmptyStateTextStyle` to `Styles/Typography.xaml` and wired
  `Controls/EmptyState.xaml` to that resource.
- Added a RED WPF assertion that the three chart headings resolve shared
  section-title typography.
- Merged `Styles/Typography.xaml` into `ChartsPanel` and replaced inline
  chart heading font/weight/foreground setters with `SectionTitleTextStyle`.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter EmptyState_RendersBoundTextWithTextAutomationId`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter DashboardView_ChartsPanelUsesSharedSectionTitleTypography`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "EmptyState_RendersBoundTextWithTextAutomationId|DashboardView_ChartsPanelUsesSharedSectionTitleTypography|DashboardView_HostsChartsPanelAndPreservesChartContent"`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-195534`.

Coverage after this slice: overall line coverage 91.3%.

Next componentization work should continue reducing remaining direct
foreground/background/font setters, with `SettingsPanel` and `HeaderStatusBar`
as likely candidates.

## 2026-04-29 WPF Header Badge Color Resource Slice

- Added a RED WPF assertion that the three header status badges use shared
  named brushes instead of inline color literals.
- Added tracking, sync, and privacy badge background/border/text brushes to
  `Styles/Colors.xaml`.
- Replaced `HeaderStatusBar` inline badge color values with static resource
  references while preserving AutomationIds and badge text bindings.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter HeaderStatusBar_UsesSharedBadgeColorResources`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-200235`.

Coverage after this slice: overall line coverage 91.3% (3353/3671).

Next componentization work should move to `SettingsPanel` typography/status
brush cleanup, because HeaderStatusBar badge color literals are now gone.

## 2026-04-29 WPF Settings Heading Typography Slice

- Added a RED WPF assertion that the Settings tab section headings use a
  shared typography resource.
- Added `SettingsSectionTitleTextStyle` to `Styles/Typography.xaml`.
- Replaced inline FontWeight, FontSize, and Foreground setters on the
  Privacy/Sync/Runtime headings in `SettingsPanel`.
- Preserved the existing Settings tab controls, privacy defaults, and sync
  bindings.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter SettingsPanel_UsesSharedSectionHeadingTypography`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "SettingsPanel_UsesSharedSectionHeadingTypography|SettingsPanel_PreservesPrivacyControlsAndSafeDefaults|SettingsPanel_PreservesSyncControlsAndTwoWayBinding"`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-200823`.

Coverage after this slice: overall line coverage 91.3%.

Next componentization work should continue in `SettingsPanel`, preferably
muted body text and sync warning/status brush resources, one RED/GREEN slice at
a time.

## 2026-04-29 WPF Settings Muted Text Typography Slice

- Added a RED WPF assertion that Settings muted helper text uses a shared
  typography resource.
- Added `SettingsMutedTextStyle` to `Styles/Typography.xaml`.
- Replaced inline Foreground/FontSize setters on the browser URL privacy note,
  sync mode label, poll interval label, and idle threshold label.
- Left the sync warning/status color as a separate future slice so the status
  color policy can be tested independently.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter SettingsPanel_UsesSharedMutedTextTypography`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "SettingsPanel_UsesSharedMutedTextTypography|SettingsPanel_PreservesPrivacyControlsAndSafeDefaults|SettingsPanel_PreservesSyncControlsAndTwoWayBinding"`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-201406`.

Coverage after this slice: overall line coverage 91.3%.

Next componentization work should test and extract the Settings sync warning
text brush/style, then audit remaining inline settings font setters such as the
sync endpoint TextBox.

## 2026-04-29 WPF Settings Warning Text Typography Slice

- Added a RED WPF assertion that the Settings sync status warning uses a shared
  warning brush/style.
- Added `WarningTextBrush` to `Styles/Colors.xaml`.
- Added `SettingsWarningTextStyle` to `Styles/Typography.xaml`.
- Replaced inline Foreground/FontSize setters on `SyncStatusLabel` with the
  shared warning style while preserving sync-off/local-only copy.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter SettingsPanel_UsesSharedWarningTextTypography`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "SettingsPanel_UsesSharedWarningTextTypography|SettingsPanel_PreservesSyncControlsAndTwoWayBinding"`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-201950`.

Coverage after this slice: overall line coverage 91.3%.

Next componentization work should address the remaining Settings TextBox font
setter with a shared input style, then re-audit XAML for remaining inline
colors and duplicated typography.

## 2026-04-29 WPF Settings Input Style Slice

- Added a RED WPF assertion that the Settings sync endpoint TextBox uses a
  shared input style.
- Added `Styles/Inputs.xaml` with `SettingsInputTextBoxStyle`.
- Merged the input style dictionary into `SettingsPanel`.
- Replaced the sync endpoint TextBox inline FontSize setter with the shared
  input style while preserving two-way binding and disabled-by-default sync
  behavior.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter SettingsPanel_UsesSharedInputStyle`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "SettingsPanel_UsesSharedInputStyle|SettingsPanel_PreservesSyncControlsAndTwoWayBinding"`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-202524`.

Coverage after this slice: overall line coverage 91.3%.

Next componentization work should re-audit WPF XAML for remaining inline
colors/typography and then decide whether the Milestone 31 style-dictionary
cleanup item can be closed or needs one more targeted slice.

## 2026-04-29 WPF Settings CheckBox Style Slice

- Added a RED WPF assertion that Settings privacy/sync checkboxes use a shared
  checkbox style.
- Added `SettingsCheckBoxStyle` to `Styles/Inputs.xaml`.
- Replaced repeated inline FontSize and Margin setters on Settings checkboxes
  with the shared style.
- Preserved safe privacy defaults and sync opt-in behavior.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter SettingsPanel_UsesSharedCheckBoxStyle`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "SettingsPanel_UsesSharedCheckBoxStyle|SettingsPanel_PreservesPrivacyControlsAndSafeDefaults|SettingsPanel_PreservesSyncControlsAndTwoWayBinding"`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-203158`.

Coverage after this slice: overall line coverage 91.3%.

Next componentization work should focus on `CurrentFocusPanel` inline title and
browser-capture status typography, since SettingsPanel duplicated setters are
now largely covered by shared styles.

## 2026-04-29 WPF Current Focus Typography Slice

- Added RED WPF expectation
  `CurrentFocusPanel_UsesSharedTypographyForTitleAndPersistenceStatus`.
- Added `CurrentFocusValueTextStyle` and `CurrentFocusSecondaryTextStyle` to
  `Styles/Typography.xaml`.
- Merged `Styles/Typography.xaml` into `CurrentFocusPanel` and replaced inline
  section title, last DB write value, and sync status font/color setters with
  shared typography resources.
- Preserved Current Focus AutomationIds and existing bindings.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter CurrentFocusPanel_UsesSharedTypographyForTitleAndPersistenceStatus`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "CurrentFocusPanel_UsesSharedTypographyForTitleAndPersistenceStatus|DashboardView_HostsCurrentFocusPanelAndPreservesCurrentFocusBindings|TypographyStyleDictionary_DefinesDashboardTextStyles"`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-204901`.

Coverage after this slice: overall line coverage 91.3%.

Next componentization work should re-audit remaining XAML inline colors and
decide whether the Milestone 31 style cleanup can close or needs one more
targeted slice.

## 2026-04-29 WPF MainWindow Background Resource Slice

- Added RED WPF expectation `MainWindow_UsesSharedBackgroundBrush`.
- Replaced the remaining `MainWindow.xaml` literal `#F6F8FB` background with
  the shared `AppBackgroundBrush`.
- Used `DynamicResource` for the `Window.Background` binding because
  `Window.Resources` are not available early enough for an attribute-level
  `StaticResource` lookup during XAML parsing.
- Preserved the root dashboard grid background resource and existing shell
  sizing.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter MainWindow_UsesSharedBackgroundBrush`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "MainWindow_UsesSharedBackgroundBrush|ColorStyleDictionary_DefinesCoreDashboardBrushes|MainWindow_ExposesDashboardControlsAndCommandBindings"`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-205628`.

Coverage after this slice: overall line coverage 91.3%.

Next componentization work should address the Details tab pager text style or
close the style-dictionary cleanup item after a final XAML audit.

## 2026-04-29 WPF Details Pager Typography Slice

- Added RED WPF expectation `DetailsTabsPanel_UsesSharedPagerTypography`.
- Merged `Styles/Typography.xaml` into `DetailsTabsPanel`.
- Replaced inline pager label/status text rendering with `MutedTextStyle` and
  `BodyTextStyle`.
- Preserved Details tab selection, row-page controls, DataGrid bindings, and
  settings tab controls.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter DetailsTabsPanel_UsesSharedPagerTypography`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "DetailsTabsPanel_UsesSharedPagerTypography|MainWindow_TabsExposeExpectedListsAndSettingsControls"`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-210315`.

Coverage after this slice: overall line coverage 91.3%.

Next componentization work should perform a final style/resource audit and then
either close the style-dictionary cleanup checklist item or open the next
specific debt slice.

## 2026-04-29 WPF Metric Card Label Typography Slice

- Added RED WPF expectation `MetricCard_UsesSharedLabelTypography`.
- Added `MetricLabelTextStyle` to `Styles/Typography.xaml`.
- Replaced `MetricCard` inline label `FontWeight` with the shared metric label
  typography resource.
- Preserved metric card label/value/subtitle bindings.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter MetricCard_UsesSharedLabelTypography`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "MetricCard_UsesSharedLabelTypography|MetricCard_RendersLabelValueAndSubtitle|DashboardView_HostsSummaryCardsPanelAndPreservesSummaryCardContent"`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-211007`.

Coverage after this slice: overall line coverage 91.3%.

Next componentization work should decide whether `StatusBadge` intrinsic
padding/font weight should stay as a reusable control default or be moved to a
style resource before closing Milestone 31 style cleanup.

## 2026-04-29 WPF Status Badge Style Slice

- Added RED WPF expectation `StatusBadge_UsesSharedShapeAndTextStyles`.
- Added color-free `Styles/Badges.xaml` with `StatusBadgeBorderStyle` and
  `StatusBadgeTextStyle`.
- Updated `StatusBadge` to consume the shared badge style dictionary instead
  of carrying inline border padding/corner radius/border thickness/font weight.
- Kept tracking/sync/privacy badge colors owned by `HeaderStatusBar` resources;
  the adjacent header badge test confirmed the brush resource identity remains
  stable after the style extraction.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter StatusBadge_UsesSharedShapeAndTextStyles`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "StatusBadge_UsesSharedShapeAndTextStyles|StatusBadge_RendersTextAndPreservesAutomationId|HeaderStatusBar_UsesSharedBadgeColorResources|DashboardView_HostsHeaderStatusBarAndPreservesHeaderContent"`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-212314`.

Coverage after this slice: overall line coverage 91.3%.

Next componentization work should address `DetailsTabsPanel` DataGrid outer
spacing (`DataGrid Margin="0,12,0,0"`) through `SessionDataGridStyle`, then
continue the remaining style/resource cleanup audit in small TDD slices.

## 2026-04-29 WPF Details DataGrid Spacing Slice

- Added a RED expectation to `DataGridStyleDictionary_DefinesReadableSessionGridStyle`
  for the common session-grid top margin.
- Moved repeated `Margin="0,12,0,0"` from the App Sessions, Web Sessions, and
  Live Event Log `DataGrid` elements into `SessionDataGridStyle`.
- Preserved explicit per-table `DataGridTextColumn` widths and min-widths in
  `DetailsTabsPanel` because those are product readability constraints, not
  generic styling.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter DataGridStyleDictionary_DefinesReadableSessionGridStyle`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "DataGridStyleDictionary_DefinesReadableSessionGridStyle|DashboardView_HostsDetailsTabsPanelAndPreservesTabsBinding|MainWindow_TabsExposeExpectedListsAndSettingsControls"`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-213053`.

Coverage after this slice: overall line coverage 91.3%.

Next componentization work should move the Settings section-heading bottom
spacing into `SettingsSectionTitleTextStyle`.

## 2026-04-29 WPF Settings Section Heading Spacing Slice

- Extended the existing Settings heading typography expectation so
  `SettingsSectionTitleTextStyle` owns the common bottom spacing.
- Moved the repeated `Margin="0,0,0,10"` from the Privacy, Sync, and Runtime
  heading `TextBlock` elements into `SettingsSectionTitleTextStyle`.
- Preserved Settings privacy-safe defaults, sync-local-only defaults, disabled
  full URL opt-in, and existing command/control bindings.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter SettingsPanel_UsesSharedSectionHeadingTypography`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "SettingsPanel_UsesSharedSectionHeadingTypography|SettingsPanel_UsesSharedMutedTextTypography|SettingsPanel_UsesSharedWarningTextTypography|SettingsPanel_PrivacyDefaultsAreSafeAndReadable|SettingsPanel_SyncDefaultsAreLocalOnly"`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-213752`.

Coverage after this slice: overall line coverage 91.3%.

Next work should leave minor visual style cleanup alone if it starts delaying
runtime confidence, and prioritize the next real tracking/browser pipeline
behavior recommended by the priority audit.

## 2026-04-29 WPF Tracking Ticker Extraction Slice

- Added `ITrackingTicker` and `DispatcherTrackingTicker` in `Windows.App`.
- Updated `MainWindow` so it no longer constructs a `DispatcherTimer`
  directly. It now receives an `ITrackingTicker`, starts it on `Loaded`, stops
  it on `Closed`, and unsubscribes the tick handler on close.
- Kept tracking visible-window scoped: the ticker only starts with the visible
  `MainWindow` lifecycle and only invokes the existing `PollTrackingCommand`;
  it does not add hidden background monitoring, server sync on tick, or any new
  data collection surface.
- Registered `ITrackingTicker` in DI and changed `MainWindow` registration to
  an explicit factory so constructor resolution stays deterministic.
- Replaced wall-clock `DispatcherTimer` waits in runtime WPF tests with a
  manual fake ticker for current-session duration, foreground-change
  persistence, and browser-domain web-session persistence.
- Added hidden-tracking safety coverage: a manual tick before Start does not
  collect or persist focus/web/outbox rows, and auto-start remains delayed
  until `MainWindow.Loaded` rather than constructor or DI resolution.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "CurrentSessionDuration_WhenManualTickerTicks_AdvancesBeyondZero|MainWindow_WhenLoadedStartsTickerAndClosedStopsTicker|AddWindowsApp_RegistersDispatcherTrackingTicker|PollTick_WhenForegroundChanges_PersistsClosedSessionAndRefreshesDashboardBeforeStop|PollTick_WhenBrowserDomainChanges_PersistsWebSessionAndRefreshesWebRowsBeforeStop"`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "MainWindow_WhenAutoStartEnabled_DoesNotStartTrackingBeforeLoaded|ManualTickerTick_WhenTrackingHasNotStarted_DoesNotCollectOrPersist|MainWindow_WhenLoadedStartsTickerAndClosedStopsTicker"`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-215459`.

Coverage after this slice: overall line coverage 91.2%.

## 2026-04-30 Android Emulator Screenshot And Connected Test Slice

- Started the existing `Medium_Phone` AVD with a 4GB RAM test profile after the
  default emulator run hit low-memory process kills during DashboardActivity
  scrolling.
- Disabled emulator animations for deterministic Espresso interactions.
- Fixed DashboardActivity instrumentation coverage to scroll to off-screen
  dashboard controls before asserting/clicking them.
- Added `SnapshotCaptureTest` so the Android screenshot script can launch
  non-exported internal activities through instrumentation instead of weakening
  the production manifest with exported dashboard/settings/session activities.
- Updated `scripts\run-android-ui-snapshots.ps1` to seed sample Room data,
  capture screens through instrumentation, pull screenshots from the app's
  external files directory, and keep the report/manifest flow unchanged.
- Generated emulator-backed screenshots for dashboard, settings, sessions, and
  daily summary. The dashboard screenshot includes the seeded location context
  card with latitude/longitude evidence, and the settings screenshot includes
  the location opt-in/privacy section.

Verified:

- `.\gradlew.bat connectedDebugAndroidTest --no-daemon --stacktrace` passed
  10 tests on `Medium_Phone`.
- `powershell -ExecutionPolicy Bypass -File scripts\run-android-ui-snapshots.ps1`
  passed with artifact `artifacts/android-ui-snapshots/20260430-091721`.

## 2026-04-29 Server Daily Summary Timezone Slice

- Added a relational regression test proving that Windows and Android
  FocusSessions are included in the same daily summary according to the
  requested user timezone, even when the persisted `LocalDate` is on the
  previous UTC date.
- Updated `DailySummaryQueryService` so daily and date-range summary queries
  filter focus sessions by `StartedAtUtc` converted through the requested
  timezone, matching the existing web-session behavior and the PRD's
  user-timezone rule.
- Fixed a date-range API test fixture so each seeded focus session's UTC start
  time matches its intended Asia/Seoul local date. This preserves the product
  behavior under the stricter timezone rule instead of relying on EF InMemory
  date shortcuts.

Verified so far:

- `dotnet test tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj --no-restore --filter "GenerateAsync_UsesRequestedTimezoneWhenGroupingFocusSessionsAcrossUtcMidnight" -v minimal`
- `dotnet test tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj --no-restore --filter "FullyQualifiedName~Summaries" -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Coverage after this slice: overall line coverage 91.3%; Server line coverage
96.5%.

## 2026-04-29 Server Integrated HTTP Runtime Flow Slice

- Added `DailySummaryApi_WhenWindowsAndAndroidClientsUploadSessions_ReturnsIntegratedSummary`
  as a relational HTTP runtime proof. The test registers Windows and Android
  devices through the public API, uploads focus sessions and a domain-only web
  session through the upload APIs, then queries the daily summary API.
- The flow proves Windows + Android active time is integrated for the same
  user, idle time is excluded from active totals, another user's sessions are
  ignored, and domain-only browser metadata contributes to `totalWebMs` without
  persisting full URL or page title.
- The test uses an in-memory SQLite connection rather than EF InMemory, so
  relational constraints stay active.

Verified so far:

- `dotnet test tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj --no-restore --filter "DailySummaryApi_WhenWindowsAndAndroidClientsUploadSessions_ReturnsIntegratedSummary" -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Coverage after this slice: overall line coverage 91.3%; Server line coverage
96.5%.

## 2026-04-29 Server Upload Relational Error Slice

- Added a shared `RelationalServerFactory` for API tests that need SQLite
  relational behavior through `WebApplicationFactory`.
- Added relational API tests proving focus uploads with an unregistered device,
  web uploads with an unregistered device, and web uploads with a missing
  focus-session link return controlled batch `Error` items and do not persist
  orphan rows.
- Updated `FocusSessionUploadService` and `WebSessionUploadService` to validate
  foreign-key preconditions before `SaveChangesAsync`, avoiding raw relational
  provider exceptions while keeping duplicate retry behavior unchanged.
- Updated older upload API test fixtures so successful uploads first seed the
  required device/focus rows instead of relying on EF InMemory's missing FK
  enforcement.

Verified so far:

- `dotnet test tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj --no-restore --filter "UploadFocusSessions_WhenDeviceIsNotRegistered_ReturnsControlledErrorAndDoesNotPersistRows|UploadWebSessions_WhenFocusSessionIsMissing_ReturnsControlledErrorAndDoesNotPersistRows" -v minimal`
- `dotnet test tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj --no-restore --filter "FullyQualifiedName~Sessions" -v minimal`
- `dotnet test tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj --no-restore --filter "UploadWebSessions_WhenDeviceIsNotRegistered_ReturnsControlledErrorAndDoesNotPersistRows|UploadWebSessions_WhenFocusSessionIsMissing_ReturnsControlledErrorAndDoesNotPersistRows|UploadFocusSessions_WhenDeviceIsNotRegistered_ReturnsControlledErrorAndDoesNotPersistRows" -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Coverage after this slice: overall line coverage 91.3%; Server line coverage
96.7%.

## 2026-04-29 Android Optional Location Settings Slice

- Added optional Android location context settings with safe defaults:
  location capture is off, approximate mode is preferred, and precise
  latitude/longitude requires a separate explicit opt-in.
- Added foreground-only manifest guardrails for optional location context:
  `ACCESS_COARSE_LOCATION` and `ACCESS_FINE_LOCATION` are allowed, while
  `ACCESS_BACKGROUND_LOCATION` remains forbidden.
- Updated Settings UI with location guidance, opt-in checkboxes, and a location
  permission button that remains disabled until the user enables location
  context.
- Added a location permission policy/controller so approximate mode requests
  coarse location only, and precise latitude/longitude requests fine location
  only after separate opt-in.
- This slice does not collect location samples, write Room location rows, upload
  coordinates, or infer location from other app content.

Focused validation completed so far:

- `.\gradlew.bat testDebugUnitTest --tests "com.woong.monitorstack.settings.SharedPreferencesAndroidLocationSettingsTest" --no-daemon --stacktrace`
- `.\gradlew.bat testDebugUnitTest --tests "com.woong.monitorstack.settings.SettingsActivityRobolectricTest" --no-daemon --stacktrace`
- `.\gradlew.bat testDebugUnitTest --tests "com.woong.monitorstack.privacy.AndroidManifestPrivacyTest.manifestUsesForegroundLocationPermissionsOnlyForOptionalLocationContext" --no-daemon --stacktrace`
- `.\gradlew.bat testDebugUnitTest --tests "com.woong.monitorstack.settings.LocationPermissionPolicyTest" --tests "com.woong.monitorstack.settings.SettingsActivityRobolectricTest.locationPermissionRequestStaysDisabledUntilLocationContextOptIn" --no-daemon --stacktrace`

Full validation after integrating the Android location settings slice and WPF
component/style guard slice:

- `.\gradlew.bat testDebugUnitTest assembleDebug --no-daemon --stacktrace`
  from `android/`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260430-001723`.

Coverage after this slice: overall line coverage 91.3%; branch coverage 70.2%.

## 2026-04-29 Android Optional Location Room Slice

- Added a local-only Room `location_context_snapshots` table for Android
  optional location context. The table stores nullable latitude, longitude,
  accuracy, permission state, capture mode, and captured UTC timestamp.
- Added `LocationContextSnapshotDao` with recent-by-device and
  captured-range-by-device queries.
- Added a `2 -> 3` Room migration that creates only the Android local table;
  no server DTO/upload path was added in this slice, and location sync remains
  off/unimplemented.
- RED/GREEN component tests now prove nullable coordinates are preserved and
  range queries do not mix devices.

Focused validation completed so far:

- `.\gradlew.bat testDebugUnitTest --tests "com.woong.monitorstack.data.local.LocationContextSnapshotDaoTest" --no-daemon --stacktrace`
- `.\gradlew.bat testDebugUnitTest assembleDebug --no-daemon --stacktrace`

## 2026-04-29 WPF Thin Shell And AutomationId Guard Slice

- Added architecture tests proving `MainWindow.xaml` remains a thin
  `Grid -> DashboardView` shell and that `DashboardView.xaml` composes the six
  reusable dashboard sections inside a vertical `ScrollViewer`.
- Added a WPF App test proving key dashboard controls keep stable
  AutomationIds required by local UI acceptance.
- Added minimal component-level AutomationIds to WPF section UserControls and
  non-visual wrappers for app/web session lists while preserving existing
  acceptance IDs on `DashboardView`.

Focused validation completed so far:

- `dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~MainWindow_XamlRemainsThinDashboardShell" -maxcpucount:1 -v minimal`
- `dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~DashboardView_ComposesReusableSectionsInsideVerticalScrollViewer" -maxcpucount:1 -v minimal`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore --filter "FullyQualifiedName~DashboardComponentXaml_ExposesStableAutomationIdsForUiAcceptance|FullyQualifiedName~MainWindow_ExposesStableAutomationIdsForSnapshotAutomation" -maxcpucount:1 -v minimal`

## 2026-04-29 Android Optional Location Dashboard Slice

- Added dashboard state for optional location context with safe defaults:
  `Location capture off`, nullable latitude/longitude display text, unavailable
  accuracy, and no captured time.
- `RoomDashboardRepository` now reads the latest local
  `location_context_snapshots` row for the selected device/date range and
  formats fake opt-in coordinates for dashboard display.
- Added a Dashboard XML location card with status, latitude, longitude,
  accuracy, and captured-at fields.
- This remains local-only and opt-in-scoped; no runtime location collector,
  server upload, or background location tracking was added.

Focused validation completed so far:

- `.\gradlew.bat testDebugUnitTest --tests "com.woong.monitorstack.dashboard.DashboardViewModelTest" --no-daemon --stacktrace`
- `.\gradlew.bat testDebugUnitTest --tests "com.woong.monitorstack.dashboard.RoomDashboardRepositoryTest" --no-daemon --stacktrace`
- `.\gradlew.bat testDebugUnitTest --tests "com.woong.monitorstack.dashboard.DashboardActivityRobolectricTest" --no-daemon --stacktrace`
- `.\gradlew.bat testDebugUnitTest assembleDebug --no-daemon --stacktrace`

## 2026-04-29 WPF Snapshot Runtime Selector Evidence Slice

- Added WPF App tests proving the UI snapshot/acceptance tool keeps using the
  stable existing DashboardView AutomationIds and records runtime selector
  evidence for Start/Stop/Sync and app/web/live-event lists.
- Updated the snapshot tool so TrackingPipeline mode treats recent app and web
  session lists as required semantic evidence rather than optional crop-only
  selectors.

Focused validation completed so far:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore --filter "FullyQualifiedName~UiSnapshotsTool_UsesStableDashboardViewAutomationIdsForAcceptanceSelectors" -maxcpucount:1 -v minimal`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore --filter "FullyQualifiedName~UiSnapshotsTool_ReportManifestAndVisualPromptIncludeRuntimeSelectorEvidence" -maxcpucount:1 -v minimal`

Full validation after integrating the Android location dashboard slice and WPF
snapshot selector-evidence slice:

- `.\gradlew.bat testDebugUnitTest assembleDebug --no-daemon --stacktrace`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260430-005505`.

Coverage after this slice: overall line coverage 91.3%; branch coverage 70.2%.

Full validation after integrating the Android Room slice and WPF guard slices:

- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260430-003857`.

Coverage after this slice: overall line coverage 91.3%; branch coverage 70.2%.

## 2026-04-29 WPF Component Guard And Acceptance Clock Slice

- Added architecture guardrails so WPF XAML color literals are centralized in
  `Styles/Colors.xaml`; `Tabs.xaml` now uses the shared `TransparentBrush`.
- Added a dashboard composition guard proving `DashboardView` owns the direct
  dashboard sections rather than hiding chart/details sections behind an extra
  generic container.
- Added product UI goal checks for Control Bar action/period grouping, summary
  metric card icon/accent slots, and separate chart card surfaces.
- Added a UI automation stability guard proving the direct dashboard sections
  expose stable AutomationIds for Header, Control Bar, Current Focus, Summary,
  Charts, and Details tabs.
- Fixed a TrackingPipeline acceptance flake where running local WPF acceptance
  immediately after midnight caused the Today filter to exclude the fake
  `Code.exe` session. The fake acceptance clock now starts at local noon.
- The runtime/product behavior is unchanged; this slice hardens local
  acceptance and WPF composition boundaries.

Validated with the full commands listed above. The first WPF acceptance rerun
failed at `artifacts/wpf-ui-acceptance/20260430-001351` because the fake
TrackingPipeline crossed the local midnight boundary; the local-noon clock fix
made the rerun pass at `artifacts/wpf-ui-acceptance/20260430-001723`.

## 2026-04-29 Android UI Snapshot Connected-Branch Slice

- Added a fake-adb architecture test proving that
  `scripts\run-android-ui-snapshots.ps1` no longer stops at the old connected
  device "capture is not implemented" blocker.
- The script now launches Dashboard, Settings, Sessions, and Daily Summary
  activities with adb, captures screenshots with `screencap`, pulls stable PNG
  files into the artifact folder, and records them in `manifest.json`.
- When `-SkipBuild` is not used, the script installs the debug APK before
  launching screens. With `-SkipBuild`, it assumes the app is already installed.
- The current local environment still has no connected Android device or
  emulator, so the real artifact run remains `BLOCKED` with an explicit report;
  fake-adb verifies the connected branch without weakening the physical-device
  evidence requirement.

Verified so far:

- `dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "AndroidUiSnapshotScript_WhenDeviceConnected_CapturesExpectedAppScreens" -v minimal`
- `dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-android-ui-snapshots.ps1 -SkipBuild`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest Android UI snapshot artifact:
`artifacts/android-ui-snapshots/20260429-230721` (`BLOCKED`, no connected
device).

Coverage after this slice: overall line coverage 91.3%; Server line coverage
96.7%.

## 2026-04-29 Android UI Snapshot Seed Slice

- Added `SnapshotSeedTest` under androidTest. The test clears the app Room DB
  and seeds deterministic local focus sessions for Chrome, YouTube, Slack, plus
  a Chrome idle interval for screenshot review.
- Updated `scripts\run-android-ui-snapshots.ps1` so connected-device runs
  install both debug and androidTest APKs, execute the seed instrumentation
  test, then capture Dashboard, Settings, Sessions, and Daily Summary screens.
- The seed path is test-only and local. It does not add product telemetry,
  background screen capture, typed text capture, or cross-app content capture.

Verified so far:

- `dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "AndroidUiSnapshotScript_WhenDeviceConnected_CapturesExpectedAppScreens" -v minimal`
- `.\gradlew.bat assembleDebugAndroidTest --no-daemon --stacktrace` from `android/`
- `powershell -ExecutionPolicy Bypass -File scripts\run-android-ui-snapshots.ps1 -SkipBuild`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `.\gradlew.bat testDebugUnitTest assembleDebug assembleDebugAndroidTest --no-daemon --stacktrace` from `android/`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest Android UI snapshot artifact:
`artifacts/android-ui-snapshots/20260429-231430` (`BLOCKED`, no connected
device).

Coverage after this slice: overall line coverage 91.3%; Server line coverage
96.7%.

## 2026-04-29 WPF Close Flush Slice

- Added a WPF behavior test proving that closing `MainWindow` while tracking is
  Running flushes the current foreground FocusSession to Windows local SQLite,
  queues a pending `focus_session` outbox item, stops the ticker, and leaves the
  dashboard state stopped with a non-zero Active Focus value.
- Updated `MainWindow` so `Closing` reuses the existing
  `StopTrackingCommand` path when tracking is active. `Closed` still stops the
  injected ticker and detaches its tick handler.
- This keeps collection visible and user-scoped: it does not add hidden
  background tracking or new data capture; it only prevents data loss during
  normal app exit.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter MainWindow_WhenClosedWhileTracking_FlushesCurrentSessionToSqliteOutboxAndStopsTicker`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-221314`.

Coverage after this slice: overall line coverage 91.3%.

Next work should continue runtime confidence slices before returning to minor
visual cleanup. Good candidates are proving stop/close flush behavior through
the manual ticker path, or tightening Chrome native messaging acceptance
without touching the user's real Chrome profile or real local DB.

## 2026-04-29 Android Location Sync Payload Gate

- Added a test-first Android sync payload factory for optional location
  context. The factory returns an empty upload payload when sync is off or when
  location context is off.
- When both sync and location context are explicitly enabled, the factory maps
  local Room `LocationContextSnapshotEntity` rows into upload DTO items with
  nullable `latitude`, `longitude`, and `accuracyMeters` preserved.
- This slice does not add a runtime location collector, server location
  storage, or default upload. Location metadata remains opt-in and sync remains
  opt-in.

Verified:

- `.\gradlew.bat testDebugUnitTest --tests "com.woong.monitorstack.sync.LocationContextSyncPayloadFactoryTest" --no-daemon --stacktrace`
- `.\gradlew.bat testDebugUnitTest assembleDebug --no-daemon --stacktrace`

## 2026-04-29 WPF Live Event And Details Visual Evidence Slice

- Added WPF App expectation coverage for chart header icon text, Details tab
  icon headers, compact details pager icon buttons, and an App Sessions
  template column with a visible app glyph plus app name.
- Updated the local WPF UI snapshot acceptance tool so TrackingPipeline checks
  Live Event Log runtime evidence across reachable details pages instead of
  only the current 10-row page.
- The latest WPF acceptance artifact is
  `artifacts/wpf-ui-acceptance/20260430-012621` with RealStart and
  TrackingPipeline both passing against temp SQLite databases.
- Coverage after this slice remains overall line coverage 91.3% and branch
  coverage 70.2%.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore --filter "FullyQualifiedName~WpfUiAcceptanceScriptTests" -maxcpucount:1 -v minimal`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "MainWindow_TabsExposeExpectedListsAndSettingsControls|DetailsTabsPanel_RendersSvgLikeTabIconsAndIconPager"`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Committed and pushed:

- `72c5928 Add location sync gate and WPF runtime UI evidence`

## 2026-04-29 Server Location Context Upload Slice

- Added a TDD-first server upload path for optional Android location context
  metadata. The endpoint is `/api/location-contexts/upload`.
- Added domain contracts for `LocationContextUploadItem` and
  `UploadLocationContextsRequest`.
- Added `location_contexts` server storage with nullable `Latitude`,
  `Longitude`, and `AccuracyMeters`, plus `deviceId + clientContextId`
  idempotency.
- Generated the PostgreSQL migration `AddLocationContextTable`.
- This server slice does not make Android upload location by default. Android
  sync runner integration remains a separate TODO and must preserve both sync
  opt-in and location-context opt-in.

Verified so far:

- RED: `dotnet test tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "FullyQualifiedName~LocationContextUploadApiTests"` failed first on missing contracts/entity.
- RED: `dotnet test tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "FullyQualifiedName~LocationContextMigration_AddsPrivacySafeNullableCoordinateTable"` failed first on missing migration.
- GREEN: `dotnet test tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "FullyQualifiedName~LocationContext"` passed 6 tests.
- `dotnet test tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj --no-restore -maxcpucount:1 -v minimal` passed 40 server tests.
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed 335 total .NET tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1` generated coverage: line 91.3% (3559/3895), branch 70.3% (486/691).

## 2026-04-30 Android Location Sync Client Contract Slice

- Updated Android location-context upload DTOs to match the server
  `/api/location-contexts/upload` contract: top-level `deviceId` plus
  `contexts`.
- `LocationContextSyncPayloadFactory` now emits `clientContextId`, UTC capture
  instant, local date, timezone id, nullable latitude/longitude/accuracy,
  capture mode, permission state, and `android_location_context` source.
- Added `AndroidSyncApi.uploadLocationContexts` and implemented
  `AndroidSyncClient` POSTing to `/api/location-contexts/upload`.
- This slice preserves the privacy gate: payloads remain empty unless both sync
  and location context capture are explicitly enabled. It does not yet wire the
  local outbox/worker path to upload location-context rows.

Verified:

- Carver: `.\gradlew.bat testDebugUnitTest --tests "com.woong.monitorstack.sync.LocationContextSyncPayloadFactoryTest" --no-daemon --stacktrace` passed.
- Carver: `.\gradlew.bat testDebugUnitTest --tests "com.woong.monitorstack.sync.AndroidSyncClientTest" --no-daemon --stacktrace` passed.
- Carver: `.\gradlew.bat testDebugUnitTest --tests "com.woong.monitorstack.sync.*" --no-daemon --stacktrace` passed.
- Main: `.\gradlew.bat testDebugUnitTest --tests "com.woong.monitorstack.sync.LocationContextSyncPayloadFactoryTest" --tests "com.woong.monitorstack.sync.AndroidSyncClientTest" --tests "com.woong.monitorstack.sync.AndroidOutboxSyncProcessorTest" --no-daemon --stacktrace` passed.
- Main: `.\gradlew.bat testDebugUnitTest assembleDebug --no-daemon --stacktrace` passed.

Remaining Android location sync work:

- Wire local location-context outbox/worker processing into the sync runner so
  rows upload only when both sync opt-in and location opt-in are enabled.

Committed and pushed:

- `33ae984 Add Android location context sync client`

## 2026-04-30 Android Location Context Outbox Sync Slice

- Added `location_context` aggregate processing to `AndroidOutboxSyncProcessor`.
- Pending location-context outbox rows are ignored, left pending, and not
  uploaded when location context opt-in is off.
- When location context opt-in is on, accepted and duplicate upload results mark
  location-context rows synced. Error or missing upload results mark rows failed
  through the same retry path as focus sessions.
- `AndroidRoomSyncRunner` now injects `SharedPreferencesAndroidLocationSettings`
  into the processor. The existing `AndroidSyncWorker` sync-off gate remains the
  first opt-in barrier, so disabled sync skips all uploads.
- This preserves the safe privacy model: Android location metadata is still
  explicit opt-in and sync remains explicit opt-in.

Verified:

- Carver: `.\gradlew.bat testDebugUnitTest --tests "com.woong.monitorstack.sync.AndroidOutboxSyncProcessorTest" --no-daemon --stacktrace` passed.
- Carver: `.\gradlew.bat testDebugUnitTest --tests "com.woong.monitorstack.sync.*" --no-daemon --stacktrace` passed.
- Main: `.\gradlew.bat testDebugUnitTest --tests "com.woong.monitorstack.sync.AndroidOutboxSyncProcessorTest" --tests "com.woong.monitorstack.sync.AndroidSyncWorkerTest" --no-daemon --stacktrace` passed.
- Main: `.\gradlew.bat testDebugUnitTest assembleDebug --no-daemon --stacktrace` passed.

Committed and pushed:

- `5946c7b Sync Android location context outbox rows`

## 2026-04-30 Android Location UI Snapshot Evidence Slice

- Updated the local Android UI snapshot script so reports, manifests, and
  visual-review prompts explicitly include expected checks for the Dashboard
  location card and Settings location section.
- Updated snapshot seed instrumentation to insert deterministic local location
  context data alongside sample app sessions.
- Added/updated androidTest coverage for the Dashboard location context card
  and Settings location section safe defaults. The Settings test verifies the
  location permission button remains disabled while location context is off.
- Fixed an existing androidTest ID typo from `topAppCard` to `topAppsCard`.
- The current local environment still has no connected Android device/emulator,
  so screenshots remain `BLOCKED` at runtime; the blocked artifact now tells a
  beginner exactly which location UI evidence to expect after connecting a
  device.

Verified:

- Carver: `dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "AndroidUiSnapshotScript" -v minimal` passed.
- Carver: `.\gradlew.bat assembleDebugAndroidTest --no-daemon --stacktrace` passed.
- Main: `dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "AndroidUiSnapshotScript" -v minimal` passed 4 tests.
- Main: `.\gradlew.bat assembleDebugAndroidTest --no-daemon --stacktrace` passed.
- Main: `powershell -ExecutionPolicy Bypass -File scripts\run-android-ui-snapshots.ps1 -SkipBuild` generated `BLOCKED` artifacts at `artifacts/android-ui-snapshots/20260430-024300` because no Android device/emulator was connected.
- Main: `.\gradlew.bat testDebugUnitTest assembleDebug assembleDebugAndroidTest --no-daemon --stacktrace` passed.

## 2026-04-30 Android Location Context Worker Runtime Slice

- Added no-hardware Robolectric coverage in `AndroidSyncWorkerTest` that routes
  pending `location_context` outbox rows through a real
  `AndroidOutboxSyncProcessor`.
- Verified the WorkManager boundary preserves both privacy gates: sync-off
  keeps location rows local, and sync plus location opt-in uploads and marks
  them synced.
- Verified failed location uploads return WorkManager retry and record the
  failed row through the same retry path as focus-session uploads.
- Updated `docs/android-ui-plan.md` so current evidence reflects the sync runner
  and WorkManager location-context path, leaving only runtime collector and
  connected-device screenshots as Android location UI gaps.
- This does not require connected-device screenshots. Real Dashboard/Settings
  location screenshots remain blocked until an emulator/device is available.

Verified:

- Carver: `.\gradlew.bat testDebugUnitTest --tests "com.woong.monitorstack.sync.AndroidSyncWorkerTest" --no-daemon --stacktrace` passed.
- Carver: `.\gradlew.bat testDebugUnitTest --tests "com.woong.monitorstack.sync.*" --no-daemon --stacktrace` passed.
- Main: `.\gradlew.bat testDebugUnitTest --tests "com.woong.monitorstack.sync.AndroidSyncWorkerTest" --no-daemon --stacktrace` passed.
- Main: `.\gradlew.bat testDebugUnitTest assembleDebug --no-daemon --stacktrace` passed.

## 2026-04-30 Android Runtime Location Provider Seam Slice

- Added a no-hardware `RuntimeLocationContextProvider` seam with injectable
  `RuntimeLocationReader`, foreground permission checker, clock, and id factory.
- Added a real Android permission checker that reports foreground fine/coarse
  permission state without using background location.
- Tests prove no local snapshot is produced while location context is off or
  foreground location permission is missing.
- Tests prove approximate mode keeps `latitude`, `longitude`, and
  `accuracyMeters` null, while precise coordinates require both separate
  precise opt-in and precise permission.
- The capture seam is local-only and deliberately independent from sync opt-in;
  sync remains a separate upload gate.
- Updated `docs/android-ui-plan.md` so remaining Android location gaps are the
  hardware-backed reader/scheduling/persistence wiring and connected-device
  screenshots.

Verified:

- Carver: `.\gradlew.bat testDebugUnitTest --tests "com.woong.monitorstack.location.RuntimeLocationContextProviderTest" --no-daemon --stacktrace` passed.
- Carver: `.\gradlew.bat testDebugUnitTest assembleDebug --no-daemon --stacktrace` passed.
- Main: `.\gradlew.bat testDebugUnitTest --tests "com.woong.monitorstack.location.RuntimeLocationContextProviderTest" --no-daemon --stacktrace` passed.
- Main: `.\gradlew.bat testDebugUnitTest assembleDebug --no-daemon --stacktrace` passed.

## 2026-04-30 Android Location Context Collection Runner Slice

- Added `LocationContextCollectionRunner` as a no-hardware local collection and
  persistence path for location context snapshots.
- Added `RuntimeLocationSnapshotProvider` so the real provider and tests share a
  public seam while tests can avoid GPS hardware.
- When the provider returns a snapshot, the runner writes
  `LocationContextSnapshotEntity` through the Room-facing DAO and enqueues a
  pending `location_context` outbox row shaped as
  `SyncLocationContextUploadItem`.
- When the provider returns `null`, no local row or outbox item is written.
- Sync opt-in remains an upload gate only; this path creates local metadata and
  pending outbox rows without uploading.
- Updated `docs/android-ui-plan.md` so the remaining location gaps are the
  hardware-backed reader and scheduling wiring, plus connected-device
  screenshot evidence.

Verified:

- Carver: `.\gradlew.bat testDebugUnitTest --tests "com.woong.monitorstack.location.*" --no-daemon --stacktrace` passed.
- Carver: `.\gradlew.bat testDebugUnitTest assembleDebug --no-daemon --stacktrace` passed.
- Main: `.\gradlew.bat testDebugUnitTest --tests "com.woong.monitorstack.location.*" --no-daemon --stacktrace` passed.
- Main: `.\gradlew.bat testDebugUnitTest assembleDebug --no-daemon --stacktrace` passed.

## 2026-04-30 Android Location Scheduling Wiring Slice

- Wired the existing `CollectUsageWorker` to invoke a `LocationContextCollector`
  after UsageStats collection.
- The worker now accepts a device id through input data, falls back to a local
  device id when absent, and reports `locationContextCaptured` in output data.
- Tests use fake collectors and prove captured/skipped location context states
  without GPS hardware, emulator, or physical device access.
- Production default wiring calls `LocationContextCollectionRunner.create(...)`
  but still uses `NoopRuntimeLocationReader`, so no real GPS hardware is read
  until a future explicit hardware-backed reader slice.
- Sync remains separate: this worker only writes local metadata/outbox, and the
  sync worker opt-in gates still control upload.
- Updated `docs/android-ui-plan.md` so the remaining Android location runtime
  gap is the hardware-backed reader plus connected-device screenshots.

Verified:

- Carver: `.\gradlew.bat testDebugUnitTest --tests "com.woong.monitorstack.usage.CollectUsageWorkerTest" --tests "com.woong.monitorstack.location.*" --no-daemon --stacktrace` passed.
- Carver: `.\gradlew.bat testDebugUnitTest assembleDebug --no-daemon --stacktrace` passed.
- Main: `.\gradlew.bat testDebugUnitTest --tests "com.woong.monitorstack.usage.CollectUsageWorkerTest" --tests "com.woong.monitorstack.location.*" --no-daemon --stacktrace` passed.
- Main: `.\gradlew.bat testDebugUnitTest assembleDebug --no-daemon --stacktrace` passed.

## 2026-04-30 Android SVG UI Flow Alignment Slice

- Checked the planned Figma/SVG flow at
  `artifacts/android-ui-flow/woong-monitor-android-ui-flow.figma-import.svg`
  against the current Android dashboard XML.
- Added a Robolectric layout test that requires the SVG-planned dashboard core:
  Usage/Sync/Privacy status chips, Current Focus rows, 1h/6h/24h/7d filters,
  Screen On and Web Focus summary cards, top apps surface, and bottom nav labels.
- Updated `activity_dashboard.xml` so the planned dashboard surface exists as
  XML/ViewBinding IDs while preserving the existing Room-backed dashboard IDs.
- Added missing string resources and lightweight built-in icon drawables for the
  summary labels and bottom navigation. No Compose migration or new telemetry
  scope was added.

Verified:

- `.\gradlew.bat :app:testDebugUnitTest --tests "com.woong.monitorstack.dashboard.DashboardActivityRobolectricTest" --no-daemon`
- `.\gradlew.bat :app:testDebugUnitTest :app:assembleDebug --no-daemon`

Committed and pushed:

- `dc4b4ff Align Android dashboard with SVG UI flow`

## 2026-04-30 WPF Selected Details Pager Slice

- Added a TDD-first Presentation behavior test proving the Details pager belongs
  to the selected tab. If the user pages through a long App Sessions list and
  then switches to a shorter Web Sessions tab, the dashboard resets to page 1
  and recalculates total pages from Web Sessions.
- Updated `DashboardViewModel` so `SelectedDetailsTab` changes reset
  `CurrentDetailsPage` and `TotalDetailsPages` uses the selected tab row count.
- This is a Presentation-only MVVM behavior fix; it does not query WPF controls,
  Windows APIs, SQLite, HTTP, or server code.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.Presentation.Tests\Woong.MonitorStack.Windows.Presentation.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "FullyQualifiedName~DetailsTabs_WhenSwitchingTabsFromLaterPage_UsesSelectedTabPageCount"`
- `dotnet test tests\Woong.MonitorStack.Windows.Presentation.Tests\Woong.MonitorStack.Windows.Presentation.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed 336 total .NET tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1` generated coverage: line 91.3% (3559/3895), branch 70.3% (486/691).

## 2026-04-30 WPF Details Pager Automation Names Slice

- Added a RED WPF App expectation test requiring the compact Details pager
  previous/next buttons and page-status text to expose readable automation
  names.
- Updated `DetailsTabsPanel.xaml` so the visible pager remains compact while UI
  automation and assistive technology can read `Previous details page`, `Next
  details page`, and `Current details page`.
- This is an App/XAML-only accessibility and automation-stability slice. It
  does not change tracking, SQLite, sync, browser capture, or privacy behavior.

Verified:

- Popper: `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "FullyQualifiedName~DetailsTabsPanel_PagerControlsExposeReadableAutomationNames"` passed.
- Popper: `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "FullyQualifiedName~DetailsTabsPanel"` passed 5 tests.
- Popper: `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "FullyQualifiedName~MainWindow_TabsExposeExpectedListsAndSettingsControls"` passed.
- Main: `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "FullyQualifiedName~DetailsTabsPanel_PagerControlsExposeReadableAutomationNames"` passed.
- Main: `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal` passed 107 WPF App tests.
- Main: `dotnet test tests\Woong.MonitorStack.Windows.Presentation.Tests\Woong.MonitorStack.Windows.Presentation.Tests.csproj --no-restore -maxcpucount:1 -v minimal` passed 53 WPF Presentation tests.
- Main: `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed 337 total .NET tests. A transient MSB3101 cache write warning appeared because build validation was running at the same time; no test failed.
- Main: `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
- Main: `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1` passed and generated coverage artifacts; overall line coverage remained 91.3%.
- Main: `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1` passed with artifact `artifacts/wpf-ui-acceptance/20260430-020432`. RealStart persisted a focus session into the temp SQLite DB and queued one outbox row. TrackingPipeline UI snapshots and semantic checks passed. Cleanup emitted a non-fatal warning because the process was already closed.

Committed and pushed:

- `87d6a48 Add WPF details pager automation names`

## 2026-04-30 WPF Presentation Pager And Sync Status Slice

- Added RED Presentation tests for three dashboard-state behaviors:
  chart details actions reset the selected details pager to page 1, rows-per-page
  changes clamp the current page to the last available page, and sync
  failure/off transitions update the dashboard sync badge and Current Focus sync
  status.
- Updated `DashboardViewModel` to route details actions through pager-aware tab
  selection, preserve valid pages during rows-per-page changes, and mirror
  `Settings.SyncStatusLabel` into `LastSyncStatusText`.
- Updated `DashboardSettingsViewModel` so disabling sync clears a previous sync
  failure and returns the settings/dashboard copy to safe local-only status.
- This is a Presentation-only MVVM slice. It does not query WPF controls,
  Windows APIs, SQLite, HTTP, or server code.

Verified so far:

- Popper: `dotnet test tests\Woong.MonitorStack.Windows.Presentation.Tests\Woong.MonitorStack.Windows.Presentation.Tests.csproj --no-restore -maxcpucount:1 -v minimal` passed 56 tests.
- Popper: `dotnet build src\Woong.MonitorStack.Windows.Presentation\Woong.MonitorStack.Windows.Presentation.csproj --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
- Popper: `git diff --check -- src\Woong.MonitorStack.Windows.Presentation tests\Woong.MonitorStack.Windows.Presentation.Tests` reported no whitespace errors, only LF-to-CRLF warnings.
- Main: `dotnet test tests\Woong.MonitorStack.Windows.Presentation.Tests\Woong.MonitorStack.Windows.Presentation.Tests.csproj --no-restore -maxcpucount:1 -v minimal` passed 56 tests.
- Main: `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed 341 total .NET tests in the current workspace.
- Main: `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors after rerunning sequentially. A prior parallel test/build attempt hit transient WPF generated-file contention, which is why solution validation should stay sequential with `-maxcpucount:1`.
- Main: prior coverage command in this workspace passed with overall line coverage at 91.3%.

Committed and pushed:

- `dc2a85e Harden WPF presentation pager and sync status`

## 2026-04-30 WPF Settings And Action Automation Names Slice

- Added RED WPF App accessibility tests for readable automation names on
  SettingsPanel primary controls, ChartsPanel app/domain details buttons, and
  the reusable SectionCard action button.
- Updated SettingsPanel controls so privacy/sync fields expose semantic names
  instead of relying only on visible content and AutomationIds.
- Updated chart details buttons to expose `Show app focus details` and
  `Show domain focus details`.
- Updated SectionCard so a card action button uses its `ActionText` as the
  readable automation name.
- This is a WPF App-only accessibility and semantic UI automation slice. It
  does not change product tracking, SQLite, sync, browser capture, Android, or
  server behavior.

Verified so far:

- Popper: `SettingsPanel_PrimarySettingsControlsExposeReadableAutomationNames` failed RED on empty name, then passed.
- Popper: `ChartsPanel_DetailActionButtonsExposeReadableAutomationNames` failed RED on empty name, then passed.
- Popper: `SectionCard_ActionButtonUsesActionTextAsReadableAutomationName` failed RED on empty name, then passed.
- Popper: `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore --filter "FullyQualifiedName~MainWindowUiExpectationTests" -maxcpucount:1 -v minimal` passed 47 tests.
- Popper: `DashboardAutomationIdContractTests` passed.
- Popper: `git diff --check -- src\Woong.MonitorStack.Windows.App tests\Woong.MonitorStack.Windows.App.Tests` passed with only LF-to-CRLF warnings.
- Main: `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "FullyQualifiedName~SettingsPanelAccessibilityTests|FullyQualifiedName~ChartsPanelAccessibilityTests|FullyQualifiedName~SectionCardAccessibilityTests"` passed 3 tests.
- Main: `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal` passed 110 WPF App tests.
- Main: `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed 343 total .NET tests.
- Main: `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
- Main: `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1` passed and generated coverage: line 91.5% (3582/3913), branch 71.1% (496/697).
- Main: `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1` passed with artifact `artifacts/wpf-ui-acceptance/20260430-022502`. RealStart and TrackingPipeline passed against a temp SQLite DB; cleanup emitted the known non-fatal already-closed process warning.

Committed and pushed:

- `d785a8f Add WPF action automation names`

## 2026-04-30 WPF Accessibility Test Helper And Details Names Slice

- Refactored duplicated WPF App accessibility test helpers into
  `WpfTestHelpers` for STA execution, visual/logical tree search, and
  automation-name assertions.
- Added a RED/GREEN WPF App test requiring DetailsTabsPanel's tab control,
  tab items, App/Web/Live lists, and rows-per-page selector to expose readable
  automation names.
- Updated DetailsTabsPanel XAML with semantic names while preserving existing
  AutomationIds and MVVM bindings.
- This is a WPF App-only accessibility/test-maintenance slice. It does not
  change tracking, SQLite, sync, browser capture, Android, or server behavior.

Verified so far:

- Popper: accessibility tests passed 3/3 before and after helper refactor.
- Popper: `DetailsTabsPanel_PrimaryTabsAndListsExposeReadableAutomationNames`
  failed RED on missing readable `DashboardTabs` name, then passed.
- Popper: accessibility tests passed 4/4 after the details names slice.
- Popper: WPF App tests passed 111/111.
- Main: `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "FullyQualifiedName~AccessibilityTests"` passed 4 tests.
- Main: `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal` passed 111 WPF App tests.
- Main: `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed 345 total .NET tests.
- Main: `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
- Main: `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1` passed and generated coverage: line 91.5% (3582/3913), branch 71.1% (496/697).
- Main: `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1` passed with artifact `artifacts/wpf-ui-acceptance/20260430-023850`. RealStart and TrackingPipeline passed against a temp SQLite DB; cleanup emitted the known non-fatal already-closed process warning.

## 2026-04-30 WPF Focus Persistence Extraction Slice

- Added a RED Windows infrastructure test for `WindowsFocusSessionPersistenceService`
  proving a focus session is saved to Windows SQLite, a pending
  `focus_session` outbox row is queued, the payload deserializes to the
  existing upload contract, process metadata remains present, and window title
  stays null for privacy.
- Moved focus-session SQLite/outbox payload creation out of
  `WindowsTrackingDashboardCoordinator` into
  `Woong.MonitorStack.Windows.Storage.WindowsFocusSessionPersistenceService`.
- Wired `Windows.App` DI so the coordinator uses the Windows infrastructure
  service for focus persistence. Browser/web-session persistence remains in
  the coordinator for a later, separate slice.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.Tests\Woong.MonitorStack.Windows.Tests.csproj --no-restore --filter "FullyQualifiedName~WindowsFocusSessionPersistenceServiceTests" -maxcpucount:1 -v minimal`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore --filter "FullyQualifiedName~WindowsTrackingDashboardCoordinatorTests" -maxcpucount:1 -v minimal`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore --filter "FullyQualifiedName~WindowsAppCompositionTests" -maxcpucount:1 -v minimal`
- `dotnet test tests\Woong.MonitorStack.Windows.Tests\Woong.MonitorStack.Windows.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed 346 total `.NET` tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1` generated coverage: line 91.6% (3624/3955), branch 70.9% (500/705).
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1` passed with artifact `artifacts/wpf-ui-acceptance/20260430-025532`. RealStart persisted a focus session into a temp SQLite DB and queued one outbox row; cleanup emitted the known non-fatal already-closed process warning.

## 2026-04-30 WPF Web Persistence Extraction Slice

- Added a RED Windows infrastructure test for `WindowsWebSessionPersistenceService`
  proving a web session is saved to Windows SQLite, a pending `web_session`
  outbox row is queued, the payload deserializes to the existing upload
  contract, client session id/duration are present, and full URL/page title
  remain null under the default domain-only policy.
- Moved WebSession SQLite/outbox payload creation out of
  `WindowsTrackingDashboardCoordinator` into
  `Woong.MonitorStack.Windows.Storage.WindowsWebSessionPersistenceService`.
- Wired `Windows.App` DI so the coordinator uses the Windows infrastructure
  service for web persistence while keeping browser capture/sessionization and
  dashboard snapshot mapping in the coordinator.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.Tests\Woong.MonitorStack.Windows.Tests.csproj --no-restore --filter "FullyQualifiedName~WindowsWebSessionPersistenceServiceTests" -maxcpucount:1 -v minimal`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore --filter "FullyQualifiedName~WindowsTrackingDashboardCoordinatorTests" -maxcpucount:1 -v minimal`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore --filter "FullyQualifiedName~WindowsAppCompositionTests" -maxcpucount:1 -v minimal`
- `dotnet test tests\Woong.MonitorStack.Windows.Tests\Woong.MonitorStack.Windows.Tests.csproj --no-restore -maxcpucount:1 -v minimal`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed 347 total `.NET` tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1` generated coverage: line 91.6% (3655/3988), branch 70.4% (504/715).
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1` passed with artifact `artifacts/wpf-ui-acceptance/20260430-031037`. RealStart persisted a focus session into a temp SQLite DB and queued one outbox row; cleanup emitted the known non-fatal already-closed process warning.

## 2026-04-30 WPF Local Style Dictionary Cleanup Slice

- Added RED architecture guard `WpfViewsAndControls_DoNotDefineLocalStyles`
  so WPF Views/Controls cannot drift back to local `Style` resource blocks.
- Moved the remaining local `SummaryMetricCardStyle` into
  `Styles/Cards.xaml` and moved Details tab header/app-session icon styles into
  `Styles/Tabs.xaml`.
- The existing color-literal guard remains clean: color literals are still
  isolated to `Styles/Colors.xaml`.

Verified:

- `dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~WpfViewsAndControls_DoNotDefineLocalStyles" -maxcpucount:1 -v minimal` failed RED on DetailsTabsPanel/SummaryCardsPanel local styles.
- `dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~WpfViewsAndControls_DoNotDefineLocalStyles|FullyQualifiedName~WpfXaml_DoesNotUseColorLiteralsOutsideColorsDictionary|FullyQualifiedName~AppResources_MergeEverySharedStyleDictionaryAtApplicationRoot" -maxcpucount:1 -v minimal` passed 3 tests.
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore --filter "FullyQualifiedName~MainWindowUiExpectationTests|FullyQualifiedName~DashboardAutomationIdContractTests|FullyQualifiedName~DashboardAccessibilityExpectationTests" -maxcpucount:1 -v minimal` passed 48 tests.
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed 348 total `.NET` tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1` generated coverage: line 91.6% (3655/3988), branch 70.4% (504/715).
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1` passed with artifact `artifacts/wpf-ui-acceptance/20260430-032325`. RealStart persisted a focus session into a temp SQLite DB and queued one outbox row; cleanup emitted the known non-fatal already-closed process warning.

## 2026-04-30 WPF Startup Lifecycle Extraction Slice

- Audited `App.xaml.cs` after commit `45c8d46`; it had grown beyond simple
  Generic Host glue by resolving `MainWindow`, selecting the Today dashboard
  period, and showing the window directly.
- Added `IWindowsAppStartupService`/`WindowsAppStartupService` in
  `Windows.App` to own initial dashboard refresh/show orchestration.
- `App.xaml.cs` now builds/starts/stops the Generic Host and delegates window
  initialization to `IWindowsAppStartupService`, keeping dashboard behavior out
  of code-behind.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore --filter "FullyQualifiedName~WindowsAppStartupService_Start_SelectsTodayAndShowsMainWindow" -maxcpucount:1 -v minimal` failed RED on missing `WindowsAppStartupService`, then passed.
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore --filter "FullyQualifiedName~WindowsAppCompositionTests" -maxcpucount:1 -v minimal` passed 8 tests.
- `dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~AppStartup_DoesNotManuallyConstructDashboardDependencies" -maxcpucount:1 -v minimal` passed 1 test.
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed 351 total `.NET` tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1` generated coverage: line 91.7% (3663/3991), branch 70.7% (506/715).
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1` passed with artifact `artifacts/wpf-ui-acceptance/20260430-033512`. RealStart persisted two focus-session rows into a temp SQLite DB and queued two outbox rows; cleanup emitted the known non-fatal already-closed process warning.

## 2026-04-29 WPF Browser Stop Flush Slice

- Added `BrowserWebSessionizer.CompleteCurrent` so an open browser domain
  session can be closed when tracking stops even if no later tab/domain event
  arrives.
- Kept privacy defaults intact: the WPF stop path persists domain-only
  `web_session` rows by default and does not store full URLs unless a future
  explicit opt-in policy enables them.
- Updated `WindowsTrackingDashboardCoordinator.StopTracking` to flush the open
  browser session, persist it to Windows local SQLite, enqueue a pending
  `web_session` outbox item, and refresh the dashboard through the existing
  web-persistence signal.
- Added WPF behavior coverage proving that starting on Chrome/github.com,
  waiting seven minutes, and clicking Stop writes the web session to SQLite,
  adds a web outbox row, and shows `github.com` in the Web Focus summary and
  Web Sessions grid.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.Tests\Woong.MonitorStack.Windows.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter CompleteCurrent_WhenTrackingStops_CreatesWebSessionForOpenDomain`
- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter StopButton_WhenBrowserSessionIsOpen_PersistsWebSessionAndRefreshesDashboard`
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1`
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1`

Latest WPF UI acceptance artifact:
`artifacts/wpf-ui-acceptance/20260429-220702`.

Coverage after this slice: overall line coverage 91.2%.

## 2026-04-30 WPF Same-Window Browser Navigation Evidence Slice

- Added a RED acceptance-coordinator test for one Chrome foreground window navigating `youtube.com -> github.com -> chatgpt.com` without closing the Chrome FocusSession.
- Updated `AcceptanceTrackingDashboardCoordinator` so same HWND/PID browser domain changes persist WebSessions only, while a foreground change to the second Chrome process/window closes the Chrome FocusSession.
- Updated WPF UI snapshot acceptance to use manual Start plus a slower test ticker, then capture before/during/after PNG evidence for same-window browser navigation, second Chrome process/window, Notepad, File Explorer, Stop, Sync, and Settings.
- Generated consolidated visual package at `artifacts/wpf-check/latest/`.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore --filter "FullyQualifiedName~AcceptanceTrackingDashboardCoordinatorTests|FullyQualifiedName~WpfUiAcceptanceScriptTests" -maxcpucount:1 -v minimal` passed 40 tests.
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed 407 tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1` passed with artifact `artifacts/wpf-ui-acceptance/20260430-163246`.
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1` generated coverage: line 91.9% (3774/4104), branch 70.8% (536/757).

## 2026-04-30 WPF Same-Window Browser Navigation Regression Slice

- Added real WPF coordinator regression coverage for repeated same Chrome HWND/PID navigation: `youtube.com -> github.com -> chatgpt.com`.
- Added MainWindow vertical coverage proving manual Start plus two poll ticks keeps tracking running, shows the current domain as `chatgpt.com`, persists prior `youtube.com` and `github.com` WebSessions to SQLite, creates pending `web_session` outbox rows, and refreshes the Web Focus summary plus Web Sessions grid before Stop.
- Verified privacy-safe domain-only behavior: test URLs include path/query values, but persisted SQLite rows and outbox payloads keep `Url = null` and do not contain path/query secrets.
- Confirmed no Chrome FocusSession is persisted while only the browser domain changes; FocusSession persistence still waits for foreground change or Stop.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore --filter "FullyQualifiedName~PollOnce_WhenSameChromeWindowVisitsYoutubeGithubChatGpt|FullyQualifiedName~PollTick_WhenSameChromeWindowDomainChangesTwice" -maxcpucount:1 -v minimal` passed 2 tests.
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed 409 total `.NET` tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1` generated coverage: line 91.9% (3774/4104), branch 70.8% (536/757).
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1` passed with artifact `artifacts/wpf-ui-acceptance/20260430-165524`. The known non-fatal cleanup warning appeared because the process was already closed.

## 2026-04-30 Chrome Native Messaging Cleanup Failure Evidence Slice

- Added Chrome native messaging acceptance script coverage requiring sandbox Chrome process cleanup, temp profile cleanup, and temp work root cleanup failures to be recorded in report/manifest artifacts.
- Added `$cleanupFailures` and grouped `Cleanup failures` safety evidence to `scripts/run-chrome-native-message-acceptance.ps1`.
- Replaced silent cleanup catches for sandbox Chrome process, temp profile, and temp work root cleanup with explicit warning/evidence rows.
- Verified cleanup-only dry-run keeps HKCU scoped to `com.woong.monitorstack.chrome_test` and writes `cleanupFailures` plus `nativeMessagingSafetyEvidence` under `artifacts/chrome-native-acceptance/latest/`.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.Tests\Woong.MonitorStack.Windows.Tests.csproj --no-restore --filter "FullyQualifiedName~ChromeNativeMessagingAcceptanceScriptTests" -maxcpucount:1 -v minimal` passed 14 tests.
- PowerShell parse check passed for `scripts\run-chrome-native-message-acceptance.ps1`.
- `powershell -ExecutionPolicy Bypass -File scripts\run-chrome-native-message-acceptance.ps1 -CleanupOnly -DryRun` passed and generated `artifacts/chrome-native-acceptance/20260430-170711`.

## 2026-04-30 Server Raw Event Unknown Device Guard Slice

- Added relational API coverage proving `/api/raw-events/upload` returns per-item `Error` for an unknown device and persists zero rows.
- Updated `RawEventUploadService` to pre-check device registration before raw-event persistence, matching the safer focus/web/location upload behavior.
- Updated the existing accepted/duplicate raw-event test to seed a registered device explicitly.

Verified:

- `dotnet test tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj --no-restore --filter "FullyQualifiedName~RawEventUploadApiTests" -maxcpucount:1 -v minimal` passed 2 tests.

## 2026-04-30 Android Usage Access Onboarding Gate Slice

- Added MainActivity launcher gating: Dashboard selection now shows `PermissionOnboardingFragment` when Usage Access is missing and `DashboardFragment` when granted.
- Added permission onboarding button behavior that launches `Settings.ACTION_USAGE_ACCESS_SETTINGS`.
- Added Robolectric tests for missing/granted Usage Access and the settings button intent.
- Added explicit Android UI snapshot evidence for permission onboarding as `13-permission-onboarding.png`.

Verified:

- `android\gradlew.bat -p android testDebugUnitTest --tests "com.woong.monitorstack.MainActivityTest" --no-daemon --rerun-tasks` passed after Kotlin daemon fallback recovered.
- `android\gradlew.bat -p android testDebugUnitTest assembleDebug assembleDebugAndroidTest --no-daemon` passed.
- `powershell -ExecutionPolicy Bypass -File scripts\run-android-ui-snapshots.ps1` passed with artifact `artifacts/android-ui-snapshots/20260430-171242` and screenshot `13-permission-onboarding.png`.

## 2026-04-30 Cross-Slice Verification

- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed 411 total `.NET` tests.
- `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1` generated coverage: line 91.9% (3783/4113), branch 70.8% (538/759).
- `powershell -ExecutionPolicy Bypass -File scripts\run-wpf-ui-acceptance.ps1` passed with artifact `artifacts/wpf-ui-acceptance/20260430-170819`. The known non-fatal already-closed process cleanup warning remains.

## 2026-04-30 Chrome Native Messaging Test Host And Allowed Origins Slice

- Added RED/green tests requiring the Chrome native messaging acceptance script to reject production host name `com.woong.monitorstack.chrome` before any production registry path can be targeted.
- Restricted acceptance to the test-only host `com.woong.monitorstack.chrome_test`; install/uninstall scripts remain generic, but acceptance is test-host only.
- Added `allowedOrigins` to `manifest.json` and a grouped `Deterministic allowed origins` sandbox safety row so reviewers can see the native manifest is scoped to the deterministic test extension origin.

Verified:

- `dotnet test tests\Woong.MonitorStack.Windows.Tests\Woong.MonitorStack.Windows.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "FullyQualifiedName~ChromeNativeMessagingAcceptanceScriptTests|FullyQualifiedName~ChromeNativeHostInstallationScriptTests"` passed 28 tests.
- `powershell -ExecutionPolicy Bypass -File scripts\run-chrome-native-message-acceptance.ps1 -HostName com.woong.monitorstack.chrome -CleanupOnly -DryRun` failed as expected before registry targeting.
- `powershell -ExecutionPolicy Bypass -File scripts\run-chrome-native-message-acceptance.ps1 -CleanupOnly -DryRun` passed and generated a cleanup-only artifact under `artifacts/chrome-native-acceptance/`.

Milestone 61 final verification update:

- `powershell -ExecutionPolicy Bypass -File scripts\run-chrome-native-message-acceptance.ps1 -DryRun` passed using cached Chrome for Testing and wrote deterministic allowed origin `chrome-extension://ilbldddbaocoepakcfpdibjdcfhdamon/` to `artifacts/chrome-native-acceptance/latest/manifest.json`.
- Full `.NET` solution tests passed 414 tests.
- Full `.NET` solution build passed with 0 warnings and 0 errors.
- Coverage generated: line 91.7% (3823/4166), branch 70.9% (544/767).

## 2026-04-30 Android Usage Access Return Recheck Slice

- Added Android tests for Usage Access missing/granted states and for returning from Settings after permission is granted.
- `MainActivity` now re-checks Usage Access on resume when the Dashboard tab is selected and reconciles UsageStats collection scheduling through `AndroidUsageCollectionScheduler`.
- `PermissionOnboardingFragment` now displays visible collection status text such as `Collection paused until Usage Access is granted.`

Verified:

- `android\gradlew.bat -p android testDebugUnitTest assembleDebug assembleDebugAndroidTest --no-daemon` passed.
- `powershell -ExecutionPolicy Bypass -File scripts\run-android-ui-snapshots.ps1` passed with artifact `artifacts/android-ui-snapshots/20260430-172700`.

## 2026-04-30 Server Daily Summary Local Midnight Split Slice

- Added relational coverage for active focus, idle focus, and web sessions that cross `Asia/Seoul` local midnight.
- `DailySummaryQueryService` now splits focus/web durations into local-date segments before calculating active, idle, web, top app, and top domain totals.
- Active totals continue to exclude idle segments; web totals preserve domain duration on the correct local date.

Verified as part of the full `.NET` solution test/build/coverage runs above.

## 2026-04-30 Android Runtime Last-Known Location Reader Slice

- Added `AndroidLastKnownLocationReader` and tests for GPS/network/passive last-known location metadata.
- The reader chooses the freshest last-known provider reading and skips provider `SecurityException`/`IllegalArgumentException` without crashing.
- `LocationContextCollectionRunner.create` now wires this Android framework reader instead of `NoopRuntimeLocationReader`.
- Privacy gates remain unchanged: no location snapshot without explicit location-context opt-in and foreground location permission; precise latitude/longitude still require separate precise opt-in and precise permission.
- No background location updates, continuous GPS polling, screen capture, app-content inference, or hidden tracking were added.

Verified:

- `android\gradlew.bat -p android testDebugUnitTest --tests "com.woong.monitorstack.location.AndroidLastKnownLocationReaderTest" --no-daemon` passed.
- `android\gradlew.bat -p android testDebugUnitTest --tests "com.woong.monitorstack.location.*" --no-daemon` passed.
- `android\gradlew.bat -p android testDebugUnitTest assembleDebug assembleDebugAndroidTest --no-daemon` passed.
- `powershell -ExecutionPolicy Bypass -File scripts\run-android-ui-snapshots.ps1` passed with artifact `artifacts/android-ui-snapshots/20260430-174439`.

## 2026-04-30 QA Checklist/Triage Slice

- Added `server_check_todo.md` for server device/focus/web/raw/location/idempotency/daily-summary/provider validation status.
- Added `docs/coverage-gap-triage.md` to explain intentional low/zero OS-bound coverage and prioritize future test gaps.
- WPF agent committed `4a89444 Add WPF check pointer package`, adding `scripts/run-wpf-check-package.ps1` and a WPF test for pointer-only report/manifest generation.

Milestone 64-65 final verification update:

- Full `.NET` solution tests passed 415 tests.
- Full `.NET` solution build passed with 0 warnings and 0 errors.
- Coverage generated: line 91.7% (3823/4166), branch 70.9% (544/767).
- Android Gradle unit/build/androidTest build passed.
- Android UI screenshots passed at `artifacts/android-ui-snapshots/20260430-174439`.

## 2026-04-30 Server Device Duplicate Registration Coverage Slice

- Added API regression coverage proving duplicate device registration updates `DeviceName`, `TimezoneId`, and `LastSeenAtUtc` while keeping a single device row and returning `isNew = false`.
- Product code already had the update behavior; this slice closes the server checklist coverage gap.

Verified:

- `dotnet test tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "FullyQualifiedName~RegisterDevice_WhenDeviceKeyAlreadyExists_UpdatesNameTimezoneAndLastSeen"` passed.

Milestone 66 final verification update:

- Full `.NET` solution tests passed 417 tests.
- Full `.NET` solution build passed with 0 warnings and 0 errors.
- Coverage generated: line 91.7% (3823/4166), branch 70.9% (544/767).
- Android emulator UI snapshots refreshed at `artifacts/android-ui-snapshots/20260430-174439` and resource measurements at `artifacts/android-resource-measurements/20260430-174552`.

## 2026-04-30 Server Focus Upload Mixed-Batch Idempotency Slice

- Added relational coverage for a focus upload batch containing an existing duplicate, a new accepted session, and an intra-batch duplicate of that new session.
- Fixed `FocusSessionUploadService` to seed a request-local idempotency set with existing DB session ids and add newly accepted ids as it processes the batch.
- This prevents EF tracker/unique-index failures and returns item-level `Duplicate`, `Accepted`, `Duplicate` statuses while persisting only one new row.

Verified:

- `dotnet test tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "FullyQualifiedName~UploadFocusSessions_WhenBatchContainsExistingAndIntraBatchDuplicate_ReturnsIndependentStatuses"` passed.
- `dotnet test tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "FullyQualifiedName~FocusSessionUpload"` passed 3 tests.

Milestone 67 final verification update:

- Full `.NET` solution tests passed 418 tests.
- Full `.NET` solution build passed with 0 warnings and 0 errors.
- Coverage generated: line 91.7% (3830/4173), branch 70.9% (544/767).

## 2026-04-30 Server Web Upload Mixed-Batch Idempotency Slice

- Added relational coverage for a web upload batch containing an existing duplicate, a new accepted web session, an intra-batch duplicate of that new session, and a web session whose focus parent is missing.
- Fixed `WebSessionUploadService` to seed a request-local idempotency set with existing DB web-session ids and add newly accepted ids as it processes the batch.
- This prevents relational unique-index failures and returns item-level `Duplicate`, `Accepted`, `Duplicate`, and `Error` statuses while persisting only one new row.

Verified:

- `dotnet test tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "FullyQualifiedName~UploadWebSessions_WhenBatchContainsDuplicateAcceptedIntraBatchDuplicateAndMissingFocusParent_ReturnsIndependentStatuses"` passed.
- `dotnet test tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "FullyQualifiedName~WebSessionUpload"` passed 5 tests.
- Full `.NET` solution tests passed 419 tests.
- Full `.NET` solution build passed with 0 warnings and 0 errors.
- Coverage generated: line 91.7% (3838/4181), branch 70.9% (544/767).

## 2026-04-30 Server Raw Event Mixed-Batch Idempotency Slice

- Added relational coverage for a raw-event upload batch containing an existing duplicate, a new accepted event, and an intra-batch duplicate of that new event.
- Fixed `RawEventUploadService` to seed a request-local idempotency set with existing DB raw-event ids and add newly accepted ids as it processes the batch.
- This prevents relational unique-index failures and returns item-level `Duplicate`, `Accepted`, and `Duplicate` statuses while persisting only one new row.

Concurrent agent breadcrumbs:

- `ddfc617` limits WPF UI Automation address-bar fallback URL metadata to origin-only while preserving domain extraction.
- `024dec1` normalizes Current Focus browser-domain display to domain-only when a URL-like value reaches the VM.
- `4fd5416` records Android Settings location opt-in UI evidence and screenshots.

Verified:

- `dotnet test tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "FullyQualifiedName~UploadRawEvents_WhenBatchContainsExistingAndIntraBatchDuplicate_ReturnsIndependentStatuses"` passed.
- `dotnet test tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "FullyQualifiedName~RawEventUpload"` passed 3 tests.
- Full `.NET` solution tests passed 421 tests.
- Full `.NET` solution build passed with 0 warnings and 0 errors.
- Coverage generated: line 91.8% (3851/4194), branch 71.0% (546/769).

## 2026-04-30 Server Location Context Relational Upload Slice

- Added relational coverage for a location-context upload batch containing an existing duplicate, a new accepted context, and an intra-batch duplicate of that new context.
- Added relational API coverage for an unregistered location device returning per-item `Error` and zero persisted rows.
- Fixed `LocationContextUploadService` to seed a request-local idempotency set with existing DB context ids and add newly accepted ids as it processes the batch.
- This prevents relational unique-index failures and preserves the privacy-safe location contract: nullable coordinates are allowed, precise latitude/longitude remain opt-in metadata, and no background tracking behavior was added.

Verified:

- `dotnet test tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "FullyQualifiedName~UploadLocationContexts_WhenBatchContainsExistingAndIntraBatchDuplicate_ReturnsIndependentStatuses|FullyQualifiedName~UploadLocationContexts_WhenDeviceIsNotRegistered_ReturnsControlledErrorAndDoesNotPersistRows"` passed 2 tests.
- `dotnet test tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "FullyQualifiedName~LocationContextUpload"` passed 4 tests.
- Full `.NET` solution tests passed 423 tests.
- Full `.NET` solution build passed with 0 warnings and 0 errors.
- Coverage generated: line 92.0% (3865/4201), branch 71.1% (547/769).

## 2026-04-30 Server Date Range Invalid Input HTTP Slice

- Added HTTP-level coverage for `/api/statistics/range` when `from` or `to` is malformed, `from > to`, `timezoneId` is invalid, or a required query value is missing.
- Updated the endpoint to validate user id, timezone id, ISO `yyyy-MM-dd` dates, and date ordering before calling `DailySummaryQueryService`.
- Invalid input now returns controlled `400 BadRequest` responses instead of leaking `FormatException`, `ArgumentException`, or timezone exceptions from the server pipeline.
- Successful range aggregation remains unchanged.

Concurrent agent breadcrumbs:

- `04e9fd7` clarifies WPF Web Sessions `URL Mode` so origin-only URLs are shown as `Domain only` rather than implying full URL storage.
- `50f8790` records Android Usage Access Settings handoff evidence with emulator before/action/after screenshots.

Verified:

- `dotnet test tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "FullyQualifiedName~GetDateRangeStatistics_WhenQueryIsInvalid_ReturnsBadRequest"` passed 5 tests.
- `dotnet test tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "FullyQualifiedName~DateRangeStatisticsApiTests"` passed 6 tests.
- Full `.NET` solution tests passed 429 tests.
- Full `.NET` solution build passed with 0 warnings and 0 errors.
- Coverage generated: line 91.7% (3916/4268), branch 71.2% (555/779).

## 2026-04-30 Server Date Range Local-Midnight Partial Allocation Slice

- Added HTTP coverage proving `/api/statistics/range` splits cross-midnight active focus, idle focus, and web sessions by requested local date and includes only the in-range portion.
- Updated `DailySummaryQueryService.GetRangeAsync` to reuse the same local-date duration segmentation used by daily summaries instead of summing whole sessions by start date.
- This keeps date-range totals, top apps, and top domains aligned with the product rule that reports are based on user-local dates and actual active/idle/web durations inside the selected period.

Verified:

- `dotnet test tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "FullyQualifiedName~GetDateRangeStatistics_SplitsCrossMidnightSessionsToOnlyInRangePortion"` passed.
- `dotnet test tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "FullyQualifiedName~DateRangeStatisticsApiTests"` passed 7 tests.
- Full `.NET` solution tests passed 430 tests.
- Full `.NET` solution build passed with 0 warnings and 0 errors.
- Coverage generated: line 91.7% (3908/4260), branch 71.5% (557/779).

## 2026-04-30 Server Raw Event Payload Privacy Guard Slice

- Added relational coverage proving safe raw-event metadata can persist while a forbidden payload containing `typedText` returns per-item `Error` and is not stored.
- Added a recursive server-side raw-event payload guard for forbidden property names associated with typed text, keylogging, passwords, form input, clipboard content, page/screen content, screenshots, and global touch coordinates.
- Duplicate detection remains first so idempotent retries keep returning `Duplicate`; the privacy invariant is that newly uploaded forbidden payloads are never persisted.

Concurrent agent breadcrumbs:

- `f6916e9` updates WPF Current Focus `LastDbWriteTimeText` when a web session persists without a focus-session close.
- `e127a52` records Android Room empty/seeded dashboard and seeded sessions evidence.

Verified:

- `dotnet test tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "FullyQualifiedName~UploadRawEvents_WhenPayloadContainsForbiddenUserInputMetadata_ReturnsErrorAndDoesNotPersistIt"` passed.
- `dotnet test tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj --no-restore -maxcpucount:1 -v minimal --filter "FullyQualifiedName~RawEventUpload"` passed 4 tests.
- Full `.NET` solution tests passed 431 tests.
- Full `.NET` solution build passed with 0 warnings and 0 errors.
- Coverage generated: line 91.5% (3957/4320), branch 71.5% (570/797).

Server checklist closure note:

- PostgreSQL-dependent concurrency idempotency work is marked `[blocked]` until a real PostgreSQL/Testcontainers fixture is available.
- `server_check_todo.md` has no remaining non-blocked unchecked server validation items after the raw-event privacy guard slice.

## 2026-04-30 Checklist Evidence Hygiene Slice

- Updated WPF checklist wording so `artifacts/wpf-check/latest/` is documented as the current consolidated WPF check package.
- Updated Android checklist evidence folder with the latest resource measurement artifact `artifacts/android-resource-measurements/20260430-174552/`.
- Physical-device Android resource measurement was treated as an external hardware blocker at that time; it is now optional future hardening and emulator evidence is accepted as the current baseline.

## 2026-04-30 - External Blocker Readiness Check

- Added `scripts/check-external-blockers.ps1` plus architecture tests so the remaining external blockers can be checked without touching registry, user DBs, screenshots, or input capture.
- Local run wrote `artifacts/external-blockers/20260430-184601` with status `BLOCKED`: only emulator `emulator-5554` is connected, and Docker CLI cannot reach the Docker Desktop Linux engine.
- Added `artifacts/external-blockers/` to `.gitignore` and recorded Milestone 75 in `total_todolist.md`.
- Focused validation: `dotnet test tests\Woong.MonitorStack.Architecture.Tests\Woong.MonitorStack.Architecture.Tests.csproj --no-restore --filter ExternalBlockerScript -v minimal` passed.
- Full validation after this slice: `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed 436 tests; `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed with 0 warnings/errors; `scripts\test-coverage.ps1` generated line 91.5% and branch 71.5% coverage.

## 2026-04-30 - Server PostgreSQL Testcontainers Validation

- Docker Desktop was started successfully; `scripts/check-external-blockers.ps1` now reports Docker daemon readiness as PASS while physical Android device readiness remains BLOCKED.
- Added `Testcontainers.PostgreSql` to server tests, `PostgresTestDatabase`, and explicit `PostgresFact` gating so routine `dotnet test` skips Docker-required tests unless `WOONG_MONITOR_RUN_POSTGRES_TESTS=1` is set.
- Added and ran `scripts/run-server-postgres-validation.ps1`; artifact `artifacts/server-postgres-validation/20260430-185823` passed.
- PostgreSQL validation applies EF Core migrations through Npgsql, verifies provider constraints, and verifies legacy `web_sessions.ClientSessionId` backfill before the required unique index is applied.
- Remaining server PostgreSQL item: concurrency/race idempotency against PostgreSQL.
- Validation after PostgreSQL slice: restore passed; standard solution test passed with 436 passed and 2 explicit PostgreSQL tests skipped by default; build passed with 0 warnings/errors; coverage remained line 91.5% and branch 71.5%; explicit PostgreSQL/Testcontainers script passed.

## 2026-04-30 - Server PostgreSQL Concurrent Idempotency

- Extended PostgreSQL/Testcontainers validation to concurrent duplicate focus, web, raw event, and location uploads.
- Fixed save-time unique-index race handling in focus/web/raw/location upload services so losing concurrent requests return idempotent `Duplicate` statuses when the row already exists.
- Latest PostgreSQL validation artifact: `artifacts/server-postgres-validation/20260430-190958`, status PASS.
- Server checklist now has no PostgreSQL/Testcontainers blocker remaining in this local environment.
- Validation after PostgreSQL concurrency slice: standard solution test passed with 436 passed and 6 explicit PostgreSQL tests skipped by default; build passed with 0 warnings/errors; coverage generated line 90.1% and branch 70.6%; explicit PostgreSQL/Testcontainers validation passed all 6 PostgreSQL tests.

## 2026-04-30 - Android Physical Device Measurement Guard

- Added `-RequirePhysicalDevice` to `scripts/run-android-resource-measurement.ps1` and architecture coverage for emulator-only blocking behavior.
- Local run `scripts\run-android-resource-measurement.ps1 -SkipBuild -RequirePhysicalDevice` generated `artifacts/android-resource-measurements/20260430-191835` with status `BLOCKED` because only `emulator-5554` is connected.
- Historical note: this preserved the previous rule that emulator evidence did not close physical-device Android resource measurement TODOs. That policy has since been revised: emulator evidence is the current acceptance baseline, and physical-device measurement is optional future hardening.
- Validation after physical-device guard slice: standard solution test passed with 437 passed and 6 explicit PostgreSQL tests skipped by default; build passed with 0 warnings/errors; coverage generated line 90.1% and branch 70.6%.

## 2026-04-30 - README Runbook Refresh

- Rewrote `README.md` as an operator/developer runbook covering WPF execution, Android emulator flow, PostgreSQL/EF/Testcontainers integration, Chrome native messaging acceptance, and test/coverage commands.
- The README keeps the PRD privacy boundary explicit: metadata-only measurement, local Windows/Android databases, and server-only integrated PostgreSQL storage.
- README runbook validation: `git diff --check` passed; standard solution test passed with 437 passed and 6 explicit PostgreSQL tests skipped by default; solution build passed with 0 warnings/errors.

## 2026-05-01 WPF Legacy SQLite Startup Fix

- [x] Reproduced WPF startup failure from the default local SQLite DB: old `web_session` schema lacked `capture_method`.
- [x] Added regression coverage proving `SqliteWebSessionRepository.Initialize()` migrates legacy `web_session` tables without losing rows.
- [x] Added automatic nullable-column migration for `capture_method`, `capture_confidence`, and `is_private_or_unknown`.
- [x] Verified the WPF exe starts against the default `%LOCALAPPDATA%\WoongMonitorStack\windows-local.db` and shows `Woong Monitor Stack`.
- [x] Added a README Quick Start clarifying that SQLite does not need to be launched separately.

## 2026-05-01 WPF Local Database Management UI

- [x] Added Settings UI for current SQLite DB path/status, create/switch DB, load existing DB, and delete/recreate local DB.
- [x] Added a testable dashboard database controller port so Presentation stays free of WPF dialogs and filesystem logic.
- [x] Added WPF App database controller implementation with SaveFileDialog/OpenFileDialog/confirmation dialog in the App layer only.
- [x] Made SQLite repositories use a switchable connection-string provider so dashboard/tracking reads and writes follow the selected DB without restarting.
- [x] Added controller, ViewModel, XAML AutomationId, and legacy schema migration regression tests.
- [x] Verified exact default local DB startup by launching the WPF exe; window title `Woong Monitor Stack` appeared and process was responding.
- [x] Validation: full `.NET` tests passed, full `.NET` build passed, coverage generated at 88.4% line and 69.7% branch coverage.

## 2026-05-01 WPF Chrome Switch Crash Fix And Runtime Logging

- [x] Diagnosed the Chrome/browser switch crash from Windows Application Event Log: `SQLite Error 19: NOT NULL constraint failed: web_session.url`.
- [x] Added a regression test for legacy `web_session.url TEXT NOT NULL` databases and changed SQLite initialization to rebuild that table with nullable `url` while preserving rows.
- [x] Added a runtime log sink and WPF Settings `Runtime log` path. Default path: `%LOCALAPPDATA%\WoongMonitorStack\logs\windows-runtime.log`.
- [x] Dashboard Start/Stop/Poll/Sync command exceptions are now caught, appended to the runtime log, and surfaced as `Runtime error` rows in the Live Event Log instead of killing the app.
- [x] Added WPF tick regression coverage proving a poll exception keeps the window open and tracking status visible.

### Validation Update

- [x] Full `.NET` solution tests passed: 450 passed, 6 explicit PostgreSQL/Testcontainers tests skipped by default.
- [x] Full `.NET` solution build passed with 0 warnings and 0 errors.
- [x] Coverage generated: line 88.6% (4277/4822), branch 69.7% (610/875).
- [x] WPF exe launched against the default local DB and remained responsive: `Woong Monitor Stack` window was running.

## 2026-05-01 Windows Taskbar, Release CI, And MSIX Packaging

- MainWindow now explicitly sets `ShowInTaskbar=True`.
- Clicking the WPF titlebar X no longer exits the app. The WM_SYSCOMMAND/SC_CLOSE path is intercepted and minimizes the window to the Windows taskbar so tracking can continue.
- Settings now includes **Exit app**, bound through `DashboardViewModel.ExitApplicationCommand` and `IDashboardApplicationLifetime`, as the explicit shutdown path.
- Added tests for taskbar/show-in-taskbar contract, system close button minimizing instead of closing, explicit application lifetime command, Settings Exit button binding, and Windows Release/CI/MSIX documentation and script contracts.
- Added `.github/workflows/windows-wpf-ci.yml` for Windows Release restore/build/test/publish/MSIX artifact packaging.
- Added `packaging/windows-msix/AppxManifest.xml`, `scripts/package-windows-msix.ps1`, and `scripts/install-windows-msix.ps1`.
- Local MSIX package succeeded at `artifacts/windows-msix/WoongMonitorStack.Windows.msix`.
- MSIX install certificate trust is explicit and CurrentUser-scoped; no LocalMachine trust modification is used.
- README and `docs/windows-release-msix.md` document Release build/run and MSIX packaging/install commands.

Validation:

- `dotnet restore Woong.MonitorStack.sln --configfile NuGet.config` passed.
- `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed: 460 passed, 6 PostgreSQL tests skipped by default.
- `dotnet build Woong.MonitorStack.sln -c Release --no-restore -m:1 -v minimal` passed with 0 warnings/errors.
- `dotnet test Woong.MonitorStack.sln -c Release --no-build -m:1 -v minimal` passed: 460 passed, 6 PostgreSQL tests skipped by default.
- `scripts\package-windows-msix.ps1` passed and generated unsigned MSIX.
- Coverage: line 87.2% (4232/4851), branch 68.1% (607/891).

Next Windows release work:

- Add signing certificate/secrets guidance for real distribution.
- Optionally add a release workflow triggered by tags after signing policy is decided.
- Release WPF run smoke passed with temp DB and auto-start disabled: `dotnet run --configuration Release --project src\Woong.MonitorStack.Windows.App\Woong.MonitorStack.Windows.App.csproj` started `Woong.MonitorStack.Windows.App` and the process was responding.

## 2026-05-01 Windows Signed MSIX CI Artifact

- Added TDD architecture checks for signed CI MSIX artifacts, CurrentUser-scoped install trust, GitHub Actions artifact contents, and `.pfx` non-upload behavior.
- Updated `.github/workflows/windows-wpf-ci.yml` so Windows CI packages with `scripts\package-windows-msix.ps1 -CreateTestCertificate`.
- `scripts\package-windows-msix.ps1 -CreateTestCertificate` now creates a transient CurrentUser test-signing cert, signs the MSIX, exports a public `.cer`, copies `install-windows-msix.ps1`, and writes an artifact README.
- The generated CI artifact `woong-monitor-windows-msix` contains the installable `.msix`, public `.cer`, install script, and README. It intentionally does not include the private `.pfx`.
- `docs/windows-release-msix.md` and `README.md` now explain how to download the GitHub Actions artifact and install it.

Validation:

- Focused Windows release packaging architecture tests passed: 6 passed.
- Local signed MSIX packaging passed with `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\package-windows-msix.ps1 -CreateTestCertificate`.
- Install script `-WhatIf` passed and reported `Cert:\CurrentUser\TrustedPeople` plus `Add-AppxPackage` actions without performing them.
- `Get-AuthenticodeSignature artifacts\windows-msix\WoongMonitorStack.Windows.msix` found signer `CN=WoongMonitorStack`; untrusted-root status is expected until the `.cer` is trusted.
- Full Debug solution test passed: 460 passed, 6 PostgreSQL/Testcontainers tests skipped by default.
- Release build passed with 0 warnings/errors.
- Release test passed: 460 passed, 6 PostgreSQL/Testcontainers tests skipped by default.
- Coverage generated: line 88.6%, branch 69.4%.

Next:

- For real public release, replace the per-run test certificate with a stable release signing certificate/secrets policy.
- Then add a tag-based release workflow.

## 2026-05-01 Windows MSIX 0x800B010A Certificate Trust Fix

- User hit Windows install error `0x800B010A`: publisher certificate could not be verified.
- Root cause: the previous install docs/script trusted the test certificate only in `Cert:\CurrentUser\TrustedPeople`, which is not reliable for Windows App Installer validation of self-signed MSIX test certificates.
- Added `-TrustScope LocalMachine|CurrentUser` to `scripts\install-windows-msix.ps1`; the default is now `LocalMachine`.
- `LocalMachine` trust imports the public `.cer` into `Cert:\LocalMachine\TrustedPeople` only when `-TrustCertificate` is explicitly passed and PowerShell is running as Administrator.
- `CurrentUser` trust remains available for development experiments, but docs warn it may not satisfy App Installer.
- Regenerated the local signed MSIX artifact and artifact README with the corrected elevated PowerShell command.
- Updated README and `docs/windows-release-msix.md` with the exact 0x800B010A remediation path.

Immediate install command for downloaded `woong-monitor-windows-msix` artifact:

```powershell
powershell -ExecutionPolicy Bypass -File .\install-windows-msix.ps1 -PackagePath .\WoongMonitorStack.Windows.msix -CertificatePath .\certificates\WoongMonitorStack.Windows.TestSigning.cer -TrustCertificate -TrustScope LocalMachine
```

Validation:

- Focused Windows release packaging architecture tests passed: 6 passed.
- Local signed MSIX packaging passed with `scripts\package-windows-msix.ps1 -CreateTestCertificate`.
- Install script `-WhatIf` passed for both `LocalMachine` and `CurrentUser` trust scopes.
- Full Debug solution test passed: 460 passed, 6 PostgreSQL/Testcontainers tests skipped by default.
- Release build passed with 0 warnings/errors.
- Release test passed: 460 passed, 6 PostgreSQL/Testcontainers tests skipped by default.
- Coverage generated: line 88.6%, branch 69.4%.

## 2026-05-01 WPF Range-Based Charts And Custom Period Picker

- Updated the WPF dashboard chart contract so App Focus and Domain Focus use horizontal LiveCharts RowSeries data. App/process labels and site/domain labels now live on the left category axis, with duration on the horizontal minute axis.
- Updated dashboard aggregation so summary cards, top apps, and top domains are calculated from the selected SQLite-backed query range rather than being filtered again to only the current local date. This keeps today's and custom focused foreground time tied to persisted local DB rows instead of process lifetime.
- Added an inline Custom period editor to ControlBar: start/end DatePicker controls, HH:mm time inputs, an Apply button, and a status label. The ViewModel converts local date/time input to UTC before querying the data source.
- Added behavior and UI contract tests for cross-local-date last-24h aggregation, custom date/time range parsing, horizontal chart mapping, and custom range control AutomationIds.

Focused validation:

- dotnet test tests\Woong.MonitorStack.Windows.Presentation.Tests\Woong.MonitorStack.Windows.Presentation.Tests.csproj --no-restore -maxcpucount:1 -v minimal passed: 67 passed.
- dotnet test tests\Woong.MonitorStack.Windows.App.Tests\Woong.MonitorStack.Windows.App.Tests.csproj --no-restore -maxcpucount:1 -v minimal passed: 155 passed.

Final validation for this slice:

- dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal passed: 464 passed, 6 explicit PostgreSQL/Testcontainers tests skipped by default.
- dotnet build Woong.MonitorStack.sln -c Release --no-restore -m:1 -v minimal initially failed because stale process Woong.MonitorStack.Windows.App (PID 16904) locked Release binaries. After stopping that local app process, the Release build passed with 0 warnings and 0 errors.
- Coverage collection passed for this slice: line 89.1% (4548/5100), branch 71.6% (734/1024). Report: artifacts/coverage/SummaryGithub.md.

## 2026-05-01 WPF Compact Top Charts, Detail Windows, And App Icons

- Dashboard App Focus and Domain Focus charts now publish only top 3 points for the main card surfaces.
- `상세보기` now goes through `IDashboardChartDetailsPresenter` and opens a dedicated WPF detail window with up to 10 horizontal bars.
- App Sessions rows now include `ProcessPath`; WPF renders real executable icons from that path when available and falls back to a glyph when missing.
- Added focused tests for top 3 vs top 10 chart behavior, chart detail presenter requests, detail window rendering, process icon conversion, and process path propagation.

Validation:

- Focused Windows Presentation tests passed: 70 passed.
- Focused Windows App tests passed: 158 passed.
- dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal passed: 470 passed, 6 explicit PostgreSQL/Testcontainers tests skipped by default.
- dotnet build Woong.MonitorStack.sln -c Release --no-restore -m:1 -v minimal passed with 0 warnings and 0 errors after closing a stale running WPF process that locked Release binaries.
- Coverage collection passed: line 89.4% (4714/5270), branch 73.4% (859/1169). Report: artifacts/coverage/SummaryGithub.md.

## 2026-05-01 WPF Session Duration Display Precision Fix

- Investigated the user-reported App/Web Sessions `1m` duration issue.
- Root cause was presentation formatting: persisted duration stayed in milliseconds, but row display used the summary-card minute formatter, so `45s`, `75s`, and similar short sessions appeared as `1m`.
- Added behavior tests for App Sessions and Web Sessions rows with second-precision durations.
- Updated row formatting only; summary card totals remain minute-oriented.

Validation:

- Focused Windows Presentation tests passed: 72 passed.
- dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal passed: 472 passed, 6 explicit PostgreSQL/Testcontainers tests skipped by default.
- dotnet build Woong.MonitorStack.sln -c Release --no-restore -m:1 -v minimal passed with 0 warnings and 0 errors.
- Coverage collection passed: line 89.3% (4737/5300), branch 73.4% (870/1185). Report: artifacts/coverage/SummaryGithub.md.

## 2026-05-01 WPF App Focus Chart Long Label Visibility Fix

- Investigated the user-reported App Focus chart card where Domain Focus showed top 3 bars but App Focus appeared blank.
- Root cause: compact dashboard horizontal charts used full app/process labels. Long executable names such as `Woong.MonitorStack.Windows.App.exe` could consume the category-axis area and leave the bar plot looking empty.
- Added label compaction for dashboard-card horizontal charts while preserving short labels like `Code.exe`.
- Detail chart windows now disable label compaction so the top-10 inspection view keeps full app/process labels.
- No telemetry, persistence, sync, or privacy behavior changed.

Validation:

- Focused Windows Presentation tests passed: 74 passed.
- Focused Windows App tests passed: 158 passed.
- dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal passed: 474 passed, 6 explicit PostgreSQL/Testcontainers tests skipped by default.
- dotnet build Woong.MonitorStack.sln -c Release --no-restore -m:1 -v minimal passed with 0 warnings and 0 errors after closing the stale local Release WPF process that was locking binaries.
- Coverage collection passed: line 88.3% (4480/5073), branch 69.6% (667/957). Report: artifacts/coverage/SummaryGithub.md.

## 2026-05-01 WPF Current Focus Icons And Rounded Badge Surface Fix

- Addressed the visual feedback that Current Focus runtime rows needed left-side icons and the header status badge colors should not paint a rectangular area outside the rounded badge shape.
- Added `StatusBadge.BadgeBackground` so badge fill is applied only to the inner rounded `Border`; `UserControl.Background` remains unset.
- Added `DetailRow.IconGlyph` and `DetailRow.IconAutomationId`, then wired icons for tracking state, current app, process, window title, browser domain, session duration, persisted session, poll time, browser capture, DB write, and sync state.
- Split the old combined `Last DB write / Sync state` visual label into clearer separate rows.
- No tracking, browser capture, SQLite, sync, or privacy behavior changed.

Validation:

- Focused badge RED/GREEN tests passed.
- Focused Current Focus icon RED/GREEN tests passed.
- Focused Windows App tests passed: 158 passed.
- Added a Presentation RED/GREEN regression so current browser-domain display preserves host labels such as `learn.microsoft.com` while still stripping URL path/query/fragment and port.
- WPF UI acceptance passed at `artifacts/wpf-ui-acceptance/20260501-040320`; the previous `learn.microsoft.com` semantic failure is resolved.
- dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal passed: 475 passed, 6 explicit PostgreSQL/Testcontainers tests skipped by default.
- dotnet build Woong.MonitorStack.sln -c Release --no-restore -m:1 -v minimal passed with 0 warnings and 0 errors.
- Coverage collection passed: line 88.2% (4511/5112), branch 69.7% (673/965). Report: artifacts/coverage/SummaryGithub.md.

## 2026-05-01 WPF Runtime Log Folder Command And Acceptance Excerpt

- Added a Settings `Open log folder` command that delegates through the Presentation runtime-log port and keeps filesystem/process launching in the WPF App adapter.
- Settings now shows a visible runtime-log status label, so users can tell whether the log folder was opened or why it could not be opened.
- The WPF UI acceptance root report and manifest now include the latest `logs/windows-runtime.log` excerpt from the temp acceptance run folder.
- Added RED/GREEN behavior tests for the command result, runtime-log folder launch adapter, Settings AutomationIds, and acceptance report/manifest contract.

Validation:

- Focused Windows Presentation tests passed: 76 passed.
- Focused Windows App tests passed: 160 passed.
- dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal passed: 478 passed, 6 explicit PostgreSQL/Testcontainers tests skipped by default.
- dotnet build Woong.MonitorStack.sln -c Release --no-restore -m:1 -v minimal passed with 0 warnings and 0 errors.
- WPF UI acceptance passed at artifacts/wpf-ui-acceptance/20260501-150730; the root report includes a Runtime Log Excerpt section.
- Coverage collection passed: line 88.0% (4526/5143), branch 69.5% (677/973). Report: artifacts/coverage/SummaryGithub.md.

## 2026-05-01 Windows Stable MSIX Signing Secrets

- Updated Windows WPF CI so release signing secrets `WINDOWS_MSIX_CERTIFICATE_BASE64` and `WINDOWS_MSIX_CERTIFICATE_PASSWORD` produce a signed MSIX with a stable certificate.
- CI now falls back to `-CreateTestCertificate` only when the release signing secrets are absent, keeping PR/main artifacts locally testable.
- `scripts/package-windows-msix.ps1` now exports a public `WoongMonitorStack.Windows.Signing.cer` from a provided PFX so install artifacts never include the private key.
- Updated README and Windows MSIX docs with the stable release signing secret path and fallback behavior.

Validation in progress:

- Focused Windows release packaging architecture tests passed: 7 passed.
- PowerShell package script parsed successfully.
- Full solution tests passed after this note: 479 passed, 6 skipped.
- Release build passed with 0 warnings and 0 errors.
- Coverage collection passed: line 88.0% (4526/5143), branch 69.5% (677/973).
## 2026-05-01 Windows Tag-Based Release Workflow

- Added `.github/workflows/windows-wpf-release.yml` for `v*` tags and manual dispatch.
- Release publishing now requires the stable MSIX signing secrets and intentionally has no ephemeral test-certificate fallback.
- The release workflow builds, tests, publishes, signs the MSIX, zips `artifacts/windows-msix`, uploads the zip artifact, and attaches it to the GitHub Release through `softprops/action-gh-release`.
- Updated README and Windows release documentation.

Validation in progress:

- RED architecture test failed before the release workflow existed.- Focused tag-release architecture tests passed: 1 passed, then coverage-stability architecture tests passed: 2 passed.
- Full solution tests passed before the final coverage-stability fix: 480 passed, 6 skipped. Final coverage run also executed all test projects successfully: 481 passed, 6 skipped.
- Release build passed after the final coverage-stability fix with 0 warnings and 0 errors.
- Coverage collection passed after disabling Presentation test collection parallelism: line 88.0% (4526/5143), branch 69.5% (677/973).
## 2026-05-01 WPF Domain Chart Title Wrap

- Updated the domain focus chart header so `도메인별` remains on the first line and `집중 시간` wraps to the next line.
- Kept the `상세보기` action button in the existing right-side header position.
- Added a WPF behavior test that locks the split title and details-button placement.

Validation:

- RED test failed before the XAML change because the split title AutomationIds did not exist.
- Focused chart/header tests passed: 7 passed.
- dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal passed: 482 passed, 6 explicit PostgreSQL/Testcontainers tests skipped by default.
- dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal passed with 0 warnings and 0 errors.
## 2026-05-01 Android Emulator README Handoff

- Added README guidance for Android emulator startup from PowerShell, including SDK env vars, AVD listing, `Medium_Phone` launch, boot-complete check, Gradle unit/build commands, install/launch commands, Usage Access settings handoff, and manual screenshot capture.
- Added an architecture test that keeps the Android emulator/build/install/launch/screenshot README flow documented.
- Verified the local SDK has a `Medium_Phone` AVD available.

Validation:

- RED README architecture test failed before the new emulator creation/boot/screenshot commands were documented.
- Focused README architecture test passed: 1 passed.
- Android Gradle `testDebugUnitTest assembleDebug` passed.
- dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal passed: 483 passed, 6 explicit PostgreSQL/Testcontainers tests skipped by default.
- dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal passed with 0 warnings and 0 errors.

## 2026-05-01 Android Emulator Manual Launch Note

- Started the `Medium_Phone` emulator and verified `sys.boot_completed=1` on `emulator-5554`.
- Installed `android/app/build/outputs/apk/debug/app-debug.apk` and launched `com.woong.monitorstack/.MainActivity`.
- Captured the current app screen at `artifacts/android-check/manual/dashboard.png`; the app opened on the Sessions tab with sample session rows visible.
- Corrected README screenshot guidance to use `adb shell screencap` plus `adb pull` because PowerShell redirection can corrupt raw `exec-out screencap` PNG bytes.

Validation:

- Focused Android README architecture test passed after the screenshot command correction.
- dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal passed: 483 passed, 6 explicit PostgreSQL/Testcontainers tests skipped by default.
- dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal passed with 0 warnings and 0 errors.

## 2026-05-01 Android Immediate Usage Collection And Bottom Navigation Fix

- [x] Compact bottom navigation now uses a 72dp base height plus runtime system navigation inset so Dashboard/Sessions/Report/Settings stay visible without hardcoded extra bottom padding.
- [x] MainActivity now performs a foreground immediate UsageStats collection when Dashboard is shown and Usage Access is granted, then refreshes the Room-backed dashboard after collection.
- [x] Collection remains metadata-only: package names and foreground intervals from UsageStatsManager; no typed text, screen content, touch coordinates, or page content are captured.
- [x] Android usage collection defaults to enabled after explicit Usage Access grant while sync remains off/local-only by default.
- [x] Current Focus now displays the latest persisted session, not the longest top app, so returning from Chrome shows Chrome as the current/latest app.
- [x] Emulator evidence captured:
  - `artifacts/android-check/manual/android-insets-start2.png`
  - `artifacts/android-check/manual/after-chrome-current-fixed.png`
- [x] Local emulator DB proof after Chrome return: `focus_sessions=9`, `sync_outbox=9`, with latest rows including `com.android.chrome`.
- [x] Validation: `android\\gradlew.bat testDebugUnitTest assembleDebug --no-daemon --stacktrace`, `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`, and `dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal` passed.

## 2026-05-01 Android Splash, Permission, And Current Focus Correction

- Fixed the user-reported Current Focus problem: when the Android app itself is foreground, Dashboard now shows `Woong Monitor / com.woong.monitorstack` instead of stale Room rows such as Nexus Launcher or Chrome.
- Added an explicit Splash route before permission/dashboard routing, with top app bar and bottom navigation hidden during Splash and permission onboarding.
- Updated Android 12+ launch splash theme branding so the OS splash uses the Woong bar logo rather than the default Android icon.
- Reworked Splash and Permission XML toward the supplied reference images while preserving XML/View MVP constraints.
- Permission onboarding now shows the shield/principles card layout and a separate runtime status text for Usage Access collection state.
- Added behavior and architecture tests covering Splash routing, permission routing, foreground Current Focus precedence, Splash branding, and updated permission layout contracts.

Validation:

- Android Gradle `testDebugUnitTest assembleDebug --no-daemon --stacktrace` passed.
- dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal passed: 486 passed, 6 explicit PostgreSQL/Testcontainers tests skipped by default.
- dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal passed with 0 warnings and 0 errors.
- Emulator screenshots:
  - `artifacts/android-check/latest/00-os-splash-branded.png`
  - `artifacts/android-check/latest/02-permission-late2.png`
  - `artifacts/android-check/latest/05-dashboard-final.png`

Remaining Android UI gaps:

- Dashboard visual still needs a fuller pass against the supplied reference.
- Top-app list/card, chart rendering, report/detail screens, and screenshot coverage for each bottom-tab flow remain open.
