namespace Woong.MonitorStack.Domain.Contracts;

public sealed record LocationContextUploadItem
{
    public LocationContextUploadItem(
        string clientContextId,
        DateTimeOffset capturedAtUtc,
        DateOnly localDate,
        string timezoneId,
        double? latitude,
        double? longitude,
        double? accuracyMeters,
        string captureMode,
        string permissionState,
        string source)
    {
        ClientContextId = RequiredContractText.Ensure(clientContextId, nameof(clientContextId));
        CapturedAtUtc = capturedAtUtc.ToUniversalTime();
        LocalDate = localDate;
        TimezoneId = RequiredContractText.Ensure(timezoneId, nameof(timezoneId));
        Latitude = latitude;
        Longitude = longitude;
        AccuracyMeters = accuracyMeters;
        CaptureMode = RequiredContractText.Ensure(captureMode, nameof(captureMode));
        PermissionState = RequiredContractText.Ensure(permissionState, nameof(permissionState));
        Source = RequiredContractText.Ensure(source, nameof(source));
    }

    public string ClientContextId { get; }

    public DateTimeOffset CapturedAtUtc { get; }

    public DateOnly LocalDate { get; }

    public string TimezoneId { get; }

    public double? Latitude { get; }

    public double? Longitude { get; }

    public double? AccuracyMeters { get; }

    public string CaptureMode { get; }

    public string PermissionState { get; }

    public string Source { get; }
}
