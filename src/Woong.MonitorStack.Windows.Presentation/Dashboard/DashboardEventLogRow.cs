namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public sealed record DashboardEventLogRow(
    string EventType,
    string OccurredAtLocal,
    string AppName,
    string Domain,
    string Message)
{
    public string Kind => EventType;
}
