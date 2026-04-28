using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Woong.MonitorStack.Windows.Tracking;

public sealed class WindowsForegroundWindowReader : IForegroundWindowReader
{
    public ForegroundWindowInfo ReadForegroundWindow()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Foreground window collection requires Windows.");
        }

        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == nint.Zero)
        {
            throw new InvalidOperationException("No foreground window is available.");
        }

        _ = NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
        using var process = Process.GetProcessById((int)processId);

        return new ForegroundWindowInfo(
            hwnd,
            (int)processId,
            process.ProcessName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ? process.ProcessName
                : $"{process.ProcessName}.exe",
            TryGetExecutablePath(process),
            ReadWindowTitle(hwnd));
    }

    private static string ReadWindowTitle(nint hwnd)
    {
        var length = NativeMethods.GetWindowTextLength(hwnd);
        if (length <= 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(length + 1);
        _ = NativeMethods.GetWindowText(hwnd, builder, builder.Capacity);

        return builder.ToString();
    }

    private static string TryGetExecutablePath(Process process)
    {
        try
        {
            return process.MainModule?.FileName ?? process.ProcessName;
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException or System.ComponentModel.Win32Exception)
        {
            return process.ProcessName;
        }
    }

    private static partial class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern nint GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(nint hWnd, out uint processId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetWindowText(nint hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetWindowTextLength(nint hWnd);
    }
}
