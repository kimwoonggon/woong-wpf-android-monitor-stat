using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Windows.Browser;

public interface IWebSessionizer
{
    IReadOnlyList<WebSession> Apply(BrowserActivitySnapshot snapshot);
}
