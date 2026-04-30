using System.Windows;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App.Views;

public partial class ChartDetailsWindow : Window
{
    public ChartDetailsWindow(DashboardChartDetailsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        InitializeComponent();
        DataContext = new ChartDetailsWindowViewModel(
            request.Title,
            DashboardLiveChartsMapper.BuildHorizontalBarChart(request.SeriesName, request.Points));
    }
}
