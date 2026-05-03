using LiveChartsCore.SkiaSharpView.WPF;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.App.Controls;
using Woong.MonitorStack.Windows.App.Views;
using Woong.MonitorStack.Windows.Presentation.Dashboard;
using static Woong.MonitorStack.Windows.App.Tests.WpfTestHelpers;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed partial class MainWindowUiExpectationTests
{

    private static TestDashboard CreateDashboard()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var dataSource = new FakeDashboardDataSource(
            [
                FocusSession.FromUtc(
                    "focus-1",
                    "windows-device-1",
                    "chrome.exe",
                    now.AddMinutes(-30),
                    now.AddMinutes(-10),
                    "Asia/Seoul",
                    isIdle: false,
                    "foreground_window"),
                FocusSession.FromUtc(
                    "focus-2",
                    "windows-device-1",
                    "devenv.exe",
                    now.AddMinutes(-10),
                    now,
                    "Asia/Seoul",
                    isIdle: true,
                    "foreground_window")
            ],
            [
                WebSession.FromUtc(
                    "focus-1",
                    "Chrome",
                    "https://example.com/docs",
                    "Docs",
                    now.AddMinutes(-25),
                    now.AddMinutes(-15))
            ]);
        var viewModel = new DashboardViewModel(dataSource, new FixedClock(now), new DashboardOptions("Asia/Seoul"));

        return new TestDashboard(now, dataSource, viewModel);
    }

    private static TestDashboard CreateEmptyDashboard()
    {
        var now = new DateTimeOffset(2026, 4, 28, 3, 0, 0, TimeSpan.Zero);
        var dataSource = new FakeDashboardDataSource([], []);
        var viewModel = new DashboardViewModel(dataSource, new FixedClock(now), new DashboardOptions("Asia/Seoul"));

        return new TestDashboard(now, dataSource, viewModel);
    }

    private static void AssertPeriodButton(
        DependencyObject root,
        string automationId,
        string expectedContent,
        DashboardPeriod expectedPeriod,
        DashboardViewModel viewModel)
    {
        Button button = FindByAutomationId<Button>(root, automationId);

        Assert.Equal(expectedContent, button.Content);
        Assert.Same(viewModel.SelectDashboardPeriodCommand, button.Command);
        Assert.Equal(expectedPeriod, button.CommandParameter);
    }

    private static void AssertReadableButton(Button button)
    {
        Assert.True(button.MinHeight >= 40, $"{AutomationProperties.GetAutomationId(button)} should have MinHeight >= 40.");
        Assert.True(button.MinWidth >= 96, $"{AutomationProperties.GetAutomationId(button)} should have MinWidth >= 96.");
        Assert.True(button.Padding.Left >= 12, $"{AutomationProperties.GetAutomationId(button)} should have horizontal padding >= 12.");
        Assert.True(button.Padding.Right >= 12, $"{AutomationProperties.GetAutomationId(button)} should have horizontal padding >= 12.");
    }

    private static void AssertCompactActionButton(Button button)
    {
        Assert.Equal(28.0, button.MinHeight);
        Assert.Equal(72.0, button.MinWidth);
        Assert.Equal(new Thickness(10, 0, 10, 0), button.Padding);
        Assert.Equal(12.0, button.FontSize);
    }

    private static void AssertBadgeUsesBrushes(
        FrameworkElement root,
        string automationId,
        string backgroundBrushKey,
        string borderBrushKey,
        string textBrushKey)
    {
        StatusBadge badge = FindByAutomationId<StatusBadge>(root, automationId);

        Assert.Same(root.FindResource(backgroundBrushKey), badge.BadgeBackground);
        Assert.Null(badge.Background);
        Assert.Same(root.FindResource(borderBrushKey), badge.BorderBrush);
        Assert.Same(root.FindResource(textBrushKey), badge.TextBrush);
    }

    private static void AssertSectionTitleStyle(TextBlock textBlock)
    {
        Style style = Assert.IsType<Style>(textBlock.Style);
        AssertStyleSetter(style, TextBlock.FontSizeProperty, 16.0);
        AssertStyleSetter(style, TextBlock.FontWeightProperty, FontWeights.SemiBold);

        Setter foregroundSetter = FindSetter(style, TextBlock.ForegroundProperty);
        var foregroundBrush = Assert.IsType<SolidColorBrush>(foregroundSetter.Value);
        Assert.Equal(Color.FromRgb(0x16, 0x20, 0x33), foregroundBrush.Color);
    }

    private static void AssertSettingsSectionTitleStyle(TextBlock textBlock)
    {
        Style style = Assert.IsType<Style>(textBlock.Style);
        AssertStyleSetter(style, TextBlock.FontSizeProperty, 15.0);
        AssertStyleSetter(style, TextBlock.FontWeightProperty, FontWeights.SemiBold);
        AssertStyleSetter(style, FrameworkElement.MarginProperty, new Thickness(0, 0, 0, 10));

        Setter foregroundSetter = FindSetter(style, TextBlock.ForegroundProperty);
        var foregroundBrush = Assert.IsType<SolidColorBrush>(foregroundSetter.Value);
        Assert.Equal(Color.FromRgb(0x16, 0x20, 0x33), foregroundBrush.Color);
    }

    private static void AssertSettingsMutedTextStyle(TextBlock textBlock)
    {
        Style style = Assert.IsType<Style>(textBlock.Style);
        AssertStyleSetter(style, TextBlock.FontSizeProperty, 13.0);

        Setter foregroundSetter = FindSetter(style, TextBlock.ForegroundProperty);
        var foregroundBrush = Assert.IsType<SolidColorBrush>(foregroundSetter.Value);
        Assert.Equal(Color.FromRgb(0x5A, 0x64, 0x72), foregroundBrush.Color);
    }

    private static void AssertSettingsWarningTextStyle(TextBlock textBlock)
    {
        Style style = Assert.IsType<Style>(textBlock.Style);
        AssertStyleSetter(style, TextBlock.FontSizeProperty, 13.0);

        Setter foregroundSetter = FindSetter(style, TextBlock.ForegroundProperty);
        var foregroundBrush = Assert.IsType<SolidColorBrush>(foregroundSetter.Value);
        Assert.Equal(Color.FromRgb(0x8A, 0x4B, 0x00), foregroundBrush.Color);
    }

    private static void AssertMutedTextStyle(TextBlock textBlock)
    {
        Style style = Assert.IsType<Style>(textBlock.Style);
        AssertStyleSetter(style, TextBlock.FontSizeProperty, 12.0);

        Setter foregroundSetter = FindSetter(style, TextBlock.ForegroundProperty);
        var foregroundBrush = Assert.IsType<SolidColorBrush>(foregroundSetter.Value);
        Assert.Equal(Color.FromRgb(0x5A, 0x64, 0x72), foregroundBrush.Color);
    }

    private static void AssertBodyTextStyle(TextBlock textBlock)
    {
        Style style = Assert.IsType<Style>(textBlock.Style);
        AssertStyleSetter(style, TextBlock.FontSizeProperty, 13.0);

        Setter foregroundSetter = FindSetter(style, TextBlock.ForegroundProperty);
        var foregroundBrush = Assert.IsType<SolidColorBrush>(foregroundSetter.Value);
        Assert.Equal(Color.FromRgb(0x16, 0x20, 0x33), foregroundBrush.Color);
    }

    private static void AssertCurrentFocusValueTextStyle(TextBlock textBlock)
    {
        Style style = Assert.IsType<Style>(textBlock.Style);
        AssertStyleSetter(style, TextBlock.FontSizeProperty, 14.0);

        Setter foregroundSetter = FindSetter(style, TextBlock.ForegroundProperty);
        var foregroundBrush = Assert.IsType<SolidColorBrush>(foregroundSetter.Value);
        Assert.Equal(Color.FromRgb(0x16, 0x20, 0x33), foregroundBrush.Color);
    }

    private static void AssertCurrentFocusSecondaryTextStyle(TextBlock textBlock)
    {
        Style style = Assert.IsType<Style>(textBlock.Style);
        AssertStyleSetter(style, TextBlock.FontSizeProperty, 13.0);

        Setter foregroundSetter = FindSetter(style, TextBlock.ForegroundProperty);
        var foregroundBrush = Assert.IsType<SolidColorBrush>(foregroundSetter.Value);
        Assert.Equal(Color.FromRgb(0x5A, 0x64, 0x72), foregroundBrush.Color);
    }

    private static void AssertMetricLabelTextStyle(TextBlock textBlock)
    {
        Style style = Assert.IsType<Style>(textBlock.Style);
        AssertStyleSetter(style, TextBlock.FontWeightProperty, FontWeights.SemiBold);
        AssertStyleSetter(style, TextBlock.FontSizeProperty, 13.0);

        Setter foregroundSetter = FindSetter(style, TextBlock.ForegroundProperty);
        var foregroundBrush = Assert.IsType<SolidColorBrush>(foregroundSetter.Value);
        Assert.Equal(Color.FromRgb(0x16, 0x20, 0x33), foregroundBrush.Color);
    }

    private static void AssertStatusBadgeTextStyle(TextBlock textBlock)
    {
        Style style = Assert.IsType<Style>(textBlock.Style);
        AssertStyleSetter(style, TextBlock.FontWeightProperty, FontWeights.SemiBold);
        AssertStyleSetter(style, TextBlock.FontSizeProperty, 13.0);
    }

    private static void AssertSettingsCheckBoxStyle(CheckBox checkBox)
    {
        Style style = Assert.IsType<Style>(checkBox.Style);
        AssertStyleSetter(style, Control.FontSizeProperty, 14.0);
        AssertStyleSetter(style, FrameworkElement.MarginProperty, new Thickness(0, 0, 0, 10));
    }

    private static void AssertColumnMinWidths(DataGrid dataGrid, IReadOnlyList<double> expectedMinWidths)
    {
        Assert.Equal(expectedMinWidths.Count, dataGrid.Columns.Count);

        for (int index = 0; index < expectedMinWidths.Count; index++)
        {
            Assert.True(
                dataGrid.Columns[index].MinWidth >= expectedMinWidths[index],
                $"{dataGrid.Columns[index].Header} MinWidth should be >= {expectedMinWidths[index]}.");
        }
    }

    private static void AssertSessionDataGridContract(DataGrid dataGrid)
    {
        Assert.NotNull(dataGrid.Style);
        Assert.False(dataGrid.AutoGenerateColumns);
        Assert.False(dataGrid.CanUserAddRows);
        Assert.False(dataGrid.CanUserDeleteRows);
        Assert.Equal(DataGridHeadersVisibility.Column, dataGrid.HeadersVisibility);
        Assert.True(dataGrid.IsReadOnly);
        Assert.True(dataGrid.MinHeight >= 260);
        Assert.Equal(ScrollBarVisibility.Auto, ScrollViewer.GetHorizontalScrollBarVisibility(dataGrid));
    }

    private static SettingsPanel ShowSettingsPanel(Window window)
    {
        window.Show();
        window.UpdateLayout();

        TabControl tabs = FindByAutomationId<TabControl>(window, "DashboardTabs");
        tabs.SelectedIndex = 3;
        window.UpdateLayout();

        return FindByAutomationId<SettingsPanel>(window, "SettingsPanel");
    }

    private static void Invoke(Button button)
    {
        if (button.Command is not null)
        {
            Assert.True(button.Command.CanExecute(button.CommandParameter));
            button.Command.Execute(button.CommandParameter);

            return;
        }

        button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, button));
    }

    private static IReadOnlyList<string> TabHeaders(TabControl tabs)
        => tabs.Items
            .OfType<TabItem>()
            .Select(tab => tab.Header switch
            {
                string text => text,
                DependencyObject header => FindVisualDescendants<TextBlock>(header)
                    .Select(textBlock => textBlock.Text)
                    .FirstOrDefault(text => text is "App Sessions" or "Web Sessions" or "Live Event Log" or "Settings")
                    ?? string.Join(" ", CollectText(header)),
                null => "",
                object header => header.ToString() ?? ""
            })
            .ToList();

    private static IReadOnlyList<string> ColumnHeaders(DataGrid dataGrid)
        => dataGrid.Columns
            .Select(column => column.Header?.ToString() ?? "")
            .ToList();

    private static ResourceDictionary LoadStyleResource(string fileName)
        => Assert.IsType<ResourceDictionary>(
            Application.LoadComponent(new Uri(
                $"/Woong.MonitorStack.Windows.App;component/Styles/{fileName}",
                UriKind.Relative)));

    private static void AssertStyleSetter<T>(Style style, DependencyProperty property, T expectedValue)
    {
        Setter setter = FindSetter(style, property);
        T actualValue = Assert.IsType<T>(setter.Value);

        Assert.Equal(expectedValue, actualValue);
    }

    private static void AssertSolidBrush(ResourceDictionary resources, string key, Color expectedColor)
    {
        Assert.True(resources.Contains(key), $"Resource dictionary should contain `{key}`.");
        var brush = Assert.IsType<SolidColorBrush>(resources[key]);

        Assert.Equal(expectedColor, brush.Color);
    }

    private static Setter FindSetter(Style style, DependencyProperty property)
    {
        Style? current = style;

        while (current is not null)
        {
            foreach (Setter setter in current.Setters.OfType<Setter>())
            {
                if (setter.Property == property)
                {
                    return setter;
                }
            }

            current = current.BasedOn;
        }

        throw new InvalidOperationException($"Style '{style}' does not set '{property.Name}'.");
    }

    private static IReadOnlySet<string> CollectText(DependencyObject root)
    {
        var values = new HashSet<string>(StringComparer.Ordinal);
        foreach (DependencyObject element in FindVisualDescendants<DependencyObject>(root))
        {
            if (element is TextBlock { Text.Length: > 0 } textBlock)
            {
                values.Add(textBlock.Text);
            }

            if (element is ContentControl { Content: string content } && !string.IsNullOrWhiteSpace(content))
            {
                values.Add(content);
            }
        }

        return values;
    }

    private static TextBlock FindTextBlock(DependencyObject root, string text)
    {
        TextBlock? match = FindVisualDescendants<TextBlock>(root)
            .FirstOrDefault(textBlock => string.Equals(textBlock.Text, text, StringComparison.Ordinal));

        return match ?? throw new InvalidOperationException($"Could not find TextBlock with text '{text}'.");
    }

    private sealed record TestDashboard(
        DateTimeOffset Now,
        FakeDashboardDataSource DataSource,
        DashboardViewModel ViewModel);

    private sealed class FakeDashboardDataSource(
        IReadOnlyList<FocusSession> focusSessions,
        IReadOnlyList<WebSession> webSessions) : IDashboardDataSource
    {
        public DateTimeOffset LastFocusQueryStartedAtUtc { get; private set; }

        public DateTimeOffset LastFocusQueryEndedAtUtc { get; private set; }

        public IReadOnlyList<FocusSession> QueryFocusSessions(DateTimeOffset startedAtUtc, DateTimeOffset endedAtUtc)
        {
            LastFocusQueryStartedAtUtc = startedAtUtc;
            LastFocusQueryEndedAtUtc = endedAtUtc;

            return focusSessions;
        }

        public IReadOnlyList<WebSession> QueryWebSessions(DateTimeOffset startedAtUtc, DateTimeOffset endedAtUtc)
            => webSessions;
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IDashboardClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }

    private sealed class CountingCommand : System.Windows.Input.ICommand
    {
        public int ExecuteCount { get; private set; }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
            => true;

        public void Execute(object? parameter)
        {
            ExecuteCount++;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
