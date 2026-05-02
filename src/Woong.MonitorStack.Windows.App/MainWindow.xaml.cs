using System.Windows;
using System.Windows.Interop;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App;

public partial class MainWindow : Window
{
    private const int WmSysCommand = 0x0112;
    private const int ScClose = 0xF060;

    private readonly ITrackingTicker _trackingTicker;
    private readonly IWindowsTrayLifecycleService _trayLifecycle;
    private readonly ITrayLifecycleWindow _trayWindow;
    private readonly DashboardViewModel _viewModel;
    private readonly MainWindowStartupOptions _startupOptions;
    private HwndSource? _windowMessageSource;
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
        : this(viewModel, startupOptions, trackingTicker, new WindowsTrayLifecycleService(
            new NoopWindowsTrayIcon(),
            new NullDashboardRuntimeLogSink()))
    {
    }

    public MainWindow(
        DashboardViewModel viewModel,
        MainWindowStartupOptions startupOptions,
        ITrackingTicker trackingTicker,
        IWindowsTrayLifecycleService trayLifecycle)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _startupOptions = startupOptions ?? throw new ArgumentNullException(nameof(startupOptions));
        _trackingTicker = trackingTicker ?? throw new ArgumentNullException(nameof(trackingTicker));
        _trayLifecycle = trayLifecycle ?? throw new ArgumentNullException(nameof(trayLifecycle));
        _trayWindow = new WpfTrayLifecycleWindow(this);
        _trayLifecycle.RegisterWindow(_trayWindow);
        DataContext = viewModel;
        _trackingTicker.Tick += OnTrackingTickerTick;
        SourceInitialized += OnSourceInitialized;
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
            _windowMessageSource?.RemoveHook(OnWindowMessage);
            _windowMessageSource = null;
        };
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _windowMessageSource = (HwndSource?)PresentationSource.FromVisual(this);
        _windowMessageSource?.AddHook(OnWindowMessage);
    }

    private IntPtr OnWindowMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == WmSysCommand && ((wParam.ToInt64() & 0xFFF0) == ScClose))
        {
            _trayLifecycle.MinimizeToTaskbar(_trayWindow);
            handled = true;
        }

        return IntPtr.Zero;
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
