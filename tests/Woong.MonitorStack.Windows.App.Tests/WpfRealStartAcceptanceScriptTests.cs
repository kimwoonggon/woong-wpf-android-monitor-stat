using System.IO;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class WpfRealStartAcceptanceScriptTests
{
    [Fact]
    public void RealStartScript_DocumentsPrivacyWarningAndSafeDefaults()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-wpf-real-start-acceptance.ps1");

        Assert.True(File.Exists(scriptPath), "RealStart acceptance script must exist.");
        string script = File.ReadAllText(scriptPath);

        Assert.Contains("This will observe foreground window metadata for local testing.", script);
        Assert.Contains("It will not record keystrokes.", script);
        Assert.Contains("It will not capture screen contents.", script);
        Assert.Contains("temp DB", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("AllowServerSync", script);
        Assert.Contains("WOONG_MONITOR_LOCAL_DB", script);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Woong.MonitorStack.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
