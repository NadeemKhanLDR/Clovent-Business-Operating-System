using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Inventory.Application.Forecasting.Dtos;
using Clovent.Inventory.Application.Forecasting.Services;
using MediatR;

namespace Clovent.Inventory.Application.Forecasting.Queries;

/// <summary>Query to calculate burn rates and depletion projections across warehouse stock.</summary>
public sealed record GetInventoryBurnRatesQuery(
    Guid WarehouseId,
    DateTimeOffset? NextScheduledGrnUtc = null,
    int ObservationHours = 168) : IRequest<IReadOnlyList<InventoryBurnRateDto>>;

/// <summary>Handler for <see cref="GetInventoryBurnRatesQuery"/>.</summary>
public sealed class GetInventoryBurnRatesQueryHandler(
    IInventoryForecastingService forecastingService) : IRequestHandler<GetInventoryBurnRatesQuery, IReadOnlyList<InventoryBurnRateDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<InventoryBurnRateDto>> Handle(
        GetInventoryBurnRatesQuery request,
        CancellationToken cancellationToken)
    {
        return await forecastingService.CalculateBurnRatesAndDepletionAsync(
            request.WarehouseId,
            request.NextScheduledGrnUtc,
            request.ObservationHours,
            cancellationToken);
    }
}
