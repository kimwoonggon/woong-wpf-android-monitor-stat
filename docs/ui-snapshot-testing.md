# Local WPF UI Snapshot Testing

This project includes a local-only WPF UI snapshot tool for beginner-friendly
visual verification. It launches the WPF app, interacts with stable controls,
saves screenshots, and writes a small Markdown report.

The first version is intentionally not a CI gate. WPF UI automation depends on
the active Windows desktop, monitor layout, DPI, focus, rendering timing, and
installed desktop dependencies. Unit and component tests remain the reliable
quality gate; UI snapshots are a local visual aid.

Privacy boundary tests verify that this tool captures only the launched Woong
Monitor WPF window or elements inside it. It must not use desktop-wide,
arbitrary screen, or other-app capture as telemetry.

The TrackingPipeline mode may reference `Microsoft.Data.Sqlite` to inspect the
temporary SQLite database created for local acceptance evidence. This is
tooling-only: it is used to count local `focus_session`, `web_session`, and
`sync_outbox` rows in generated artifacts, and must not introduce SQLite as a
dependency of `Windows.App` or `Windows.Presentation`.

## Why AutomationId Matters

The tool uses stable `AutomationProperties.AutomationId` values instead of only
visible text. Visible labels can change during copy edits or localization, while
AutomationIds are intended as durable automation selectors.

Current important IDs include:

- `MainWindow`
- `RefreshButton`
- `PeriodSelector`
- `SummaryCardsContainer`
- `ChartArea`
- `RecentAppSessionsList`
- `WebSessionsTab`
- `RecentWebSessionsList`
- `LiveEventsTab`
- `LiveEventsList`
- `SettingsTab`

## How To Run

From the repository root on Windows:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-ui-snapshots.ps1
```

The script builds the WPF app and the snapshot tool, then runs the tool.
By default it also requests the local viewport matrix `1920,1366,1024`.

You can pass an explicit app executable if needed:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-ui-snapshots.ps1 -AppPath "D:\path\to\Woong.MonitorStack.Windows.App.exe"
```

## Output

Screenshots are saved under:

```text
artifacts/ui-snapshots/<timestamp>/
artifacts/ui-snapshots/latest/
```

Stable primary names:

- `01-startup.png`
- `02-dashboard-after-refresh.png`
- `03-dashboard-period-change.png`
- `04-settings.png`
- `report.md`

Optional region crops may also be present:

- `summary-cards.png`
- `chart-area.png`
- `recent-sessions.png`
- `recent-web-sessions.png`
- `live-events.png`

Viewport matrix screenshots are also produced when `--viewport-widths` is used
by the tool or by the scripts:

- `viewport-1920-dashboard.png`
- `viewport-1366-dashboard.png`
- `viewport-1024-dashboard.png`
- `viewport-<width>-summary-cards.png`
- `viewport-<width>-chart-area.png`
- `viewport-<width>-recent-sessions.png`
- `viewport-<width>-recent-web-sessions.png`
- `viewport-<width>-live-events.png`

Some WPF/LiveCharts regions may not expose a reliable UI Automation element in
every environment. In that case the tool records a skipped screenshot reason in
`report.md` and `manifest.json`, then keeps the primary full-window screenshots.

Open `artifacts/ui-snapshots/latest/report.md` first. It links the screenshots
and includes pass/fail notes from the run. In TrackingPipeline mode, the report
and manifest also include SQLite evidence proving that the fake activity flow
created focus sessions, web sessions, and outbox rows in the temp DB.

## What The Tool Does

1. Locates or accepts the WPF app executable path.
2. Launches the app with FlaUI.
3. Waits for `MainWindow`.
4. Moves the window to a stable screen position when possible.
5. Applies requested viewport widths when provided.
6. Captures startup.
7. Clicks Refresh when available.
8. Captures after refresh.
9. Selects the 6-hour period when available.
10. Captures the changed dashboard.
11. Brings known sections into view before optional crop captures.
12. Selects Live Event Log and captures an optional crop.
13. Selects Settings.
14. Captures Settings.
15. Closes the app cleanly.

## Not Implemented Yet

- Strict visual regression gate.
- CI execution.
- Installer-based test.
- Multi-DPI, multi-theme, or multi-monitor matrix.
- Mandatory pixel comparison.
- Baseline/diff images with mismatch percentage.
- Automated human/GPT visual review upload. The generated prompt remains
  local-only unless a user explicitly reviews the artifacts.

Those can be added later once the local snapshot flow is stable and useful.
