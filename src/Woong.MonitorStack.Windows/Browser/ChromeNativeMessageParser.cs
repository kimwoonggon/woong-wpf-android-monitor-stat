using System.Text.Json;

namespace Woong.MonitorStack.Windows.Browser;

public static class ChromeNativeMessageParser
{
    public static ChromeTabChangedMessage ParseActiveTabChanged(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Value must not be empty.", nameof(json));
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var type = root.GetProperty("type").GetString();
        if (!string.Equals(type, "activeTabChanged", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unsupported Chrome native message type '{type}'.");
        }

        string? clientEventId = root.TryGetProperty("clientEventId", out JsonElement clientEventIdElement)
            ? clientEventIdElement.GetString()
            : null;

        return ChromeTabChangedMessage.FromExtensionPayload(
            windowId: root.GetProperty("windowId").GetInt32(),
            tabId: root.GetProperty("tabId").GetInt32(),
            url: root.GetProperty("url").GetString() ?? "",
            title: root.GetProperty("title").GetString() ?? "",
            observedAtUtc: root.GetProperty("observedAtUtc").GetDateTimeOffset(),
            browserFamily: root.GetProperty("browserFamily").GetString() ?? "",
            clientEventId: clientEventId);
    }
}
