namespace Woong.MonitorStack.Windows.Tracking;

public interface ILastInputReader
{
    DateTimeOffset ReadLastInputAtUtc(DateTimeOffset nowUtc);
}
