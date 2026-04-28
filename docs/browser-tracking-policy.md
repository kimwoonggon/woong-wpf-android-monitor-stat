# Browser Tracking Policy

Updated: 2026-04-29

Browser tracking distinguishes generic browser app usage from website/domain
usage.

## Supported Browsers

Initial supported process names:

- `chrome.exe`
- `msedge.exe`
- `firefox.exe`
- `brave.exe`

Generic browser focus sessions are always valid app usage sessions. Web
sessions are created only when URL/domain information is available under the
configured privacy policy.

## Required Model

`BrowserActivitySnapshot`:

- `CapturedAtUtc`
- `BrowserName`
- `ProcessName`
- `ProcessId`
- `WindowHandle`
- `WindowTitle`
- `TabTitle`
- `Url`
- `Domain`
- `CaptureMethod`
- `CaptureConfidence`
- `IsPrivateOrUnknown`

`CaptureMethod` values:

- `None`
- `WindowTitleOnly`
- `UIAutomationAddressBar`
- `BrowserExtensionFuture`
- `FakeTestData`

`CaptureConfidence` values:

- `Unknown`
- `Low`
- `Medium`
- `High`

Required interfaces:

- `IBrowserProcessClassifier`
- `IBrowserActivityReader`
- `IBrowserUrlSanitizer`
- `IWebSessionizer`

## Privacy Settings

Full URL storage is opt-in.

Recommended levels:

- Off: no browser URL/domain capture; only focus sessions are stored.
- Domain only: store normalized domain, not full URL.
- Full URL: store full URL only when explicitly enabled.

Window titles and tab titles may contain sensitive text. They must obey the
same privacy-aware display/storage settings as window titles in Windows focus
tracking.

URLs must be sanitized before persistence:

- Strip or redact fragments where possible.
- Consider redacting query strings unless full URL opt-in is explicit.
- Do not persist browser page contents.
- Do not scrape forms, messages, passwords, or typed text.

## Capture Rules

- Classify supported browser processes as browser activity.
- Attempt URL/domain capture only through privacy-safe methods.
- Prefer a visible browser extension/native messaging path or UI Automation
  address bar extraction when feasible.
- If URL/domain is unavailable, save only the normal FocusSession.
- If URL/domain is available, save a WebSession linked to the current browser
  FocusSession.
- URL/domain changes close the previous WebSession and start a new WebSession.
- Duplicate tab events must not inflate duration.
- Do not invent fake domains from window titles unless explicitly marked
  `Low` confidence and `FakeTestData` or test-only fallback.

## Required Tests

- `chrome.exe` is classified as a browser.
- Non-browser processes do not create WebSessions.
- Fake Chrome URL `github.com` creates a WebSession.
- URL changes from `github.com` to `chatgpt.com` close/start WebSessions.
- URL unavailable falls back to FocusSession only.
- Domain-only privacy setting stores domain but not full URL.
- Full URL is stored only when opt-in allows it.
- WebSession is persisted to SQLite.
- WebSession creates an outbox item.
- WebSession upload payload includes domain and duration.
- Duplicate upload is idempotent.

## Current Implementation Notes

The repository already contains Chrome extension/native messaging primitives
and a browser raw event repository. The remaining restoration work is to apply
URL sanitization/privacy settings to persistence flows, add native host
packaging/connection status, evaluate UI Automation fallback, and strengthen
correlation between browser tab events and the active browser focus session.

Milestone 23 now includes the first privacy-safe browser abstraction layer:
`BrowserActivitySnapshot`, `CaptureMethod`, `CaptureConfidence`,
`IBrowserProcessClassifier`, `IBrowserActivityReader`,
`IBrowserUrlSanitizer`, and `IWebSessionizer`. `BrowserProcessClassifier`
recognizes `chrome.exe`, `msedge.exe`, `firefox.exe`, and `brave.exe`
case-insensitively, including process names reported without the `.exe`
suffix.

`BrowserUrlSanitizer` now enforces the URL storage policy boundary:

- Off clears URL/domain data and downgrades capture metadata to
  `None`/`Unknown`.
- Domain only keeps the registrable domain and clears the full URL.
- Full URL keeps the URL only when explicitly requested and strips URL
  fragments before persistence.

`BrowserWebSessionizer` now accepts sanitized `BrowserActivitySnapshot` inputs.
Snapshots with a domain create/close web sessions, while snapshots with no URL
and no domain are ignored for web-session creation so the browser still remains
represented by the normal FocusSession only. Domain-only snapshots produce
`WebSession` rows with `Url = null`.

Windows local SQLite `web_session` rows now preserve optional capture
provenance: capture method, capture confidence, and private/unknown state.

Chrome native-message ingestion can now enqueue a `web_session` outbox item
when a web session completes. The upload payload includes device id, focus
session id, domain, duration, nullable URL/title fields, and capture
provenance.

Server web-session upload is retry-safe for domain-only payloads where
`url = null`; duplicate uploads return `Duplicate` rather than inserting a
second row.
