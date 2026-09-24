namespace Clovent.Restaurant.Application.OrderHealth;

/// <summary>The thresholds that split order wait times into health colors - a POCO the Desktop layer persists via its settings JSON (no table).</summary>
public sealed record OrderHealthThresholds
{
    /// <summary>Minutes after which an open order turns Orange. Defaults to 10.</summary>
    public int GreenMinutes { get; init; } = 10;

    /// <summary>Minutes after which an Orange order turns Red. Defaults to 20.</summary>
    public int OrangeMinutes { get; init; } = 20;

    /// <summary>Validates that the thresholds are positive and ordered (green before orange before red).</summary>
    /// <exception cref="ArgumentOutOfRangeException">Either threshold is not positive, or <see cref="GreenMinutes"/> is not less than <see cref="OrangeMinutes"/>.</exception>
    public void Validate()
    {
        if (GreenMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(GreenMinutes), GreenMinutes, "Green threshold must be positive.");
        if (OrangeMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(OrangeMinutes), OrangeMinutes, "Orange threshold must be positive.");
        if (GreenMinutes >= OrangeMinutes)
            throw new ArgumentOutOfRangeException(nameof(OrangeMinutes), OrangeMinutes, "Green threshold must be less than the orange threshold.");
    }
}
