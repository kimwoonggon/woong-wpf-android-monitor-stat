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
    public void DashboardView_HostsHeaderStatusBarAndPreservesHeaderContent()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                window.Show();
                window.UpdateLayout();

                HeaderStatusBar header = FindByAutomationId<HeaderStatusBar>(window, "HeaderArea");
                IReadOnlySet<string> headerText = CollectText(header);

                Assert.Contains("Woong Monitor Stack", headerText);
                Assert.Contains("Windows Focus Tracker", headerText);
                Assert.Contains("Tracking Stopped", headerText);
                Assert.Contains("Sync Off", headerText);
                Assert.Contains("Privacy Safe", headerText);
                Assert.DoesNotContain("chrome.exe", headerText);
                Assert.NotNull(FindByAutomationId<StatusBadge>(header, "TrackingStatusBadge"));
                Assert.NotNull(FindByAutomationId<StatusBadge>(header, "SyncStatusBadge"));
                Assert.NotNull(FindByAutomationId<StatusBadge>(header, "PrivacyStatusBadge"));
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void HeaderStatusBar_UsesSharedBadgeColorResources()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                window.Show();
                window.UpdateLayout();

                HeaderStatusBar header = FindByAutomationId<HeaderStatusBar>(window, "HeaderArea");
                AssertBadgeUsesBrushes(
                    header,
                    "TrackingStatusBadge",
                    "TrackingBadgeBackgroundBrush",
                    "TrackingBadgeBorderBrush",
                    "TrackingBadgeTextBrush");
                AssertBadgeUsesBrushes(
                    header,
                    "SyncStatusBadge",
                    "SyncBadgeBackgroundBrush",
                    "SyncBadgeBorderBrush",
                    "SyncBadgeTextBrush");
                AssertBadgeUsesBrushes(
                    header,
                    "PrivacyStatusBadge",
                    "PrivacyBadgeBackgroundBrush",
                    "PrivacyBadgeBorderBrush",
                    "PrivacyBadgeTextBrush");
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void StatusBadge_RendersTextAndPreservesAutomationId()
        => RunOnStaThread(() =>
        {
            var badge = new StatusBadge
            {
                Text = "Tracking Stopped"
            };
            AutomationProperties.SetAutomationId(badge, "TestStatusBadge");
            var window = new Window { Content = badge };

            try
            {
                window.Show();
                window.UpdateLayout();

                StatusBadge renderedBadge = FindByAutomationId<StatusBadge>(window, "TestStatusBadge");
                Assert.Same(badge, renderedBadge);
                Assert.Contains("Tracking Stopped", CollectText(renderedBadge));
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void StatusBadge_UsesSharedShapeAndTextStyles()
        => RunOnStaThread(() =>
        {
            var uniqueBackground = new SolidColorBrush(Color.FromRgb(0x11, 0x22, 0x33));
            var badge = new StatusBadge
            {
                Text = "Tracking Running",
                BadgeBackground = uniqueBackground
            };
            var window = new Window { Content = badge };

            try
            {
                window.Show();
                window.UpdateLayout();

                Border border = Assert.Single(
                    FindVisualDescendants<Border>(badge).Distinct(),
                    candidate => ReferenceEquals(candidate.Background, uniqueBackground)
                        && candidate.Child is TextBlock textBlock
                        && string.Equals(textBlock.Text, "Tracking Running", StringComparison.Ordinal));
                Style borderStyle = Assert.IsType<Style>(border.Style);
                AssertStyleSetter(borderStyle, Border.CornerRadiusProperty, new CornerRadius(16));
                AssertStyleSetter(borderStyle, Border.PaddingProperty, new Thickness(14, 8, 14, 8));
                AssertStyleSetter(borderStyle, Border.BorderThicknessProperty, new Thickness(1));

                TextBlock label = FindTextBlock(badge, "Tracking Running");
                AssertStatusBadgeTextStyle(label);
                Assert.Null(badge.Background);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void DetailRow_RendersLabelAndValueWithStableValueAutomationId()
        => RunOnStaThread(() =>
        {
            var row = new DetailRow
            {
                Label = "Current app",
                Value = "Chrome",
                ValueAutomationId = "CurrentAppNameText",
                IconGlyph = "A",
                IconAutomationId = "CurrentAppNameIcon"
            };
            var window = new Window { Content = row };

            try
            {
                window.Show();
                window.UpdateLayout();

                IReadOnlySet<string> text = CollectText(row);
                Assert.Contains("Current app", text);
                Assert.Contains("Chrome", text);
                TextBlock value = FindByAutomationId<TextBlock>(row, "CurrentAppNameText");
                Assert.Equal("Chrome", value.Text);
                Assert.Equal(TextTrimming.CharacterEllipsis, value.TextTrimming);
                TextBlock icon = FindByAutomationId<TextBlock>(row, "CurrentAppNameIcon");
                Assert.Equal("A", icon.Text);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void SectionCard_RendersContentAndOptionalActionCommand()
        => RunOnStaThread(() =>
        {
            var command = new CountingCommand();
            var card = new SectionCard
            {
                Title = "앱별 집중 시간",
                ActionText = "상세보기",
                ActionCommand = command,
                CardContent = new TextBlock { Text = "Chrome" }
            };
            var window = new Window { Content = card };

            try
            {
                window.Show();
                window.UpdateLayout();

                IReadOnlySet<string> text = CollectText(card);
                Assert.Contains("앱별 집중 시간", text);
                Assert.Contains("상세보기", text);
                Assert.Contains("Chrome", text);

                Button actionButton = FindByAutomationId<Button>(card, "SectionCardActionButton");
                AssertCompactActionButton(actionButton);
                Invoke(actionButton);
                Assert.Equal(1, command.ExecuteCount);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void MetricCard_RendersLabelValueAndSubtitle()
        => RunOnStaThread(() =>
        {
            var card = new MetricCard
            {
                Label = "Active Focus",
                Value = "3h 12m",
                Subtitle = "Today's focused foreground time"
            };
            var window = new Window { Content = card };

            try
            {
                window.Show();
                window.UpdateLayout();

                IReadOnlySet<string> cardText = CollectText(card);
                Assert.Contains("Active Focus", cardText);
                Assert.Contains("3h 12m", cardText);
                Assert.Contains("Today's focused foreground time", cardText);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void MetricCard_RendersGoalIconAccentSlot()
        => RunOnStaThread(() =>
        {
            var accentBrush = new SolidColorBrush(Color.FromRgb(0x0F, 0x6B, 0xDE));
            var iconBackgroundBrush = new SolidColorBrush(Color.FromRgb(0xEA, 0xF3, 0xFF));
            var card = new MetricCard
            {
                Label = "Active Focus",
                Value = "3h 12m",
                Subtitle = "Today's focused foreground time",
                IconText = "◎",
                AccentBrush = accentBrush,
                IconBackground = iconBackgroundBrush
            };
            var window = new Window { Content = card };

            try
            {
                window.Show();
                window.UpdateLayout();

                Border iconCircle = FindByAutomationId<Border>(card, "MetricCardIconCircle");
                TextBlock icon = FindByAutomationId<TextBlock>(card, "MetricCardIconText");

                Assert.Same(iconBackgroundBrush, iconCircle.Background);
                Assert.Same(accentBrush, icon.Foreground);
                Assert.Equal("◎", icon.Text);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void MetricCard_UsesSharedLabelTypography()
        => RunOnStaThread(() =>
        {
            var card = new MetricCard
            {
                Label = "Active Focus",
                Value = "3h 12m",
                Subtitle = "Today's focused foreground time"
            };
            var window = new Window { Content = card };

            try
            {
                window.Show();
                window.UpdateLayout();

                TextBlock label = FindTextBlock(card, "Active Focus");

                AssertMetricLabelTextStyle(label);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void EmptyState_RendersBoundTextWithTextAutomationId()
        => RunOnStaThread(() =>
        {
            var emptyState = new EmptyState
            {
                Text = "No data for selected period",
                TextAutomationId = "TestEmptyStateText"
            };
            var window = new Window { Content = emptyState };

            try
            {
                window.Show();
                window.UpdateLayout();

                TextBlock text = FindByAutomationId<TextBlock>(emptyState, "TestEmptyStateText");
                Assert.Equal("No data for selected period", text.Text);
                Style style = Assert.IsType<Style>(text.Style);
                AssertStyleSetter(style, TextBlock.FontSizeProperty, 13.0);

                Setter foregroundSetter = FindSetter(style, TextBlock.ForegroundProperty);
                var foregroundBrush = Assert.IsType<SolidColorBrush>(foregroundSetter.Value);
                Assert.Equal(Color.FromRgb(0x5A, 0x64, 0x72), foregroundBrush.Color);
            }
            finally
            {
                window.Close();
            }
        });

}
