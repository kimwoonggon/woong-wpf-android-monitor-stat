using System.Windows;
using System.Windows.Threading;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _trackingTimer;

    public MainWindow(DashboardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _trackingTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _trackingTimer.Tick += (_, _) =>
        {
            if (viewModel.PollTrackingCommand.CanExecute(null))
            {
                viewModel.PollTrackingCommand.Execute(null);
            }
        };
        Loaded += (_, _) => _trackingTimer.Start();
        Closed += (_, _) => _trackingTimer.Stop();
    }
}
