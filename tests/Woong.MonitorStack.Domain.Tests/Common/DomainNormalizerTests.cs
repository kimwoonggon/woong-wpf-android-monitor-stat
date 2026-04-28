using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Domain.Tests.Common;

public sealed class DomainNormalizerTests
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=abc", "youtube.com")]
    [InlineData("news.google.co.kr", "google.co.kr")]
    [InlineData("HTTP://Sub.Example.COM/path", "example.com")]
    public void ExtractRegistrableDomain_NormalizesUrlOrHost(string input, string expected)
    {
        var domain = DomainNormalizer.ExtractRegistrableDomain(input);

        Assert.Equal(expected, domain);
    }
}
