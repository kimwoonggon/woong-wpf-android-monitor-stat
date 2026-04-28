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
- `live-events.png`

Some WPF/LiveCharts regions may not expose a reliable UI Automation element in
every environment. In that case the tool records a note in `report.md` and keeps
the primary full-window screenshots.

Open `artifacts/ui-snapshots/latest/report.md` first. It links the screenshots
and includes pass/fail notes from the run.

## What The Tool Does

1. Locates or accepts the WPF app executable path.
2. Launches the app with FlaUI.
3. Waits for `MainWindow`.
4. Moves the window to a stable screen position when possible.
5. Captures startup.
6. Clicks Refresh when available.
7. Captures after refresh.
8. Selects the 6-hour period when available.
9. Captures the changed dashboard.
10. Selects Live Event Log and captures an optional crop.
11. Selects Settings.
12. Captures Settings.
13. Closes the app cleanly.

## Not Implemented Yet

- Strict visual regression gate.
- CI execution.
- Installer-based test.
- Multi-DPI, multi-theme, or multi-monitor matrix.
- Mandatory pixel comparison.
- Baseline/diff images with mismatch percentage.
- More reliable chart-area crop support if LiveCharts UI Automation exposure is
  inconsistent on a local machine.

Those can be added later once the local snapshot flow is stable and useful.
