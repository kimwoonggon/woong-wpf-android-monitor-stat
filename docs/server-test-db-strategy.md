# Server Integration Test Database Strategy

Updated: 2026-04-30

Server tests must not rely on EF InMemory when the behavior under test depends
on relational constraints, unique indexes, idempotency, or provider-specific SQL
behavior.

## Strategy

- Preferred production-like path: PostgreSQL with Testcontainers.
- Current production-like validation: `scripts/run-server-postgres-validation.ps1`
  enables `WOONG_MONITOR_RUN_POSTGRES_TESTS=1` for that process and runs the
  PostgreSQL-specific test class against a Testcontainers PostgreSQL instance.
- Standard `dotnet test` keeps PostgreSQL tests skipped unless the explicit
  environment flag is set, so routine unit/integration runs do not fail when
  Docker Desktop is stopped.
- Current automated fallback: EF Core SQLite in-memory relational database for
  fast relational constraint and unique-index verification.
- EF InMemory remains acceptable only for HTTP route smoke tests where
  relational constraints are not the behavior under test.

## Reset

`RelationalTestDatabase` owns a single in-memory SQLite connection, creates the
real EF relational schema with `EnsureCreatedAsync`, and resets with
`EnsureDeletedAsync` + `EnsureCreatedAsync` plus change-tracker clearing.

## Coverage

- `DeviceUniqueIndex_IsEnforcedByRelationalProvider` proves the
  `(UserId, Platform, DeviceKey)` unique index rejects duplicates through a
  relational provider.
- `ResetAsync_RecreatesEmptyRelationalSchema` proves the reset hook clears rows
  and recreates an empty relational schema.

## PostgreSQL/Testcontainers Work

When Docker is available, run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-server-postgres-validation.ps1
```

The validation applies EF Core migrations through Npgsql, verifies the legacy
`web_sessions.ClientSessionId` backfill path before the unique index becomes
required, checks PostgreSQL relational constraints, and verifies concurrent
duplicate focus/web/raw/location uploads return idempotent statuses.

Current availability check:

- `tests/Woong.MonitorStack.Server.Tests` references
  `Testcontainers.PostgreSql` and includes the `PostgresFactAttribute` gate.
- Routine `dotnet test` skips PostgreSQL tests unless
  `WOONG_MONITOR_RUN_POSTGRES_TESTS=1` is set, so Docker capacity remains an
  explicit validation choice.
- User-auth response-policy tests are HTTP contract tests and do not claim
  PostgreSQL-specific relational behavior. Any future relationship, unique
  index, migration, or idempotency proof must use SQLite relational tests at
  minimum and PostgreSQL/Testcontainers for production-readiness evidence.

Latest local artifact:

- `artifacts/server-postgres-validation/20260430-190958`
