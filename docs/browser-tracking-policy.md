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
- Administrator rights are not a browser-domain capture strategy. Elevation can
  change some Windows accessibility boundaries, but it does not provide browser
  active-tab URL APIs and should not be presented as the fix for missing
  domains.
- If URL/domain is unavailable, save only the normal FocusSession.
- If URL/domain is available, save a WebSession linked to the current browser
  FocusSession.
- In Domain only mode, show the domain as soon as the capture channel provides
  it. The absence of a domain means the browser capture channel is not
  connected, configured, or reporting yet; it is not a privacy block.
- URL/domain changes close the previous WebSession and start a new WebSession.
- Duplicate tab events must not inflate duration.
- Do not invent fake domains from window titles unless explicitly marked
  `Low` confidence and `FakeTestData` or test-only fallback.

## Production Capture Path

Production browser-domain capture should prefer explicit browser integration:
an installed extension that the user can see and approve, connected to the
Windows app through native messaging. This is the stable path for Chrome and
Edge domain metadata because the browser owns active-tab URL access.

The WPF app also registers a metadata-only UI Automation address-bar fallback
so domain-only metadata can appear as soon as tracking starts when the active
browser exposes a recognizable address bar. This fallback reads only a browser
address-bar value to derive a registrable domain, never reads page contents,
forms, messages, passwords, or typed text, and never stores full URLs while the
storage policy is Domain only. Browser UI availability can vary by browser
version, focus state, profile, accessibility settings, and operating
environment, so extension/native messaging remains the more reliable path.

The current browser-domain field should therefore distinguish capture status
from privacy state:

- Privacy status: full URL storage is off unless explicitly enabled.
- Capture status: no domain is available until extension/native messaging or
  the address-bar fallback reports metadata.
- Product state: foreground app/window focus remains valid and should display
  immediately even when browser-domain capture is unavailable.

The WPF Current Focus panel keeps domain and capture status separate. The
domain value should be a domain such as `github.com` when available. The
browser capture status should communicate the source or problem:

- `Browser extension connected`
- `Domain from address bar fallback`
- `Browser capture unavailable`
- `Browser capture error`

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

Chrome native-message ingestion now sanitizes browser URL data before writing
browser raw events. In DomainOnly mode the raw event keeps the normalized
domain but stores no full URL, and the resulting WebSession also has
`Url = null`.

The WPF Settings tab now includes visible browser URL privacy copy:
`Browser URL storage is domain-only by default. Full URLs require explicit
future opt-in.` This is intentionally conservative until a dedicated user-facing
URL storage selector is implemented. The current tests cover browser process
classification, DomainOnly and FullUrl sanitizer behavior, nullable URL
persistence, domain-only raw event ingestion, web-session outbox payloads, and
idempotent server upload.

`WindowsTrackingDashboardCoordinator` now has an optional browser reader path
for the live WPF tracking loop. The path sanitizes browser snapshots with
DomainOnly storage by default, so `web_session.url` and upload payload `url`
remain null unless a future explicit opt-in enables full URL storage. A browser
domain change while Chrome stays foreground persists the completed previous
domain session, creates a pending `web_session` outbox row, and signals the
dashboard to reload from SQLite.

The Windows infrastructure can now generate the Chrome native messaging host
manifest JSON through `NativeMessagingHostManifestGenerator`. The manifest
declares the stable native host name, host executable path, `stdio` transport,
and explicit allowed extension origins.

WPF browser UI copy must not present a safe privacy state as a broken product
state. When no browser domain is available in the Current Focus panel, the
fallback text is:

`No browser domain yet. Connect browser capture; app focus is tracked.`

This means foreground app/window focus tracking can still be working while the
browser-domain capture channel is not connected, not configured, or has not yet
reported a domain. Full URL capture remains off by default and must require a
future explicit opt-in before any URL path/query is stored.

Running the WPF app as Administrator is not a reliable browser-domain capture
solution. Administrator rights can affect some Windows UI Automation access,
but they do not grant Chrome, Edge, Firefox, or Brave active-tab URL APIs. The
robust path is explicit browser integration: Chrome/Edge extension plus native
messaging, or a documented opt-in UI Automation address-bar fallback where it
is technically available. The app should still show foreground app/process
metadata immediately on Start even when browser-domain capture is not connected.

The Chrome native messaging path now has a local host executable and install
script. The extension keeps a persistent native port open so ordered tab/domain
changes can be sessionized by the host process. This is more stable than
administrator elevation because it uses browser-granted active-tab metadata
rather than trying to pierce browser internals from the outside. Edge and Brave
can follow the same Chromium extension/native-messaging pattern with browser
specific registration/packaging, while Firefox requires its own extension and
native-messaging manifest path. Until those browser-specific installers are
added, the generic WPF browser-domain fallback remains best-effort for
non-Chrome browsers.
