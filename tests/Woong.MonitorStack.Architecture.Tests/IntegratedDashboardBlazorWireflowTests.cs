namespace Woong.MonitorStack.Architecture.Tests;

public sealed class IntegratedDashboardBlazorWireflowTests
{
    [Fact]
    public void IntegratedDashboardPage_ShowsCombinedWindowsAndroidAndLocationRouteSections()
    {
        string repoRoot = FindRepositoryRoot();
        string pagePath = Path.Combine(
            repoRoot,
            "src",
            "Woong.MonitorStack.Server",
            "Components",
            "Pages",
            "IntegratedDashboard.razor");

        string page = File.ReadAllText(pagePath);

        Assert.Contains("Combined View", page, StringComparison.Ordinal);
        Assert.Contains("Windows View", page, StringComparison.Ordinal);
        Assert.Contains("Android View", page, StringComparison.Ordinal);
        Assert.Contains("Windows apps and domains", page, StringComparison.Ordinal);
        Assert.Contains("Android apps and domains", page, StringComparison.Ordinal);
        Assert.Contains("Location Movement Route", page, StringComparison.Ordinal);
        Assert.Contains("_snapshot.PlatformUsage", page, StringComparison.Ordinal);
        Assert.Contains("_snapshot.LocationRoute", page, StringComparison.Ordinal);
        Assert.Contains("<svg", page, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("polyline", page, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Woong.MonitorStack.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
