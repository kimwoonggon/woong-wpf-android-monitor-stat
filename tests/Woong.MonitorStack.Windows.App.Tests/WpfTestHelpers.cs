using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Media;
using System.Windows.Threading;

namespace Woong.MonitorStack.Windows.App.Tests;

internal static class WpfTestHelpers
{
    public static void AssertAutomationName<T>(DependencyObject root, string automationId, string expectedName)
        where T : DependencyObject
    {
        T element = FindByAutomationId<T>(root, automationId);

        Assert.Equal(expectedName, AutomationProperties.GetName(element));
    }

    public static T FindByAutomationId<T>(DependencyObject root, string automationId)
        where T : DependencyObject
    {
        if (root is T candidate && AutomationProperties.GetAutomationId(root) == automationId)
        {
            return candidate;
        }

        foreach (DependencyObject child in GetChildren(root))
        {
            try
            {
                return FindByAutomationId<T>(child, automationId);
            }
            catch (InvalidOperationException)
            {
            }
        }

        throw new InvalidOperationException($"Could not find {typeof(T).Name} with AutomationId '{automationId}'.");
    }

    public static T FindVisualDescendant<T>(DependencyObject root)
        where T : DependencyObject
    {
        if (root is T current)
        {
            return current;
        }

        foreach (DependencyObject child in GetChildren(root))
        {
            try
            {
                return FindVisualDescendant<T>(child);
            }
            catch (InvalidOperationException)
            {
            }
        }

        throw new InvalidOperationException($"Could not find visual descendant {typeof(T).Name}.");
    }

    public static void RunOnStaThread(Action action)
    {
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
            SynchronizationContext? previousContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));

            try
            {
                action();
                DrainDispatcher();
            }
            catch (Exception exception)
            {
                failure = ExceptionDispatchInfo.Capture(exception);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previousContext);
                dispatcher.InvokeShutdown();
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        failure?.Throw();
    }

    public static void RunWindowTest<TWindow>(Func<TWindow> createWindow, Action<TWindow> assertion)
        where TWindow : Window
        => RunOnStaThread(() =>
        {
            TWindow? window = null;

            try
            {
                window = createWindow();
                window.Show();
                window.UpdateLayout();
                DrainDispatcher();

                assertion(window);
            }
            finally
            {
                window?.Close();
                DrainDispatcher();
            }
        });

    public static void RunContentWindowTest<TContent>(Func<TContent> createContent, Action<TContent> assertion)
        where TContent : FrameworkElement
        => RunContentWindowTest(createContent, (_, content) => assertion(content));

    public static void RunContentWindowTest<TContent>(Func<TContent> createContent, Action<Window, TContent> assertion)
        where TContent : FrameworkElement
        => RunWindowTest(
            () =>
            {
                TContent content = createContent();

                return new Window { Content = content };
            },
            window =>
            {
                var content = Assert.IsType<TContent>(window.Content);
                assertion(window, content);
            });

    public static void DrainDispatcher()
    {
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
        {
            return;
        }

        var frame = new DispatcherFrame();
        dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            () => frame.Continue = false);
        Dispatcher.PushFrame(frame);
    }

    private static IEnumerable<DependencyObject> GetChildren(DependencyObject root)
    {
        int visualChildCount = 0;
        try
        {
            visualChildCount = VisualTreeHelper.GetChildrenCount(root);
        }
        catch (InvalidOperationException)
        {
        }

        for (var index = 0; index < visualChildCount; index++)
        {
            yield return VisualTreeHelper.GetChild(root, index);
        }

        foreach (object child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is DependencyObject dependencyObject)
            {
                yield return dependencyObject;
            }
        }
    }
}
