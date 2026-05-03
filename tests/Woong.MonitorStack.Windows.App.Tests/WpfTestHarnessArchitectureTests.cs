using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class WpfTestHarnessArchitectureTests
{
    private static readonly string[] MigratedTestFiles =
    [
        "MainWindowAutomationIdTests.cs",
        "MainWindowTrackingPipelineTests.cs",
        "MainWindowUiExpectationTestHelpers.cs",
        "WindowsAppCompositionTests.cs"
    ];

    private static readonly string[] VisualTraversalMigratedTestFiles =
    [
        "MainWindowAutomationIdTests.cs",
        "MainWindowTrackingPipelineTests.cs",
        "MainWindowUiExpectationTestHelpers.cs"
    ];

    private static readonly Regex[] ForbiddenRawStaHelperPatterns =
    [
        new(@"\bnew\s+Thread\s*\(", RegexOptions.Compiled),
        new(@"\.SetApartmentState\s*\(", RegexOptions.Compiled),
        new(@"\bprivate\s+static\s+void\s+RunOnStaThread\b", RegexOptions.Compiled)
    ];

    private static readonly Regex[] ForbiddenVisualTraversalHelperPatterns =
    [
        new(@"\bprivate\s+static\s+T\s+FindByAutomationId\s*<", RegexOptions.Compiled),
        new(@"\bprivate\s+static\s+T\s+FindVisualDescendant\s*<", RegexOptions.Compiled),
        new(@"\bprivate\s+static\s+IReadOnlyList\s*<\s*T\s*>\s+FindVisualDescendants\s*<", RegexOptions.Compiled),
        new(@"\bprivate\s+static\s+void\s+CollectVisualDescendants\s*<", RegexOptions.Compiled),
        new(@"\bprivate\s+static\s+IEnumerable\s*<\s*DependencyObject\s*>\s+GetChildren\b", RegexOptions.Compiled),
        new(@"\bVisualTreeHelper\.GetChildrenCount\b", RegexOptions.Compiled),
        new(@"\bLogicalTreeHelper\.GetChildren\b", RegexOptions.Compiled)
    ];

    [Fact]
    public void MigratedWpfAppTests_DoNotDuplicateRawStaThreadHelpers()
    {
        string[] violations = MigratedTestFiles
            .SelectMany(fileName => FindForbiddenPatternViolations(fileName, ForbiddenRawStaHelperPatterns, "raw WPF STA helper plumbing"))
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void MigratedWpfAppTests_DoNotDuplicateVisualTraversalHelpers()
    {
        string[] violations = VisualTraversalMigratedTestFiles
            .SelectMany(fileName => FindForbiddenPatternViolations(fileName, ForbiddenVisualTraversalHelperPatterns, "WPF visual traversal helper plumbing"))
            .ToArray();

        Assert.Empty(violations);
    }

    private static IEnumerable<string> FindForbiddenPatternViolations(
        string fileName,
        IEnumerable<Regex> forbiddenPatterns,
        string duplicationDescription)
    {
        string path = Path.Combine(FindRepositoryRoot(), "tests", "Woong.MonitorStack.Windows.App.Tests", fileName);
        string source = RemoveComments(File.ReadAllText(path));

        return forbiddenPatterns
            .Where(pattern => pattern.IsMatch(source))
            .Select(pattern => $"{fileName} duplicates {duplicationDescription}: {pattern}");
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
