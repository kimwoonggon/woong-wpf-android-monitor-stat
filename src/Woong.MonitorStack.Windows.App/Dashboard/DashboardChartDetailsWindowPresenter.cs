using System.Windows;
using Woong.MonitorStack.Windows.App.Views;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App.Dashboard;

public sealed class DashboardChartDetailsWindowPresenter : IDashboardChartDetailsPresenter
{
    public void ShowChartDetails(DashboardChartDetailsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var window = new ChartDetailsWindow(request)
        {
            Owner = Application.Current?.Windows
                .OfType<Window>()
                .FirstOrDefault(candidate => candidate.IsActive)
        };
        window.Show();
    }
}
