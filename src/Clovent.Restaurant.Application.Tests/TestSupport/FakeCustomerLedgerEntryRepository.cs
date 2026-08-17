using Clovent.Restaurant.Customers;

namespace Clovent.Restaurant.Application.Tests.TestSupport;

internal sealed class FakeCustomerLedgerEntryRepository : ICustomerLedgerEntryRepository
{
    private readonly List<CustomerLedgerEntry> _entries = [];

    public void Add(CustomerLedgerEntry entry) => _entries.Add(entry);

    public Task<IReadOnlyCollection<CustomerLedgerEntry>> GetByCustomerIdAsync(CustomerId customerId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<CustomerLedgerEntry>>(
            [.. _entries.Where(e => e.CustomerId == customerId).OrderBy(e => e.Date)]);

    public Task AddAsync(CustomerLedgerEntry entry, CancellationToken cancellationToken = default)
    {
        _entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task<Dictionary<CustomerId, DateTimeOffset>> GetLastTransactionDatesAsync(CancellationToken cancellationToken = default)
    {
        var dict = _entries
            .GroupBy(e => e.CustomerId)
            .ToDictionary(g => g.Key, g => g.Max(e => e.Date));
        return Task.FromResult(dict);
    }
}
