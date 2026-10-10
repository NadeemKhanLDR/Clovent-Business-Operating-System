using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Inventory.Application.Forecasting.Dtos;
using Clovent.Inventory.Application.Forecasting.Services;
using MediatR;

namespace Clovent.Inventory.Application.Forecasting.Queries;

/// <summary>Query to retrieve predictive stockout alerts for inventory depleting prior to GRN.</summary>
public sealed record GetStockoutAlertsQuery(
    Guid WarehouseId,
    DateTimeOffset? NextScheduledGrnUtc = null,
    int ObservationHours = 168) : IRequest<IReadOnlyList<StockoutAlertDto>>;

/// <summary>Handler for <see cref="GetStockoutAlertsQuery"/>.</summary>
public sealed class GetStockoutAlertsQueryHandler(
    IInventoryForecastingService forecastingService) : IRequestHandler<GetStockoutAlertsQuery, IReadOnlyList<StockoutAlertDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<StockoutAlertDto>> Handle(
        GetStockoutAlertsQuery request,
        CancellationToken cancellationToken)
    {
        return await forecastingService.GenerateStockoutAlertsAsync(
            request.WarehouseId,
            request.NextScheduledGrnUtc,
            request.ObservationHours,
            cancellationToken);
    }
}
