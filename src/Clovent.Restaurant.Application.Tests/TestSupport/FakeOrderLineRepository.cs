using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;

namespace Clovent.Restaurant.Application.Tests.TestSupport;

internal sealed class FakeOrderLineRepository : IOrderLineRepository
{
    private readonly Dictionary<OrderLineId, OrderLine> _lines = [];

    public void Add(OrderLine line) => _lines[line.Id] = line;

    public Task<OrderLine?> GetByIdAsync(OrderLineId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_lines.GetValueOrDefault(id));

    public Task<IReadOnlyCollection<OrderLine>> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<OrderLine>>([.. _lines.Values.Where(l => l.OrderId == orderId)]);

    public Task AddAsync(OrderLine orderLine, CancellationToken cancellationToken = default)
    {
        _lines[orderLine.Id] = orderLine;
        return Task.CompletedTask;
    }
}
