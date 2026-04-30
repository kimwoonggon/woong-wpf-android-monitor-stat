# QA External Blockers

Updated: 2026-04-30

This note records environment-bound validation limits. These are not product
failures and should be refreshed as soon as the relevant local capability is
available.

## Docker/Testcontainers

Current evidence:

- `docker --version` succeeds: Docker CLI version `29.2.1`.
- Docker Desktop was started locally and `docker ps` succeeds.
- PostgreSQL/Testcontainers validation passed with artifact
  `artifacts/server-postgres-validation/20260430-190958`.

Remaining validation:

- No Docker/Testcontainers validation blocker remains in the current local
  environment.

Recheck command:

```powershell
docker info
```

Unblock criteria:

- `docker info` returns a running Server section.
- A PostgreSQL/Testcontainers fixture can start and reset reliably.
- Rerun `scripts/run-server-postgres-validation.ps1` after future server DB
  changes.

## Android Physical Device Optional Hardening

Current evidence:

- `adb devices` lists only `emulator-5554`.
- No physical Android device is connected.
- Emulator UI/resource evidence is the current acceptance baseline.

Optional validation:

- Physical-device Android resource measurement for battery, thermal, OEM
  background policy, and real hardware variability.

Recheck command:

```powershell
adb devices -l
```

Future hardening criteria:

- At least one non-`emulator-*` Android device appears as `device`.
- Run physical-device resource measurement and record the new artifact path as
  optional hardening evidence.
- Keep emulator evidence and physical-device evidence separate in reports.
