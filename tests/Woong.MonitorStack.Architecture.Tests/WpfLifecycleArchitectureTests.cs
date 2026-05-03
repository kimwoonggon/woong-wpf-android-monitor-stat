using System.Text.RegularExpressions;

namespace Woong.MonitorStack.Architecture.Tests;

public sealed class WpfLifecycleArchitectureTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();
    private static readonly Regex RawTrackingTickerFieldPattern = new(
        @"\bprivate\s+(?:readonly\s+)?[\w.]*TrackingTicker\??\s+_\w+",
        RegexOptions.Compiled);

    [Fact]
    public void ReferenceRules_DocumentMainWindowLifecycleBoundary()
    {
        string rules = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "docs",
            "architecture",
            "reference-rules.md"));

        Assert.Contains("MainWindow", rules, StringComparison.Ordinal);
        Assert.Contains("thin WPF shell", rules, StringComparison.Ordinal);
        Assert.Contains("StartTrackingCommand", rules, StringComparison.Ordinal);
        Assert.Contains("PollTrackingCommand", rules, StringComparison.Ordinal);
        Assert.Contains("StopTrackingCommand", rules, StringComparison.Ordinal);
        Assert.Contains("ITrackingTicker", rules, StringComparison.Ordinal);
        Assert.Contains("lifecycle coordinator", rules, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MainWindow_CodeBehindDoesNotOrchestrateDashboardTrackingCommands()
    {
        string source = ReadMainWindowSourceWithoutComments();
        string[] forbiddenCommandTokens =
        [
            "StartTrackingCommand",
            "PollTrackingCommand",
            "StopTrackingCommand"
        ];

        string[] violations = forbiddenCommandTokens
            .Where(token => source.Contains(token, StringComparison.Ordinal))
            .Select(token => $"MainWindow.xaml.cs directly references `{token}`")
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void MainWindow_CodeBehindDoesNotOwnRawTrackingTickerFields()
    {
        string source = ReadMainWindowSourceWithoutComments();
        string[] violations = RawTrackingTickerFieldPattern
            .Matches(source)
            .Select(match => $"MainWindow.xaml.cs owns raw tracking ticker field `{match.Value}`")
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void WindowsAppSourceOutsideMainWindowOwnsTrackingCommandAndTickerOrchestration()
    {
        string appRoot = Path.Combine(RepositoryRoot, "src", "Woong.MonitorStack.Windows.App");
        string mainWindowPath = Path.Combine(appRoot, "MainWindow.xaml.cs");
        string orchestrationSource = string.Join(
            Environment.NewLine,
            Directory
                .EnumerateFiles(appRoot, "*.cs", SearchOption.AllDirectories)
                .Where(path => !IsIgnoredPath(path))
                .Where(path => !string.Equals(path, mainWindowPath, StringComparison.OrdinalIgnoreCase))
                .Select(path => RemoveComments(File.ReadAllText(path))));

        Assert.Contains("StartTrackingCommand", orchestrationSource, StringComparison.Ordinal);
        Assert.Contains("PollTrackingCommand", orchestrationSource, StringComparison.Ordinal);
        Assert.Contains("StopTrackingCommand", orchestrationSource, StringComparison.Ordinal);
        Assert.Contains("ITrackingTicker", orchestrationSource, StringComparison.Ordinal);
    }

    private static string ReadMainWindowSourceWithoutComments()
        => RemoveComments(File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "src",
            "Woong.MonitorStack.Windows.App",
            "MainWindow.xaml.cs")));

    private static string RemoveComments(string source)
    {
        string withoutBlockComments = Regex.Replace(source, @"/\*.*?\*/", "", RegexOptions.Singleline);
        return Regex.Replace(withoutBlockComments, @"//.*?$", "", RegexOptions.Multiline);
    }

    private static bool IsIgnoredPath(string path)
    {
        string normalized = path.Replace('\\', '/');

        return normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase);
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
