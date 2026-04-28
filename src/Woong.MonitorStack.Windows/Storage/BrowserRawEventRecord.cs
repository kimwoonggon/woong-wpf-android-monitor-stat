namespace Woong.MonitorStack.Windows.Storage;

public sealed record BrowserRawEventRecord(
    string BrowserFamily,
    int WindowId,
    int TabId,
    string? Url,
    string? Title,
    string? Domain,
    DateTimeOffset ObservedAtUtc);
