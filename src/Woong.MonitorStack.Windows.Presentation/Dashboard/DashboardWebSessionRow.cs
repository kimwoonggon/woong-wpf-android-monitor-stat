namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public sealed record DashboardWebSessionRow(
    string Domain,
    string PageTitle,
    string UrlMode,
    string StartedAtLocal,
    string EndedAtLocal,
    string Duration,
    string Browser,
    string Confidence);
