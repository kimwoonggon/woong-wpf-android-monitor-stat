namespace Woong.MonitorStack.Windows.Browser;

public sealed record BrowserActivitySnapshot
{
    public BrowserActivitySnapshot(
        DateTimeOffset capturedAtUtc,
        string browserName,
        string processName,
        int? processId,
        long? windowHandle,
        string? windowTitle,
        string? tabTitle,
        string? url,
        string? domain,
        CaptureMethod captureMethod,
        CaptureConfidence captureConfidence,
        bool? isPrivateOrUnknown)
    {
        CapturedAtUtc = capturedAtUtc.ToUniversalTime();
        BrowserName = EnsureRequired(browserName, nameof(browserName));
        ProcessName = EnsureRequired(processName, nameof(processName));
        ProcessId = processId;
        WindowHandle = windowHandle;
        WindowTitle = NormalizeOptional(windowTitle);
        TabTitle = NormalizeOptional(tabTitle);
        Url = NormalizeOptional(url);
        Domain = NormalizeOptional(domain);
        CaptureMethod = captureMethod;
        CaptureConfidence = captureConfidence;
        IsPrivateOrUnknown = isPrivateOrUnknown;
    }

    public DateTimeOffset CapturedAtUtc { get; }

    public string BrowserName { get; }

    public string ProcessName { get; }

    public int? ProcessId { get; }

    public long? WindowHandle { get; }

    public string? WindowTitle { get; }

    public string? TabTitle { get; }

    public string? Url { get; }

    public string? Domain { get; }

    public CaptureMethod CaptureMethod { get; }

    public CaptureConfidence CaptureConfidence { get; }

    public bool? IsPrivateOrUnknown { get; }

    private static string EnsureRequired(string value, string parameterName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value must not be empty.", parameterName)
            : value.Trim();

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
