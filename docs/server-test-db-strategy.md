# Server Integration Test Database Strategy

Updated: 2026-04-28

Server tests must not rely on EF InMemory when the behavior under test depends
on relational constraints, unique indexes, idempotency, or provider-specific SQL
behavior.

## Strategy

- Preferred production-like path: PostgreSQL with Testcontainers.
- Current environment note: Docker is not available in this workspace, so
  Testcontainers cannot run here yet.
- Current automated fallback: EF Core SQLite in-memory relational database for
  relational constraint and unique-index verification.
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

## Future PostgreSQL/Testcontainers Work

When Docker is available, add a PostgreSQL Testcontainers fixture and run the
same relational/idempotency tests against Npgsql before production database use.
