using Woong.MonitorStack.Windows.Sync;

namespace Woong.MonitorStack.Windows.Tests.Sync;

public sealed class WindowsSyncClientOptionsTests
{
    [Fact]
    public void Constructor_WhenHttpsEndpointAndTokenProvided_StoresServerBaseUriAndToken()
    {
        var options = new WindowsSyncClientOptions(
            new Uri("https://monitor.example"),
            "device-token-1");

        Assert.Equal(new Uri("https://monitor.example/"), options.ServerBaseUri);
        Assert.Equal("device-token-1", options.DeviceToken);
    }

    [Fact]
    public void Constructor_WhenLoopbackHttpEndpointProvided_AllowsLocalDevelopment()
    {
        var options = new WindowsSyncClientOptions(
            new Uri("http://localhost:5050"),
            "device-token-1");

        Assert.Equal(new Uri("http://localhost:5050/"), options.ServerBaseUri);
    }

    [Theory]
    [InlineData("http://monitor.example")]
    [InlineData("https://user:secret@monitor.example")]
    [InlineData("https://monitor.example/api?token=secret")]
    public void Constructor_WhenEndpointIsUnsafe_ThrowsWithoutLeakingConfiguredValue(string endpoint)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new WindowsSyncClientOptions(new Uri(endpoint), "device-token-1"));

        Assert.DoesNotContain("secret", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(endpoint, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_WhenDeviceTokenIsMissing_ThrowsWithoutLeakingEndpoint()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new WindowsSyncClientOptions(new Uri("https://monitor.example"), " "));

        Assert.DoesNotContain("monitor.example", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
