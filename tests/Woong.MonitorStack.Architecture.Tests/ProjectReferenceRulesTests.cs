using System.Reflection;
using System.Xml.Linq;
using NetArchTest.Rules;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Server.Summaries;
using Woong.MonitorStack.Windows.App;
using Woong.MonitorStack.Windows.Presentation.Dashboard;
using Woong.MonitorStack.Windows.Tracking;

namespace Woong.MonitorStack.Architecture.Tests;

public sealed class ProjectReferenceRulesTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    public static TheoryData<string, string[]> ProductionProjectRules =>
        new()
        {
            {
                "src/Woong.MonitorStack.Domain/Woong.MonitorStack.Domain.csproj",
                [
                    "Woong.MonitorStack.Windows",
                    "Woong.MonitorStack.Windows.App",
                    "Woong.MonitorStack.Windows.Presentation",
                    "Woong.MonitorStack.Server"
                ]
            },
            {
                "src/Woong.MonitorStack.Windows.Presentation/Woong.MonitorStack.Windows.Presentation.csproj",
                [
                    "Woong.MonitorStack.Windows.App",
                    "Woong.MonitorStack.Windows",
                    "Woong.MonitorStack.Server"
                ]
            },
            {
                "src/Woong.MonitorStack.Windows/Woong.MonitorStack.Windows.csproj",
                [
                    "Woong.MonitorStack.Windows.App",
                    "Woong.MonitorStack.Server"
                ]
            },
            {
                "src/Woong.MonitorStack.Server/Woong.MonitorStack.Server.csproj",
                [
                    "Woong.MonitorStack.Windows",
                    "Woong.MonitorStack.Windows.App",
                    "Woong.MonitorStack.Windows.Presentation"
                ]
            }
        };

    [Theory]
    [MemberData(nameof(ProductionProjectRules))]
    public void ProductionProjects_DoNotReferenceForbiddenProjects(
        string projectPath,
        string[] forbiddenProjectNameFragments)
    {
        XDocument project = LoadProject(projectPath);
        string[] projectReferences = project
            .Descendants("ProjectReference")
            .Select(reference => NormalizePath(reference.Attribute("Include")?.Value ?? ""))
            .ToArray();

        foreach (string forbiddenFragment in forbiddenProjectNameFragments)
        {
            Assert.DoesNotContain(projectReferences, reference => reference.Contains(forbiddenFragment, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void DomainAssembly_DoesNotDependOnPlatformInfrastructureOrUi()
    {
        TestResult result = Types
            .InAssembly(typeof(FocusSession).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Woong.MonitorStack.Windows",
                "Woong.MonitorStack.Windows.App",
                "Woong.MonitorStack.Windows.Presentation",
                "Woong.MonitorStack.Server",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "System.Windows",
                "LiveChartsCore")
            .GetResult();

        AssertArchitectureResult(result);
    }

    [Fact]
    public void PresentationAssembly_DoesNotDependOnAppServerWpfOrInfrastructure()
    {
        TestResult result = Types
            .InAssembly(typeof(DashboardViewModel).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Woong.MonitorStack.Windows.App",
                "Woong.MonitorStack.Server",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "System.Windows",
                "Microsoft.Win32",
                "System.Net.Http")
            .GetResult();

        AssertArchitectureResult(result);
    }

    [Fact]
    public void WindowsInfrastructure_DoesNotDependOnServerOrWpfApp()
    {
        TestResult result = Types
            .InAssembly(typeof(TrackingPoller).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Woong.MonitorStack.Server",
                "Woong.MonitorStack.Windows.App",
                "System.Windows")
            .GetResult();

        AssertArchitectureResult(result);
    }

    [Fact]
    public void Server_DoesNotDependOnWindowsProjects()
    {
        TestResult result = Types
            .InAssembly(typeof(AppFamilyMapper).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Woong.MonitorStack.Windows",
                "Woong.MonitorStack.Windows.App",
                "Woong.MonitorStack.Windows.Presentation")
            .GetResult();

        AssertArchitectureResult(result);
    }

    [Fact]
    public void WindowsApp_IsTheOnlyProductionWpfCompositionRoot()
    {
        string[] productionProjects = Directory
            .EnumerateFiles(Path.Combine(RepositoryRoot, "src"), "*.csproj", SearchOption.AllDirectories)
            .ToArray();

        string[] wpfProjects = productionProjects
            .Where(projectPath => UsesWpf(projectPath))
            .Select(projectPath => Path.GetRelativePath(RepositoryRoot, projectPath).Replace('\\', '/'))
            .ToArray();

        Assert.Equal(["src/Woong.MonitorStack.Windows.App/Woong.MonitorStack.Windows.App.csproj"], wpfProjects);
    }

    [Fact]
    public void TestProjects_AreExcludedFromProductionDependencyChecks()
    {
        string[] checkedProjects = Directory
            .EnumerateFiles(Path.Combine(RepositoryRoot, "src"), "*.csproj", SearchOption.AllDirectories)
            .Select(projectPath => Path.GetRelativePath(RepositoryRoot, projectPath).Replace('\\', '/'))
            .ToArray();

        Assert.DoesNotContain(checkedProjects, projectPath => projectPath.StartsWith("tests/", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PresentationSource_DoesNotUseDirectPlatformApis()
    {
        string[] forbiddenTokens =
        [
            "System.Windows",
            "MessageBox",
            "DllImport",
            "LibraryImport",
            "Microsoft.Win32",
            "System.IO",
            "HttpClient"
        ];
        string[] sourceFiles = Directory
            .EnumerateFiles(
                Path.Combine(RepositoryRoot, "src", "Woong.MonitorStack.Windows.Presentation"),
                "*.cs",
                SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (string sourceFile in sourceFiles)
        {
            string source = File.ReadAllText(sourceFile);
            foreach (string forbiddenToken in forbiddenTokens)
            {
                Assert.DoesNotContain(forbiddenToken, source, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void AppStartup_DoesNotManuallyConstructDashboardDependencies()
    {
        string source = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "src",
            "Woong.MonitorStack.Windows.App",
            "App.xaml.cs"));

        Assert.DoesNotContain("new DashboardViewModel", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new EmptyDashboardDataSource", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new SystemDashboardClock", source, StringComparison.Ordinal);
        Assert.Contains("Host", source, StringComparison.Ordinal);
    }

    [Fact]
    public void DomainProject_HasNoForbiddenPackages()
    {
        XDocument project = LoadProject("src/Woong.MonitorStack.Domain/Woong.MonitorStack.Domain.csproj");
        string[] packages = PackageReferences(project);

        Assert.DoesNotContain(packages, package => package.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(packages, package => package.StartsWith("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(packages, package => package.StartsWith("LiveChartsCore", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PresentationProject_HasNoInfrastructureOrWpfPackages()
    {
        XDocument project = LoadProject("src/Woong.MonitorStack.Windows.Presentation/Woong.MonitorStack.Windows.Presentation.csproj");
        string[] packages = PackageReferences(project);

        Assert.DoesNotContain(packages, package => package.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(packages, package => package.StartsWith("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase));
        Assert.False(UsesWpf("src/Woong.MonitorStack.Windows.Presentation/Woong.MonitorStack.Windows.Presentation.csproj"));
    }

    private static XDocument LoadProject(string relativePath)
        => XDocument.Load(Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string[] PackageReferences(XDocument project)
        => project
            .Descendants("PackageReference")
            .Select(reference => reference.Attribute("Include")?.Value ?? "")
            .Where(package => package.Length > 0)
            .ToArray();

    private static bool UsesWpf(string projectPath)
    {
        string fullPath = Path.IsPathRooted(projectPath)
            ? projectPath
            : Path.Combine(RepositoryRoot, projectPath.Replace('/', Path.DirectorySeparatorChar));
        XDocument project = XDocument.Load(fullPath);

        return project
            .Descendants("UseWPF")
            .Any(value => string.Equals(value.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizePath(string path)
        => path.Replace('\\', '/');

    private static void AssertArchitectureResult(TestResult result)
    {
        if (result.IsSuccessful)
        {
            return;
        }

        string failingTypes = string.Join(
            Environment.NewLine,
            result.FailingTypes?
                .Select(type => type.FullName)
                .OfType<string>()
                .Order(StringComparer.Ordinal) ?? Enumerable.Empty<string>());
        Assert.Fail($"Architecture rule failed. Failing types:{Environment.NewLine}{failingTypes}");
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
