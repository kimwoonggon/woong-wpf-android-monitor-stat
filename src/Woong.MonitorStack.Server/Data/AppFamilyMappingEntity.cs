namespace Woong.MonitorStack.Server.Data;

public sealed class AppFamilyMappingEntity
{
    public long Id { get; set; }

    public long AppFamilyId { get; set; }

    public string MappingType { get; set; } = "";

    public string MatchKey { get; set; } = "";

    public DateTimeOffset CreatedAtUtc { get; set; }

    public AppFamilyEntity? AppFamily { get; set; }
}
