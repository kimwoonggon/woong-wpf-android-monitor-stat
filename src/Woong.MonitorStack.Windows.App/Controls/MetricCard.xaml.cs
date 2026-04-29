using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Woong.MonitorStack.Windows.App.Controls;

public partial class MetricCard : UserControl
{
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label),
        typeof(string),
        typeof(MetricCard),
        new PropertyMetadata(""));

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(string),
        typeof(MetricCard),
        new PropertyMetadata(""));

    public static readonly DependencyProperty SubtitleProperty = DependencyProperty.Register(
        nameof(Subtitle),
        typeof(string),
        typeof(MetricCard),
        new PropertyMetadata(""));

    public static readonly DependencyProperty IconTextProperty = DependencyProperty.Register(
        nameof(IconText),
        typeof(string),
        typeof(MetricCard),
        new PropertyMetadata("◎"));

    public static readonly DependencyProperty AccentBrushProperty = DependencyProperty.Register(
        nameof(AccentBrush),
        typeof(Brush),
        typeof(MetricCard),
        new PropertyMetadata(Brushes.DodgerBlue));

    public static readonly DependencyProperty IconBackgroundProperty = DependencyProperty.Register(
        nameof(IconBackground),
        typeof(Brush),
        typeof(MetricCard),
        new PropertyMetadata(Brushes.AliceBlue));

    public MetricCard()
    {
        InitializeComponent();
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public string IconText
    {
        get => (string)GetValue(IconTextProperty);
        set => SetValue(IconTextProperty, value);
    }

    public Brush AccentBrush
    {
        get => (Brush)GetValue(AccentBrushProperty);
        set => SetValue(AccentBrushProperty, value);
    }

    public Brush IconBackground
    {
        get => (Brush)GetValue(IconBackgroundProperty);
        set => SetValue(IconBackgroundProperty, value);
    }
}
