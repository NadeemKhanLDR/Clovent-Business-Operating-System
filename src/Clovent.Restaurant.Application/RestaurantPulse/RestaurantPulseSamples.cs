namespace Clovent.Restaurant.Application.RestaurantPulse;

/// <summary>One aggregated line sample fed to <see cref="RestaurantPulseCalculator"/> - variant, display name, quantity, and revenue.</summary>
public sealed record RestaurantPulseLineSample(Guid VariantId, string ProductName, decimal Quantity, decimal LineTotal);

/// <summary>
/// One aggregated order sample fed to <see cref="RestaurantPulseCalculator"/>: a
/// completed order's timestamps, total, lines, and whether any line was a
/// beverage (the handler decides that using catalog category names).
/// </summary>
public sealed record RestaurantPulseOrderSample(
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    decimal TotalAmount,
    IReadOnlyCollection<RestaurantPulseLineSample> Lines,
    bool HasBeverageLine);
