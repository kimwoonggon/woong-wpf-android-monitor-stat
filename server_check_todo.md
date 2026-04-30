# Server Validation Checklist

Updated: 2026-04-30

This checklist tracks the server/API validation surface for the Windows +
Android integrated PostgreSQL roadmap. It is server-owned and should stay in
sync with `docs/prd.md`, `docs/contracts.md`, `docs/runtime-pipeline.md`, and
`docs/server-test-db-strategy.md`.

Status legend:

- `[x]` Covered by existing code/tests/docs.
- `[ ]` Still needs a TDD slice.
- `[blocked]` Waiting for PostgreSQL/Testcontainers or production-like DB access.

## Device Registration

- [x] Registering the same `userId + platform + deviceKey` returns the same
  device id and marks the retry as not new.
  Evidence: `DeviceRegistrationApiTests.RegisterDevice_ReturnsStableDeviceIdForSameDeviceKey`.
- [x] Device registration persists platform, device key/name, timezone, and
  timestamps.
  Evidence: `DeviceRegistrationApiTests.RegisterDevice_PersistsDeviceRow`.
- [x] Relational unique index rejects duplicate device identity rows.
  Evidence: `RelationalMonitorDbContextTests.DeviceUniqueIndex_IsEnforcedByRelationalProvider`.
- [x] Duplicate registration updates device name/timezone and `LastSeenAtUtc`
  without creating a second device row.
  Evidence: `DeviceRegistrationApiTests.RegisterDevice_WhenDeviceKeyAlreadyExists_UpdatesNameTimezoneAndLastSeen`.

## Focus Session Upload

- [x] Focus uploads persist a new session and mark retry uploads as duplicate.
  Evidence: `FocusSessionUploadApiTests.UploadFocusSessions_PersistsNewSessionAndMarksDuplicateRetry`.
- [x] Unregistered focus-session device returns per-item `Error` and persists
  zero rows under a relational provider.
  Evidence: `FocusSessionUploadApiRelationalTests.UploadFocusSessions_WhenDeviceIsNotRegistered_ReturnsControlledErrorAndDoesNotPersistRows`.
- [x] Focus session `(DeviceId, ClientSessionId)` unique index is modeled and
  exercised through relational FK/idempotency coverage.
  Evidence: `ServerDbContextModelTests`, `RelationalMonitorDbContextTests`.
- [x] Mixed-batch focus upload returns independent statuses for existing
  duplicate, accepted new item, and intra-batch duplicate while persisting only
  accepted rows.
  Evidence: `FocusSessionUploadApiRelationalTests.UploadFocusSessions_WhenBatchContainsExistingAndIntraBatchDuplicate_ReturnsIndependentStatuses`.

## Web Session Upload

- [x] Web uploads persist a new session and mark retry uploads as duplicate.
  Evidence: `WebSessionUploadApiTests.UploadWebSessions_PersistsNewSessionAndMarksDuplicateRetry`.
- [x] Domain-only web uploads allow null `Url` and `PageTitle` while preserving
  capture metadata.
  Evidence: `WebSessionUploadApiTests.UploadWebSessions_WhenUrlIsNull_PreservesCaptureMetadataAndMarksDuplicateRetry`.
- [x] Unregistered device returns controlled per-item `Error`.
  Evidence: `WebSessionUploadApiRelationalTests.UploadWebSessions_WhenDeviceIsNotRegistered_ReturnsControlledErrorAndDoesNotPersistRows`.
- [x] Missing focus-session parent returns controlled per-item `Error`.
  Evidence: `WebSessionUploadApiRelationalTests.UploadWebSessions_WhenFocusSessionIsMissing_ReturnsControlledErrorAndDoesNotPersistRows`.
- [x] Relational schema links `web_sessions(DeviceId, FocusSessionId)` to
  `focus_sessions(DeviceId, ClientSessionId)`.
  Evidence: `ServerDbContextModelTests.WebSessionEntity_HasCompositeForeignKeyToFocusSessionClientSession`.
- [x] Mixed-batch web upload returns independent statuses for existing
  duplicate, accepted new item, intra-batch duplicate, and missing focus parent
  while persisting only accepted rows.
  Evidence: `WebSessionUploadApiRelationalTests.UploadWebSessions_WhenBatchContainsDuplicateAcceptedIntraBatchDuplicateAndMissingFocusParent_ReturnsIndependentStatuses`.

## Raw Event Upload

- [x] Raw event uploads persist a new event and mark retry uploads as duplicate
  when the device is registered.
  Evidence: `RawEventUploadApiTests.UploadRawEvents_PersistsNewEventAndMarksDuplicateRetry`.
- [x] Unknown raw-event device returns controlled per-item `Error` and persists
  zero rows under a relational provider.
  Evidence: `RawEventUploadApiTests.UploadRawEvents_WhenDeviceIsNotRegistered_ReturnsControlledErrorAndDoesNotPersistRows`.
- [x] Relational FK requires raw events to belong to a registered device.
  Evidence: `ServerDbContextModelTests.ServerSessionEntities_HaveRequiredDeviceForeignKeys`.
- [x] Mixed-batch raw event upload returns independent statuses for existing
  duplicate, accepted new event, and intra-batch duplicate while persisting only
  accepted rows.
  Evidence: `RawEventUploadApiTests.UploadRawEvents_WhenBatchContainsExistingAndIntraBatchDuplicate_ReturnsIndependentStatuses`.
