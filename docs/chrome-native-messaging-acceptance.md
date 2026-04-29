# Chrome Native Messaging Acceptance

This acceptance path verifies Chrome domain metadata through the intended
browser-owned channel:

```text
Chrome extension -> Native Messaging host -> temp SQLite DB -> dashboard data source
```

It is intentionally separate from WPF UI automation. FlaUI is used for this
app's WPF UI only; it must not scrape Chrome's address bar, accessibility tree,
screenshots, page contents, typed text, forms, passwords, messages, or
clipboard contents.

## Local-Only Sandbox

`scripts/run-chrome-native-message-acceptance.ps1` launches Chrome with a
temporary profile:

```text
--user-data-dir=<temp-profile>
```

Cleanup searches Windows processes for that exact temporary profile path and
stops only matching Chrome processes. Existing user Chrome windows and profiles
are outside the sandbox and must not be closed by this test.

The test DB is also local to the artifact run:

```text
artifacts/chrome-native-acceptance/<timestamp>/chrome-native-acceptance.db
```

The native host is run with `WOONG_MONITOR_REQUIRE_EXPLICIT_DB=1`, so the
acceptance host fails instead of falling back to the user's real
`windows-local.db` if the explicit temp DB path is missing.

## Registry Safety

Chrome native messaging requires an HKCU host registration for local
acceptance. The scripts use a scoped test host name by default:

```text
com.woong.monitorstack.chrome_test
```

Rules enforced by tests:

- HKCU only; HKLM is never used.
- Only the scoped child host key is created, restored, or removed.
- The parent `NativeMessagingHosts` key is never deleted.
- Blank or malformed `HostName` values are rejected before a registry path is
  built.
- If the scoped key already exists, its previous default value is backed up and
  restored during cleanup.
- If there was no previous value, cleanup deletes only the scoped child key.
- The script prints the exact registry key it creates, restores, or removes.

Dry run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/run-chrome-native-message-acceptance.ps1 -DryRun
```

Cleanup only:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/run-chrome-native-message-acceptance.ps1 -CleanupOnly
```

## Current Status

The safety harness, dry-run path, scoped HKCU registration, scoped cleanup, and
temp-profile Chrome cleanup are covered by automated tests. The full headed
Chrome acceptance should not be marked complete until it receives active-tab
messages and writes the expected `github.example` and `chatgpt.example` domain
rows to the temp SQLite DB.

Latest known blocker: the full acceptance can still time out waiting for native
messages to reach SQLite. That is a functional Chrome/native messaging issue,
not permission to use address-bar scraping or the user's real Chrome profile.
