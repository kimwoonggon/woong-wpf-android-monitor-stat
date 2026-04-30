# Coverage Gap Triage

Updated: 2026-04-30

This note explains the latest `.NET` coverage report and separates intentional
low/zero coverage from future actionable test gaps. It is a QA coordination
guide, not a waiver for behavior that can be tested through public interfaces.

Latest local report:

- Source: `artifacts/coverage/SummaryGithub.md`
- Line coverage: 91.7% (3823/4166)
- Branch coverage: 70.9% (544/767)

## Intentional Low Or Zero Coverage

These areas are OS-bound, process-bound, or bootstrap-only. Keep them covered
by smoke/acceptance checks, architecture guards, and privacy documentation
rather than forcing brittle unit tests around platform calls.

| Area | Latest coverage signal | Why this is acceptable | Preferred evidence |
|---|---:|---|---|
| `Woong.MonitorStack.Windows.Tracking.WindowsForegroundWindowReader` | 0% | Calls Windows foreground-window APIs and depends on real desktop state. | Windows smoke tool, RealStart acceptance, WPF acceptance temp DB evidence. |
| `Woong.MonitorStack.Windows.Tracking.WindowsLastInputReader` | 0% | Wraps Windows last-input APIs and real user idle state. | Idle detector unit tests around abstractions, Windows smoke/RealStart evidence. |
| `Woong.MonitorStack.Windows.Storage.WindowsRegistryWriter` | 0% | Writes HKCU registry values and should not be exercised casually in unit tests. | Chrome native acceptance dry-run/cleanup artifacts and registry safety script tests. |
| `Woong.MonitorStack.Windows.App.App` | 0% | WPF process bootstrap, Generic Host lifecycle, and STA app entry are covered indirectly. | App composition tests, startup-service tests, WPF UI acceptance launch evidence. |
| `Woong.MonitorStack.Windows.App.Browser.WindowsUiAutomationAddressBarReader` | 0% | Best-effort browser UI Automation fallback is environment-dependent and must not scrape page content. | Privacy guard tests, explicit fallback policy docs, targeted smoke only when manually enabled. |
| `Woong.MonitorStack.Windows.App.Dashboard.EmptyDashboardDataSource` | 0% | Deterministic mode surface; behavior is checked through EmptyData snapshot artifacts. | `scripts/run-ui-snapshots.ps1 -Mode EmptyData` and zero-row SQLite evidence. |
| `Woong.MonitorStack.Windows.Tracking.SystemClock` / `SystemDashboardClock` | 0% | Thin clock adapters around system time. | Behavior tests use fake clocks at public boundaries. |

## Actionable Future Test Gaps

These are good candidates for independent future slices because they can be
tested without crossing privacy boundaries or coupling to private internals.

| Priority | Gap | Suggested owner | Suggested validation |
|---:|---|---|---|
| 1 | Add a checklist/report package for Server API coverage similar to `android_check_todo.md` and `wpf_check_todo.md`. | Server/QA | Focused server tests plus `dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal`. |
| 2 | Add policy tests or source guards around `WindowsUiAutomationAddressBarReader` confirming it remains metadata-only and never reads page contents or typed text. | WPF/Windows | Focused Windows App/Windows tests plus privacy grep/source guard. |
| 3 | Add branch-focused tests for `DailySummaryQueryService` edge cases such as empty ranges, user isolation, and timezone boundary combinations. | Server | `dotnet test tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj --no-restore -maxcpucount:1 -v minimal`. |
| 4 | Add explicit coverage for browser raw-event retention policy boundary values. | WPF/Windows | Focused Windows storage tests. |
| 5 | Refresh Android check artifacts after permission-onboarding changes so `android_check_todo.md` points to a package that includes `13-permission-onboarding.png`. | Android/QA | Android architecture tests, Gradle unit/build/androidTest, screenshot script. |

## Commands

Use these commands to refresh and inspect the coverage picture:

```powershell
dotnet test Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal
dotnet build Woong.MonitorStack.sln --no-restore -maxcpucount:1 -v minimal
powershell -ExecutionPolicy Bypass -File scripts\test-coverage.ps1
```

Review:

```text
artifacts/coverage/SummaryGithub.md
artifacts/coverage/index.html
```
