using Clovent.Restaurant.Customers;
using Clovent.Restaurant.Orders;

namespace Clovent.Restaurant.Application.Tests.TestSupport;

public sealed class FakeCustomerPaymentAllocationRepository : ICustomerPaymentAllocationRepository
{
    private readonly List<CustomerPaymentAllocation> _allocations = [];

    public Task AddAsync(CustomerPaymentAllocation allocation, CancellationToken cancellationToken = default)
    {
        _allocations.Add(allocation);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<CustomerPaymentAllocation>> GetByCustomerIdAsync(CustomerId customerId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<CustomerPaymentAllocation>>(_allocations.Where(a => a.CustomerId == customerId).ToList());

    public Task<IReadOnlyCollection<CustomerPaymentAllocation>> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<CustomerPaymentAllocation>>(_allocations.Where(a => a.OrderId == orderId).ToList());

    public Task<IReadOnlyCollection<CustomerPaymentAllocation>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<CustomerPaymentAllocation>>(_allocations.ToList());

    public Task RemoveAsync(CustomerPaymentAllocation allocation, CancellationToken cancellationToken = default)
    {
        _allocations.Remove(allocation);
        return Task.CompletedTask;
    }
}
