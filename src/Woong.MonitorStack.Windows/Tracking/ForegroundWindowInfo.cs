namespace Woong.MonitorStack.Windows.Tracking;

public sealed record ForegroundWindowInfo
{
    public ForegroundWindowInfo(
        nint hwnd,
        int processId,
        string processName,
        string executablePath,
        string windowTitle)
    {
        Hwnd = hwnd;
        ProcessId = processId;
        ProcessName = RequiredText.Ensure(processName, nameof(processName));
        ExecutablePath = RequiredText.Ensure(executablePath, nameof(executablePath));
        WindowTitle = windowTitle;
    }

    public nint Hwnd { get; }

    public int ProcessId { get; }

    public string ProcessName { get; }

    public string ExecutablePath { get; }

    public string WindowTitle { get; }
}
