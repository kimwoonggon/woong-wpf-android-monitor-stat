namespace Woong.MonitorStack.Windows.Tracking;

public sealed record ForegroundWindowSnapshot
{
    public ForegroundWindowSnapshot(
        nint hwnd,
        int processId,
        string processName,
        string executablePath,
        string windowTitle,
        DateTimeOffset timestampUtc)
    {
        Hwnd = hwnd;
        ProcessId = processId;
        ProcessName = RequiredText.Ensure(processName, nameof(processName));
        ExecutablePath = RequiredText.Ensure(executablePath, nameof(executablePath));
        WindowTitle = windowTitle;
        TimestampUtc = timestampUtc.ToUniversalTime();
    }

    public nint Hwnd { get; }

    public int ProcessId { get; }

    public string ProcessName { get; }

    public string ExecutablePath { get; }

    public string WindowTitle { get; }

    public DateTimeOffset TimestampUtc { get; }

    public bool HasSameFocusIdentity(ForegroundWindowSnapshot other)
        => Hwnd == other.Hwnd &&
           ProcessId == other.ProcessId &&
           string.Equals(ProcessName, other.ProcessName, StringComparison.OrdinalIgnoreCase);
}
