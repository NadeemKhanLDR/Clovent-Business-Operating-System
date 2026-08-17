using Clovent.MasterData.Shared.ValueObjects;

namespace Clovent.Restaurant.Customers;

/// <summary>Persistence contract for <see cref="Customer"/> aggregates.</summary>
public interface ICustomerRepository
{
    /// <summary>Retrieves a customer by identity, or <see langword="null"/> if none exists.</summary>
    Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a customer by short code, or <see langword="null"/> if none exists.</summary>
    Task<Customer?> GetByCodeAsync(EntityCode code, CancellationToken cancellationToken = default);

    /// <summary>Retrieves all customer records.</summary>
    Task<IReadOnlyCollection<Customer>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created customer.</summary>
    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing customer.</summary>
    Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists <em>only</em> <see cref="Customer.IsActive"/> (and the audit
    /// timestamp that goes with it), never any other column.
    /// </summary>
    /// <remarks>
    /// Activating or deactivating an account is a status change, not a
    /// financial one, so it must not be able to write a balance at all - not
    /// even the value the in-memory instance happens to be carrying.
    /// <see cref="Customer.OutstandingBalance"/> is derived from the ledger
    /// and owned by the payment/credit-sale flows; a status change that
    /// round-trips the whole aggregate can silently revert a balance those
    /// flows have since moved on, leaving the balance and the ledger
    /// permanently inconsistent (defect D23). Narrowing the write to the one
    /// column the operation is actually about makes that impossible rather
    /// than merely unlikely.
    /// </remarks>
    Task UpdateStatusAsync(Customer customer, CancellationToken cancellationToken = default);
}
