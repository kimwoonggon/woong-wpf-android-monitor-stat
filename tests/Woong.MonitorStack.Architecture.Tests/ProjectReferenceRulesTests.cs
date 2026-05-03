using System.Reflection;
using System.Xml.Linq;
using System.Xml;
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
    private static readonly string[] ColorTargetProperties =
    [
        "Background",
        "BorderBrush",
        "CaretBrush",
        "Color",
        "Fill",
        "Foreground",
        "SelectionBrush",
        "SelectionTextBrush",
        "Stroke"
    ];

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
                    "Woong.MonitorStack.Windows.Presentation",
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
    public void WindowsInfrastructure_DoesNotDependOnPresentationServerOrWpfApp()
    {
        TestResult result = Types
            .InAssembly(typeof(TrackingPoller).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Woong.MonitorStack.Windows.Presentation",
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
    public void AppResources_MergeEverySharedStyleDictionaryAtApplicationRoot()
    {
        XDocument appXaml = XDocument.Load(Path.Combine(
            RepositoryRoot,
            "src",
            "Woong.MonitorStack.Windows.App",
            "App.xaml"));

        string[] expectedStyleDictionaries = Directory
            .EnumerateFiles(
                Path.Combine(RepositoryRoot, "src", "Woong.MonitorStack.Windows.App", "Styles"),
                "*.xaml",
                SearchOption.TopDirectoryOnly)
            .Select(path => $"Styles/{Path.GetFileName(path)}")
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        string[] mergedDictionaries = appXaml
            .Descendants()
            .Where(element => element.Name.LocalName == "ResourceDictionary")
            .Select(element => element.Attribute("Source")?.Value)
            .OfType<string>()
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Equal(expectedStyleDictionaries, mergedDictionaries);
    }

    [Fact]
    public void MainWindow_DoesNotDuplicateApplicationLevelStyleDictionaries()
    {
        XDocument mainWindowXaml = XDocument.Load(Path.Combine(
            RepositoryRoot,
            "src",
            "Woong.MonitorStack.Windows.App",
            "MainWindow.xaml"));

        string[] mergedDictionaries = mainWindowXaml
            .Descendants()
            .Where(element => element.Name.LocalName == "ResourceDictionary")
            .Select(element => element.Attribute("Source")?.Value)
            .OfType<string>()
            .ToArray();

        Assert.Empty(mergedDictionaries);
    }

    [Fact]
    public void MainWindow_XamlRemainsThinDashboardShell()
    {
        XDocument mainWindowXaml = XDocument.Load(Path.Combine(
            RepositoryRoot,
            "src",
            "Woong.MonitorStack.Windows.App",
            "MainWindow.xaml"));

        XElement mainWindow = Assert.Single(mainWindowXaml.Elements());
        XElement contentGrid = Assert.Single(
            mainWindow.Elements(),
            element => element.Name.LocalName != "Window.Resources");
        XElement dashboardView = Assert.Single(contentGrid.Elements());
        string[] forbiddenInlineDashboardControls =
        [
            "HeaderStatusBar",
            "ControlBar",
            "CurrentFocusPanel",
            "SummaryCardsPanel",
            "ChartsPanel",
            "DetailsTabsPanel",
            "DataGrid",
            "TabControl",
            "CartesianChart",
            "PieChart"
        ];
        string[] mainWindowElementNames = mainWindow
            .Descendants()
            .Select(element => element.Name.LocalName)
            .ToArray();

        Assert.Equal("Grid", contentGrid.Name.LocalName);
        Assert.Equal("DashboardView", dashboardView.Name.LocalName);
        foreach (string forbiddenControl in forbiddenInlineDashboardControls)
        {
            Assert.DoesNotContain(forbiddenControl, mainWindowElementNames);
        }
    }

    [Fact]
    public void WpfXaml_DoesNotUseColorLiteralsOutsideColorsDictionary()
    {
        string wpfAppRoot = Path.Combine(RepositoryRoot, "src", "Woong.MonitorStack.Windows.App");
        string colorsDictionaryPath = Path.GetFullPath(Path.Combine(wpfAppRoot, "Styles", "Colors.xaml"));

        string[] violations = Directory
            .EnumerateFiles(wpfAppRoot, "*.xaml", SearchOption.AllDirectories)
            .Where(path => !string.Equals(Path.GetFullPath(path), colorsDictionaryPath, StringComparison.OrdinalIgnoreCase))
            .SelectMany(FindColorLiteralViolations)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void WpfViewsAndControls_DoNotDefineLocalStyles()
    {
        string[] xamlRoots =
        [
            Path.Combine(RepositoryRoot, "src", "Woong.MonitorStack.Windows.App", "Views"),
            Path.Combine(RepositoryRoot, "src", "Woong.MonitorStack.Windows.App", "Controls")
        ];

        string[] violations = xamlRoots
            .SelectMany(root => Directory.EnumerateFiles(root, "*.xaml", SearchOption.AllDirectories))
            .SelectMany(FindLocalStyleViolations)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void DashboardView_LaysOutDashboardSectionsAsDirectGridRows()
    {
        XDocument dashboardViewXaml = XDocument.Load(Path.Combine(
            RepositoryRoot,
            "src",
            "Woong.MonitorStack.Windows.App",
            "Views",
            "DashboardView.xaml"));

        XElement layoutGrid = dashboardViewXaml
            .Descendants()
            .Single(element => element.Name.LocalName == "ScrollViewer")
            .Elements()
            .Single(element => element.Name.LocalName == "Grid");
        string[] directDashboardSections = layoutGrid
            .Elements()
            .Where(element => element.Name.LocalName != "Grid.RowDefinitions")
            .Select(element => element.Name.LocalName)
            .ToArray();

        Assert.Equal(
            [
                "HeaderStatusBar",
                "ControlBar",
                "CurrentFocusPanel",
                "SummaryCardsPanel",
                "ChartsPanel",
                "DetailsTabsPanel"
            ],
            directDashboardSections);
        Assert.Equal(6, layoutGrid.Elements().Single(element => element.Name.LocalName == "Grid.RowDefinitions").Elements().Count());
        Assert.Equal("4", GetXamlAttribute(layoutGrid.Elements().Single(element => element.Name.LocalName == "ChartsPanel"), "Grid.Row"));
        Assert.Equal("5", GetXamlAttribute(layoutGrid.Elements().Single(element => element.Name.LocalName == "DetailsTabsPanel"), "Grid.Row"));
    }

    [Fact]
    public void DashboardView_ComposesReusableSectionsInsideVerticalScrollViewer()
    {
        XDocument dashboardViewXaml = XDocument.Load(Path.Combine(
            RepositoryRoot,
            "src",
            "Woong.MonitorStack.Windows.App",
            "Views",
            "DashboardView.xaml"));

        XElement dashboardView = Assert.Single(dashboardViewXaml.Elements());
        XElement scrollViewer = Assert.Single(
            dashboardView.Elements(),
            element => element.Name.LocalName == "ScrollViewer");
        XElement layoutGrid = Assert.Single(scrollViewer.Elements());
        string[] requiredSectionControls =
        [
            "HeaderStatusBar",
            "ControlBar",
            "CurrentFocusPanel",
            "SummaryCardsPanel",
            "ChartsPanel",
            "DetailsTabsPanel"
        ];
        string[] directCompositionControls = layoutGrid
            .Elements()
            .Where(element => element.Name.LocalName != "Grid.RowDefinitions")
            .Select(element => element.Name.LocalName)
            .ToArray();
        string[] unexpectedInlineElements = layoutGrid
            .Descendants()
            .Select(element => element.Name.LocalName)
            .Where(elementName => elementName != "Grid.RowDefinitions" && elementName != "RowDefinition")
            .Except(requiredSectionControls)
            .ToArray();

        Assert.Equal("Auto", GetXamlAttribute(scrollViewer, "VerticalScrollBarVisibility"));
        Assert.Equal(requiredSectionControls, directCompositionControls);
        Assert.Empty(unexpectedInlineElements);
    }

    [Fact]
    public void DashboardView_DirectSectionsExposeStableAutomationIds()
    {
        XDocument dashboardViewXaml = XDocument.Load(Path.Combine(
            RepositoryRoot,
            "src",
            "Woong.MonitorStack.Windows.App",
            "Views",
            "DashboardView.xaml"));

        XElement layoutGrid = dashboardViewXaml
            .Descendants()
            .Single(element => element.Name.LocalName == "ScrollViewer")
            .Elements()
            .Single(element => element.Name.LocalName == "Grid");
        Dictionary<string, string?> automationIdsBySection = layoutGrid
            .Elements()
            .Where(element => element.Name.LocalName != "Grid.RowDefinitions")
            .ToDictionary(
                element => element.Name.LocalName,
                element => GetXamlAttribute(element, "AutomationProperties.AutomationId"));

        Assert.Equal("HeaderArea", automationIdsBySection["HeaderStatusBar"]);
        Assert.Equal("PeriodSelector", automationIdsBySection["ControlBar"]);
        Assert.Equal("CurrentActivityPanel", automationIdsBySection["CurrentFocusPanel"]);
        Assert.Equal("SummaryCardsContainer", automationIdsBySection["SummaryCardsPanel"]);
        Assert.Equal("ChartArea", automationIdsBySection["ChartsPanel"]);
        Assert.Equal("DetailsTabsPanel", automationIdsBySection["DetailsTabsPanel"]);
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

    private static IEnumerable<string> FindColorLiteralViolations(string xamlPath)
    {
        XDocument xaml = XDocument.Load(xamlPath, LoadOptions.SetLineInfo);

        if (xaml.Root is null)
        {
            yield break;
        }

        foreach (XElement element in xaml.Root.DescendantsAndSelf())
        {
            foreach (XAttribute attribute in element.Attributes())
            {
                if (attribute.IsNamespaceDeclaration)
                {
                    continue;
                }

                string value = attribute.Value.Trim();
                if (IsHexColorLiteral(value)
                    || (IsColorTargetAttribute(element, attribute) && IsNamedColorLiteral(value)))
                {
                    yield return FormatXamlViolation(xamlPath, attribute, value);
                }
            }
        }
    }

    private static bool IsColorTargetAttribute(XElement element, XAttribute attribute)
    {
        string attributeName = attribute.Name.LocalName;
        if (ColorTargetProperties.Contains(attributeName, StringComparer.Ordinal))
        {
            return true;
        }

        if (element.Name.LocalName != "Setter" || attributeName != "Value")
        {
            return false;
        }

        string? setterProperty = element
            .Attributes()
            .FirstOrDefault(candidate => candidate.Name.LocalName == "Property")
            ?.Value;

        return IsColorTargetProperty(setterProperty);
    }

    private static bool IsColorTargetProperty(string? property)
    {
        if (string.IsNullOrWhiteSpace(property))
        {
            return false;
        }

        string propertyName = property.Trim();
        if (propertyName.StartsWith('(') && propertyName.EndsWith(')'))
        {
            propertyName = propertyName[1..^1];
        }

        int qualifierIndex = propertyName.LastIndexOf('.');
        if (qualifierIndex >= 0)
        {
            propertyName = propertyName[(qualifierIndex + 1)..];
        }

        return ColorTargetProperties.Contains(propertyName, StringComparer.Ordinal);
    }

    private static bool IsHexColorLiteral(string value)
    {
        if (!value.StartsWith('#'))
        {
            return false;
        }

        string hex = value[1..];
        return hex.Length is 3 or 4 or 6 or 8
            && hex.All(Uri.IsHexDigit);
    }

    private static bool IsNamedColorLiteral(string value)
        => value.Length > 0
            && value.All(char.IsAsciiLetter);

    private static string FormatXamlViolation(string xamlPath, XAttribute attribute, string value)
    {
        string relativePath = Path.GetRelativePath(RepositoryRoot, xamlPath).Replace('\\', '/');
        string lineSuffix = attribute is IXmlLineInfo lineInfo && lineInfo.HasLineInfo()
            ? $":{lineInfo.LineNumber}"
            : "";

        return $"{relativePath}{lineSuffix}: `{attribute.Name.LocalName}` uses color literal `{value}`";
    }

    private static IEnumerable<string> FindLocalStyleViolations(string xamlPath)
    {
        XDocument xaml = XDocument.Load(xamlPath, LoadOptions.SetLineInfo);

        foreach (XElement style in xaml.Descendants().Where(element => element.Name.LocalName == "Style"))
        {
            string relativePath = Path.GetRelativePath(RepositoryRoot, xamlPath).Replace('\\', '/');
            string? key = style
                .Attributes()
                .FirstOrDefault(attribute => attribute.Name.LocalName == "Key")
                ?.Value;
            string lineSuffix = style is IXmlLineInfo lineInfo && lineInfo.HasLineInfo()
                ? $":{lineInfo.LineNumber}"
                : "";
            string keySuffix = string.IsNullOrWhiteSpace(key)
                ? ""
                : $" `{key}`";

            yield return $"{relativePath}{lineSuffix}: local Style{keySuffix} belongs in App/Styles dictionaries";
        }
    }

    private static string? GetXamlAttribute(XElement element, string attributeName)
        => element
            .Attributes()
            .FirstOrDefault(attribute => attribute.Name.LocalName == attributeName)
            ?.Value;

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
