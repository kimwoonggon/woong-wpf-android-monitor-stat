using System.IO;
using System.Text.RegularExpressions;

namespace Woong.MonitorStack.Windows.App.Tests.Browser;

public sealed class BrowserMetadataOnlySourceGuardTests
{
    [Fact]
    public void BrowserUiAutomationFallback_DoesNotUseForbiddenContentCaptureApis()
    {
        string repoRoot = FindRepositoryRoot();
        string browserSourceRoot = Path.Combine(repoRoot, "src", "Woong.MonitorStack.Windows.App", "Browser");

        Assert.True(Directory.Exists(browserSourceRoot), "Browser fallback source folder must exist.");
        string source = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(browserSourceRoot, "*.cs", SearchOption.AllDirectories)
                .Order(StringComparer.Ordinal)
                .Select(File.ReadAllText));

        Assert.Contains("WindowsUiAutomationAddressBarReader", source);
        Assert.Contains("IBrowserAddressBarReader", source);
        Assert.Contains("ValuePattern.Pattern", source);

        string[] forbiddenContentCapturePatterns =
        [
            @"\bTextPattern\b",
            @"\bTextPatternRange\b",
            @"\bClipboard\b",
            @"GetText\s*\(",
            @"GetSelection\s*\(",
            @"GetVisibleRanges\s*\(",
            @"DocumentRange",
            @"SendKeys",
            @"GetAsyncKeyState",
            @"GetKeyState",
            @"password",
            @"pageContent",
            @"page_content",
            @"PrintWindow",
            @"BitBlt",
            @"CopyFromScreen",
            @"ScreenCapture",
            @"Screenshot"
        ];

        foreach (string forbiddenPattern in forbiddenContentCapturePatterns)
        {
            Assert.DoesNotMatch(
                new Regex(forbiddenPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
                source);
        }
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
