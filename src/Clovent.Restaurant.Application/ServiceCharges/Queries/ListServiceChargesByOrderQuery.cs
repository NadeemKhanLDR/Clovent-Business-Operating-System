using Clovent.Restaurant.Application.ServiceCharges.Dtos;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.ServiceCharges;
using MediatR;

namespace Clovent.Restaurant.Application.ServiceCharges.Queries;

/// <summary>Retrieves every service charge applied to an order.</summary>
public sealed record ListServiceChargesByOrderQuery(Guid OrderId) : IRequest<IReadOnlyCollection<ServiceChargeDto>>;

/// <summary>Handles <see cref="ListServiceChargesByOrderQuery"/>.</summary>
public sealed class ListServiceChargesByOrderQueryHandler(IServiceChargeRepository repository)
    : IRequestHandler<ListServiceChargesByOrderQuery, IReadOnlyCollection<ServiceChargeDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<ServiceChargeDto>> Handle(ListServiceChargesByOrderQuery request, CancellationToken cancellationToken)
    {
        var charges = await repository.GetByOrderIdAsync(new OrderId(request.OrderId), cancellationToken);
        return [.. charges.Select(ServiceChargeDto.FromDomain)];
    }
}