- [ ] Add raw-event payload privacy guard coverage if raw events become more
  than metadata/debug diagnostics.

## Location Context Upload

- [x] Location upload persists nullable coordinates and marks retry uploads as
  duplicate.
  Evidence: `LocationContextUploadApiTests.UploadLocationContexts_PersistsNullableCoordinatesAndMarksDuplicate`.
- [x] Location upload accepts unavailable coordinates as null values.
  Evidence: `LocationContextUploadApiTests.UploadLocationContexts_AllowsApproximateOrUnavailableCoordinates`.
- [x] Relational unique index and device FK are enforced.
  Evidence: `RelationalMonitorDbContextTests.LocationContextClientContextUniqueIndex_IsEnforcedByRelationalProvider`,
  `RelationalMonitorDbContextTests.LocationContextForeignKey_IsEnforcedByRelationalProvider`.
- [x] Unregistered location device returns per-item `Error` and persists zero
  rows under a relational provider.
  Evidence: `LocationContextUploadApiTests.UploadLocationContexts_WhenDeviceIsNotRegistered_ReturnsControlledErrorAndDoesNotPersistRows`.
- [x] Mixed-batch location upload returns independent statuses for existing
  duplicate, accepted new context, and intra-batch duplicate while persisting
  only accepted rows.
  Evidence: `LocationContextUploadApiTests.UploadLocationContexts_WhenBatchContainsExistingAndIntraBatchDuplicate_ReturnsIndependentStatuses`.

## Idempotency

- [x] Device idempotency: `userId + platform + deviceKey`.
- [x] Focus idempotency: `deviceId + clientSessionId`.
- [x] Web idempotency: `deviceId + clientSessionId`.
- [x] Raw event idempotency: `deviceId + clientEventId`.
- [x] Location context idempotency: `deviceId + clientContextId`.
- [ ] Add concurrency-oriented idempotency tests once the PostgreSQL fixture is
  available.

## Daily Summary And Date Range

- [x] Daily summary combines Windows and Android devices for the same user.
  Evidence: `DailySummaryApiTests.GetDailySummary_CombinesUserDevicesAndExcludesIdleFromActiveTotal`.
- [x] Daily summary excludes idle from active totals and reports idle
  separately.
  Evidence: `DailySummaryAggregationServiceTests.GenerateAsync_PersistsIntegratedDailySummaryForUserTimezone`.
- [x] Daily summary groups known Windows/Android app keys into shared app
  families.
  Evidence: `DailySummaryAggregationServiceTests.GenerateAsync_GroupsKnownPlatformAppsIntoSharedAppFamily`.
- [x] Daily summary includes domain totals and top domains.
  Evidence: `DailySummaryRuntimeFlowTests.DailySummaryApi_WhenWindowsAndAndroidClientsUploadSessions_ReturnsIntegratedSummary`.
- [x] Date range statistics combines inclusive local dates.
  Evidence: `DateRangeStatisticsApiTests.GetDateRangeStatistics_CombinesUserDevicesWithinInclusiveLocalDateRange`.
- [x] Running daily summary generation twice updates a single summary row
  without inflating totals.
  Evidence: `DailySummaryAggregationServiceTests.GenerateAsync_WhenRunTwice_UpdatesSingleSummaryWithoutInflatingTotals`.
- [ ] Add HTTP-level invalid input coverage for malformed dates, missing
  query-string values, invalid timezone ids, and `from > to`.

## Cross-Midnight Split

- [x] Focus sessions near UTC midnight are grouped by requested user timezone
  rather than stored client local date alone.
  Evidence: `DailySummaryAggregationServiceTests.GenerateAsync_UsesRequestedTimezoneWhenGroupingFocusSessionsAcrossUtcMidnight`.
- [x] Focus active, focus idle, and web sessions spanning requested local
  midnight are split across adjacent daily summaries.
  Evidence: `DailySummaryAggregationServiceTests.GenerateAsync_SplitsFocusIdleAndWebSessionsAcrossRequestedLocalMidnight`.
- [ ] Add equivalent split behavior for date-range statistics if range views
  must allocate only the in-range portion of sessions instead of including the
  whole session by start date.

## Database Provider Strategy

- [x] EF InMemory is documented as unsuitable for relational constraints,
  idempotency indexes, and provider-specific SQL.
  Evidence: `docs/server-test-db-strategy.md`.
- [x] Current fallback uses in-memory SQLite relational tests for FK and unique
  index behavior.
  Evidence: `RelationalServerFactory`, `RelationalTestDatabase`.
- [x] Npgsql/PostgreSQL model metadata and production migration files exist.
  Evidence: `ServerDbContextModelTests`, `ProductionMigrationFilesTests`.
- [blocked] Apply migrations to a real PostgreSQL database with
  Testcontainers and rerun device/focus/web/raw/location idempotency and FK
  tests against Npgsql.
- [blocked] Verify PostgreSQL migration SQL for legacy web-session
  `ClientSessionId` backfill.
- [blocked] Add concurrent duplicate upload tests against PostgreSQL to prove
  race-safe idempotency.

## Focused Server Commands

```powershell
dotnet test tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj --no-restore -maxcpucount:1 -v minimal
dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal
dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal
```
