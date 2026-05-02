# Android Location Map

The Dashboard location preview is a local XML/View drawing, not an external map
tile integration. It renders persisted `location_visits` points on a simple
local grid and road context inside `LocationMiniMapView`.

Current behavior:

- Each map point comes from persisted Room `location_visits`, not transient UI
  state.
- Each point exposes the last captured local time, latitude, longitude,
  duration, and sample count through the view content description.
- If the latest coordinate snapshot is older than the freshness window, the
  dashboard says `Location context stale - last captured ... ago` instead of
  pretending live capture is current.
- The map is intentionally local-only and Google-map-like rather than Google
  Maps-backed. Adding real map tiles later would require an explicit provider,
  key, network policy, and privacy review.

Privacy boundaries:

- Location capture remains off by default.
- Coordinates are stored only after explicit location opt-in, foreground
  permission, and precise-coordinate opt-in.
- The mini map does not call external map providers or download map tiles.
- Screenshots for this feature must capture only Woong Monitor UI.
- Do not collect typed text, clipboard contents, browser/page contents,
  screenshots of other apps, or Android touch coordinates.
