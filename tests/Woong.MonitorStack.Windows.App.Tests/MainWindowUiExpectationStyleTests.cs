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

    [Fact]
    public void ButtonStyleDictionary_DefinesReadableDashboardButtonStyles()
        => RunOnStaThread(() =>
        {
            ResourceDictionary resources = LoadStyleResource("Buttons.xaml");

            Assert.True(resources.Contains("DashboardButtonStyle"));
            Assert.True(resources.Contains("PrimaryButtonStyle"));
            Assert.True(resources.Contains("DangerButtonStyle"));
            Assert.True(resources.Contains("SecondaryButtonStyle"));
            Assert.True(resources.Contains("PeriodButtonStyle"));
            Assert.True(resources.Contains("CompactActionButtonStyle"));

            Style primaryButtonStyle = Assert.IsType<Style>(resources["PrimaryButtonStyle"]);
            AssertStyleSetter(primaryButtonStyle, Button.MinWidthProperty, 96.0);
            AssertStyleSetter(primaryButtonStyle, Button.MinHeightProperty, 40.0);
            AssertStyleSetter(primaryButtonStyle, Control.PaddingProperty, new Thickness(12, 0, 12, 0));

            Style compactActionButtonStyle = Assert.IsType<Style>(resources["CompactActionButtonStyle"]);
            AssertStyleSetter(compactActionButtonStyle, Button.MinWidthProperty, 72.0);
            AssertStyleSetter(compactActionButtonStyle, Button.MinHeightProperty, 28.0);
            AssertStyleSetter(compactActionButtonStyle, Control.PaddingProperty, new Thickness(10, 0, 10, 0));
            AssertStyleSetter(compactActionButtonStyle, Control.FontSizeProperty, 12.0);
        });

    [Fact]
    public void CardStyleDictionary_DefinesReusableDashboardCardStyles()
        => RunOnStaThread(() =>
        {
            ResourceDictionary resources = LoadStyleResource("Cards.xaml");

            Assert.True(resources.Contains("DashboardCardBorderStyle"));
            Assert.True(resources.Contains("CompactSurfaceBorderStyle"));

            Style dashboardCardStyle = Assert.IsType<Style>(resources["DashboardCardBorderStyle"]);
            AssertStyleSetter(dashboardCardStyle, Border.CornerRadiusProperty, new CornerRadius(8));
            AssertStyleSetter(dashboardCardStyle, Border.PaddingProperty, new Thickness(18));
            AssertStyleSetter(dashboardCardStyle, Border.BorderThicknessProperty, new Thickness(1));

            Style compactSurfaceStyle = Assert.IsType<Style>(resources["CompactSurfaceBorderStyle"]);
            AssertStyleSetter(compactSurfaceStyle, Border.CornerRadiusProperty, new CornerRadius(8));
            AssertStyleSetter(compactSurfaceStyle, Border.PaddingProperty, new Thickness(12));
            AssertStyleSetter(compactSurfaceStyle, Border.BorderThicknessProperty, new Thickness(1));
        });

    [Fact]
    public void ColorStyleDictionary_DefinesCoreDashboardBrushes()
        => RunOnStaThread(() =>
        {
            ResourceDictionary resources = LoadStyleResource("Colors.xaml");

            AssertSolidBrush(resources, "AppBackgroundBrush", Color.FromRgb(0xF6, 0xF8, 0xFB));
            AssertSolidBrush(resources, "SurfaceBrush", Colors.White);
            AssertSolidBrush(resources, "BorderBrush", Color.FromRgb(0xDC, 0xE1, 0xE8));
            AssertSolidBrush(resources, "TextPrimaryBrush", Color.FromRgb(0x16, 0x20, 0x33));
            AssertSolidBrush(resources, "TextMutedBrush", Color.FromRgb(0x5A, 0x64, 0x72));
        });

    [Fact]
    public void MainWindow_UsesSharedBackgroundBrush()
        => RunOnStaThread(() =>
        {
            ResourceDictionary colors = LoadStyleResource("Colors.xaml");
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);
            window.Resources.MergedDictionaries.Add(colors);

            try
            {
                window.Show();
                window.UpdateLayout();

                object backgroundBrush = window.FindResource("AppBackgroundBrush");
                Assert.Same(backgroundBrush, window.Background);
                var root = Assert.IsType<Grid>(window.Content);
                Assert.Same(backgroundBrush, root.Background);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void TypographyStyleDictionary_DefinesDashboardTextStyles()
        => RunOnStaThread(() =>
        {
            ResourceDictionary resources = LoadStyleResource("Typography.xaml");

            Assert.True(resources.Contains("HeadingTextStyle"));
            Assert.True(resources.Contains("SubtitleTextStyle"));
            Assert.True(resources.Contains("SectionTitleTextStyle"));
            Assert.True(resources.Contains("BodyTextStyle"));
            Assert.True(resources.Contains("MutedTextStyle"));

            Style headingStyle = Assert.IsType<Style>(resources["HeadingTextStyle"]);
            AssertStyleSetter(headingStyle, TextBlock.FontSizeProperty, 24.0);
            AssertStyleSetter(headingStyle, TextBlock.FontWeightProperty, FontWeights.SemiBold);

            Style sectionTitleStyle = Assert.IsType<Style>(resources["SectionTitleTextStyle"]);
            AssertStyleSetter(sectionTitleStyle, TextBlock.FontSizeProperty, 16.0);
            AssertStyleSetter(sectionTitleStyle, TextBlock.FontWeightProperty, FontWeights.SemiBold);

            Style mutedStyle = Assert.IsType<Style>(resources["MutedTextStyle"]);
            AssertStyleSetter(mutedStyle, TextBlock.FontSizeProperty, 12.0);
        });

    [Fact]
    public void DataGridStyleDictionary_DefinesReadableSessionGridStyle()
        => RunOnStaThread(() =>
        {
            ResourceDictionary resources = LoadStyleResource("DataGrid.xaml");

            Assert.True(resources.Contains("SessionDataGridStyle"));

            Style sessionGridStyle = Assert.IsType<Style>(resources["SessionDataGridStyle"]);
            AssertStyleSetter(sessionGridStyle, DataGrid.AutoGenerateColumnsProperty, false);
            AssertStyleSetter(sessionGridStyle, DataGrid.CanUserAddRowsProperty, false);
            AssertStyleSetter(sessionGridStyle, DataGrid.CanUserDeleteRowsProperty, false);
            AssertStyleSetter(sessionGridStyle, DataGrid.HeadersVisibilityProperty, DataGridHeadersVisibility.Column);
            AssertStyleSetter(sessionGridStyle, DataGrid.IsReadOnlyProperty, true);
            AssertStyleSetter(sessionGridStyle, FrameworkElement.MarginProperty, new Thickness(0, 12, 0, 0));
            AssertStyleSetter(sessionGridStyle, FrameworkElement.MinHeightProperty, 260.0);
            AssertStyleSetter(
                sessionGridStyle,
                ScrollViewer.HorizontalScrollBarVisibilityProperty,
                ScrollBarVisibility.Auto);
        });

    [Fact]
    public void TabsStyleDictionary_DefinesReadableDashboardTabsStyle()
        => RunOnStaThread(() =>
        {
            ResourceDictionary resources = LoadStyleResource("Tabs.xaml");

            Assert.True(resources.Contains("DashboardTabControlStyle"));
            Assert.True(resources.Contains("DashboardTabItemStyle"));

            Style tabItemStyle = Assert.IsType<Style>(resources["DashboardTabItemStyle"]);
            AssertStyleSetter(tabItemStyle, FrameworkElement.MinHeightProperty, 36.0);
            AssertStyleSetter(tabItemStyle, Control.PaddingProperty, new Thickness(14, 8, 14, 8));
        });

}
