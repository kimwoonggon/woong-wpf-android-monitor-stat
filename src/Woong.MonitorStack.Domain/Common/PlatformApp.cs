namespace Woong.MonitorStack.Domain.Common;

public sealed record PlatformApp
{
    public PlatformApp(
        Platform platform,
        string appKey,
        string displayName,
        string? appFamilyKey)
    {
        Platform = platform;
        AppKey = RequiredText.Ensure(appKey, nameof(appKey));
        DisplayName = RequiredText.Ensure(displayName, nameof(displayName));
        AppFamilyKey = string.IsNullOrWhiteSpace(appFamilyKey) ? null : appFamilyKey;
    }

    public Platform Platform { get; }

    public string AppKey { get; }

    public string DisplayName { get; }

    public string? AppFamilyKey { get; }
}
