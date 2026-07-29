using Clovent.Restaurant.Discounts;
using Clovent.Restaurant.Orders;

namespace Clovent.Restaurant.Application.Tests.TestSupport;

internal sealed class FakeDiscountRepository : IDiscountRepository
{
    private readonly Dictionary<DiscountId, Discount> _discounts = [];

    public void Add(Discount discount) => _discounts[discount.Id] = discount;

    public Task<Discount?> GetByIdAsync(DiscountId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_discounts.GetValueOrDefault(id));

    public Task<IReadOnlyCollection<Discount>> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Discount>>([.. _discounts.Values.Where(d => d.OrderId == orderId)]);

    public Task AddAsync(Discount discount, CancellationToken cancellationToken = default)
    {
        _discounts[discount.Id] = discount;
        return Task.CompletedTask;
    }
}
