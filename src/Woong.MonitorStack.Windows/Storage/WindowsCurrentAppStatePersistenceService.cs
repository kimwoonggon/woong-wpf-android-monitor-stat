using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.Tracking;

namespace Woong.MonitorStack.Windows.Storage;

public sealed class WindowsCurrentAppStatePersistenceService
{
    private readonly SqliteCurrentAppStateRepository _repository;

    public WindowsCurrentAppStatePersistenceService(SqliteCurrentAppStateRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public void SaveCurrentAppState(FocusSessionizerResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        FocusSession currentSession = result.CurrentSession;
        var state = new CurrentAppStateRecord(
            currentSession.DeviceId,
            currentSession.PlatformAppKey,
            currentSession.ProcessId,
            currentSession.ProcessName,
            currentSession.ProcessPath,
            currentSession.WindowHandle,
            result.ForegroundWindow?.TimestampUtc ?? currentSession.EndedAtUtc,
            currentSession.LocalDate,
            currentSession.TimezoneId,
            status: currentSession.IsIdle ? "Idle" : "Active",
            source: "windows_foreground_current_app");

        _repository.Upsert(state);
    }
}
