# WPF Design Reference

Updated: 2026-05-02

This page preserves the WPF dashboard design references that were previously
only present as loose local files. They are design and implementation handoff
assets only. They are not product telemetry, not user activity screenshots, and
must not be treated as evidence captured from a monitored user's machine.

## Assets

- Reference image: `docs/assets/wpf/wpf-design-reference.png`
- Figma-importable SVG board:
  `docs/assets/wpf/woong-monitor-wpf-elements.figma-import.svg`

The SVG board is intended for design review or Figma import. The app
implementation remains WPF MVVM in `src/Woong.MonitorStack.Windows.App` and
`src/Woong.MonitorStack.Windows.Presentation`.

## Design Intent

The WPF UI should read as a desktop productivity dashboard:

- Header status bar for tracking, sync, privacy, and runtime health.
- Control bar for explicit user actions such as Start, Stop, Refresh, and Sync
  Now.
- Current Focus panel for the foreground app/window/domain metadata and current
  duration.
- Summary cards for today, foreground time, idle time, and sync/local state.
- Chart area for hourly activity, app focus, and domain focus.
- Details tabs for app sessions, web sessions, and live events.

The original loose `wpfelements.md` reference repeated this shell/component
structure but was encoded poorly. This clean guide replaces it for future
agents; use `docs/wpf-ui-plan.md` for the detailed implementation plan and
`docs/wpf-ui-acceptance-checklist.md` for behavior and screenshot validation.

## Privacy Boundary

WPF design references may show mock UI, placeholder rows, and intended layout.
They must never imply permission to capture private screen contents, typed text,
clipboard contents, messages, passwords, browser page contents, or periodic user
activity screenshots. Runtime WPF evidence must prove metadata-only behavior:
app/window/site timing, local SQLite persistence, visible tracking controls, and
explicit opt-in sync.
