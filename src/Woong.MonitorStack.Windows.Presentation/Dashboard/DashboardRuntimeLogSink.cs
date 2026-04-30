namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public interface IDashboardRuntimeLogSink
{
    string LogPath { get; }

    void WriteEvent(DashboardRuntimeLogEvent logEvent);

    void WriteException(string operation, Exception exception);
}

public sealed record DashboardRuntimeLogEvent(
    DateTimeOffset OccurredAtUtc,
    string EventType,
    string AppName,
    string Domain,
    string Message);

public sealed class NullDashboardRuntimeLogSink : IDashboardRuntimeLogSink
{
    public string LogPath => "runtime log disabled";

    public void WriteEvent(DashboardRuntimeLogEvent logEvent)
    {
    }

    public void WriteException(string operation, Exception exception)
    {
    }
}
