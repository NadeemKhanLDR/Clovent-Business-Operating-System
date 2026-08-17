using Clovent.MasterData.Shared.ValueObjects;
using Clovent.Restaurant.Customers;

namespace Clovent.Restaurant.Application.Tests.TestSupport;

/// <summary>
/// Stands in for <see cref="ICustomerRepository"/>, keeping the persisted
/// <em>row</em> deliberately separate from the in-memory aggregate.
/// </summary>
/// <remarks>
/// A dictionary of live object references cannot express the failure this
/// double exists to guard against: the real repository hands out an aggregate
/// its change tracker may have loaded long ago, so the instance's
/// <see cref="Customer.OutstandingBalance"/> and the row's can legitimately
/// disagree. Storing the balance separately lets a test make them disagree on
/// purpose (see <see cref="SetPersistedBalance"/>) and then assert which one a
/// given write actually persists - the whole point of defect D23.
/// </remarks>
internal sealed class FakeCustomerRepository : ICustomerRepository
{
    private readonly Dictionary<CustomerId, Customer> _customers = [];
    private readonly Dictionary<CustomerId, decimal> _persistedBalances = [];
    private readonly Dictionary<CustomerId, bool> _persistedStatuses = [];

    /// <summary>How many times a caller persisted the whole aggregate.</summary>
    public int FullUpdateCount { get; private set; }

    /// <summary>How many times a caller persisted the status column alone.</summary>
    public int StatusUpdateCount { get; private set; }

    public void Add(Customer customer) => Store(customer);

    /// <summary>Rewrites the stored row's balance without touching the aggregate - stands in for a POS credit sale or a payment committed by another scope.</summary>
    public void SetPersistedBalance(CustomerId id, decimal balance) => _persistedBalances[id] = balance;

    /// <summary>Reads the stored row's balance back, independently of whatever the aggregate instance holds.</summary>
    public decimal GetPersistedBalance(CustomerId id) => _persistedBalances[id];

    /// <summary>Reads the stored row's status back.</summary>
    public bool GetPersistedStatus(CustomerId id) => _persistedStatuses[id];

    public Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Fresh(_customers.GetValueOrDefault(id)));

    public Task<Customer?> GetByCodeAsync(EntityCode code, CancellationToken cancellationToken = default) =>
        Task.FromResult(Fresh(_customers.Values.FirstOrDefault(c => c.Code == code)));

    public Task<IReadOnlyCollection<Customer>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Customer>>([.. _customers.Values.Select(c => Fresh(c)!)]);

    public Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        Store(customer);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        FullUpdateCount++;
        Store(customer);
        return Task.CompletedTask;
    }

    public Task UpdateStatusAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        StatusUpdateCount++;
        _customers[customer.Id] = customer;
        _persistedStatuses[customer.Id] = customer.IsActive;
        // Pointedly does not write _persistedBalances: the real repository's
        // UPDATE names only the status columns, so the row's balance survives
        // whatever the aggregate instance is carrying.
        return Task.CompletedTask;
    }

    private void Store(Customer customer)
    {
        _customers[customer.Id] = customer;
        _persistedBalances[customer.Id] = customer.OutstandingBalance;
        _persistedStatuses[customer.Id] = customer.IsActive;
    }

    /// <summary>Mirrors the real repository re-reading a tracked aggregate, so a caller sees the row rather than a value loaded earlier.</summary>
    private Customer? Fresh(Customer? customer)
    {
        if (customer is null)
        {
            return null;
        }

        customer.AdjustBalance(_persistedBalances[customer.Id] - customer.OutstandingBalance);
        customer.SetStatus(_persistedStatuses[customer.Id]);
        return customer;
    }
}
