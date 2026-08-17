using Clovent.Restaurant.Orders;

namespace Clovent.Restaurant.Application.Tests.TestSupport;

internal sealed class FakeOrderNumberSequenceRepository : IOrderNumberSequenceRepository
{
    private OrderNumberSequence? _sequence;

    public void Add(OrderNumberSequence sequence) => _sequence = sequence;

    public Task<OrderNumberSequence?> GetSingletonAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_sequence);

    public Task AddAsync(OrderNumberSequence sequence, CancellationToken cancellationToken = default)
    {
        _sequence = sequence;
        return Task.CompletedTask;
    }
}
