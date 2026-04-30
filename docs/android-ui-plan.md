# Android UI Plan

Updated: 2026-04-30

This document is the Android XML/View UI plan for Woong Monitor Stack. The
Android app measures usage metadata: which apps were foreground for how long,
local collection/sync status, daily summaries, and optional location context
only when the user explicitly enables it.

The Android MVP remains Kotlin + XML/View. Do not replace it with Compose for
the MVP.

## Core Screens

- Splash / loading.
- Permission guidance.
- Dashboard.
- Sessions.
- App detail.
- Reports / daily summary.
- Settings.

## Dashboard Questions

The dashboard should answer:

1. Is Usage Access granted?
2. Is collection enabled?
3. What app is currently or most recently foreground?
4. How much Active Focus Time was recorded for the selected period?
5. Which apps consumed the most time?
6. Is sync off/local-only or enabled?
7. Are privacy settings safe?
8. Is optional location context disabled, unavailable, or enabled?

## Optional Location Context

Latitude and longitude are sensitive metadata. They may be added to the Android
UI plan, but they must stay opt-in and permission-gated.

Korean UI labels:

- 위치 상태
- 위도
- 경도
- 정확도
- 마지막 위치 수집
- 위치 기록: 꺼짐

English model fields:

- `latitude`
- `longitude`
- `accuracyMeters`
- `capturedAtUtc`
- `locationPermissionState`
- `locationCaptureEnabled`
- `locationStorageMode`

Rules:

- Location capture is off by default.
- Location capture requires explicit opt-in in Settings.
- Location capture requires Android runtime permission:
  `ACCESS_COARSE_LOCATION` and, only when the user chooses precise location,
  `ACCESS_FINE_LOCATION`.
- The app must not infer location from other app content.
- The app must not infer location from screenshots, notifications, messages,
  browser pages, form input, clipboard content, or typed text.
- The app must not collect continuous background GPS traces for MVP.
- The app may show the latest user-approved location context on the Dashboard.
- Latitude and longitude must be nullable when permission or opt-in is missing.
- Sync remains opt-in. Location data must not upload when sync is off.
- Prefer approximate/coarse location by default. Precise latitude/longitude is
  a separate explicit opt-in.

Recommended Dashboard placement:

- Add a compact `Location Context` card under Current Focus or Settings status.
- Show `Location capture off` when disabled.
- Show `위도`, `경도`, and accuracy only when location capture is enabled and
  permission is granted.
- Show a clear permission-needed state and a button that opens Android location
  permission settings when permission is missing.

Recommended Settings placement:

- Location section after Privacy and before Sync.
- Toggle: `Enable location context` off by default.
- Mode selector: `Approximate` default, `Precise latitude/longitude` explicit.
- Text: `Location metadata is optional and is stored locally unless sync is
  enabled.`
- Clear location metadata action, guarded by confirmation.

## Data Model Direction

Initial implementation should keep location separate from app usage sessions
until product behavior is proven.

Suggested local Room entity:

```text
location_context_snapshot
  id
  deviceId
  capturedAtUtc
  latitude nullable
  longitude nullable
  accuracyMeters nullable
  provider nullable
  permissionState
  captureMode
  createdAtUtc
```

If later attached to app sessions, use nullable foreign keys or a join table so
existing `focus_session` rows remain valid without location.

Suggested server direction:

- Add optional DTO/upload contracts only after the local privacy UX and tests exist.
- Keep `latitude` and `longitude` nullable.
- Do not include location fields in default upload payloads.
- Do not integrate Windows and Android local DBs directly.

## Required Tests

Add tests before implementation:

- `LocationSettings_DefaultsToOff`
- `LocationSettings_PreciseCoordinatesRequireExplicitOptIn`
- `LocationPermissionMissing_ShowsGuidanceWithoutCoordinates`
- `LocationDashboard_WhenOptInAndFakeLocationAvailable_ShowsLatitudeLongitude`
- `LocationDashboard_WhenOff_DoesNotShowCoordinates`
- `LocationRepository_PersistsNullableLatitudeLongitude`
- `LocationSync_WhenSyncOff_DoesNotUploadCoordinates`
- `LocationSync_WhenOptInAndSyncEnabled_IncludesNullableCoordinates`
- `PrivacyBoundary_DoesNotInferLocationFromOtherAppContent`

