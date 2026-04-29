using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using Woong.MonitorStack.Windows.App.Controls;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class SectionCardAccessibilityTests
{
    [Fact]
    public void SectionCard_ActionButtonUsesActionTextAsReadableAutomationName()
        => RunOnStaThread(() =>
        {
            var card = new SectionCard
            {
                Title = "Focus by app",
                ActionText = "Show app details",
                ActionCommand = new CountingCommand(),
                CardContent = new TextBlock { Text = "Chrome" }
            };
            var window = new Window { Content = card };

            try
            {
                window.Show();
                window.UpdateLayout();

                Button actionButton = FindByAutomationId<Button>(card, "SectionCardActionButton");

                Assert.Equal("Show app details", AutomationProperties.GetName(actionButton));
            }
            finally
            {
                window.Close();
            }
        });

    private static T FindByAutomationId<T>(DependencyObject root, string automationId)
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

    private static void RunOnStaThread(Action action)
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

    private sealed class CountingCommand : System.Windows.Input.ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
            => true;

        public void Execute(object? parameter)
            => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
