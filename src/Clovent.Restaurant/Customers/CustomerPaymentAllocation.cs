using Clovent.Domain;
using Clovent.Restaurant.Orders;

namespace Clovent.Restaurant.Customers;

/// <summary>
/// An explicit allocation linking a customer payment (or portion thereof) to a specific order
/// that was placed On Account. Enables precise order-level outstanding balance tracking.
/// </summary>
public sealed class CustomerPaymentAllocation : AggregateRoot<CustomerPaymentAllocationId>
{
    /// <summary>The customer who made the payment.</summary>
    public CustomerId CustomerId { get; }

    /// <summary>The order whose on-account receivable was settled by this allocation.</summary>
    public OrderId OrderId { get; }

    /// <summary>The customer ledger entry recording the payment transaction, if linked.</summary>
    public CustomerLedgerEntryId? CustomerLedgerEntryId { get; }

    /// <summary>The monetary amount allocated to the order.</summary>
    public decimal Amount { get; }

    /// <summary>UTC instant this allocation was created.</summary>
    public DateTimeOffset AllocatedAtUtc { get; }

    /// <summary>Optional notes regarding the allocation.</summary>
    public string? Notes { get; }

    /// <summary>Takes every persisted field explicitly for EF Core mapping.</summary>
    private CustomerPaymentAllocation(
        CustomerPaymentAllocationId id,
        CustomerId customerId,
        OrderId orderId,
        CustomerLedgerEntryId? customerLedgerEntryId,
        decimal amount,
        DateTimeOffset allocatedAtUtc,
        string? notes)
    {
        Id = id;
        CustomerId = customerId;
        OrderId = orderId;
        CustomerLedgerEntryId = customerLedgerEntryId;
        Amount = amount;
        AllocatedAtUtc = allocatedAtUtc;
        Notes = notes;
    }

    /// <summary>Creates a new allocation.</summary>
    public static CustomerPaymentAllocation Create(
        CustomerId customerId,
        OrderId orderId,
        decimal amount,
        CustomerLedgerEntryId? customerLedgerEntryId = null,
        string? notes = null)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Allocation amount must be positive.");

        return new CustomerPaymentAllocation(
            CustomerPaymentAllocationId.New(),
            customerId,
            orderId,
            customerLedgerEntryId,
            amount,
            DateTimeOffset.UtcNow,
            notes?.Trim());
    }
}
