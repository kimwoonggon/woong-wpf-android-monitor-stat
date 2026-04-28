using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Windows.Tracking;

public sealed class FocusSessionizer
{
    private readonly string _deviceId;
    private readonly string _timezoneId;
    private ForegroundWindowSnapshot? _currentSnapshot;
    private DateTimeOffset? _currentStartedAtUtc;
    private bool _currentIsIdle;

    public FocusSessionizer(string deviceId, string timezoneId)
    {
        _deviceId = RequiredText.Ensure(deviceId, nameof(deviceId));
        _timezoneId = RequiredText.Ensure(timezoneId, nameof(timezoneId));
    }

    public FocusSessionizerResult Process(ForegroundWindowSnapshot snapshot, bool isIdle)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (_currentSnapshot is null || _currentStartedAtUtc is null)
        {
            Start(snapshot, isIdle);

            return new FocusSessionizerResult(null, CreateCurrentSession(snapshot.TimestampUtc));
        }

        if (_currentSnapshot.HasSameFocusIdentity(snapshot) && _currentIsIdle == isIdle)
        {
            return new FocusSessionizerResult(null, CreateCurrentSession(snapshot.TimestampUtc));
        }

        var closed = CreateCurrentSession(snapshot.TimestampUtc);
        Start(snapshot, isIdle);

        return new FocusSessionizerResult(closed, CreateCurrentSession(snapshot.TimestampUtc));
    }

    private void Start(ForegroundWindowSnapshot snapshot, bool isIdle)
    {
        _currentSnapshot = snapshot;
        _currentStartedAtUtc = snapshot.TimestampUtc;
        _currentIsIdle = isIdle;
    }

    private FocusSession CreateCurrentSession(DateTimeOffset endedAtUtc)
    {
        if (_currentSnapshot is null || _currentStartedAtUtc is null)
        {
            throw new InvalidOperationException("No active focus snapshot.");
        }

        var normalizedEndedAtUtc = endedAtUtc > _currentStartedAtUtc.Value
            ? endedAtUtc
            : _currentStartedAtUtc.Value.AddMilliseconds(1);

        return FocusSession.FromUtc(
            clientSessionId: FocusSessionClientId.Create(_currentSnapshot, _currentStartedAtUtc.Value),
            deviceId: _deviceId,
            platformAppKey: _currentSnapshot.ProcessName,
            startedAtUtc: _currentStartedAtUtc.Value,
            endedAtUtc: normalizedEndedAtUtc,
            timezoneId: _timezoneId,
            isIdle: _currentIsIdle,
            source: "foreground_window");
    }
}
