namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public sealed record DashboardWebSessionRow(
    string Domain,
    string PageTitle,
    string StartedAtLocal,
    string Duration);
