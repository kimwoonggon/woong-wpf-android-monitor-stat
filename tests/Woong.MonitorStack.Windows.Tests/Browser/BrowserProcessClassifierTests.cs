using Woong.MonitorStack.Windows.Browser;

namespace Woong.MonitorStack.Windows.Tests.Browser;

public sealed class BrowserProcessClassifierTests
{
    [Theory]
    [InlineData("chrome.exe", "Chrome")]
    [InlineData("msedge.exe", "Microsoft Edge")]
    [InlineData("firefox.exe", "Firefox")]
    [InlineData("brave.exe", "Brave")]
    [InlineData("CHROME.EXE", "Chrome")]
    [InlineData("chrome", "Chrome")]
    public void Classify_WhenProcessIsSupportedBrowser_ReturnsBrowserName(
        string processName,
        string expectedBrowserName)
    {
        var classifier = new BrowserProcessClassifier();

        BrowserProcessClassification classification = classifier.Classify(processName);

        Assert.True(classification.IsBrowser);
        Assert.Equal(expectedBrowserName, classification.BrowserName);
    }

    [Theory]
    [InlineData("Code.exe")]
    [InlineData("notepad.exe")]
    [InlineData("")]
    [InlineData("   ")]
    public void Classify_WhenProcessIsNotSupportedBrowser_ReturnsNonBrowser(string processName)
    {
        var classifier = new BrowserProcessClassifier();

        BrowserProcessClassification classification = classifier.Classify(processName);

        Assert.False(classification.IsBrowser);
        Assert.Null(classification.BrowserName);
    }
}
