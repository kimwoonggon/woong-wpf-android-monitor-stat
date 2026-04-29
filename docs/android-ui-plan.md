# Android UI Plan

Updated: 2026-04-29

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

- Add optional DTO fields only after the local privacy UX and tests exist.
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

## Not Implemented Yet

- Runtime location collector/provider.
- Dashboard location card.
- Server location DTO/storage.
- Connected-device screenshot evidence for location UI.
