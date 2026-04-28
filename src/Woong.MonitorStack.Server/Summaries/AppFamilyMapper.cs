namespace Woong.MonitorStack.Server.Summaries;

public static class AppFamilyMapper
{
    private static readonly Dictionary<string, string> KnownFamilyByPlatformApp = new(StringComparer.OrdinalIgnoreCase)
    {
        ["chrome.exe"] = "Chrome",
        ["com.android.chrome"] = "Chrome",
        ["code.exe"] = "VS Code",
        ["com.microsoft.vscode"] = "VS Code",
        ["slack.exe"] = "Slack",
        ["com.slack"] = "Slack"
    };

    public static string GetFamilyLabel(string platformAppKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platformAppKey);

        return KnownFamilyByPlatformApp.GetValueOrDefault(platformAppKey, platformAppKey);
    }
}
