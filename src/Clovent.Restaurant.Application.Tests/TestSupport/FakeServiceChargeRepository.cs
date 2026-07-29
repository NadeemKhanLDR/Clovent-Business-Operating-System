using Clovent.Restaurant.Orders;
using Clovent.Restaurant.ServiceCharges;

namespace Clovent.Restaurant.Application.Tests.TestSupport;

internal sealed class FakeServiceChargeRepository : IServiceChargeRepository
{
    private readonly Dictionary<ServiceChargeId, ServiceCharge> _charges = [];

    public void Add(ServiceCharge charge) => _charges[charge.Id] = charge;

    public Task<ServiceCharge?> GetByIdAsync(ServiceChargeId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_charges.GetValueOrDefault(id));

    public Task<IReadOnlyCollection<ServiceCharge>> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<ServiceCharge>>([.. _charges.Values.Where(c => c.OrderId == orderId)]);

    public Task AddAsync(ServiceCharge serviceCharge, CancellationToken cancellationToken = default)
    {
        _charges[serviceCharge.Id] = serviceCharge;
        return Task.CompletedTask;
    }
}
