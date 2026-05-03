using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Woong.MonitorStack.Windows.Sync;

public sealed class DpapiWindowsUserDataProtector : IWindowsUserDataProtector
{
    private const int CryptProtectUiForbidden = 0x1;
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Woong.MonitorStack.Windows.Sync.DeviceToken.v1");

    public byte[] Protect(byte[] plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        EnsureWindows();

        return ExecuteDpapi(plaintext, protect: true);
    }

    public byte[] Unprotect(byte[] protectedData)
    {
        ArgumentNullException.ThrowIfNull(protectedData);
        EnsureWindows();

        return ExecuteDpapi(protectedData, protect: false);
    }

    private static byte[] ExecuteDpapi(byte[] input, bool protect)
    {
        DataBlob inputBlob = CreateBlob(input);
        DataBlob entropyBlob = CreateBlob(Entropy);
        DataBlob outputBlob = default;

        try
        {
            bool succeeded = protect
                ? CryptProtectData(
                    ref inputBlob,
                    "Woong Monitor Stack sync device token",
                    ref entropyBlob,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    CryptProtectUiForbidden,
                    out outputBlob)
                : CryptUnprotectData(
                    ref inputBlob,
                    IntPtr.Zero,
                    ref entropyBlob,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    CryptProtectUiForbidden,
                    out outputBlob);

            if (!succeeded)
            {
                throw new CryptographicException(Marshal.GetLastWin32Error());
            }

            return ReadBlob(outputBlob);
        }
        finally
        {
            FreeInputBlob(inputBlob);
            FreeInputBlob(entropyBlob);
            FreeOutputBlob(outputBlob);
        }
    }

    private static DataBlob CreateBlob(byte[] data)
    {
        if (data.Length == 0)
        {
            return default;
        }

        IntPtr buffer = Marshal.AllocHGlobal(data.Length);
        Marshal.Copy(data, 0, buffer, data.Length);

        return new DataBlob(data.Length, buffer);
    }

    private static byte[] ReadBlob(DataBlob blob)
    {
        if (blob.Length <= 0 || blob.Data == IntPtr.Zero)
        {
            return [];
        }

        byte[] output = new byte[blob.Length];
        Marshal.Copy(blob.Data, output, 0, blob.Length);
        return output;
    }

    private static void FreeInputBlob(DataBlob blob)
    {
        if (blob.Data != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(blob.Data);
        }
    }

    private static void FreeOutputBlob(DataBlob blob)
    {
        if (blob.Data != IntPtr.Zero)
        {
            _ = LocalFree(blob.Data);
        }
    }

    private static void EnsureWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Windows sync token protection requires Windows user data protection.");
        }
    }

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CryptProtectData(
        ref DataBlob input,
        string? description,
        ref DataBlob optionalEntropy,
        IntPtr reserved,
        IntPtr promptStruct,
        int flags,
        out DataBlob output);

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CryptUnprotectData(
        ref DataBlob input,
        IntPtr description,
        ref DataBlob optionalEntropy,
        IntPtr reserved,
        IntPtr promptStruct,
        int flags,
        out DataBlob output);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr memory);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct DataBlob
    {
        public DataBlob(int length, IntPtr data)
        {
            Length = length;
            Data = data;
        }

        public int Length { get; }

        public IntPtr Data { get; }
    }
}
