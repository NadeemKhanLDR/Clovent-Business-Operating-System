using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Tables;

namespace Clovent.Restaurant.Application.Tests.TestSupport;

internal sealed class FakeOrderRepository : IOrderRepository
{
    private readonly Dictionary<OrderId, Order> _orders = [];

    public void Add(Order order) => _orders[order.Id] = order;

    public Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_orders.GetValueOrDefault(id));

    public Task<IReadOnlyCollection<Order>> GetOpenOrHeldByTableIdAsync(TableId tableId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Order>>([.. _orders.Values.Where(o => o.TableId == tableId && o.Status is OrderStatus.Open or OrderStatus.Held)]);

    public Task<IReadOnlyCollection<Order>> GetOpenAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Order>>([.. _orders.Values.Where(o => o.Status == OrderStatus.Open)]);

    public Task<IReadOnlyCollection<Order>> GetHeldAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Order>>([.. _orders.Values.Where(o => o.Status == OrderStatus.Held)]);

    public Task<IReadOnlyCollection<Order>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Order>>([.. _orders.Values]);

    public Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        _orders[order.Id] = order;
        return Task.CompletedTask;
    }
}
