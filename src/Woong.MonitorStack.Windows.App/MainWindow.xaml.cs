using System.Windows;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App;

public partial class MainWindow : Window
{
    private readonly ITrackingTicker _trackingTicker;
    private readonly DashboardViewModel _viewModel;
    private readonly MainWindowStartupOptions _startupOptions;
    private bool _hasAppliedStartupOptions;

    public MainWindow(DashboardViewModel viewModel)
        : this(viewModel, MainWindowStartupOptions.Manual, new DispatcherTrackingTicker())
    {
    }

    public MainWindow(DashboardViewModel viewModel, WindowsAppOptions options)
        : this(
            viewModel,
            new MainWindowStartupOptions(options.AutoStartTracking),
            new DispatcherTrackingTicker())
    {
    }

    public MainWindow(DashboardViewModel viewModel, MainWindowStartupOptions startupOptions)
        : this(viewModel, startupOptions, new DispatcherTrackingTicker())
    {
    }

    public MainWindow(DashboardViewModel viewModel, ITrackingTicker trackingTicker)
        : this(viewModel, MainWindowStartupOptions.Manual, trackingTicker)
    {
    }

    public MainWindow(
        DashboardViewModel viewModel,
        MainWindowStartupOptions startupOptions,
        ITrackingTicker trackingTicker)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _startupOptions = startupOptions ?? throw new ArgumentNullException(nameof(startupOptions));
        _trackingTicker = trackingTicker ?? throw new ArgumentNullException(nameof(trackingTicker));
        DataContext = viewModel;
        _trackingTicker.Tick += OnTrackingTickerTick;
        Closing += (_, _) => FlushTrackingBeforeClose();
        Loaded += (_, _) =>
        {
            ApplyStartupOptions();
            _trackingTicker.Start();
        };
        Closed += (_, _) =>
        {
            _trackingTicker.Stop();
            _trackingTicker.Tick -= OnTrackingTickerTick;
        };
    }

    private void FlushTrackingBeforeClose()
    {
        if (_viewModel.StopTrackingCommand.CanExecute(null))
        {
            _viewModel.StopTrackingCommand.Execute(null);
        }
    }

    private void OnTrackingTickerTick(object? sender, EventArgs e)
    {
        if (_viewModel.PollTrackingCommand.CanExecute(null))
        {
            _viewModel.PollTrackingCommand.Execute(null);
        }
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
