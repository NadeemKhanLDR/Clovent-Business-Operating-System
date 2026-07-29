using Clovent.Restaurant.Application.Discounts.Dtos;
using Clovent.Restaurant.Discounts;
using Clovent.Restaurant.Orders;
using MediatR;

namespace Clovent.Restaurant.Application.Discounts.Queries;

/// <summary>Retrieves every discount applied to an order.</summary>
public sealed record ListDiscountsByOrderQuery(Guid OrderId) : IRequest<IReadOnlyCollection<DiscountDto>>;

/// <summary>Handles <see cref="ListDiscountsByOrderQuery"/>.</summary>
public sealed class ListDiscountsByOrderQueryHandler(IDiscountRepository repository)
    : IRequestHandler<ListDiscountsByOrderQuery, IReadOnlyCollection<DiscountDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<DiscountDto>> Handle(ListDiscountsByOrderQuery request, CancellationToken cancellationToken)
    {
        var discounts = await repository.GetByOrderIdAsync(new OrderId(request.OrderId), cancellationToken);
        return [.. discounts.Select(DiscountDto.FromDomain)];
    }
}
