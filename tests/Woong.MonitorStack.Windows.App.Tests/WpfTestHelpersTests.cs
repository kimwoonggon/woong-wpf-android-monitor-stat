using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class WpfTestHelpersTests
{
    [Fact]
    public void RunOnStaThread_ExecutesActionWithDispatcherSynchronizationContext()
    {
        ApartmentState? apartmentState = null;
        SynchronizationContext? synchronizationContext = null;
        Dispatcher? dispatcher = null;

        WpfTestHelpers.RunOnStaThread(() =>
        {
            apartmentState = Thread.CurrentThread.GetApartmentState();
            synchronizationContext = SynchronizationContext.Current;
            dispatcher = Dispatcher.CurrentDispatcher;
        });

        Assert.Equal(ApartmentState.STA, apartmentState);
        Assert.IsType<DispatcherSynchronizationContext>(synchronizationContext);
        Assert.NotNull(dispatcher);
    }

    [Fact]
    public void RunWindowTest_ShowsAndUpdatesWindowBeforeAssertion()
    {
        bool wasVisibleDuringAssertion = false;
        bool hadLayoutDuringAssertion = false;
        bool wasClosed = false;

        WpfTestHelpers.RunWindowTest(
            () =>
            {
                var window = new Window
                {
                    Width = 320,
                    Height = 180,
                    Content = new Border
                    {
                        Width = 120,
                        Height = 40
                    }
                };
                window.Closed += (_, _) => wasClosed = true;

                return window;
            },
            window =>
            {
                wasVisibleDuringAssertion = window.IsVisible;
                hadLayoutDuringAssertion = window.ActualWidth > 0 && window.ActualHeight > 0;
            });

        Assert.True(wasVisibleDuringAssertion);
        Assert.True(hadLayoutDuringAssertion);
        Assert.True(wasClosed);
    }

    [Fact]
    public void RunWindowTest_ClosesWindowBeforeRethrowingAssertionFailure()
    {
        bool wasClosed = false;

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() =>
            WpfTestHelpers.RunWindowTest(
                () =>
                {
                    var window = new Window();
                    window.Closed += (_, _) => wasClosed = true;

                    return window;
                },
                _ => throw new InvalidOperationException("expected failure")));

        Assert.Equal("expected failure", failure.Message);
        Assert.True(wasClosed);
    }

    [Fact]
    public void FindVisualDescendants_ReturnsRootAndNestedMatches()
        => WpfTestHelpers.RunOnStaThread(() =>
        {
            var root = new StackPanel();
            root.Children.Add(new TextBlock { Text = "first" });
            root.Children.Add(new Border
            {
                Child = new TextBlock { Text = "second" }
            });

            IReadOnlyList<TextBlock> textBlocks = WpfTestHelpers.FindVisualDescendants<TextBlock>(root);

            Assert.Equal(["first", "second"], textBlocks.Select(textBlock => textBlock.Text));
        });
}
