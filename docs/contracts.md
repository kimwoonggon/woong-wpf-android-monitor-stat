# Common Domain And API Contract Policy

Updated: 2026-04-29

## Time And Date

- Persist instants as UTC `DateTimeOffset` values.
- Convert to local dates with the device/user timezone only at domain or
  presentation boundaries that explicitly need `DateOnly`.
- `FocusSession.LocalDate` is derived from session start time and `timezoneId`.
- `DailySummary` is grouped by local date and must exclude idle sessions from
  active totals.
- Web session daily totals are grouped by the requested timezone because
  `WebSession` stores UTC timestamps and domain details, not a local date field.

## Device And Platform

- `Platform` is limited to `Windows` and `Android` for MVP.
- `Device.DeviceKey` and `RegisterDeviceRequest.DeviceKey` are stable client
  identifiers and must not be empty.
- Windows and Android local stores keep only their own device data.
- Cross-device integration happens only through the server API and PostgreSQL.
- Android UsageStats app intervals sync through the same `FocusSession`
  contract as Windows foreground app intervals. See
  `docs/android-app-usage-contract-decision.md`.

## Upload Idempotency

- Focus session uploads must include `clientSessionId`.
- Focus session uploads may include Windows metadata fields:
  `processId`, `processName`, `processPath`, `windowHandle`, and `windowTitle`.
  These fields are nullable so Android app-usage sessions and privacy-masked
  Windows sessions can use the same endpoint.
- Web session uploads must include `clientSessionId`. Browser clients should
  generate it from the local web-session/outbox aggregate id so retry uploads
  remain idempotent even when `Url` is null in domain-only privacy mode.
- Raw event uploads must include `clientEventId`.
- Current upload endpoints are `POST /api/focus-sessions/upload`,
  `POST /api/web-sessions/upload`, and `POST /api/raw-events/upload`.
- Server idempotency will be based on `deviceId + clientSessionId` for sessions
  and `deviceId + clientEventId` for raw events.
- Batch responses use `UploadItemStatus`: `Accepted`, `Duplicate`, or `Error`.

Window titles can expose sensitive context. Windows should send `windowTitle`
only when the user's privacy setting allows it; process id/name/path and HWND
metadata are the safe default metadata for process/window duration tracking.
Android clients should leave Windows-specific process/window metadata fields
null and use `source = android_usage_stats`.

Android sync is opt-in at the client boundary. The persisted Android sync
setting defaults to disabled, and `AndroidSyncWorker` must return a skipped
success without invoking upload when sync is disabled. This keeps local
UsageStats collection and local Room storage separate from server upload
consent.

## Daily Summary Query

- Current daily summary endpoint is
  `GET /api/daily-summaries/{summaryDate}?userId={userId}&timezoneId={timezoneId}`.
- Current date-range statistics endpoint is
  `GET /api/statistics/range?userId={userId}&from={fromDate}&to={toDate}&timezoneId={timezoneId}`.
- `summaryDate` is an ISO local date in the requested/user timezone.
- `fromDate` and `toDate` are inclusive ISO local dates in the requested/user
  timezone.
- The server combines all devices registered to `userId`.
- `totalActiveMs` excludes idle focus sessions; `totalIdleMs` reports idle
  focus sessions separately.
- `totalWebMs` and `topDomains` are grouped by converting web session UTC start
  timestamps into `timezoneId`.

## Web Domains

- MVP domain normalization uses `DomainNormalizer.ExtractRegistrableDomain`.
- It supports regular hostnames and a small built-in set of common two-part
  suffixes such as `co.kr` and `co.uk`.
- A full public suffix list dependency can be evaluated later if Phase 2 web
  tracking needs broader global domain precision.
- `WebSession.Url` and `WebSession.PageTitle` are nullable because domain-only
  browser privacy mode must allow site-duration measurement without storing the
  full URL or tab title. `WebSession.Domain` remains required for any persisted
  web session.
- Browser capture provenance is optional on the shared `WebSession` contract:
  `CaptureMethod`, `CaptureConfidence`, and `IsPrivateOrUnknown` are carried
  when the client knows them, but older/imported sessions may leave them null.
