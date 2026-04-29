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

`scripts/run-chrome-native-message-acceptance.ps1` launches Chrome for Testing
by default with a temporary profile:

```text
--user-data-dir=<temp-profile>
```

Cleanup searches Windows processes for that exact temporary profile path and
stops only matching Chrome processes. Existing user Chrome windows and profiles
are outside the sandbox and must not be closed by this test.

As an extra guard, cleanup first verifies that the profile path belongs under
the acceptance temp root named `woong-chrome-native-*`. If a normal Chrome
profile path, a blank path, or any non-acceptance path is passed to cleanup, the
script refuses to stop Chrome before enumerating processes.

The script must not fall back to the user's installed Chrome automatically. If
Chrome for Testing is not already in `.cache/chrome-for-testing/`, run the
helper installer or pass `-InstallChromeForTesting`. Installed Chrome fallback
is available only with the explicit `-AllowInstalledChromeFallback` switch for
isolated manual debugging.

The test DB is also local to the artifact run:

```text
artifacts/chrome-native-acceptance/<timestamp>/chrome-native-acceptance.db
```

The native host is run with `WOONG_MONITOR_REQUIRE_EXPLICIT_DB=1`, so the
acceptance host fails instead of falling back to the user's real
`windows-local.db` if the explicit temp DB path is missing.

Chrome for Testing is required by default for this acceptance path because it
keeps the user's real Chrome profile out of the test harness. Recent official
Google Chrome stable builds can also block command-line unpacked extension
loading. The helper `scripts/install-chrome-for-testing.ps1` downloads the
official Chrome for Testing win64 archive into the ignored local cache:

```text
.cache/chrome-for-testing/
```

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

Dry-run cleanup also passes `-DryRun` to the uninstall script, so it reports
which scoped HKCU key would be restored or removed without changing registry
values.

Install Chrome for Testing and run against the sandbox browser:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/run-chrome-native-message-acceptance.ps1 -InstallChromeForTesting
```

Cleanup only:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/run-chrome-native-message-acceptance.ps1 -CleanupOnly
```

`-CleanupOnly` is a cleanup path, not a browser acceptance run. It executes
before Chrome for Testing discovery, so manual cleanup is not blocked when the
sandbox browser is missing. The script also records that native-host cleanup
has already run so the `finally` block does not uninstall/restore the scoped
HKCU key a second time.

## Current Status

The safety harness, dry-run path, scoped HKCU registration, scoped cleanup,
temp-profile Chrome cleanup, non-temp-profile cleanup refusal, and full headed
Chrome for Testing acceptance are covered locally. The latest passing
acceptance wrote domain-only
`github.example` and `chatgpt.example` web sessions plus pending outbox rows to
the temp SQLite DB, with full URL and page title values redacted by default.
