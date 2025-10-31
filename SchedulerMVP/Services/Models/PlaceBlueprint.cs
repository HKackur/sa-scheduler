namespace SchedulerMVP.Services.Models;

public sealed class PlaceBlueprint
{
    public Guid PlaceId { get; set; }
    public string TopAreaName { get; set; } = "Yta";
    public List<string> LeafNames { get; set; } = new();
    public List<MidAreaBlueprint> MidAreas { get; set; } = new();
}

public sealed class MidAreaBlueprint
{
    public string Name { get; set; } = string.Empty;
    public List<string> LeafNames { get; set; } = new();
}


