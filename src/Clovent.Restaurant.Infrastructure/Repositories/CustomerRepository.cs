using Clovent.MasterData.Shared.ValueObjects;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Restaurant.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="ICustomerRepository"/>.</summary>
/// <remarks>
/// A Desktop screen resolves one DI scope - and therefore one
/// <see cref="RestaurantDbContext"/> - for as long as the screen is open,
/// which can be hours. Two consequences shape every method below:
/// <list type="bullet">
/// <item>A read that tracks its result leaves that instance in the change
/// tracker, so a later read of the same row returns the values loaded the
/// first time (EF's identity resolution), not the row as it stands now.</item>
/// <item><c>DbSet.Update</c> marks <em>every</em> property modified, so
/// saving an instance holding those older values rewrites columns the caller
/// never touched.</item>
/// </list>
/// Together those two are what let a customer's ledger-derived
/// <see cref="Customer.OutstandingBalance"/> be silently reverted by an
/// unrelated status change (defect D23), so reads and writes here are both
/// deliberately narrowed.
/// </remarks>
public sealed class CustomerRepository(RestaurantDbContext dbContext) : ICustomerRepository
{
    /// <inheritdoc/>
    /// <remarks>
    /// Command handlers read-modify-write through this method, so it
    /// guarantees a value read from the database now rather than whatever
    /// this scope's change tracker loaded earlier.
    /// </remarks>
    public async Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        return customer is null ? null : await EnsureFreshAsync(customer, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Customer?> GetByCodeAsync(EntityCode code, CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.Customers.FirstOrDefaultAsync(c => c.Code == code, cancellationToken);
        return customer is null ? null : await EnsureFreshAsync(customer, cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Read-only projection for list screens: <c>AsNoTracking</c> keeps these
    /// rows out of the change tracker entirely, so browsing the customer list
    /// can neither serve stale values on a later refresh nor seed the tracker
    /// with instances a later command could write back.
    /// </remarks>
    public async Task<IReadOnlyCollection<Customer>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Customers.AsNoTracking().ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default) =>
        await dbContext.Customers.AddAsync(customer, cancellationToken);

    /// <inheritdoc/>
    /// <remarks>
    /// Deliberately not <c>DbSet.Update</c>: the aggregate is already tracked
    /// (it came from <see cref="GetByIdAsync"/>), so change tracking writes
    /// exactly the columns the handler actually changed. Forcing every
    /// property modified would additionally rewrite the ones it did not.
    /// </remarks>
    public Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        if (dbContext.Entry(customer).State == EntityState.Detached)
        {
            dbContext.Customers.Attach(customer).State = EntityState.Modified;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task UpdateStatusAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        var entry = dbContext.Entry(customer);
        if (entry.State == EntityState.Detached)
        {
            entry = dbContext.Attach(customer);
        }

        var isActive = customer.IsActive;

        // Re-reading the row discards every other in-memory difference this
        // instance is carrying, so the write below has nothing stale left to
        // carry with it. Clearing the modified flags alone would not do:
        // change detection re-derives them by comparing the aggregate against
        // the values originally loaded, and would flag the drifted balance
        // again on the way into SaveChanges (defect D23).
        await entry.ReloadAsync(cancellationToken);
        if (entry.State == EntityState.Detached)
        {
            return;
        }

        // Applied after the reload, so change detection now sees exactly one
        // difference - the status - plus the timestamp SetStatus touches.
        customer.SetStatus(isActive);
    }

    /// <summary>
    /// Re-reads an already-tracked aggregate from the database so a caller
    /// never modifies values this scope loaded before another writer moved
    /// them on. Returns <see langword="null"/> when the row no longer exists.
    /// </summary>
    private async Task<Customer?> EnsureFreshAsync(Customer customer, CancellationToken cancellationToken)
    {
        var entry = dbContext.Entry(customer);
        await entry.ReloadAsync(cancellationToken);
        return entry.State == EntityState.Detached ? null : customer;
    }
}
