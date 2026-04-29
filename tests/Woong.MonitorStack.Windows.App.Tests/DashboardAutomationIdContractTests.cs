using System.IO;
using System.Xml.Linq;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class DashboardAutomationIdContractTests
{
    [Fact]
    public void DashboardComponentXaml_ExposesStableAutomationIdsForUiAcceptance()
    {
        string viewsRoot = Path.Combine(FindRepositoryRoot(), "src", "Woong.MonitorStack.Windows.App", "Views");
        string[] requiredAutomationIds =
        [
            "HeaderStatusBar",
            "ControlBar",
            "CurrentFocusPanel",
            "SummaryCardsPanel",
            "ChartsPanel",
            "DetailsTabsPanel",
            "StartTrackingButton",
            "StopTrackingButton",
            "RefreshButton",
            "SyncNowButton",
            "AppSessionsList",
            "WebSessionsList",
            "LiveEventsList"
        ];
        ISet<string> automationIds = Directory
            .EnumerateFiles(viewsRoot, "*.xaml", SearchOption.TopDirectoryOnly)
            .SelectMany(CollectAutomationIds)
            .ToHashSet(StringComparer.Ordinal);
        string[] missingAutomationIds = requiredAutomationIds
            .Where(requiredAutomationId => !automationIds.Contains(requiredAutomationId))
            .ToArray();

        Assert.Empty(missingAutomationIds);
    }

    private static IEnumerable<string> CollectAutomationIds(string xamlPath)
        => XDocument
            .Load(xamlPath)
            .Descendants()
            .SelectMany(element => element.Attributes())
            .Where(attribute => attribute.Name.LocalName == "AutomationProperties.AutomationId")
            .Select(attribute => attribute.Value);

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
