namespace Woong.MonitorStack.Server.Data;

public sealed class WebSessionEntity
{
    public long Id { get; set; }

    public Guid DeviceId { get; set; }

    public string FocusSessionId { get; set; } = "";

    public string BrowserFamily { get; set; } = "";

    public string Url { get; set; } = "";

    public string Domain { get; set; } = "";

    public string PageTitle { get; set; } = "";

    public DateTimeOffset StartedAtUtc { get; set; }

    public DateTimeOffset EndedAtUtc { get; set; }

    public long DurationMs { get; set; }
}
