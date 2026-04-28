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
- Added `extensions/chrome/manifest.json` as an MV3 Chrome extension manifest.
- The manifest declares `tabs`, `webNavigation`, and `nativeMessaging`
  permissions, with HTTP/HTTPS host permissions visible at install time.
- Added `background.js` listeners for active tab activation and URL/title
  updates. The listener sends only URL, title, tab/window ids, browser family,
  and timestamp to the native host.
- Added `NativeMessagingHostRegistration` and a testable registry writer
  abstraction for the Chrome HKCU native messaging host key.
- Added `WindowsRegistryWriter` as the Windows-only implementation. Tests use a
  fake writer and do not mutate the developer machine registry.
- Added `ChromeNativeMessageParser` for the extension `activeTabChanged` JSON
  contract. The parsed Windows DTO includes browser family, tab/window ids, URL,
  title, registrable domain, and UTC observation timestamp.
- Added `ChromeNativeMessageReceiver` for Chrome's native messaging protocol:
  4-byte little-endian length prefix followed by a UTF-8 JSON payload.

## Native Message Contract

```json
{
  "type": "activeTabChanged",
  "browserFamily": "Chrome",
  "windowId": 7,
  "tabId": 42,
  "url": "https://example.com/page",
  "title": "Example",
  "observedAtUtc": "2026-04-28T01:02:03Z"
}
```

## Next Slice

Begin converting active tab messages into browser raw events and `web_session`
rows.
