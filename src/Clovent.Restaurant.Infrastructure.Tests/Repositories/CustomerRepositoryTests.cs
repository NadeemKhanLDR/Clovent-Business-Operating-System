using Clovent.MasterData.Shared.ValueObjects;
using Clovent.Restaurant.Application.Customers.Commands;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.Infrastructure.Repositories;
using Clovent.Restaurant.Infrastructure.Tests.TestSupport;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Repositories;

/// <summary>
/// Covers defect D23 against a real relational engine, because the defect only
/// exists at that level: it is produced by EF Core's change tracker serving a
/// previously-loaded instance and by <c>DbSet.Update</c> flagging every column
/// modified. A hand-written double cannot reproduce either.
/// </summary>
public class CustomerRepositoryTests : SqliteTestBase
{
    private static Customer NewCustomer(decimal openingBalance = 517.50m) => Customer.Create(
        EntityCode.Create("QACUST01"),
        "QA-CUSTOMER-01",
        "03001234567",
        "Main Street",
        null,
        openingBalance,
        500m,
        null);

    private async Task<Customer> SeedAsync(decimal openingBalance = 517.50m)
    {
        var customer = NewCustomer(openingBalance);
        await using var context = CreateContext();
        await new CustomerRepository(context).AddAsync(customer);
        await context.SaveChangesAsync();
        return customer;
    }

    /// <summary>Writes a balance the way another scope would - its own context, committed independently.</summary>
    private async Task SetBalanceExternallyAsync(CustomerId id, decimal balance)
    {
        await using var context = CreateContext();
        var repository = new CustomerRepository(context);
        var customer = await repository.GetByIdAsync(id);
        customer!.AdjustBalance(balance - customer.OutstandingBalance);
        await repository.UpdateAsync(customer);
        await context.SaveChangesAsync();
    }

    private async Task<Customer> ReadRowAsync(CustomerId id)
    {
        await using var context = CreateContext();
        return (await new CustomerRepository(context).GetByIdAsync(id))!;
    }

    /// <summary>
    /// The QA repro, end to end: DB holds 111.11, the screen's long-lived
    /// context still holds 999.99, the operator clicks Deactivate. Only
    /// <see cref="Customer.IsActive"/> may change.
    /// </summary>
    [Fact]
    public async Task SetCustomerStatus_WithStaleTrackedBalance_PersistsStatusOnly()
    {
        var seeded = await SeedAsync();

        // The screen's scope: one context, held open for the screen's lifetime.
        await using var screenContext = CreateContext();
        var screenRepository = new CustomerRepository(screenContext);

        // The screen loads the customer once, at 517.50, and tracks it.
        var cached = await screenRepository.GetByIdAsync(seeded.Id);
        Assert.Equal(517.50m, cached!.OutstandingBalance);

        // The screen's cached copy drifts to 999.99 (its own stale view)...
        cached.AdjustBalance(999.99m - cached.OutstandingBalance);

        // ...while the authoritative row moves to 111.11 from elsewhere.
        await SetBalanceExternallyAsync(seeded.Id, 111.11m);

        var handler = new SetCustomerStatusCommandHandler(screenRepository);
        var result = await handler.Handle(new SetCustomerStatusCommand(seeded.Id.Value, false), CancellationToken.None);
        await screenContext.SaveChangesAsync();

        Assert.False(result.IsActive);

        var row = await ReadRowAsync(seeded.Id);
        Assert.Equal(111.11m, row.OutstandingBalance);
        Assert.False(row.IsActive);
    }

