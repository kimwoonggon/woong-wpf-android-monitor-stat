# Production Migration Review

Updated: 2026-05-03

## Generated Migration

- Migration: `20260428131352_InitialCreate`
- Migration: `20260428165251_AddFocusSessionWindowMetadata`
- Migration: `20260428170042_AddDeviceStateAndAppFamilyTables`
- Migration: `20260429101507_AddWebSessionClientSessionId`
- Migration: `20260429102602_AddServerSessionForeignKeys`
- Migration: `20260429163620_AddLocationContextTable`
- Migration: `20260501225456_AddDeviceTokenVerifier`
- Migration: `20260503130736_AddCurrentAppStateTable`
- Context: `MonitorDbContext`
- Provider: Npgsql / PostgreSQL
- Local tool: `dotnet-ef` 10.0.4 in `dotnet-tools.json`

## Tables

- `devices`
- `focus_sessions`
- `web_sessions`
- `raw_events`
- `daily_summaries`
- `device_state_sessions`
- `app_families`
- `app_family_mappings`
- `location_contexts`
- `current_app_states`

## Schema Restoration Migration

`20260428165251_AddFocusSessionWindowMetadata` updates the integrated server
schema for Original Intent Restoration:

- Adds nullable focus-session process/window columns: `ProcessId`,
  `ProcessName`, `ProcessPath`, `WindowHandle`, and `WindowTitle`.
- Changes `web_sessions.Url` and `web_sessions.PageTitle` to nullable so
  domain-only browser privacy mode can sync without storing full URLs or tab
  titles.
- Adds nullable browser capture provenance columns:
  `CaptureMethod`, `CaptureConfidence`, and `IsPrivateOrUnknown`.

`20260428170042_AddDeviceStateAndAppFamilyTables` adds the remaining
relationship tables needed before richer integration:

- `device_state_sessions` stores active/idle/lock/screen state intervals with
  duplicate protection through `(DeviceId, ClientSessionId)`.
- `app_families` stores cross-platform app family labels such as Chrome or VS
  Code.
- `app_family_mappings` maps platform app keys or domains to a family through a
  unique `(MappingType, MatchKey)` index.

`20260429101507_AddWebSessionClientSessionId` aligns web-session upload
idempotency with the PRD:

- Adds required `web_sessions.ClientSessionId`.
- Backfills legacy rows as `legacy-web-session-{Id}` before enforcing
  non-null and unique constraints.
- Replaces the prior nullable-URL-based duplicate key with unique
  `(DeviceId, ClientSessionId)`.

`20260429102602_AddServerSessionForeignKeys` enforces the integrated server
relationships that were previously scalar ids only:

- `focus_sessions`, `web_sessions`, `raw_events`, and
  `device_state_sessions` reference `devices`.
- `web_sessions(DeviceId, FocusSessionId)` references
  `focus_sessions(DeviceId, ClientSessionId)`.
- Foreign keys use `Restrict` delete behavior so device/session history is not
  accidentally cascade-deleted.

`20260429163620_AddLocationContextTable` adds optional location context rows:

- `location_contexts` stores coarse or precise location context only when the
  client explicitly opts in.
- `Latitude`, `Longitude`, and `AccuracyMeters` are nullable so coarse context
  can remain metadata-only.
- `(DeviceId, ClientContextId)` is unique for idempotent sync.
- `DeviceId` references `devices.Id` with restricted delete behavior.

`20260501225456_AddDeviceTokenVerifier` adds the server-side verifier fields
for upload device-token enforcement:

- `devices.DeviceTokenSalt` stores the per-device salt needed to reproduce the
  issued opaque token.
- `devices.DeviceTokenHash` stores the token verifier hash; the plaintext
  `X-Device-Token` value is not persisted.
- Upload endpoints use the verifier fields to reject missing, invalid, or
  mismatched device tokens before accepting client batches.

`20260503130736_AddCurrentAppStateTable` adds a metadata-only current app
state table for local integrated dashboard freshness:

- `current_app_states` stores the latest observed app/window metadata per
  registered device without requiring an ended session or duration.
- `DeviceId` is unique so each device has one latest current state row.
- The table is server/PostgreSQL integrated state only. Clients still persist
  their local metadata in their own local stores and upload through API DTOs.

## Idempotency Indexes

- `devices`: unique `(UserId, Platform, DeviceKey)`
- `focus_sessions`: unique `(DeviceId, ClientSessionId)`
- `raw_events`: unique `(DeviceId, ClientEventId)`
- `web_sessions`: unique `(DeviceId, ClientSessionId)`
- `daily_summaries`: unique `(UserId, SummaryDate, TimezoneId)`
- `device_state_sessions`: unique `(DeviceId, ClientSessionId)`
- `app_families`: unique `(Key)`
- `app_family_mappings`: unique `(MappingType, MatchKey)`
- `location_contexts`: unique `(DeviceId, ClientContextId)`
- `current_app_states`: unique `(DeviceId)`

## Relationship Constraints

- `focus_sessions.DeviceId -> devices.Id`
- `web_sessions.DeviceId -> devices.Id`
- `web_sessions(DeviceId, FocusSessionId) -> focus_sessions(DeviceId, ClientSessionId)`
- `raw_events.DeviceId -> devices.Id`
- `device_state_sessions.DeviceId -> devices.Id`
- `location_contexts.DeviceId -> devices.Id`
- `current_app_states.DeviceId -> devices.Id`

