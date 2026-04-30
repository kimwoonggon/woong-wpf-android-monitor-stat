using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App.Views;

public sealed record ChartDetailsWindowViewModel(
    string Title,
    DashboardLiveChartsData Chart);
