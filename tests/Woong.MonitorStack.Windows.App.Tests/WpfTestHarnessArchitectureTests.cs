using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class WpfTestHarnessArchitectureTests
{
    private static readonly Regex[] ForbiddenRawStaHelperPatterns =
    [
        new(@"\bnew\s+Thread\s*\(", RegexOptions.Compiled),
        new(@"\.SetApartmentState\s*\(", RegexOptions.Compiled),
        new(@"\bprivate\s+static\s+void\s+RunOnStaThread\b", RegexOptions.Compiled)
    ];

    [Fact]
    public void MigratedWpfAppTests_DoNotDuplicateRawStaThreadHelpers()
    {
        string[] migratedTestFiles =
        [
            "MainWindowAutomationIdTests.cs",
            "MainWindowTrackingPipelineTests.cs",
            "WindowsAppCompositionTests.cs"
        ];

        string[] violations = migratedTestFiles
            .SelectMany(FindRawStaHelperViolations)
            .ToArray();

        Assert.Empty(violations);
    }

    private static IEnumerable<string> FindRawStaHelperViolations(string fileName)
    {
        string path = Path.Combine(FindRepositoryRoot(), "tests", "Woong.MonitorStack.Windows.App.Tests", fileName);
        string source = RemoveComments(File.ReadAllText(path));

        return ForbiddenRawStaHelperPatterns
            .Where(pattern => pattern.IsMatch(source))
            .Select(pattern => $"{fileName} duplicates raw WPF STA helper plumbing: {pattern}");
    }

    private static string RemoveComments(string source)
    {
        string withoutBlockComments = Regex.Replace(source, @"/\*.*?\*/", "", RegexOptions.Singleline);

        return Regex.Replace(withoutBlockComments, @"//.*?$", "", RegexOptions.Multiline);
    }

    private static string FindRepositoryRoot([CallerFilePath] string sourceFilePath = "")
    {
        DirectoryInfo? current = new(Path.GetDirectoryName(sourceFilePath) ?? AppContext.BaseDirectory);

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
