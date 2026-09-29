using Clovent.Restaurant.Customers;
using Clovent.Restaurant.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Restaurant.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="ICustomerLedgerEntryRepository"/>.</summary>
public sealed class CustomerLedgerEntryRepository(RestaurantDbContext dbContext) : ICustomerLedgerEntryRepository
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<CustomerLedgerEntry>> GetByCustomerIdAsync(CustomerId customerId, CancellationToken cancellationToken = default)
    {
        var entries = await dbContext.CustomerLedgerEntries
            .Where(e => e.CustomerId == customerId)
            .ToListAsync(cancellationToken);
        return entries.OrderBy(e => e.Date).ToList();
    }

    /// <inheritdoc/>
    public async Task AddAsync(CustomerLedgerEntry entry, CancellationToken cancellationToken = default) =>
        await dbContext.CustomerLedgerEntries.AddAsync(entry, cancellationToken);

    /// <inheritdoc/>
    public async Task<Dictionary<CustomerId, DateTimeOffset>> GetLastTransactionDatesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.CustomerLedgerEntries
            .GroupBy(e => e.CustomerId)
            .Select(g => new { CustomerId = g.Key, MaxDate = g.Max(e => e.Date) })
            .ToDictionaryAsync(x => x.CustomerId, x => x.MaxDate, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<CustomerLedgerEntry>> GetByShiftIdAsync(Clovent.Restaurant.Shifts.ShiftId shiftId, CancellationToken cancellationToken = default)
    {
        var entries = await dbContext.CustomerLedgerEntries
            .Where(e => e.ShiftId == shiftId)
            .ToListAsync(cancellationToken);
        return entries.OrderBy(e => e.Date).ToList();
    }
}
