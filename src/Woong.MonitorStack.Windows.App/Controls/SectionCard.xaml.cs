using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace Woong.MonitorStack.Windows.App.Controls;

[ContentProperty(nameof(CardContent))]
public partial class SectionCard : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(SectionCard),
            new PropertyMetadata("", OnHeaderPropertyChanged));

    public static readonly DependencyProperty ActionTextProperty =
        DependencyProperty.Register(
            nameof(ActionText),
            typeof(string),
            typeof(SectionCard),
            new PropertyMetadata("", OnHeaderPropertyChanged));

    public static readonly DependencyProperty ActionCommandProperty =
        DependencyProperty.Register(
            nameof(ActionCommand),
            typeof(ICommand),
            typeof(SectionCard),
            new PropertyMetadata(null, OnHeaderPropertyChanged));

    public static readonly DependencyProperty CardContentProperty =
        DependencyProperty.Register(
            nameof(CardContent),
            typeof(object),
            typeof(SectionCard),
            new PropertyMetadata(null));

    public static readonly DependencyProperty CardMarginProperty =
        DependencyProperty.Register(
            nameof(CardMargin),
            typeof(Thickness),
            typeof(SectionCard),
            new PropertyMetadata(new Thickness(0, 4, 0, 16)));

    public static readonly DependencyProperty HasActionProperty =
        DependencyProperty.Register(
            nameof(HasAction),
            typeof(bool),
            typeof(SectionCard),
            new PropertyMetadata(false));

    public static readonly DependencyProperty HasHeaderProperty =
        DependencyProperty.Register(
            nameof(HasHeader),
            typeof(bool),
            typeof(SectionCard),
            new PropertyMetadata(false));

    public SectionCard()
    {
        InitializeComponent();
        UpdateHeaderState();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string ActionText
    {
        get => (string)GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    public ICommand? ActionCommand
    {
        get => (ICommand?)GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    public object? CardContent
    {
        get => GetValue(CardContentProperty);
        set => SetValue(CardContentProperty, value);
    }

    public Thickness CardMargin
    {
        get => (Thickness)GetValue(CardMarginProperty);
        set => SetValue(CardMarginProperty, value);
    }

    public bool HasAction
    {
        get => (bool)GetValue(HasActionProperty);
        private set => SetValue(HasActionProperty, value);
    }

    public bool HasHeader
    {
        get => (bool)GetValue(HasHeaderProperty);
        private set => SetValue(HasHeaderProperty, value);
    }

    private static void OnHeaderPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
    {
        if (dependencyObject is SectionCard sectionCard)
        {
            sectionCard.UpdateHeaderState();
        }
    }

    private void UpdateHeaderState()
    {
        HasAction = !string.IsNullOrWhiteSpace(ActionText) && ActionCommand is not null;
        HasHeader = !string.IsNullOrWhiteSpace(Title) || HasAction;
    }
}
