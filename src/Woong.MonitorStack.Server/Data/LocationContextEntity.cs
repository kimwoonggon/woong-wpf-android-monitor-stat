namespace Woong.MonitorStack.Server.Data;

public sealed class LocationContextEntity
{
    public long Id { get; set; }

    public Guid DeviceId { get; set; }

    public string ClientContextId { get; set; } = "";

    public DateTimeOffset CapturedAtUtc { get; set; }

    public DateOnly LocalDate { get; set; }

    public string TimezoneId { get; set; } = "";

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public double? AccuracyMeters { get; set; }

    public string CaptureMode { get; set; } = "";

    public string PermissionState { get; set; } = "";

    public string Source { get; set; } = "";

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
