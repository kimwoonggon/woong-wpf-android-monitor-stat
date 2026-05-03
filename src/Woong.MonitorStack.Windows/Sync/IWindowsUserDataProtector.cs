namespace Woong.MonitorStack.Windows.Sync;

public interface IWindowsUserDataProtector
{
    byte[] Protect(byte[] plaintext);

    byte[] Unprotect(byte[] protectedData);
}
