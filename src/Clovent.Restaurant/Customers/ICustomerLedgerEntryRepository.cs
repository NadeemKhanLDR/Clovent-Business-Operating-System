namespace Clovent.Restaurant.Customers;

/// <summary>Persistence contract for <see cref="CustomerLedgerEntry"/>.</summary>
public interface ICustomerLedgerEntryRepository
{
    /// <summary>Retrieves all ledger entries for a customer, in chronological order.</summary>
    Task<IReadOnlyCollection<CustomerLedgerEntry>> GetByCustomerIdAsync(CustomerId customerId, CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created ledger entry.</summary>
    Task AddAsync(CustomerLedgerEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the latest transaction date for each customer who has ledger entries.</summary>
    Task<Dictionary<CustomerId, DateTimeOffset>> GetLastTransactionDatesAsync(CancellationToken cancellationToken = default);
}
