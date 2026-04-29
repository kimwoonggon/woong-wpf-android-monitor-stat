# Chrome Extension + Native Messaging

Updated: 2026-04-29

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
- Added `SqliteBrowserRawEventRepository` and `browser_raw_event` local SQLite
  storage for active tab URL/title/domain metadata.
- Added `BrowserWebSessionizer` to convert ordered active-tab messages into
  `web_session` intervals linked to the current Windows focus session.
- Duplicate active-tab events with the same tab/window/url/title/domain/browser
  identity are ignored so they do not create short extra sessions or inflate
  duration.
- Added `ChromeNativeMessageIngestionFlow` to connect native message reading,
  browser raw event persistence, and completed `web_session` persistence.
- Component coverage verifies a Chrome active tab URL is stored in Windows local
  SQLite and that the previous tab becomes a `web_session` when the active tab
  changes.
- Added `NativeMessagingHostManifestGenerator` so the Windows side can generate
  the Chrome native messaging host manifest JSON with the stable host name,
  executable path, stdio type, and allowed extension origins.
- Added `ChromeNativeMessageHostRunner` plus
  `tools/Woong.MonitorStack.ChromeNativeHost`, a console native host executable
  that reads Chrome's native-messaging stdin stream until EOF and feeds messages
  into `ChromeNativeMessageIngestionFlow`.
- Switched the Chrome extension from one-shot `sendNativeMessage` calls to a
  persistent `chrome.runtime.connectNative` port, so ordered active-tab changes
  can share one in-memory web-sessionizer during the host lifetime.
- Added `scripts/install-chrome-native-host.ps1` to publish the native host,
  write a native-messaging manifest, and register
  `com.woong.monitorstack.chrome` under the current user's Chrome
  `NativeMessagingHosts` registry key.
- The native host uses DomainOnly URL storage by default. Full URL paths and
  query strings are not written to the local DB or outbox unless a future
  explicit opt-in changes the storage policy.

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

Remaining work is physical Chrome installation/manual verification with a real
extension id, native host packaging polish, and tighter correlation between
extension tab events and the currently foreground Windows browser
`FocusSession`. The current host stores domain-only web-session metadata with a
stable placeholder focus-session id when a live WPF focus-session id is not
available, while the WPF foreground pipeline still provides the authoritative
app/window focus session.
