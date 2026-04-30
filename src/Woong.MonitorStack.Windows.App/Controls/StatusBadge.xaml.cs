using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Woong.MonitorStack.Windows.App.Controls;

public partial class StatusBadge : UserControl
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(StatusBadge),
            new PropertyMetadata(""));

    public static readonly DependencyProperty TextBrushProperty =
        DependencyProperty.Register(
            nameof(TextBrush),
            typeof(Brush),
            typeof(StatusBadge),
            new PropertyMetadata(Brushes.Black));

    public static readonly DependencyProperty BadgeBackgroundProperty =
        DependencyProperty.Register(
            nameof(BadgeBackground),
            typeof(Brush),
            typeof(StatusBadge),
            new PropertyMetadata(Brushes.Transparent));

    public StatusBadge()
    {
        InitializeComponent();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public Brush TextBrush
    {
        get => (Brush)GetValue(TextBrushProperty);
        set => SetValue(TextBrushProperty, value);
    }

    public Brush BadgeBackground
    {
        get => (Brush)GetValue(BadgeBackgroundProperty);
        set => SetValue(BadgeBackgroundProperty, value);
    }
}
