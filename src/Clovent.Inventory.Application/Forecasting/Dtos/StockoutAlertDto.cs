using System;

namespace Clovent.Inventory.Application.Forecasting.Dtos;

/// <summary>
/// Operational stockout alert surfaced when projected consumption threatens inventory exhaustion prior to replenishment.
/// </summary>
public sealed record StockoutAlertDto(
    Guid ProductVariantId,
    string Sku,
    string ItemName,
    Guid WarehouseId,
    decimal CurrentAvailable,
    decimal MinimumStock,
    decimal HourlyBurnRate,
    decimal HoursUntilDepletion,
    DateTimeOffset? ProjectedDepletionUtc,
    DateTimeOffset NextScheduledGrnUtc,
    decimal HoursUntilGrn,
    bool IsDepletedBeforeGrn,
    decimal RecommendedReorderQuantity,
    string Severity, // Critical, High, Medium
    string RiskSummary);
