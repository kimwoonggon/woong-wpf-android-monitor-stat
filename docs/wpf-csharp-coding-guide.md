# WPF C# Coding Guide

Updated: 2026-04-29

This guide is the detailed WPF/.NET companion to `docs/coding-guide.md`. It
explains where C# code belongs across Domain, Presentation, Windows
infrastructure, and the WPF App composition root.

## Golden Rule

Dependencies point toward stable, platform-neutral code. WPF and Windows API
details stay at the edge.

```text
Woong.MonitorStack.Domain
  <- Woong.MonitorStack.Windows.Presentation
  <- Woong.MonitorStack.Windows.App

Woong.MonitorStack.Domain
  <- Woong.MonitorStack.Windows
  <- Woong.MonitorStack.Windows.App

Woong.MonitorStack.Domain
  <- Woong.MonitorStack.Server
```

`Woong.MonitorStack.Windows.Presentation` and
`Woong.MonitorStack.Windows` are siblings. They should not reference each other
unless a new port/adapter design is explicitly documented and protected by
architecture tests.

## Project Responsibilities

| Project | Owns | Must Not Own |
| --- | --- | --- |
| `Woong.MonitorStack.Domain` | Pure models, value objects, calculators, normalizers, stable shared DTO contracts | WPF, Windows APIs, SQLite, EF Core, ASP.NET Core, LiveCharts, file/network access |
| `Woong.MonitorStack.Windows.Presentation` | ViewModels, row models, summary cards, command state, display formatting, dashboard mappers, UI-neutral chart models | WPF controls/windows, `System.Windows`, `MessageBox`, user32, registry, filesystem, HTTP, SQLite, server APIs |
| `Woong.MonitorStack.Windows` | Foreground tracking, idle detection adapters, Chrome native messaging, SQLite repositories, outbox sync, Windows HTTP clients, OS wrappers | XAML, WPF controls, App composition root, server implementation |
| `Woong.MonitorStack.Windows.App` | `App.xaml`, `MainWindow.xaml`, Generic Host lifecycle, DI registration, WPF resources, visual layout, UI adapters | Domain calculations, persistence rules, sync algorithms, direct business logic |
| `Woong.MonitorStack.Server` | ASP.NET Core API, EF Core PostgreSQL, integrated summaries | Windows projects, WPF presentation, local client DB assumptions |

Test projects may reference the production projects they validate, but
production projects must follow the table above.

## Placement Decision Guide

Use this guide before adding a file:

- New time range, duration, local date, domain normalization, or summary
  calculation rule: put it in `Domain`.
- New API upload/query DTO shared by Windows, Android, and Server: put it in
  `Domain.Contracts` for now. If contracts grow or version separately, move
  them to a future `Woong.MonitorStack.Contracts` project.
- New dashboard label, formatted duration, row model, tab state, selected
  period, command, or ViewModel behavior: put it in `Windows.Presentation`.
- New WPF control, XAML binding, style, resource dictionary, window, data
  template, or automation ID: put it in `Windows.App`.
- New SQLite query, sync outbox processing, Windows collector, native messaging
  receiver, registry writer, or user32 wrapper: put it in `Windows`.
- New server endpoint, EF entity, DbContext mapping, idempotency index, or
  integrated daily summary persistence: put it in `Server`.

When a feature touches several layers, start with the most stable public
behavior and add one TDD slice at a time. For example:

1. Domain test for a calculation rule.
2. Presentation test for how that result is exposed to the dashboard.
3. Windows component test for storing or syncing the data.
4. WPF App smoke/UI test for binding the ViewModel to XAML.

## Domain Rules

Domain code must be boring, deterministic, and easy to test.

- Use records or small immutable types for facts and value objects.
- Store persisted instants as UTC `DateTimeOffset`.
- Convert to local `DateOnly` only where a timezone is explicit.
- Keep calculators independent from WPF, Windows, Android, EF Core, ASP.NET
  Core, and LiveCharts.
- Prefer constructor/factory validation over nullable partially-valid objects.
- Do not read clocks directly; pass time in or use an injected clock at a
  higher layer.

