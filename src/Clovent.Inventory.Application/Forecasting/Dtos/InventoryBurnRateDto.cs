using System;
using System.Collections.Generic;

namespace Clovent.Inventory.Application.Forecasting.Dtos;

/// <summary>
/// Velocity-based consumption analysis and projected depletion metrics for a product variant or raw ingredient.
/// Accounts for rolling sales history, day-of-week seasonality, and scheduled goods received note (GRN) arrivals.
/// </summary>
public sealed record InventoryBurnRateDto(
    Guid ProductVariantId,
    string Sku,
    string ItemName,
    Guid WarehouseId,
    decimal QuantityOnHand,
    decimal QuantityReserved,
    decimal QuantityAvailable,
    decimal MinimumStock,
    decimal MaximumStock,
    decimal BaseHourlyBurnRate,
    IReadOnlyDictionary<DayOfWeek, decimal> DayOfWeekSeasonality,
    decimal HoursUntilDepletion,
    DateTimeOffset? ProjectedDepletionUtc,
    DateTimeOffset NextScheduledGrnUtc,
    decimal HoursUntilGrn,
    bool IsDepletedBeforeGrn,
    decimal ProjectedDeficitAtGrn,
    decimal RecommendedReorderQuantity,
    string Urgency, // Critical, High, Medium, Adequate
    IReadOnlyList<decimal> Projected24HourDemand);
