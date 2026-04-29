using System.Windows;
using System.Windows.Controls;

namespace Woong.MonitorStack.Windows.App.Controls;

public partial class EmptyState : UserControl
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(EmptyState),
        new PropertyMetadata(""));

    public static readonly DependencyProperty TextAutomationIdProperty = DependencyProperty.Register(
        nameof(TextAutomationId),
        typeof(string),
        typeof(EmptyState),
        new PropertyMetadata(""));

    public EmptyState()
    {
        InitializeComponent();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string TextAutomationId
    {
        get => (string)GetValue(TextAutomationIdProperty);
        set => SetValue(TextAutomationIdProperty, value);
    }
}
