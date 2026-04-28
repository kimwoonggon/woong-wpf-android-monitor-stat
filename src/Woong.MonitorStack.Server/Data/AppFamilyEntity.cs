namespace Woong.MonitorStack.Server.Data;

public sealed class AppFamilyEntity
{
    public long Id { get; set; }

    public string Key { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public DateTimeOffset CreatedAtUtc { get; set; }
}
