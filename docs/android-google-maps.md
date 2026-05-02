# Android Google Maps Option

The Android dashboard can now use Google Maps SDK for the location context card
when an operator provides a Google Maps API key at build time.

This is optional. If no key is configured, the app keeps using the local
`LocationMiniMapView` fallback so debug builds and emulator QA still run without
Google Cloud setup.

## Why It Is Optional

Google Maps tiles are loaded from Google services. That means enabled map
rendering can disclose approximate viewed coordinates to the map provider as
part of normal map-tile/network behavior. For this project, location context is
already opt-in, and external map rendering must also stay explicit.

The app still does not collect typed text, passwords, messages, page contents,
clipboard contents, screenshots of other apps, or Android global touch
coordinates.

## Configure A Key

Do not commit API keys.

Use either a Gradle property:

```powershell
cd D:\woong-monitor-stack\android
.\gradlew.bat :app:assembleDebug -PwoongGoogleMapsApiKey=YOUR_ANDROID_MAPS_KEY
```

Or an environment variable:

```powershell
$env:WOONG_ANDROID_GOOGLE_MAPS_API_KEY='YOUR_ANDROID_MAPS_KEY'
cd D:\woong-monitor-stack\android
.\gradlew.bat :app:assembleDebug
Remove-Item Env:\WOONG_ANDROID_GOOGLE_MAPS_API_KEY
```

The build wires the key into:

- `BuildConfig.GOOGLE_MAPS_API_KEY`
- Android manifest placeholder `${googleMapsApiKey}`
- manifest metadata `com.google.android.geo.API_KEY`

When the key is blank, `DashboardLocationMapController` does not create a
`MapView`; it shows the local map preview instead.

## Runtime Behavior

- Google Maps enabled: the Dashboard location card renders Google map markers
  and a route line from persisted Room `location_visits`.
- Google Maps not configured: the Dashboard location card renders the local
  no-network preview with roads, blocks, grid, timestamp labels, and visit
  bubbles.
- Location rows remain Room-backed metadata. The UI does not use screenshots or
  invasive accessibility capture.

## Emulator Requirement

Use an emulator image with Google APIs or Google Play services when validating
the real Google map path. The local fallback works on ordinary emulator images.

## References

- Google Maps SDK Android setup:
  `https://developers.google.com/maps/documentation/android-sdk/config`
- Google Maps SDK API key setup:
  `https://developers.google.com/maps/documentation/android-sdk/get-api-key`
