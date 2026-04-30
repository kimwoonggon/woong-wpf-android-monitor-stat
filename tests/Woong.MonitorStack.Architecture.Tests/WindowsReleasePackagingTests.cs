using System.Xml.Linq;

namespace Woong.MonitorStack.Architecture.Tests;

public sealed class WindowsReleasePackagingTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void MainWindow_XamlExplicitlyShowsWindowInTaskbar()
    {
        XDocument xaml = XDocument.Load(Path.Combine(
            RepositoryRoot,
            "src",
            "Woong.MonitorStack.Windows.App",
            "MainWindow.xaml"));

        XElement window = Assert.Single(xaml.Elements());

        Assert.Equal("True", GetXamlAttribute(window, "ShowInTaskbar"));
    }

    [Fact]
    public void Readme_DocumentsReleaseBuildAndRunCommandsForWindowsApp()
    {
        string readme = File.ReadAllText(Path.Combine(RepositoryRoot, "README.md"));

        Assert.Contains(
            "dotnet build Woong.MonitorStack.sln -c Release --no-restore -m:1 -v minimal",
            readme,
            StringComparison.Ordinal);
        Assert.Contains(
            "dotnet run --configuration Release --project src\\Woong.MonitorStack.Windows.App\\Woong.MonitorStack.Windows.App.csproj",
            readme,
            StringComparison.Ordinal);
    }

    [Fact]
    public void WindowsCiWorkflow_BuildsTestsPublishesAndPackagesReleaseArtifacts()
    {
        string workflowPath = Path.Combine(RepositoryRoot, ".github", "workflows", "windows-wpf-ci.yml");

        Assert.True(File.Exists(workflowPath), "Windows GitHub Actions workflow must exist.");
        string workflow = File.ReadAllText(workflowPath);

        Assert.Contains("runs-on: windows-latest", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet restore Woong.MonitorStack.sln --configfile NuGet.config", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet build Woong.MonitorStack.sln -c Release --no-restore -m:1 -v minimal", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet test Woong.MonitorStack.sln -c Release --no-build -m:1 -v minimal", workflow, StringComparison.Ordinal);
        Assert.Contains("scripts\\package-windows-msix.ps1", workflow, StringComparison.Ordinal);
        Assert.Contains("actions/upload-artifact", workflow, StringComparison.Ordinal);
        Assert.Contains("woong-monitor-windows-app", workflow, StringComparison.Ordinal);
        Assert.Contains("woong-monitor-windows-msix", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void WindowsMsixManifest_DeclaresFullTrustDesktopAppAndCorrectExecutable()
    {
        string manifestPath = Path.Combine(RepositoryRoot, "packaging", "windows-msix", "AppxManifest.xml");

        Assert.True(File.Exists(manifestPath), "MSIX manifest template must exist.");
        string manifest = File.ReadAllText(manifestPath);

        Assert.Contains("WoongMonitorStack.Windows", manifest, StringComparison.Ordinal);
        Assert.Contains("Woong Monitor Stack", manifest, StringComparison.Ordinal);
        Assert.Contains("App\\Woong.MonitorStack.Windows.App.exe", manifest, StringComparison.Ordinal);
        Assert.Contains("Windows.FullTrustApplication", manifest, StringComparison.Ordinal);
        Assert.Contains("runFullTrust", manifest, StringComparison.Ordinal);
        Assert.Contains("Square44x44Logo.png", manifest, StringComparison.Ordinal);
        Assert.Contains("Square150x150Logo.png", manifest, StringComparison.Ordinal);
    }

    [Fact]
    public void WindowsMsixScripts_PackageWithWindowsSdkAndInstallOnlyWithExplicitUserTrust()
    {
        string packageScriptPath = Path.Combine(RepositoryRoot, "scripts", "package-windows-msix.ps1");
        string installScriptPath = Path.Combine(RepositoryRoot, "scripts", "install-windows-msix.ps1");

        Assert.True(File.Exists(packageScriptPath), "MSIX package script must exist.");
        Assert.True(File.Exists(installScriptPath), "MSIX install script must exist.");

        string packageScript = File.ReadAllText(packageScriptPath);
        string installScript = File.ReadAllText(installScriptPath);

        Assert.Contains("MakeAppx.exe", packageScript, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SignTool.exe", packageScript, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dotnet publish", packageScript, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("packaging\\windows-msix\\AppxManifest.xml", packageScript, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("artifacts\\windows-msix", packageScript, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("NewPlaceholderPng", packageScript, StringComparison.Ordinal);
        Assert.Contains("Assert-NativeCommandSucceeded", packageScript, StringComparison.Ordinal);
        Assert.Contains("Copy-Item -Path", packageScript, StringComparison.Ordinal);
        Assert.Contains("Add-AppxPackage", installScript, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Cert:\\CurrentUser\\TrustedPeople", installScript, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("TrustCertificate", installScript, StringComparison.Ordinal);
        Assert.DoesNotContain("Cert:\\LocalMachine", installScript, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Remove-Item Cert:", installScript, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WindowsMsixDocumentation_ExplainsUnsignedSignedAndInstallPaths()
    {
        string docPath = Path.Combine(RepositoryRoot, "docs", "windows-release-msix.md");

        Assert.True(File.Exists(docPath), "Windows release/MSIX documentation must exist.");
        string doc = File.ReadAllText(docPath);

        Assert.Contains("dotnet build Woong.MonitorStack.sln -c Release --no-restore -m:1 -v minimal", doc, StringComparison.Ordinal);
        Assert.Contains("dotnet run --configuration Release --project src\\Woong.MonitorStack.Windows.App\\Woong.MonitorStack.Windows.App.csproj", doc, StringComparison.Ordinal);
        Assert.Contains("scripts\\package-windows-msix.ps1", doc, StringComparison.Ordinal);
        Assert.Contains("scripts\\install-windows-msix.ps1", doc, StringComparison.Ordinal);
        Assert.Contains("unsigned MSIX", doc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("signed MSIX", doc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CurrentUser", doc, StringComparison.Ordinal);
    }

    private static string? GetXamlAttribute(XElement element, string attributeName)
        => element
            .Attributes()
            .FirstOrDefault(attribute => attribute.Name.LocalName == attributeName)
            ?.Value;

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