Good Domain candidates:

- `TimeRange`
- `FocusSession`
- `DailySummaryCalculator`
- `DomainNormalizer`
- upload contract DTOs while the surface remains small

Bad Domain candidates:

- `MainWindow`
- `DashboardViewModel`
- `SqliteFocusSessionRepository`
- `WindowsForegroundWindowReader`
- EF Core entities

## Presentation Layer Rules

`Windows.Presentation` is MVVM presentation logic, not WPF UI.

Allowed:

- `ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`, and
  `[AsyncRelayCommand]` from CommunityToolkit.Mvvm.
- Interfaces such as `IDashboardDataSource` and `IDashboardClock`.
- Plain row models for app sessions, web sessions, live events, and settings.
- Mappers and formatters that turn domain/session data into display state.
- LiveCharts view-model series only under the documented adapter exception.

Forbidden:

- `System.Windows`, `Window`, `Control`, `DataGrid`, `MessageBox`, `Dispatcher`.
- user32, registry, native messaging streams, SQLite, filesystem, HTTP.
- Direct references to `Woong.MonitorStack.Windows`,
  `Woong.MonitorStack.Windows.App`, or `Woong.MonitorStack.Server`.

ViewModels should expose stable observable state:

- Commands for user actions.
- Immutable or read-only collections for UI rows where possible.
- Explicit empty/loading/error/sync states.
- Formatted strings only when they are display concerns.

ViewModels should not:

- Query Windows APIs.
- Open databases.
- Send HTTP requests directly.
- Inspect WPF controls.
- Know about XAML element names.

If a ViewModel needs data, define a port in Presentation and implement it in
Windows infrastructure or App composition:

```csharp
namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public interface IDashboardDataSource
{
    Task<DashboardSnapshot> LoadAsync(TimeRange range, CancellationToken ct);
}
```

## Windows Infrastructure Rules

`Woong.MonitorStack.Windows` is the implementation edge for Windows-local
behavior.

- Wrap all platform APIs behind interfaces.
- Keep P/Invoke and registry calls isolated in small adapter classes.
- Make collectors and sessionizers testable with fake readers and fake clocks.
- Keep local SQLite repositories Windows-only; do not model Android tables here.
- Sync through server DTO contracts and the outbox pattern.
- Chrome native messaging may collect active tab URL/title/domain only through
  explicit extension/native messaging setup. It must not collect passwords,
  messages, form input, or typed text.

Infrastructure can depend on Domain contracts, but should not take a dependency
on ViewModels. If infrastructure output needs to reach the UI, expose it through
a port that the App composition root wires together.

## WPF App Rules

`Woong.MonitorStack.Windows.App` is the WPF composition root and visual layer.

- `App.xaml.cs` owns `IHost` lifecycle.
- Register services through DI. Prefer extension methods when registration
  grows, such as `AddDashboardPresentation()` or `AddWindowsInfrastructure()`.
- Resolve `MainWindow` from `IServiceProvider`.
- `MainWindow.xaml.cs` should stay thin: `InitializeComponent()` plus
  `DataContext` assignment.
- Keep business logic out of code-behind.
- Use XAML bindings for commands, selected periods, summary cards, grids,
  settings, and charts.
- Add stable `AutomationProperties.AutomationId` values for important controls.
- Keep WPF resources and styles in App, not Presentation.
- Keep reusable WPF visuals such as `StatusBadge`, `MetricCard`,
  `SectionCard`, `DetailRow`, and `EmptyState` in `Windows.App.Controls`.
  These controls may expose dependency properties for XAML composition, but
  must not own product calculations, persistence, sync, or tracking behavior.
- Verify visual changes with WPF App tests and local UI snapshots.

Example code-behind shape:

```csharp
namespace Woong.MonitorStack.Windows.App;

public partial class MainWindow : Window
{
    public MainWindow(DashboardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
```

## C# Style

