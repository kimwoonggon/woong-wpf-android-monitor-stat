namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public sealed record DashboardSessionRow(
    string AppName,
    string StartedAtLocal,
    string Duration,
    bool IsIdle);
