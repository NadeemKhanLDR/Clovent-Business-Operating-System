using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Inventory.Application.Forecasting.Dtos;
using Clovent.Inventory.Application.Forecasting.Services;
using MediatR;

namespace Clovent.Inventory.Application.Forecasting.Queries;

/// <summary>Query to retrieve predictive replenishment reorder recommendations.</summary>
public sealed record GetReorderRecommendationsQuery(
    Guid WarehouseId,
    DateTimeOffset? NextScheduledGrnUtc = null,
    int ObservationHours = 168) : IRequest<IReadOnlyList<ReorderRecommendationDto>>;

/// <summary>Handler for <see cref="GetReorderRecommendationsQuery"/>.</summary>
public sealed class GetReorderRecommendationsQueryHandler(
    IInventoryForecastingService forecastingService) : IRequestHandler<GetReorderRecommendationsQuery, IReadOnlyList<ReorderRecommendationDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<ReorderRecommendationDto>> Handle(
        GetReorderRecommendationsQuery request,
        CancellationToken cancellationToken)
    {
        return await forecastingService.GenerateReorderRecommendationsAsync(
            request.WarehouseId,
            request.NextScheduledGrnUtc,
            request.ObservationHours,
            cancellationToken);
    }
}
