using System.Windows;
using System.Windows.Controls;
using Woong.MonitorStack.Windows.App.Views;
using static Woong.MonitorStack.Windows.App.Tests.WpfTestHelpers;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class ChartsPanelAccessibilityTests
{
    [Fact]
    public void ChartsPanel_DetailActionButtonsExposeReadableAutomationNames()
        => RunOnStaThread(() =>
        {
            var panel = new ChartsPanel();
            var window = new Window { Content = panel };

            try
            {
                window.Show();
                window.UpdateLayout();

                AssertAutomationName<Button>(
                    panel,
                    "AppChartDetailsButton",
                    "Show app focus details");
                AssertAutomationName<Button>(
                    panel,
                    "DomainChartDetailsButton",
                    "Show domain focus details");
            }
            finally
            {
                window.Close();
            }
        });

}
