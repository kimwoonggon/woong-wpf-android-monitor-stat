using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App;

public sealed class MainWindowLifecycleCoordinator
{
    private readonly DashboardViewModel _viewModel;
    private readonly MainWindowStartupOptions _startupOptions;
    private readonly ITrackingTicker _trackingTicker;
    private readonly IWindowsTrayLifecycleService _trayLifecycle;
    private readonly ITrayLifecycleWindow _trayWindow;
    private bool _hasAppliedStartupOptions;
    private bool _hasClosed;

    public MainWindowLifecycleCoordinator(
        DashboardViewModel viewModel,
        MainWindowStartupOptions startupOptions,
        ITrackingTicker trackingTicker,
        IWindowsTrayLifecycleService trayLifecycle,
        ITrayLifecycleWindow trayWindow)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _startupOptions = startupOptions ?? throw new ArgumentNullException(nameof(startupOptions));
        _trackingTicker = trackingTicker ?? throw new ArgumentNullException(nameof(trackingTicker));
        _trayLifecycle = trayLifecycle ?? throw new ArgumentNullException(nameof(trayLifecycle));
        _trayWindow = trayWindow ?? throw new ArgumentNullException(nameof(trayWindow));
        _trayLifecycle.RegisterWindow(_trayWindow);
        _trackingTicker.Tick += OnTrackingTickerTick;
    }

    public void HandleLoaded()
    {
        ApplyStartupOptions();
        _trackingTicker.Start();
    }

    public void HandleClosing()
    {
        if (_viewModel.StopTrackingCommand.CanExecute(null))
        {
            _viewModel.StopTrackingCommand.Execute(null);
        }
    }

    public void HandleClosed()
    {
        if (_hasClosed)
        {
            return;
        }

        _hasClosed = true;
        _trackingTicker.Stop();
        _trackingTicker.Tick -= OnTrackingTickerTick;
    }

    public void MinimizeToTaskbar()
        => _trayLifecycle.MinimizeToTaskbar(_trayWindow);

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
