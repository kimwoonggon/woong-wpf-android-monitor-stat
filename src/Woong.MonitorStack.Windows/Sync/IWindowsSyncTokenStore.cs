namespace Woong.MonitorStack.Windows.Sync;

public interface IWindowsSyncTokenStore
{
    string? GetDeviceToken();

    void SaveDeviceToken(string deviceToken);

    void DeleteDeviceToken();
}
