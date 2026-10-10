using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Inventory.Application.Forecasting.Dtos;

namespace Clovent.Inventory.Application.Forecasting.Services;

/// <summary>
/// Forecasting engine calculating velocity-based burn rates for ingredients and menu items,
/// factoring in day-of-week seasonality, and generating predictive stockout alerts and reorders.
/// </summary>
public interface IInventoryForecastingService
{
    /// <summary>
    /// Calculates granular velocity, seasonality coefficients, and hours-until-depletion projections for all stock in a warehouse.
    /// </summary>
    Task<IReadOnlyList<InventoryBurnRateDto>> CalculateBurnRatesAndDepletionAsync(
        Guid warehouseId,
        DateTimeOffset? nextScheduledGrnUtc = null,
        int observationHours = 168,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Surfaces high-priority stockout alerts where items will exhaust stock before the next scheduled GRN arrival.
    /// </summary>
    Task<IReadOnlyList<StockoutAlertDto>> GenerateStockoutAlertsAsync(
        Guid warehouseId,
        DateTimeOffset? nextScheduledGrnUtc = null,
        int observationHours = 168,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Automatically calculates dynamic replenishment reorder recommendations to prevent stockouts across future demand cycles.
    /// </summary>
    Task<IReadOnlyList<ReorderRecommendationDto>> GenerateReorderRecommendationsAsync(
        Guid warehouseId,
        DateTimeOffset? nextScheduledGrnUtc = null,
        int observationHours = 168,
        CancellationToken cancellationToken = default);
}
