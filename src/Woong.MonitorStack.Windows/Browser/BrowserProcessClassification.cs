namespace Woong.MonitorStack.Windows.Browser;

public sealed record BrowserProcessClassification(bool IsBrowser, string? BrowserName)
{
    public static BrowserProcessClassification NonBrowser { get; } = new(false, null);
}