    /// <summary>Reactivating carries the same guarantee as deactivating.</summary>
    [Fact]
    public async Task SetCustomerStatus_Activate_WithStaleTrackedBalance_PersistsStatusOnly()
    {
        var seeded = await SeedAsync();

        await using (var deactivateContext = CreateContext())
        {
            var repository = new CustomerRepository(deactivateContext);
            var customer = await repository.GetByIdAsync(seeded.Id);
            customer!.SetStatus(false);
            await repository.UpdateStatusAsync(customer);
            await deactivateContext.SaveChangesAsync();
        }

        await using var screenContext = CreateContext();
        var screenRepository = new CustomerRepository(screenContext);

        var cached = await screenRepository.GetByIdAsync(seeded.Id);
        cached!.AdjustBalance(999.99m - cached.OutstandingBalance);

        await SetBalanceExternallyAsync(seeded.Id, 111.11m);

        var handler = new SetCustomerStatusCommandHandler(screenRepository);
        await handler.Handle(new SetCustomerStatusCommand(seeded.Id.Value, true), CancellationToken.None);
        await screenContext.SaveChangesAsync();

        var row = await ReadRowAsync(seeded.Id);
        Assert.Equal(111.11m, row.OutstandingBalance);
        Assert.True(row.IsActive);
    }

    /// <summary>
    /// The narrowed write on its own, with the reload deliberately bypassed:
    /// even handed an aggregate carrying a stale balance,
    /// <see cref="CustomerRepository.UpdateStatusAsync"/> must emit an UPDATE
    /// that does not name the balance column.
    /// </summary>
    [Fact]
    public async Task UpdateStatusAsync_StaleBalanceOnTrackedInstance_DoesNotWriteBalance()
    {
        var seeded = await SeedAsync();

        await using var screenContext = CreateContext();
        var screenRepository = new CustomerRepository(screenContext);

        var cached = await screenRepository.GetByIdAsync(seeded.Id);
        cached!.AdjustBalance(999.99m - cached.OutstandingBalance);

        await SetBalanceExternallyAsync(seeded.Id, 111.11m);

        cached.SetStatus(false);
        await screenRepository.UpdateStatusAsync(cached);
        await screenContext.SaveChangesAsync();

        var row = await ReadRowAsync(seeded.Id);
        Assert.Equal(111.11m, row.OutstandingBalance);
        Assert.False(row.IsActive);
    }

    /// <summary>
    /// Defect D5's persistence half: a command-side read must return the row as
    /// it stands now, not the values this context loaded earlier.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_AfterExternalChange_ReturnsCurrentRow()
    {
        var seeded = await SeedAsync();

        await using var screenContext = CreateContext();
        var screenRepository = new CustomerRepository(screenContext);

        var first = await screenRepository.GetByIdAsync(seeded.Id);
        Assert.Equal(517.50m, first!.OutstandingBalance);

        await SetBalanceExternallyAsync(seeded.Id, 111.11m);

        var second = await screenRepository.GetByIdAsync(seeded.Id);
        Assert.Equal(111.11m, second!.OutstandingBalance);
    }

    /// <summary>
    /// Defect D5's list half: re-running the list query must re-read the
    /// database rather than replay whatever the change tracker holds.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_AfterExternalChange_ReturnsCurrentRows()
    {
        var seeded = await SeedAsync();

        await using var screenContext = CreateContext();
        var screenRepository = new CustomerRepository(screenContext);

        var first = await screenRepository.GetAllAsync();
        Assert.Equal(517.50m, Assert.Single(first).OutstandingBalance);

        await SetBalanceExternallyAsync(seeded.Id, 999.99m);

        var second = await screenRepository.GetAllAsync();
        Assert.Equal(999.99m, Assert.Single(second).OutstandingBalance);
    }

    /// <summary>An ordinary profile edit still persists, and still leaves the ledger-derived balance alone.</summary>
    [Fact]
    public async Task UpdateAsync_ProfileEdit_PersistsEditsWithoutRewritingBalance()
    {
        var seeded = await SeedAsync();
        await SetBalanceExternallyAsync(seeded.Id, 111.11m);

        await using (var context = CreateContext())
        {
            var repository = new CustomerRepository(context);
            var customer = await repository.GetByIdAsync(seeded.Id);
            customer!.Update("Renamed Customer", "03009999999", "Second Street", "a@b.com", 800m, "notes");
            await repository.UpdateAsync(customer);
            await context.SaveChangesAsync();
        }

        var row = await ReadRowAsync(seeded.Id);
        Assert.Equal("Renamed Customer", row.Name);
        Assert.Equal(800m, row.CreditLimit);
        Assert.Equal(111.11m, row.OutstandingBalance);
    }
}
