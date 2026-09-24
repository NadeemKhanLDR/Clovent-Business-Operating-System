using Clovent.Restaurant.Application.CustomerReorder.Dtos;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using MediatR;

namespace Clovent.Restaurant.Application.CustomerReorder.Queries;

/// <summary>
/// Retrieves a customer's most frequently ordered variants, computed
/// deterministically: the customer's completed order lines grouped by
/// variant, ordered by order count descending then last-purchased
/// descending, keeping only variants currently active in the catalog.
/// Each line's quantity is the number of the customer's completed orders
/// the variant appeared in, and the price is today's selling price.
/// </summary>
public sealed record GetCustomerFrequentProductsQuery(Guid CustomerId, int Take = 5) : IRequest<IReadOnlyCollection<CustomerReorderLineDto>>;

/// <summary>Handles <see cref="GetCustomerFrequentProductsQuery"/>.</summary>
public sealed class GetCustomerFrequentProductsQueryHandler(
    ICustomerRepository customerRepository,
    IOrderRepository orderRepository,
    IOrderLineRepository orderLineRepository,
    IMediator mediator) : IRequestHandler<GetCustomerFrequentProductsQuery, IReadOnlyCollection<CustomerReorderLineDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<CustomerReorderLineDto>> Handle(GetCustomerFrequentProductsQuery request, CancellationToken cancellationToken)
    {
        if (request.Take <= 0)
        {
            return [];
        }

        var customerId = new CustomerId(request.CustomerId);
        var customer = await customerRepository.GetByIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.CustomerId);

        var completedOrders = (await orderRepository.GetAllAsync(cancellationToken))
            .Where(o => o.CustomerId == customerId && o.Status == OrderStatus.Completed)
            .ToList();

        // variant -> (order count, latest purchase instant), one entry per variant per order.
        var statsByVariantId = new Dictionary<Guid, (int OrderCount, DateTimeOffset LastPurchasedUtc)>();
        foreach (var order in completedOrders.OrderByDescending(o => o.UpdatedAtUtc))
        {
            var lines = await orderLineRepository.GetByOrderIdAsync(order.Id, cancellationToken);
            foreach (var variantId in lines.Where(l => !l.IsVoided).Select(l => l.ProductVariantId.Value).Distinct())
            {
                var (count, lastPurchased) = statsByVariantId.GetValueOrDefault(variantId);
                statsByVariantId[variantId] = (count + 1, order.UpdatedAtUtc > lastPurchased ? order.UpdatedAtUtc : lastPurchased);
            }
        }

        var catalog = await CatalogSnapshot.LoadAsync(mediator, cancellationToken);

        return [.. statsByVariantId
            .OrderByDescending(kv => kv.Value.OrderCount)
            .ThenByDescending(kv => kv.Value.LastPurchasedUtc)
            .ThenBy(kv => kv.Key)
            .Select(kv => catalog.ProjectSnapshot(kv.Key, kv.Value.OrderCount))
            .Where(line => line.IsAvailable)
            .Take(request.Take)];
    }
}
