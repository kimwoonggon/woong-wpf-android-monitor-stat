using System.Windows;
using System.Windows.Interop;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App;

public partial class MainWindow : Window
{
    private const int WmSysCommand = 0x0112;
    private const int ScClose = 0xF060;

    private readonly MainWindowLifecycleCoordinator _lifecycleCoordinator;
    private HwndSource? _windowMessageSource;

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
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(startupOptions);
        ArgumentNullException.ThrowIfNull(trackingTicker);
        ArgumentNullException.ThrowIfNull(trayLifecycle);

        var trayWindow = new WpfTrayLifecycleWindow(this);
        _lifecycleCoordinator = new MainWindowLifecycleCoordinator(
            viewModel,
            startupOptions,
            trackingTicker,
            trayLifecycle,
            trayWindow);
        DataContext = viewModel;
        SourceInitialized += OnSourceInitialized;
        Closing += (_, _) => _lifecycleCoordinator.HandleClosing();
        Loaded += (_, _) => _lifecycleCoordinator.HandleLoaded();
        Closed += (_, _) =>
        {
            _lifecycleCoordinator.HandleClosed();
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
            _lifecycleCoordinator.MinimizeToTaskbar();
            handled = true;
        }

        return IntPtr.Zero;
    }
}
