namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public sealed record DashboardEventLogRow(
    string Kind,
    string OccurredAtLocal,
    string Message);
