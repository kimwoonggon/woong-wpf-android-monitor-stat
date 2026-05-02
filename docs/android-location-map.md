# Android Location Map

The Dashboard location card supports two map providers:

1. Local fallback preview through `LocationMiniMapView`.
2. Optional Google Maps SDK rendering when a build-time API key is provided.

The default remains local-only. The app does not create a Google `MapView` when
`BuildConfig.GOOGLE_MAPS_API_KEY` is blank.

Current behavior:

- Each map point comes from persisted Room `location_visits`, not transient UI
  state.
- Each point exposes the last captured local time, latitude, longitude,
  duration, and sample count through the view content description.
- If the latest coordinate snapshot is older than the freshness window, the
  dashboard says `Location context stale - last captured ... ago` instead of
  pretending live capture is current.
- Without a Google Maps API key, the local preview draws a no-network
  Google-map-like road/block/grid context.
- With a Google Maps API key, the dashboard renders markers and a route line on
  a real Google map from persisted Room location visits.

Privacy boundaries:

- Location capture remains off by default.
- Coordinates are stored only after explicit location opt-in, foreground
  permission, and precise-coordinate opt-in.
- The local mini map does not call external map providers or download map tiles.
- The Google Maps path is explicit because map tiles require network access and
  provider/API-key setup.
- Screenshots for this feature must capture only Woong Monitor UI.
- Do not collect typed text, clipboard contents, browser/page contents,
  screenshots of other apps, or Android touch coordinates.

Configuration details are in `docs/android-google-maps.md`.
