using Woong.MonitorStack.Windows.Browser;

namespace Woong.MonitorStack.Windows.Tests.Browser;

public sealed class BrowserUrlSanitizerTests
{
    [Fact]
    public void Sanitize_WhenPolicyIsOff_RemovesUrlAndDomain()
    {
        var sanitizer = new BrowserUrlSanitizer();

        BrowserActivitySnapshot sanitized = sanitizer.Sanitize(
            CreateSnapshot("https://github.com/org/repo?token=secret#section", domain: "github.com"),
            BrowserUrlStoragePolicy.Off);

        Assert.Null(sanitized.Url);
        Assert.Null(sanitized.Domain);
        Assert.Equal(CaptureMethod.None, sanitized.CaptureMethod);
        Assert.Equal(CaptureConfidence.Unknown, sanitized.CaptureConfidence);
    }

    [Fact]
    public void Sanitize_WhenPolicyIsDomainOnly_StoresDomainButNotFullUrl()
    {
        var sanitizer = new BrowserUrlSanitizer();

        BrowserActivitySnapshot sanitized = sanitizer.Sanitize(
            CreateSnapshot("https://www.github.com/org/repo?token=secret#section", domain: null),
            BrowserUrlStoragePolicy.DomainOnly);

        Assert.Null(sanitized.Url);
        Assert.Equal("github.com", sanitized.Domain);
        Assert.Equal(CaptureMethod.UIAutomationAddressBar, sanitized.CaptureMethod);
        Assert.Equal(CaptureConfidence.High, sanitized.CaptureConfidence);
    }

    [Fact]
    public void Sanitize_WhenPolicyIsFullUrl_StripsFragmentAndStoresDomain()
    {
        var sanitizer = new BrowserUrlSanitizer();

        BrowserActivitySnapshot sanitized = sanitizer.Sanitize(
            CreateSnapshot("https://learn.microsoft.com/dotnet/csharp?view=net-10.0#classes", domain: null),
            BrowserUrlStoragePolicy.FullUrl);

        Assert.Equal("https://learn.microsoft.com/dotnet/csharp?view=net-10.0", sanitized.Url);
        Assert.Equal("microsoft.com", sanitized.Domain);
    }

    private static BrowserActivitySnapshot CreateSnapshot(string? url, string? domain)
        => new(
            capturedAtUtc: DateTimeOffset.Parse("2026-04-29T00:00:00Z"),
            browserName: "Chrome",
            processName: "chrome.exe",
            processId: 42,
            windowHandle: 100,
            windowTitle: "Browser",
            tabTitle: "Tab",
            url,
            domain,
            CaptureMethod.UIAutomationAddressBar,
            CaptureConfidence.High,
            isPrivateOrUnknown: false);
}
