using System.Windows;
using System.Windows.Threading;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _trackingTimer;
    private readonly DashboardViewModel _viewModel;
    private readonly MainWindowStartupOptions _startupOptions;
    private bool _hasAppliedStartupOptions;

    public MainWindow(DashboardViewModel viewModel)
        : this(viewModel, MainWindowStartupOptions.Manual)
    {
    }

    public MainWindow(DashboardViewModel viewModel, WindowsAppOptions options)
        : this(
            viewModel,
            new MainWindowStartupOptions(options.AutoStartTracking))
    {
    }

    public MainWindow(DashboardViewModel viewModel, MainWindowStartupOptions startupOptions)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _startupOptions = startupOptions ?? throw new ArgumentNullException(nameof(startupOptions));
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
        Loaded += (_, _) =>
        {
            ApplyStartupOptions();
            _trackingTimer.Start();
        };
        Closed += (_, _) => _trackingTimer.Stop();
    }

    private void ApplyStartupOptions()
    {
        if (_hasAppliedStartupOptions)
        {
            return;
        }

        _hasAppliedStartupOptions = true;
        if (_startupOptions.AutoStartTracking && _viewModel.StartTrackingCommand.CanExecute(null))
        {
            _viewModel.StartTrackingCommand.Execute(null);
        }
    }
}