- Use file-scoped namespaces.
- Keep nullable reference types enabled.
- Use explicit names that describe domain behavior.
- Prefer small public interfaces with deep implementations.
- Prefer `DateTimeOffset` for instants.
- Use `CancellationToken` on async infrastructure and data-loading paths.
- Avoid `.Result`, `.Wait()`, and `.GetAwaiter().GetResult()`.
- Avoid static service locators and global mutable state.
- Do not add shallow abstractions only to make tests mock-heavy.
- Do not add comments that restate obvious code; document non-obvious rules.

## MVVM Conventions

- Use CommunityToolkit.Mvvm for observable properties and commands.
- Commands should delegate to public ViewModel behavior that is testable without
  WPF controls.
- Keep mapping/formatting logic in mapper or formatter classes once it grows
  beyond trivial property assignment.
- Use fake clocks and fake data sources in ViewModel tests.
- Prefer observable behavior assertions: selected range, summary values,
  sorted rows, command effects, safe empty state.
- Do not test private methods.

## Async And Threading

- Library/infrastructure code should accept `CancellationToken`.
- UI-triggered async commands should surface failure state instead of throwing
  into the dispatcher.
- Do not block the STA thread with synchronous waits.
- Keep `Dispatcher` usage in App/UI adapters. Presentation should not need WPF
  dispatcher types.
- If a background worker updates UI state, marshal at the App boundary or use a
  testable dispatcher abstraction that is documented.

## XAML Rules

- Bind to ViewModel properties and commands.
- Do not add code-behind event handlers for ordinary dashboard actions.
- Keep `x:Class` and namespaces stable.
- Use readable, formatted XAML; do not commit minified or unreadable XAML.
- Use shared resource dictionaries for repeated button/card/tab/grid styles.
  Do not inline repeated MinWidth, MinHeight, Padding, FontSize, or brush
  setters in feature panels when an existing style can carry the rule.
- Merge shared WPF style dictionaries at `App.xaml` root. `MainWindow.xaml`
  should stay a shell and should not duplicate application-level style
  dictionary merges.
- Add AutomationIds for controls that are user-visible or automation-relevant:
  refresh button, period selector, summary cards, charts, tabs, grids, settings
  controls.
- Preserve current smoke and snapshot selectors when changing names.

## Testing Matrix

| Change Type | Test First |
| --- | --- |
| Domain calculation | `Woong.MonitorStack.Domain.Tests` unit test |
| Presentation ViewModel state | `Woong.MonitorStack.Windows.Presentation.Tests` unit test with fake data source/clock |
| SQLite repository/outbox | `Woong.MonitorStack.Windows.Tests` component test |
| Windows collector/sessionizer | `Woong.MonitorStack.Windows.Tests` unit/component test with fake readers |
| WPF binding/layout expectation | `Woong.MonitorStack.Windows.App.Tests` STA smoke/XAML test |
| Visual WPF change | WPF App tests plus `scripts/run-ui-snapshots.ps1` |
| Dependency direction change | `Woong.MonitorStack.Architecture.Tests` architecture test |

Use `dotnet test Woong.MonitorStack.sln --no-build -maxcpucount:1 -v minimal`
for the final .NET test pass after focused tests are green.

## Review Checklist

Before opening or committing a WPF/.NET slice, check:

- Does the file live in the correct project?
- Did the test describe behavior through a public interface?
- Does Domain remain platform-neutral?
- Does Presentation avoid WPF, Windows APIs, DB, filesystem, HTTP, and server
  references?
- Is App only composing, binding, and rendering?
- Is infrastructure hidden behind interfaces where OS or IO behavior is
  involved?
- Are privacy boundaries still intact?
- Did architecture tests still pass?
- Did build and relevant tests pass?
- Were docs and `total_todolist.md` updated?

## Common Mistakes To Avoid

- Putting `System.Windows` types in Presentation because it feels convenient.
- Letting `DashboardViewModel` open SQLite or call a Windows collector.
- Passing WPF controls into mappers or formatters.
- Adding a server reference from Windows local infrastructure.
- Treating Windows SQLite as an integrated database.
- Testing private helper methods instead of command/ViewModel/repository
  behavior.
- Changing AutomationIds casually and breaking UI snapshots.
- Adding package dependencies without checking existing conventions and skill
  discovery.
