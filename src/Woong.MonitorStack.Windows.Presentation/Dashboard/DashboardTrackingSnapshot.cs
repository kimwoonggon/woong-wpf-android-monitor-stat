namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public sealed record DashboardTrackingSnapshot(
    string? AppName,
    string? ProcessName,
    string? WindowTitle,
    TimeSpan CurrentSessionDuration,
    DashboardPersistedSessionSnapshot? LastPersistedSession)
{
    public static DashboardTrackingSnapshot Empty { get; } = new(
        AppName: null,
        ProcessName: null,
        WindowTitle: null,
        CurrentSessionDuration: TimeSpan.Zero,
        LastPersistedSession: null);
}

public sealed record DashboardPersistedSessionSnapshot(
    string? AppName,
    string? ProcessName,
    DateTimeOffset EndedAtUtc,
    TimeSpan Duration);
