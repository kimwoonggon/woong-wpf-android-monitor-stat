# Project Reference Rules

These rules define the intended dependency direction for Woong Monitor Stack.
They are enforced by `tests/Woong.MonitorStack.Architecture.Tests`.

## Direction

```text
Domain
  -> used by Windows, Windows.Presentation, Windows.App, Server

Windows.Presentation
  -> Domain only

Windows
  -> Domain only

Windows.App
  -> Domain, Windows.Presentation, Windows

Server
  -> Domain only
```

Test projects are excluded from production dependency checks. They may reference
the production projects they validate.

## Woong.MonitorStack.Domain

`Woong.MonitorStack.Domain` is OS-neutral and dependency-light.

It must not reference:

- `Woong.MonitorStack.Windows`
- `Woong.MonitorStack.Windows.App`
- `Woong.MonitorStack.Windows.Presentation`
- `Woong.MonitorStack.Server`
- Entity Framework or database providers
- ASP.NET Core
- WPF or `System.Windows`
- LiveCharts
- platform-specific APIs

It may contain pure domain models, value objects, calculators, time/date logic,
domain normalizers, and stable shared DTO contracts.

The current API DTOs live under `Woong.MonitorStack.Domain.Contracts` because
the contract surface is still small and stable. If those DTOs begin changing at
a different cadence from the domain model, or if client/server versioning
becomes heavier, move them into a dedicated `Woong.MonitorStack.Contracts`
project.

## Woong.MonitorStack.Windows.Presentation

`Woong.MonitorStack.Windows.Presentation` is UI-platform-neutral presentation
logic.

It may reference:

- `Woong.MonitorStack.Domain`
- CommunityToolkit.Mvvm
- LiveCharts core/SkiaSharp view-model-level chart types as a deliberate
  presentation adapter exception

It may contain ViewModels, mapper classes, row models, chart data models,
command state, options, and formatting logic.

It must not reference:

- `Woong.MonitorStack.Windows.App`
- `Woong.MonitorStack.Windows`
- `Woong.MonitorStack.Server`
- WPF window/control types or `System.Windows`
- `MessageBox`
- user32/PInvoke or registry APIs
- database providers
- filesystem or HTTP clients
- ASP.NET Core

LiveCharts types are currently retained here because the WPF dashboard binds to
series models directly and the mapper is covered by presentation tests. If
charting becomes more WPF-specific, move LiveCharts construction into
`Woong.MonitorStack.Windows.App` and keep only plain chart-point models in
Presentation.

## Woong.MonitorStack.Windows

`Woong.MonitorStack.Windows` is the Windows infrastructure project.

It may reference:

- `Woong.MonitorStack.Domain`

It contains Windows-specific implementation such as foreground tracking,
browser/native messaging collection, local SQLite storage, sync clients, and OS
API wrappers.

It must not reference:

- `Woong.MonitorStack.Windows.App`
- XAML UI or WPF
- `Woong.MonitorStack.Server`

Avoid referencing `Woong.MonitorStack.Windows.Presentation`. If infrastructure
needs to drive a presentation workflow, prefer an application/contracts port
instead of pointing infrastructure at ViewModels.

## Woong.MonitorStack.Windows.App

`Woong.MonitorStack.Windows.App` is the WPF composition root.

It may reference:

- `Woong.MonitorStack.Domain`
- `Woong.MonitorStack.Windows.Presentation`
- `Woong.MonitorStack.Windows`

It contains `App.xaml`, `MainWindow.xaml`, host/DI setup, WPF resources, and UI
adapters. Code-behind should stay thin: initialization, `DataContext`
assignment, and WPF lifecycle integration only.

`App.xaml.cs` owns the Generic Host lifecycle and resolves `MainWindow` from
DI. It must not manually construct `DashboardViewModel`,
`EmptyDashboardDataSource`, or `SystemDashboardClock`.

## Woong.MonitorStack.Server

`Woong.MonitorStack.Server` is the ASP.NET Core/PostgreSQL integration layer.

It may reference:

- `Woong.MonitorStack.Domain`

It must not reference:

- `Woong.MonitorStack.Windows`
- `Woong.MonitorStack.Windows.App`
- `Woong.MonitorStack.Windows.Presentation`

The server is the only integrated database boundary for Windows + Android data.
Windows SQLite and Android Room remain device-local and never connect to each
other directly.
