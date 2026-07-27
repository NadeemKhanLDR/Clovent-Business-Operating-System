namespace Clovent.Core.Results;

public sealed class DoctorCheckItem
{
    public required string Name { get; set; }
    public required bool IsPassed { get; set; }
    public required string Details { get; set; }
}

public sealed class DoctorResult
{
    public bool IsHealthy => Checks.All(c => c.IsPassed);
    public List<DoctorCheckItem> Checks { get; } = new();
}
