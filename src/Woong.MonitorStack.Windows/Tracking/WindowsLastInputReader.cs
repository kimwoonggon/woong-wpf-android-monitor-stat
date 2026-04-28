using System.Runtime.InteropServices;

namespace Woong.MonitorStack.Windows.Tracking;

public sealed class WindowsLastInputReader : ILastInputReader
{
    public DateTimeOffset ReadLastInputAtUtc(DateTimeOffset nowUtc)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Last input collection requires Windows.");
        }

        var info = new LastInputInfo
        {
            CbSize = (uint)Marshal.SizeOf<LastInputInfo>()
        };

        if (!NativeMethods.GetLastInputInfo(ref info))
        {
            throw new InvalidOperationException("Unable to read last input time.");
        }

        var idleMilliseconds = Environment.TickCount64 - info.DwTime;
        return nowUtc.ToUniversalTime().Subtract(TimeSpan.FromMilliseconds(idleMilliseconds));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint CbSize;
        public uint DwTime;
    }

    private static partial class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetLastInputInfo(ref LastInputInfo info);
    }
}
