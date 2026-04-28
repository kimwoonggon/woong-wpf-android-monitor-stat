# Chrome Extension + Native Messaging

Updated: 2026-04-28

Milestone 4.5 tracks Windows Chrome active-tab URL/title/domain collection
through an explicit Chrome extension and Windows native messaging host.

## Privacy Boundaries

- Collect only active tab URL, title, domain, tab/window ids, and timestamps.
- Do not collect passwords, messages, form inputs, typed text, or page content.
- Do not run as hidden surveillance; the extension must be explicitly installed
  and Chrome permissions must be visible to the user.
- Store events only in the Windows local SQLite path before optional sync.

## Current Slice

- Added `ChromeTabChangedMessage` as the Windows-side native-message DTO seam.
- The DTO normalizes the extension URL payload into a registrable domain through
  the shared `DomainNormalizer`.
- TDD coverage proves `https://www.youtube.com/watch?v=abc` becomes
  `youtube.com`.

## Next Slice

Create the Chrome extension project and manifest, then add the native messaging
host contract from the extension side.
