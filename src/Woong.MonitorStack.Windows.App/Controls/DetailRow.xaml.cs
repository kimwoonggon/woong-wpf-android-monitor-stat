using System.Windows;
using System.Windows.Controls;

namespace Woong.MonitorStack.Windows.App.Controls;

public partial class DetailRow : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(
            nameof(Label),
            typeof(string),
            typeof(DetailRow),
            new PropertyMetadata(""));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(string),
            typeof(DetailRow),
            new PropertyMetadata(""));

    public static readonly DependencyProperty ValueAutomationIdProperty =
        DependencyProperty.Register(
            nameof(ValueAutomationId),
            typeof(string),
            typeof(DetailRow),
            new PropertyMetadata(""));

    public static readonly DependencyProperty IconGlyphProperty =
        DependencyProperty.Register(
            nameof(IconGlyph),
            typeof(string),
            typeof(DetailRow),
            new PropertyMetadata(""));

    public static readonly DependencyProperty IconAutomationIdProperty =
        DependencyProperty.Register(
            nameof(IconAutomationId),
            typeof(string),
            typeof(DetailRow),
            new PropertyMetadata(""));

    public static readonly DependencyProperty RowMarginProperty =
        DependencyProperty.Register(
            nameof(RowMargin),
            typeof(Thickness),
            typeof(DetailRow),
            new PropertyMetadata(new Thickness(0, 0, 18, 12)));

    public static readonly DependencyProperty ValueFontSizeProperty =
        DependencyProperty.Register(
            nameof(ValueFontSize),
            typeof(double),
            typeof(DetailRow),
            new PropertyMetadata(16.0));

    public static readonly DependencyProperty ValueFontWeightProperty =
        DependencyProperty.Register(
            nameof(ValueFontWeight),
            typeof(FontWeight),
            typeof(DetailRow),
            new PropertyMetadata(FontWeights.SemiBold));

    public static readonly DependencyProperty ValueTextWrappingProperty =
        DependencyProperty.Register(
            nameof(ValueTextWrapping),
            typeof(TextWrapping),
            typeof(DetailRow),
            new PropertyMetadata(TextWrapping.NoWrap));

    public static readonly DependencyProperty ValueTextTrimmingProperty =
        DependencyProperty.Register(
            nameof(ValueTextTrimming),
            typeof(TextTrimming),
            typeof(DetailRow),
            new PropertyMetadata(TextTrimming.CharacterEllipsis));

    public DetailRow()
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

    public string ValueAutomationId
    {
        get => (string)GetValue(ValueAutomationIdProperty);
        set => SetValue(ValueAutomationIdProperty, value);
    }

    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    public string IconAutomationId
    {
        get => (string)GetValue(IconAutomationIdProperty);
        set => SetValue(IconAutomationIdProperty, value);
    }

    public Thickness RowMargin
    {
        get => (Thickness)GetValue(RowMarginProperty);
        set => SetValue(RowMarginProperty, value);
    }

    public double ValueFontSize
    {
        get => (double)GetValue(ValueFontSizeProperty);
        set => SetValue(ValueFontSizeProperty, value);
    }

    public FontWeight ValueFontWeight
    {
        get => (FontWeight)GetValue(ValueFontWeightProperty);
        set => SetValue(ValueFontWeightProperty, value);
    }

    public TextWrapping ValueTextWrapping
    {
        get => (TextWrapping)GetValue(ValueTextWrappingProperty);
        set => SetValue(ValueTextWrappingProperty, value);
    }

    public TextTrimming ValueTextTrimming
    {
        get => (TextTrimming)GetValue(ValueTextTrimmingProperty);
        set => SetValue(ValueTextTrimmingProperty, value);
    }
}
