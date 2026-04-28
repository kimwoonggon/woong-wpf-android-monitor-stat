# Future macOS Feasibility

Updated: 2026-04-29

macOS is future research only. It is not part of the current implementation
slice and should not be implemented until the PRD explicitly adds it.

## Research Areas

- `NSWorkspace` active application notifications.
- Accessibility API requirements and user consent flow.
- Screen Time API limitations and availability.
- Browser URL capture limitations by browser.
- Menu/window-title capture reliability.
- Local storage strategy and sync DTO reuse.
- Permission and privacy copy required by macOS.

## Likely Constraints

- Accessibility permissions are user-visible and must be explicit.
- Browser URL capture may require extension-based approaches.
- Page contents, typed text, messages, forms, screenshots, and clipboard data
  remain forbidden by project policy.
- macOS implementation must remain metadata-only: app/window/site duration.

## Deferred Decision

Do not add macOS projects, package targets, collectors, or UI code in the
current Windows + Android MVP. Keep this document as research notes only.

