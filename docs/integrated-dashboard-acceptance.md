# Integrated Dashboard Acceptance

This acceptance flow proves that Windows and Android metadata can travel
through the public server API contracts into PostgreSQL and then render in the
C# Blazor integrated dashboard.

It uses synthetic metadata only. It does not read Windows SQLite, Android Room,
browser UI, Android screens, typed text, clipboard contents, passwords, message
contents, page contents, or screenshots of other apps.

## Command

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\run-integrated-dashboard-acceptance.ps1
```

The script starts Docker PostgreSQL through
`scripts\start-server-postgres.ps1`, runs the ASP.NET Core server on a local
port, registers one synthetic Windows WPF device and one synthetic Android
device, uploads focus/web/location metadata, checks the JSON API, checks the
Blazor page, and captures dashboard screenshots with Playwright.

If Playwright browsers are missing locally, install the Chromium runtime once:

```powershell
npx playwright install chromium
```

## Artifacts

Successful runs write timestamped artifacts under:

```text
artifacts/integrated-dashboard-acceptance/<timestamp>/
```

The latest run is also copied to:

```text
artifacts/integrated-dashboard-acceptance/latest/
```

Expected files:

- `api-dashboard.json`
- `dashboard.html`
- `dashboard-1440.png`
- `dashboard-390.png`
- `manifest.json`
- `report.md`
- `integrated-dashboard-design.svg`
- `server.stdout.log`
- `server.stderr.log`

The design source SVG for Figma import is committed at:

```text
artifacts/blazor-dashboard-design/integrated-dashboard-design.svg
```

## Verified Behavior

- Windows WPF focus sessions upload through `/api/focus-sessions/upload`.
- Windows browser domain sessions upload through `/api/web-sessions/upload`.
- Android UsageStats-style sessions upload through `/api/focus-sessions/upload`.
- Android opted-in coarse location metadata uploads through
  `/api/location-contexts/upload`.
- PostgreSQL-backed `/api/dashboard/integrated` combines Windows and Android
  data by user/date/timezone.
- Blazor `/dashboard` renders Active Focus, Platform Totals, Top Apps, Top
  Domains, Location Samples, and Devices.
- Dashboard HTML is checked for forbidden privacy markers such as device
  tokens, passwords, clipboard, and typed text.

## Dry Run

Use `-DryRun` to inspect planned local actions without starting PostgreSQL,
server processes, API uploads, or Playwright:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\run-integrated-dashboard-acceptance.ps1 -DryRun
```
