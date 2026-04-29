using System.Text;
using Woong.MonitorStack.Windows.Browser;

namespace Woong.MonitorStack.Windows.Tests.Browser;

public sealed class ChromeNativeMessageReceiverTests
{
    [Fact]
    public async Task ReadNextAsync_ReadsLengthPrefixedActiveTabMessage()
    {
        const string json = """
            {
              "type": "activeTabChanged",
              "browserFamily": "Chrome",
              "windowId": 7,
              "tabId": 42,
              "url": "https://sub.example.com/path",
              "title": "Example",
              "observedAtUtc": "2026-04-28T01:02:03Z"
            }
            """;
        using var stream = new MemoryStream();
        var payload = Encoding.UTF8.GetBytes(json);
        stream.Write(BitConverter.GetBytes(payload.Length));
        stream.Write(payload);
        stream.Position = 0;

        var message = await ChromeNativeMessageReceiver.ReadNextAsync(stream, CancellationToken.None);

        Assert.NotNull(message);
        Assert.Equal("example.com", message.Domain);
        Assert.Equal(42, message.TabId);
    }

    [Fact]
    public async Task ReadNextAsync_RejectsOversizedNativeMessage()
    {
        using var stream = new MemoryStream();
        stream.Write(BitConverter.GetBytes(1024 * 1024 + 1));
        stream.Position = 0;

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            ChromeNativeMessageReceiver.ReadNextAsync(stream, CancellationToken.None));
    }
}
