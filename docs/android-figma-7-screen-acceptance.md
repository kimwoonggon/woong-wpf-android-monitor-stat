# Android Figma 7-Screen Acceptance

Updated: 2026-05-02

This inventory maps the user-provided Android Figma/SVG flow to concrete
Android XML/View implementation surfaces, behavior tests, and screenshot
artifacts. It is the working acceptance map for `android_check_todo.md`.

Android UI flow reference image:

```text
docs/assets/android/android-ui-flow-reference.png
```

This image is a design reference for the Android XML/View flow. It is not
product telemetry, not a monitored-user screenshot, and not runtime collection
evidence.

Latest clean emulator evidence:

```text
artifacts/android-ui-snapshots/20260502-073133/
artifacts/android-ui-snapshots/latest/
```

Latest snapshot result: `report.md` status is `PASS`, all seven canonical
Figma screenshots are `PASS`, and the crash buffer remained empty. Dashboard,
App Detail, Report, and Settings first-viewports were tightened against the
reference so charts, ranked rows, settings sections, and session lists appear
earlier without clipping. The latest `figma-06-report.png` shows a seeded
multi-day, multi-point trend; Sessions rows are compact; Settings is grouped by
Permissions, Collection, Sync, and Privacy, including sync server URL and device
ID configuration fields.

## 2026-05-02 Seven-Screen Visual Review

Reference compared:

```text
docs/assets/android/android-ui-flow-reference.png
```

Latest local evidence compared:

```text
artifacts/android-ui-snapshots/20260502-073133/
artifacts/android-ui-snapshots/latest/
artifacts/android-ui-snapshots/latest/figma-01-splash.png
artifacts/android-ui-snapshots/latest/figma-02-permission.png
artifacts/android-ui-snapshots/latest/figma-03-dashboard.png
artifacts/android-ui-snapshots/latest/figma-04-sessions.png
artifacts/android-ui-snapshots/latest/figma-05-app-detail.png
artifacts/android-ui-snapshots/latest/figma-06-report.png
artifacts/android-ui-snapshots/latest/figma-07-settings.png
```

Aligned now:

- Splash keeps the shell hidden and presents the logo, product name, subtitle,
  and loading state as a clean first-run screen.
- Permission keeps the back affordance, shield visual, Usage Access purpose,
  privacy principles, and primary Settings action visible.
- Dashboard now matches the intended information order: status chips, current
  focus, summary cards, period filters, compact chart, and an App usage hint in
  the first viewport.
- Sessions now shows the title, filters, count, and readable compact rows with
  app name, package, time range, duration, and state.
- App Detail now brings the selected app identity, total/session cards, compact
  hourly chart, and session rows into a reference-like first viewport.
- Report now uses two summary cards, a compact trend chart, and ranked Top apps
  rows with proportional bars visible earlier.
- Settings now groups Permissions, Collection, Sync, and privacy-oriented
  controls clearly while keeping Usage Access, sync-off, server/device fields,
  and local-only/privacy copy visible.

Future polish:

- Copy density differs from the compact Figma reference because the runtime UI
  uses larger accessible text and a mix of Korean and English strings.
- Dashboard status chips and period filters are wider than the reference on
  narrow phone screenshots.
- Placeholder app icons are functional initials rather than real package icons.
- Settings has extra production-safe configuration copy and sync fields that
  are intentionally more explicit than the reference.
- Bottom navigation and Android status bar proportions remain emulator/device
  dependent and should be checked in future visual sweeps.

Privacy boundary: these screenshots are local developer evidence only. They are
not product telemetry and must not be used as monitored-user screenshots. The
product must continue to avoid typed text, passwords, form contents, clipboard
contents, browser/page contents, other-app screenshots, and global touch
coordinates.

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

Settings sync-config behavior is now part of the Settings acceptance surface:
server base URL and device ID default blank, persisted values are trimmed, sync
off keeps Manual Sync local-only, sync on with missing configuration shows a
configuration-required result without enqueueing work, and sync on with valid
configuration enqueues `AndroidSyncWorker` with explicit base URL/device ID
input data. Real server auth, device registration, and production endpoint
hardening remain future release work.

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
the Figma flow. `DashboardActivity`, `SessionsActivity`, and `SettingsActivity`
are retained only as internal compatibility/dev entry points with
`android:exported="false"`; each hosts the same canonical `DashboardFragment`,
`SessionsFragment`, or `SettingsFragment` content used by the shell. There is no
separate `ReportActivity` or `AppDetailActivity` path. `DailySummaryActivity`
remains a separate previous-day summary compatibility surface and is not one of
the seven canonical Figma screens.

Legacy compatibility Activities must not inflate stale standalone Activity XML.
The obsolete `activity_dashboard.xml`, `activity_sessions.xml`, and
`activity_settings.xml` layouts have been removed; resource/build contracts now
keep the retained Activities on canonical Fragment content.

## Chrome/app-switch QA

Android cannot track browser domains like the Windows Chrome native-messaging
pipeline. Android app-switch QA must focus on app usage metadata:

1. Launch Woong Monitor.
2. Open Chrome or another app without capturing that app's page contents.
3. Return to Woong Monitor.
4. Run/trigger recent UsageStats collection.
5. Prove the external app foreground interval with package/process/window
   metadata such as `dumpsys`, not screenshots of browser/page contents.
6. Verify Room `focus_session` rows include the external app.
7. Verify `sync_outbox` rows are created while sync remains opt-in.
8. Verify Dashboard and Sessions refresh from Room.

Use `scripts/start-android-emulator-stable.ps1` before emulator QA so stale
snapshot state does not hide app-switch regressions.

Screenshots for this QA must be limited to Woong Monitor UI after returning to
the app. They are local developer evidence only and are not product telemetry.
Do not capture Chrome screenshots, Chrome UI hierarchy dumps, page text, page
titles, URL paths, typed text, form contents, or browser page contents. Chrome
foreground proof must come from package/process/window metadata such as
`dumpsys`, plus Room rows and Woong Monitor UI after return.

### App-switch gate states

- `PASS`: emulator/app install succeeds, Chrome or another external package is
  proven foreground by metadata, collection returns to Woong Monitor, Room
  `focus_session` includes the external package, `sync_outbox` rows are created
  while sync remains opt-in, Dashboard and Sessions refresh from Room, and
  diagnostics show no product crash.
- `BLOCKED`: emulator boot, adb targeting, APK install, instrumentation install,
  or device connectivity prevents the QA from reaching product behavior. Record
  the blocker, command output, device serial, and any partial artifact folder
  without marking product behavior pass/fail.
- `FAIL`: the QA reaches the product flow but UsageStats collection, Room
  persistence, sync outbox creation, Dashboard refresh, Sessions refresh, or
  privacy boundaries behave incorrectly.

### Latest app-switch result

Status: `PASS`

Evidence:

```text
artifacts/android-app-switch-qa/20260502-073336/
artifacts/android-app-switch-qa/latest/
```

Verified:

- `report.md` status is `PASS`.
- `room-assertions.json` status is `PASS`.
- Room assertions include Chrome focus-session rows and pending sync outbox
  rows.
- Foreground-after-return shows `com.woong.monitorstack`.
- `dashboard-after-app-switch.png` and `sessions-after-app-switch.png` exist.
- No Chrome screenshots or Chrome UI dumps were captured.
