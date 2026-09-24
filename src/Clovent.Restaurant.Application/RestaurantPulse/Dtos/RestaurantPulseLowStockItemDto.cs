namespace Clovent.Restaurant.Application.RestaurantPulse.Dtos;

/// <summary>A low-stock inventory item on the Restaurant Pulse panel (unpopulated until inventory data lives in the Restaurant module).</summary>
public sealed record RestaurantPulseLowStockItemDto(
    Guid VariantId,
    string ProductName,
    string VariantName,
    decimal QuantityOnHand);
