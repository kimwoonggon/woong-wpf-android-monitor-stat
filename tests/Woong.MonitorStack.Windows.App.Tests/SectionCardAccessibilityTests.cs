using System.Windows;
using System.Windows.Controls;
using Woong.MonitorStack.Windows.App.Controls;
using static Woong.MonitorStack.Windows.App.Tests.WpfTestHelpers;

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

                AssertAutomationName<Button>(card, "SectionCardActionButton", "Show app details");
            }
            finally
            {
                window.Close();
            }
        });

    private sealed class CountingCommand : System.Windows.Input.ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
            => true;

        public void Execute(object? parameter)
            => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
