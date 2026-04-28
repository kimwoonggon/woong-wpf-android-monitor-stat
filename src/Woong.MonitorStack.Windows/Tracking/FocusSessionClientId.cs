namespace Woong.MonitorStack.Windows.Tracking;

internal static class FocusSessionClientId
{
    public static string Create(ForegroundWindowSnapshot snapshot, DateTimeOffset startedAtUtc)
        => $"{snapshot.ProcessId}:{snapshot.Hwnd}:{startedAtUtc.ToUnixTimeMilliseconds()}";
}
