using System.Windows;
using System.Windows.Automation;
using System.Windows.Media;

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

    public static void RunOnStaThread(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            throw failure;
        }
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
