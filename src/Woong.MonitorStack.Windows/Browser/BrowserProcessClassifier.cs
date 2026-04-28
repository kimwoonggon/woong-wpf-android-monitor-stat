namespace Woong.MonitorStack.Windows.Browser;

public sealed class BrowserProcessClassifier : IBrowserProcessClassifier
{
    private static readonly IReadOnlyDictionary<string, string> BrowserNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["chrome.exe"] = "Chrome",
            ["msedge.exe"] = "Microsoft Edge",
            ["firefox.exe"] = "Firefox",
            ["brave.exe"] = "Brave"
        };

    public BrowserProcessClassification Classify(string? processName)
    {
        string? normalized = NormalizeProcessName(processName);
        if (normalized is null)
        {
            return BrowserProcessClassification.NonBrowser;
        }

        return BrowserNames.TryGetValue(normalized, out string? browserName)
            ? new BrowserProcessClassification(true, browserName)
            : BrowserProcessClassification.NonBrowser;
    }

    private static string? NormalizeProcessName(string? processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return null;
        }

        string normalized = processName.Trim();
        return normalized.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? normalized
            : $"{normalized}.exe";
    }
}
