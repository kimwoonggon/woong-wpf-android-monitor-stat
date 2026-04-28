# Common Domain And API Contract Policy

Updated: 2026-04-28

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

## Upload Idempotency

- Focus session uploads must include `clientSessionId`.
- Raw event uploads must include `clientEventId`.
- Server idempotency will be based on `deviceId + clientSessionId` for sessions
  and `deviceId + clientEventId` for raw events.
- Batch responses use `UploadItemStatus`: `Accepted`, `Duplicate`, or `Error`.

## Web Domains

- MVP domain normalization uses `DomainNormalizer.ExtractRegistrableDomain`.
- It supports regular hostnames and a small built-in set of common two-part
  suffixes such as `co.kr` and `co.uk`.
- A full public suffix list dependency can be evaluated later if Phase 2 web
  tracking needs broader global domain precision.
