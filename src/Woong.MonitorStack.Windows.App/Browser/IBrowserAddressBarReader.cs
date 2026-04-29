using Woong.MonitorStack.Windows.Tracking;

namespace Woong.MonitorStack.Windows.App.Browser;

public interface IBrowserAddressBarReader
{
    string? TryReadAddress(ForegroundWindowSnapshot foregroundWindow);
}
