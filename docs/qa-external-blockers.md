# QA External Blockers

Updated: 2026-04-30

This note records environment-bound blockers only. These are not product
failures and should be reversed as soon as the required local capability is
available.

## Docker/Testcontainers

Current evidence:

- `docker --version` succeeds: Docker CLI version `29.2.1`.
- `docker info` fails because the Docker Desktop Linux engine pipe is not
  available:
  `npipe:////./pipe/dockerDesktopLinuxEngine`.

Blocked validation:

- PostgreSQL/Testcontainers migration application.
- PostgreSQL-backed FK, unique-index, idempotency, and concurrency validation.
- Legacy web-session `ClientSessionId` backfill verification against real
  PostgreSQL.

Recheck command:

```powershell
docker info
```

Unblock criteria:

- `docker info` returns a running Server section.
- A PostgreSQL/Testcontainers fixture can start and reset reliably.
- After that, rerun the PostgreSQL-backed server validation slices before
  marking the related `[blocked]` items complete.

## Android Physical Device

Current evidence:

- `adb devices` lists only `emulator-5554`.
- No physical Android device is connected.
- Emulator UI/resource evidence remains valid emulator evidence, but it must
  not close the physical-device resource measurement item.

Blocked validation:

- Physical-device Android resource measurement.

Recheck command:

```powershell
adb devices -l
```

Unblock criteria:

- At least one non-`emulator-*` Android device appears as `device`.
- Run physical-device resource measurement and record the new artifact path.
- Keep emulator evidence and physical-device evidence separate in checklists.
