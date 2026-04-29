namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public sealed record DashboardSessionRow(
    string AppName,
    string ProcessName,
    string StartedAtLocal,
    string EndedAtLocal,
    string Duration,
    string State,
    string WindowTitle,
    string Source,
    bool IsIdle);
