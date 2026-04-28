namespace Woong.MonitorStack.Domain.Common;

public sealed record AppFamily
{
    public AppFamily(string key, string displayName)
    {
        Key = RequiredText.Ensure(key, nameof(key));
        DisplayName = RequiredText.Ensure(displayName, nameof(displayName));
    }

    public string Key { get; }

    public string DisplayName { get; }
}
