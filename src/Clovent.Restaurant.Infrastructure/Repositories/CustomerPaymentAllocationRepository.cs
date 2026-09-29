using Clovent.Restaurant.Customers;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Orders;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Restaurant.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="ICustomerPaymentAllocationRepository"/>.</summary>
public sealed class CustomerPaymentAllocationRepository(RestaurantDbContext dbContext) : ICustomerPaymentAllocationRepository
{
    /// <inheritdoc/>
    public async Task AddAsync(CustomerPaymentAllocation allocation, CancellationToken cancellationToken = default) =>
        await dbContext.CustomerPaymentAllocations.AddAsync(allocation, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<CustomerPaymentAllocation>> GetByCustomerIdAsync(CustomerId customerId, CancellationToken cancellationToken = default) =>
        await dbContext.CustomerPaymentAllocations
            .Where(a => a.CustomerId == customerId)
            .OrderBy(a => a.AllocatedAtUtc)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<CustomerPaymentAllocation>> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default) =>
        await dbContext.CustomerPaymentAllocations
            .Where(a => a.OrderId == orderId)
            .OrderBy(a => a.AllocatedAtUtc)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<CustomerPaymentAllocation>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.CustomerPaymentAllocations
            .OrderBy(a => a.AllocatedAtUtc)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public Task RemoveAsync(CustomerPaymentAllocation allocation, CancellationToken cancellationToken = default)
    {
        dbContext.CustomerPaymentAllocations.Remove(allocation);
        return Task.CompletedTask;
    }
}