## Screenshot And Device Automation

Android screenshot automation should capture the Settings location section and
Dashboard location card when the feature exists. Screenshots remain local test
artifacts only. They must not capture other apps as telemetry.

## Current XML Implementation Direction

The current Android app now has a `MainActivity` shell with
`FragmentContainerView`, `MaterialToolbar`, and `BottomNavigationView`, matching
the Android XML wireframe skeleton. Existing Activity screens remain in place
as stable Room-backed runtime surfaces while the fragment shell is brought up
screen by screen.

The user-provided XML wireframe skeleton is the target shape for the fragment
shell, not just a loose reference. A fragment shell screen is not complete until
it is both Room-backed and visually usable: compact toolbar/header, reachable
bottom navigation, card-based dashboard sections, period filters, recent
sessions, report/settings flows, optional latitude/longitude location context,
and feature screenshots for each tab.

- Shared `wms_*` color tokens.
- Shared `WmsCard`, status chip, section title, key/value, and period button
  styles.
- `activity_main.xml` shell with top app bar, fragment container, and Material
  bottom navigation.
- Fragment XML skeletons for Splash, Permission onboarding, Dashboard,
  Sessions, App detail, Report, and Settings.
- Dashboard card/chip/period/current-focus/summary/chart/recent-session
  hierarchy.
- Settings grouped cards for permissions, sync, privacy, location, and storage.
- Sessions and Daily Summary card-based screens.

## Implemented So Far

- Settings copy states that location context is off by default.
- Settings copy states that latitude/longitude are not stored unless location
  context is enabled.
- Settings copy states that precise latitude/longitude requires separate
  explicit opt-in.
- Foreground location permissions are declared for optional location context:
  coarse and fine only. Background location is not declared.
- Location settings persist with safe defaults: location capture off,
  approximate mode preferred, and precise latitude/longitude disabled unless
  location context is already enabled.
- The location permission request button stays disabled until location context
  is enabled.
- Location permission policy requests coarse location for approximate mode and
  adds fine location only after precise latitude/longitude opt-in.
- Room now has a local-only `location_context_snapshots` table and DAO.
  Component tests prove nullable `latitude`, `longitude`, `accuracyMeters`,
  and `capturedAtUtcMillis` are preserved, and that snapshots can be queried by
  device plus captured UTC range.
- Dashboard ViewModel, Room repository, and XML layout tests now cover a
  `Location context` card with safe default text plus fake opt-in
  latitude/longitude display from local Room data.
- Sync payload factory tests now prove location snapshots are excluded while
  sync is off or location context is off. Nullable `latitude`, `longitude`, and
  accuracy values are included only when both sync and location context are
  explicitly enabled.
- Android sync runner and WorkManager tests now prove pending
  `location_context` outbox rows stay local while sync is off, stay local while
  location context is off, upload only when both opt-ins are enabled, and retry
  failed location uploads through the same path as focus sessions.
- Runtime location capture policy/provider seam now has no-hardware unit tests:
  it returns no local snapshot while location context is off or foreground
  permission is missing, keeps approximate-mode latitude/longitude null, stores
  precise coordinates only after separate precise opt-in plus precise
  permission, and stays independent from sync opt-in.
- Dedicated local location-context collection now has no-hardware unit tests:
  when the provider returns a snapshot, the runner writes
  `LocationContextSnapshotEntity` to Room-facing storage and enqueues a
  `location_context` outbox row; when the provider returns null, it writes
  nothing. Sync opt-in remains an upload gate, not a local capture gate.
- The existing WorkManager usage collection worker now invokes the local
  location-context collector with a device id and reports whether a local
  snapshot was captured. Tests use fake collectors and do not require a device;
  the default runtime path still uses a no-op location reader until a
  hardware-backed reader is implemented.
