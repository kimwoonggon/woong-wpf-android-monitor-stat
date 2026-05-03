using System.Windows.Controls;
using Woong.MonitorStack.Windows.App.Views;
using static Woong.MonitorStack.Windows.App.Tests.WpfTestHelpers;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class ChartsPanelAccessibilityTests
{
    [Fact]
    public void ChartsPanel_DomainChartTitleWrapsBeforeFocusTimeAndKeepsDetailsButton()
        => RunContentWindowTest(
            () => new ChartsPanel(),
            panel =>
            {
                var firstLine = FindByAutomationId<TextBlock>(panel, "DomainUsageChartTitleFirstLine");
                var secondLine = FindByAutomationId<TextBlock>(panel, "DomainUsageChartTitleSecondLine");
                var detailsButton = FindByAutomationId<Button>(panel, "DomainChartDetailsButton");

                Assert.Equal("도메인별", firstLine.Text);
                Assert.Equal("집중 시간", secondLine.Text);
                Assert.Equal(2, Grid.GetColumn(detailsButton));
                Assert.Equal("상세보기", detailsButton.Content);
            });

    [Fact]
    public void ChartsPanel_DetailActionButtonsExposeReadableAutomationNames()
        => RunContentWindowTest(
            () => new ChartsPanel(),
            panel =>
            {
                AssertAutomationName<Button>(
                    panel,
                    "AppChartDetailsButton",
                    "Show app focus details");
                AssertAutomationName<Button>(
                    panel,
                    "DomainChartDetailsButton",
                    "Show domain focus details");
            });

}
