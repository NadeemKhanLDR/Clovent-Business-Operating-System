using Clovent.Restaurant.Orders;

namespace Clovent.Restaurant.Customers;

/// <summary>Repository for <see cref="CustomerPaymentAllocation"/>.</summary>
public interface ICustomerPaymentAllocationRepository
{
    /// <summary>Adds a new allocation.</summary>
    Task AddAsync(CustomerPaymentAllocation allocation, CancellationToken cancellationToken = default);

    /// <summary>Gets all allocations for a given customer.</summary>
    Task<IReadOnlyCollection<CustomerPaymentAllocation>> GetByCustomerIdAsync(CustomerId customerId, CancellationToken cancellationToken = default);

    /// <summary>Gets all allocations for a given order.</summary>
    Task<IReadOnlyCollection<CustomerPaymentAllocation>> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default);

    /// <summary>Gets all allocations across all orders/customers.</summary>
    Task<IReadOnlyCollection<CustomerPaymentAllocation>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Removes an allocation (e.g. on order void reversal).</summary>
    Task RemoveAsync(CustomerPaymentAllocation allocation, CancellationToken cancellationToken = default);
}
