# Android Figma 7-Screen Acceptance

Updated: 2026-05-02

This inventory maps the user-provided Android Figma/SVG flow to concrete
Android XML/View implementation surfaces, behavior tests, and screenshot
artifacts. It is the working acceptance map for `android_check_todo.md`.

Latest clean emulator evidence:

```text
artifacts/android-ui-snapshots/20260502-040753/
artifacts/android-ui-snapshots/latest/
```

The Android app remains UsageStatsManager metadata only. It measures which apps
were foreground for how long, local Room persistence, sync state, optional
permission-gated location context, and daily summaries. It must collect no typed
text, passwords, form contents, clipboard contents, browser/page contents,
other-app screenshots, or global touch coordinates.

Privacy shorthand for this acceptance run: no typed text, passwords, form contents, clipboard contents, browser/page contents, other-app screenshots, or global touch coordinates.

## Canonical Screen Map

| Figma screen | XML layout | Runtime surface | Current behavior gate | Canonical screenshot |
|---|---|---|---|---|
| Splash | `fragment_splash.xml` | `SplashFragment` | `MainActivityTest.launcherShowsSplashBeforeRoutingToDashboard` | `figma-01-splash.png` |
| Permission | `fragment_permission_onboarding.xml` | `PermissionOnboardingFragment` | `MainActivityTest.whenUsageAccessMissingShowsPermissionOnboarding` | `figma-02-permission.png` |
| Dashboard | `fragment_dashboard.xml` | `DashboardFragment` | `MainActivityTest.dashboardRollingPeriodButtonsReloadRoomBackedSummary` | `figma-03-dashboard.png` |
| Sessions | `fragment_sessions.xml` | `SessionsFragment` | `MainActivityTest.sessionsPeriodButtonsReflectSelectedRange` | `figma-04-sessions.png` |
| App Detail | `fragment_app_detail.xml` | `AppDetailFragment` | `RoomSessionsRepositoryTest` | `figma-05-app-detail.png` |
| Report | `fragment_report.xml` | `ReportFragment` | `MainActivityTest.reportTabLoadsRoomBackedSevenDaySummary` | `figma-06-report.png` |
| Settings | `fragment_settings.xml` | `SettingsFragment` | `MainActivityTest.settingsTabShowsRuntimePrivacySyncAndLocationControls` | `figma-07-settings.png` |

## Screenshot Contract

`scripts/run-android-ui-snapshots.ps1` must pull the seven canonical Figma
screenshots from the instrumentation output directory:

```text
figma-01-splash.png
figma-02-permission.png
figma-03-dashboard.png
figma-04-sessions.png
figma-05-app-detail.png
figma-06-report.png
figma-07-settings.png
```

These screenshots are local developer evidence only. They are not product
telemetry and must not screenshot other apps as monitoring data.

Before launching screenshot instrumentation, the local snapshot script clears
external Android UI interference so stale browser ANR/system dialogs do not
cover the Woong Monitor screenshots. This cleanup is scoped to local QA:
force-stop known Chrome packages, broadcast `CLOSE_SYSTEM_DIALOGS`, and send a
best-effort BACK key event. It does not change product telemetry behavior.

## Legacy Activity cleanup

The MainActivity Fragment shell is the target user-facing implementation for
the Figma flow. Older standalone `DashboardActivity`, `SessionsActivity`,
`DailySummaryActivity`, and `SettingsActivity` still exist as compatibility and
test surfaces. They must either be removed in a later cleanup slice or
documented as deliberate deep-link/dev surfaces. They must not become stale
alternate UI paths that contradict the shell.

## Chrome/app-switch QA

Android cannot track browser domains like the Windows Chrome native-messaging
pipeline. Android app-switch QA must focus on app usage metadata:

1. Launch Woong Monitor.
2. Open Chrome or another app.
3. Return to Woong Monitor.
4. Run/trigger recent UsageStats collection.
5. Verify Room `focus_session` rows include the external app.
6. Verify `sync_outbox` rows are created while sync remains opt-in.
7. Verify Dashboard and Sessions refresh from Room.

Use `scripts/start-android-emulator-stable.ps1` before emulator QA so stale
snapshot state does not hide app-switch regressions.
