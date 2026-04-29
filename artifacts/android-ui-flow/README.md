# Android UI Flow Figma Import

This folder contains a Figma-importable SVG board for the Woong Monitor Stack Android UI flow.

## Files

- `woong-monitor-android-ui-flow.figma-import.svg`

## Import

Open a Figma design file, then drag the SVG onto the canvas. The board mirrors the planning image with seven Android XML/View screens:

1. Splash
2. Permission onboarding
3. Dashboard
4. Sessions
5. App detail
6. Report
7. Settings

The artifact is design/planning material only. Implementation must remain Android XML/View based and must respect the privacy boundary: collect app/package/time metadata only, never typed text, screen content, passwords, clipboard content, screenshots, or touch coordinates.

Optional latitude/longitude location context is included in the plan as
sensitive metadata. It is off by default and requires Android location
permission plus explicit in-app opt-in before any coordinate is shown, stored,
or synchronized.