- Server now exposes a dedicated `location_contexts` upload path and PostgreSQL
  table. The table keeps `latitude`, `longitude`, and `accuracyMeters`
  nullable, uses `deviceId + clientContextId` idempotency, and is separate from
  app usage sessions so location remains optional metadata.
- Activity XML layouts now follow the Android wireframe skeleton's card/chip
  visual hierarchy while preserving existing ViewBinding IDs.
- Android screenshot automation now captures numbered feature screenshots for
  dashboard overview, summary/location, charts, recent sessions, settings
  privacy/sync, settings location permission, sessions list, and daily summary.
- Primary Android Activity layouts now opt into system-window fitting so content
  does not render underneath the status bar.
- Sessions and Dashboard recent-session lists now use a structured
  `item_focus_session` row with package, local time range, active/idle state,
  and duration.
- `MainActivity` no longer redirects to `DashboardActivity`; it owns the
  Material shell and selects Dashboard, Sessions, Report, and Settings
  fragments through bottom navigation.
- Android UI snapshot automation now captures `09-main-shell.png` to prove the
  real launcher shell is visible on an emulator.
- Fragment Dashboard summary cards now use distinct labels for Active Focus,
  Screen On, Idle time, and local-only sync state instead of repeating one
  placeholder title.
- `DashboardFragment` now loads `RoomDashboardRepository` through
  `DashboardViewModel`, removes hardcoded demo runtime data, and renders seeded
  local Room totals/current-focus copy in the launcher shell screenshot.
- `SessionsFragment` now reads persisted Room focus sessions through
  `RoomSessionsRepository`; screenshot automation captures
  `10-main-shell-sessions.png` after selecting the Sessions bottom-navigation
  item.
- The launcher shell has been corrected back toward the user-provided XML
  skeleton: `activity_main.xml` uses a compact 72dp `BottomNavigationView`
  directly under the `FragmentContainerView`, and the temporary oversized
  overlay label row has been removed.
- `DashboardFragment` now shows the optional local Room-backed
  `Location context` card with labeled `Latitude`, `Longitude`, `Accuracy`,
  and `Captured` rows when location context is explicitly enabled in test data.
- Dashboard fragment ordering now keeps the period filter directly after the
  summary cards before optional location context, preserving the wireframe
  dashboard flow while still including latitude/longitude evidence.
- Dashboard chart configuration now uses human-readable hour/minute/app labels
  instead of decimal placeholder axes. The latest chart evidence shows `09`,
  `10`, `11` hour labels, `0m` to `60m` minute labels, and app labels such as
  `Chrome`, `YouTube`, and `Slack`.
- Room-backed Dashboard and Sessions rows now separate user-facing app names
  from package names. Rows keep package metadata such as `com.android.chrome`
  while showing `Chrome` as the primary label.
- Android screenshot scrolling now targets nested descendants correctly, so the
  `03-dashboard-charts.png` artifact captures the actual chart section.
- The shared focus-session row layout now uses enough height for app name,
  package name, time range, active/idle state, and duration without clipping.
- `SettingsFragment` is now wired to the same runtime-safe privacy, sync,
  Usage Access, notification permission, and optional location context controls
  as the Settings Activity. The launcher shell Settings tab is no longer a
  generic placeholder include stack.
- Android screenshot automation now captures `11-main-shell-settings.png` after
  selecting the Settings bottom-navigation item, and
  `06-settings-location-permission.png` scrolls to the actual Location context
  card so the optional latitude/longitude controls are visible.

## Current Fragment Shell Gaps

These are intentionally tracked as incomplete because the latest screenshot
evidence still does not match the user-provided XML wireframe skeleton closely
enough:

- The launcher toolbar/header is too large and must be normalized to the
  compact MaterialToolbar shell from the XML skeleton.
- Report fragment still needs to be wired to runtime summary repository/client
  behavior instead of remaining a skeleton-only view.
- Screenshot automation should keep adding one feature screenshot per shell tab
  as each fragment becomes runtime-backed.

## Not Implemented Yet

- Hardware-backed runtime location reader.
- Wiring the new Report fragment to the same runtime behavior as the existing
  Activity screen.