## Production Deployment Path

Production database changes must be applied deliberately by an operator, not as
an incidental side effect of application startup. The ASP.NET Core server should
start against an already-migrated PostgreSQL schema.

Use one of these deployment forms:

1. Preferred release artifact: build a reviewed EF migration bundle.

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build-server-migration-bundle.ps1 `
  -Configuration Release `
  -OutputPath artifacts\server-migrations\woong-server-migrations.exe
```

The helper script wraps `dotnet ef migrations bundle` for
`src\Woong.MonitorStack.Server\Woong.MonitorStack.Server.csproj` and
`MonitorDbContext`. It only builds the bundle; it must never apply migrations or
accept a production connection string.

Use `-Help` to print usage, or `-DryRun` with the intended `-Configuration` and
`-OutputPath` to print the generated `dotnet ef migrations bundle` command
without creating a bundle.

Apply the reviewed bundle with an explicit production connection string:

```powershell
$env:ConnectionStrings__MonitorDb = "<production PostgreSQL connection string>"
artifacts\server-migrations\woong-server-migrations.exe --connection "$env:ConnectionStrings__MonitorDb"
```

2. Manual operations fallback: apply EF migrations from the checked-out release
   tag when a bundle is not available.

```powershell
$env:ConnectionStrings__MonitorDb = "<production PostgreSQL connection string>"
dotnet ef database update `
  --project src\Woong.MonitorStack.Server\Woong.MonitorStack.Server.csproj `
  --startup-project src\Woong.MonitorStack.Server\Woong.MonitorStack.Server.csproj `
  --context MonitorDbContext `
  --configuration Release `
  --connection "$env:ConnectionStrings__MonitorDb"
```

3. New migration authoring, for maintainers only. Do not run this on a
   production host.

```powershell
dotnet ef migrations add <MigrationName> `
  --project src\Woong.MonitorStack.Server\Woong.MonitorStack.Server.csproj `
  --startup-project src\Woong.MonitorStack.Server\Woong.MonitorStack.Server.csproj `
  --context MonitorDbContext `
  --output-dir Data\Migrations
```

## Backup, Reset, And Rollback Expectations

Before every production migration, take and verify a PostgreSQL backup. At
minimum:

```powershell
pg_dump --format=custom --file "backups\woong-monitor-$(Get-Date -Format yyyyMMdd-HHmmss).dump" "$env:ConnectionStrings__MonitorDb"
```

The operator must verify that the dump file exists, is non-empty, and can be
restored in a non-production PostgreSQL instance before continuing with a risky
schema change.

Production reset is not a migration strategy. Do not run `dotnet ef database
drop`, delete PostgreSQL volumes, or truncate production tables to recover from
a failed deployment. If rollback is required, prefer restoring the verified
`pg_dump` backup into a replacement database and repointing the production
connection string after validation. EF down migrations may be reviewed for
development or staging, but production rollback must be treated as a data
recovery operation unless an explicit release runbook says otherwise.

## CI And Manual Validation

Run the normal server build before a migration release:

```powershell
dotnet restore tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj --configfile NuGet.config
dotnet build src\Woong.MonitorStack.Server\Woong.MonitorStack.Server.csproj --no-restore -maxcpucount:1 -v minimal
dotnet test tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj --no-restore -maxcpucount:1 -v minimal
```

Run PostgreSQL-specific validation when Docker/Testcontainers is available:

```powershell
$env:WOONG_MONITOR_RUN_POSTGRES_TESTS=1
powershell -ExecutionPolicy Bypass -File scripts\run-server-postgres-validation.ps1
```

The validation script applies EF Core migrations through Npgsql/PostgreSQL and
checks provider-specific relational behavior. Do not treat EF InMemory or local
SQLite tests as proof that production PostgreSQL migrations are safe.

Before applying to production, also verify the release artifact can start
against a migrated staging database:

```powershell
$env:ConnectionStrings__MonitorDb = "<staging PostgreSQL connection string>"
dotnet run --configuration Release --project src\Woong.MonitorStack.Server\Woong.MonitorStack.Server.csproj
```

The production deployment record should capture:

- release commit/tag;
- migration list and generated bundle path;
- backup file path and restore-verification result;
- command used to apply the migration;
- validation command output;
- operator and timestamp.

## Review Notes

- Persisted instants map to PostgreSQL `timestamp with time zone`.
- Local dates map to PostgreSQL `date`.
- `daily_summaries` is still a derived table and can be rebuilt from source
  sessions.
- The migration intentionally belongs only to the server PostgreSQL store. It
  must not be reused by Windows SQLite or Android Room.
- Windows local SQLite handles its own local `focus_session` schema evolution
  inside `SqliteFocusSessionRepository.Initialize`; Android Room migrations
  remain under the Android project.
- Before production deployment, run the migration against a real PostgreSQL
  test database or PostgreSQL/Testcontainers environment when Docker is
  available.
- Keep production connection strings outside source control. Use environment
  variables such as `ConnectionStrings__MonitorDb` or the deployment platform's
  secret manager.
