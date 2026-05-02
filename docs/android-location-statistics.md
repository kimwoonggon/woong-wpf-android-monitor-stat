# Android Location Statistics

Android location context is optional and off by default. It is not part of app
usage tracking unless the user explicitly enables location context and grants
the required Android location permission.

## What Is Stored

The app stores metadata only:

- latitude/longitude when location context is enabled
- coordinate precision used for grouping
- first/last captured timestamps
- duration spent near a rounded coordinate cell
- sample count
- approximate accuracy
- permission/capture mode

It does not store screen contents, typed text, touch coordinates from other
apps, messages, passwords, browser page contents, or screenshots of user
activity.

## Why Statistics May Look Empty

Location statistics are empty when any of these are true:

- location context is disabled in Settings
- Android location permission is not granted
- no location snapshot has been captured yet
- no `location_visits` rows exist inside the selected dashboard period

The dashboard now reads `location_visits` from Room and shows:

- number of location visits
- top location by duration
- a local mini map based on persisted visit points

## Map Rendering

The Android dashboard always has a local fallback map. `LocationMiniMapView`
draws persisted Room visit points as bubbles sized by duration. In fallback
mode it does not request external map tiles, call a map API, or send coordinates
to a map provider.

The dashboard can also render real Google Maps when a Google Maps API key is
provided at build time. The Google path is optional and explicit because map
tiles are fetched from Google services. See `docs/android-google-maps.md`.

## Other Real Map Options

Other map providers remain possible later, but they need an explicit product
decision:

- MapLibre/OSM tiles: more control, but tile server/privacy policy must be
  selected.
- Offline tiles: strongest privacy, but larger app/storage footprint.

Any real map must remain opt-in and must not upload local Room coordinates
unless sync is enabled and the server contract explicitly supports it.
