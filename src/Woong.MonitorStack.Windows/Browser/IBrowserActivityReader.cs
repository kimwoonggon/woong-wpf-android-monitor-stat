using Woong.MonitorStack.Windows.Tracking;

namespace Woong.MonitorStack.Windows.Browser;

public interface IBrowserActivityReader
{
    BrowserActivitySnapshot? TryRead(ForegroundWindowSnapshot foregroundWindow);
}
