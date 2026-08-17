using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Restaurant.Application.Customers.Commands;
using Clovent.Restaurant.Application.Customers.Queries;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.Customers;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Customers;

public class CustomerHandlerTests
{
    [Fact]
    public async Task CreateCustomer_Valid_CreatesCustomerAndOpeningLedger()
    {
        var customerRepo = new FakeCustomerRepository();
        var ledgerRepo = new FakeCustomerLedgerEntryRepository();
        var handler = new CreateCustomerCommandHandler(customerRepo, ledgerRepo);

        var cmd = new CreateCustomerCommand("CUST-100", "John Tester", "12345678", "Main Street", "test@test.com", 150m, 500m, "Initial notes");
        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.Equal("CUST-100", result.Code);
        Assert.Equal("John Tester", result.Name);
        Assert.Equal(150m, result.OpeningBalance);
        Assert.Equal(150m, result.OutstandingBalance);
        Assert.Equal(500m, result.CreditLimit);

        // Verify ledger entry created
        var entries = await ledgerRepo.GetByCustomerIdAsync(new CustomerId(result.CustomerId));
        var openingEntry = Assert.Single(entries);
        Assert.Equal("OPENING", openingEntry.Reference);
        Assert.Equal("Opening Balance", openingEntry.Description);
        Assert.Equal(150m, openingEntry.Debit);
        Assert.Equal(0m, openingEntry.Credit);
        Assert.Equal(150m, openingEntry.RunningBalance);
    }

    [Fact]
    public async Task UpdateCustomer_Valid_UpdatesProperties()
    {
        var customerRepo = new FakeCustomerRepository();
        var customer = Customer.Create(
            Clovent.MasterData.Shared.ValueObjects.EntityCode.Create("CUST-100"),
            "John Tester",
            "12345678",
            "Main Street",
            "test@test.com",
            0m,
            500m,
            "Initial notes");
        await customerRepo.AddAsync(customer);

        var handler = new UpdateCustomerCommandHandler(customerRepo);
        var cmd = new UpdateCustomerCommand(customer.Id.Value, "John Updated", "87654321", "Second Street", "updated@test.com", 800m, "Updated notes");
        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.Equal("John Updated", result.Name);
        Assert.Equal("87654321", result.MobileNumber);
        Assert.Equal("Second Street", result.Address);
        Assert.Equal("updated@test.com", result.Email);
        Assert.Equal(800m, result.CreditLimit);
        Assert.Equal("Updated notes", result.Notes);
    }

    [Fact]
    public async Task SetCustomerStatus_Toggle_StatusChanges()
    {
        var customerRepo = new FakeCustomerRepository();
        var customer = Customer.Create(
            Clovent.MasterData.Shared.ValueObjects.EntityCode.Create("CUST-100"),
            "John Tester",
            "12345678",
            "Main Street",
            "test@test.com",
            0m,
            500m,
            "Initial notes");
        await customerRepo.AddAsync(customer);

        var handler = new SetCustomerStatusCommandHandler(customerRepo);

        // Deactivate
        var deacResult = await handler.Handle(new SetCustomerStatusCommand(customer.Id.Value, false), CancellationToken.None);
        Assert.False(deacResult.IsActive);

        // Activate
        var acResult = await handler.Handle(new SetCustomerStatusCommand(customer.Id.Value, true), CancellationToken.None);
        Assert.True(acResult.IsActive);
    }

    /// <summary>
    /// Defect D23. Reproduces the exact sequence QA isolated: a screen caches a
    /// customer, the balance moves underneath it (a POS credit sale does this in
    /// normal operation), and the operator then deactivates the customer from the
    /// still-stale screen. Only the status may reach the database.
    /// </summary>
    [Fact]
    public async Task SetCustomerStatus_StaleCachedBalance_LeavesPersistedBalanceIntact()
    {
        var customerRepo = new FakeCustomerRepository();
        var customer = Customer.Create(
            Clovent.MasterData.Shared.ValueObjects.EntityCode.Create("QACUST01"),
            "QA-CUSTOMER-01",
            "12345678",
            "Main Street",
            null,
            517.50m,
            500m,
            null);
        await customerRepo.AddAsync(customer);

        // The authoritative row now reads 111.11 - written by some other flow.
        customerRepo.SetPersistedBalance(customer.Id, 111.11m);

        // The screen is still holding the 999.99 it read earlier.
        customer.AdjustBalance(999.99m - customer.OutstandingBalance);
        Assert.Equal(999.99m, customer.OutstandingBalance);

        var handler = new SetCustomerStatusCommandHandler(customerRepo);
        var result = await handler.Handle(new SetCustomerStatusCommand(customer.Id.Value, false), CancellationToken.None);

        Assert.False(result.IsActive);
        Assert.False(customerRepo.GetPersistedStatus(customer.Id));
        Assert.Equal(111.11m, customerRepo.GetPersistedBalance(customer.Id));

        // The status path must never round-trip the whole aggregate.
        Assert.Equal(0, customerRepo.FullUpdateCount);
        Assert.Equal(1, customerRepo.StatusUpdateCount);
    }

    /// <summary>Defect D23: reactivating is the same write, so it carries the same guarantee.</summary>
    [Fact]
    public async Task SetCustomerStatus_Activate_ChangesOnlyStatus()
    {
        var customerRepo = new FakeCustomerRepository();
        var customer = Customer.Create(
            Clovent.MasterData.Shared.ValueObjects.EntityCode.Create("QACUST01"),
            "QA-CUSTOMER-01",
            "12345678",
            "Main Street",
            null,
            0m,
            500m,
            null);
        customer.SetStatus(false);
        await customerRepo.AddAsync(customer);

        customerRepo.SetPersistedBalance(customer.Id, 111.11m);
        customer.AdjustBalance(999.99m - customer.OutstandingBalance);

        var handler = new SetCustomerStatusCommandHandler(customerRepo);
        var result = await handler.Handle(new SetCustomerStatusCommand(customer.Id.Value, true), CancellationToken.None);

        Assert.True(result.IsActive);
        Assert.True(customerRepo.GetPersistedStatus(customer.Id));
        Assert.Equal(111.11m, customerRepo.GetPersistedBalance(customer.Id));
        Assert.Equal(0, customerRepo.FullUpdateCount);
    }

    [Fact]
    public async Task RecordCustomerPayment_ValidPayment_UpdatesBalanceAndCreatesLedger()
    {
        var customerRepo = new FakeCustomerRepository();
        var ledgerRepo = new FakeCustomerLedgerEntryRepository();
        
        var customer = Customer.Create(
            Clovent.MasterData.Shared.ValueObjects.EntityCode.Create("CUST-100"),
            "John Tester",
            "12345678",
            "Main Street",
            "test@test.com",
            100m, // outstanding balance
            500m,
            "Initial notes");
        await customerRepo.AddAsync(customer);

        var handler = new RecordCustomerPaymentCommandHandler(customerRepo, ledgerRepo);

        // Receive payment of 40
        var cmd = new RecordCustomerPaymentCommand(customer.Id.Value, 40m, "Card", "REF-999", "Tester payment notes");
        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.Equal(100m, result.OutstandingBefore);
        Assert.Equal(40m, result.PaymentReceived);
        Assert.Equal(40m, result.AppliedAmount);
        Assert.Equal(60m, result.OutstandingAfter);
        Assert.Equal(0m, result.ChangeAmount);

        // Verify ledger entry
        var entries = await ledgerRepo.GetByCustomerIdAsync(customer.Id);
        var entry = Assert.Single(entries);
        Assert.Equal("REF-999", entry.Reference);
        Assert.Equal("Customer Payment (Card): Tester payment notes", entry.Description);
        Assert.Equal(0m, entry.Debit);
        Assert.Equal(40m, entry.Credit);
        Assert.Equal(60m, entry.RunningBalance);
    }

    [Fact]
    public async Task RecordCustomerPayment_ZeroOrNegativeAmount_Throws()
    {
        var customerRepo = new FakeCustomerRepository();
        var ledgerRepo = new FakeCustomerLedgerEntryRepository();
        var handler = new RecordCustomerPaymentCommandHandler(customerRepo, ledgerRepo);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            handler.Handle(new RecordCustomerPaymentCommand(Guid.NewGuid(), 0m), CancellationToken.None));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            handler.Handle(new RecordCustomerPaymentCommand(Guid.NewGuid(), -10m), CancellationToken.None));
    }

    [Fact]
    public async Task RecordCustomerPayment_InactiveCustomer_Throws()
    {
        var customerRepo = new FakeCustomerRepository();
        var ledgerRepo = new FakeCustomerLedgerEntryRepository();
        
        var customer = Customer.Create(
            Clovent.MasterData.Shared.ValueObjects.EntityCode.Create("CUST-100"),
            "John Tester",
            "12345678",
            "Main Street",
            "test@test.com",
            100m,
            500m,
            null);
        customer.SetStatus(false); // Inactive
        await customerRepo.AddAsync(customer);

        var handler = new RecordCustomerPaymentCommandHandler(customerRepo, ledgerRepo);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new RecordCustomerPaymentCommand(customer.Id.Value, 20m), CancellationToken.None));
    }

    [Fact]
    public async Task ListCustomers_WithLastTransactionDates_ReturnsCorrectDates()
    {
        var customerRepo = new FakeCustomerRepository();
        var ledgerRepo = new FakeCustomerLedgerEntryRepository();

        var customer = Customer.Create(
            Clovent.MasterData.Shared.ValueObjects.EntityCode.Create("CUST-100"),
            "John Tester",
            "12345678",
            "Main Street",
            "test@test.com",
            0m,
            500m,
            null);
        await customerRepo.AddAsync(customer);

        var date = DateTimeOffset.UtcNow.AddDays(-2);
        var entry = CustomerLedgerEntry.Create(customer.Id, "REF-1", "Test", 50m, 0m, 50m);
        // Force the date inside fake ledger entry to match our test target (using reflection since Date is a getter-only auto-property)
        var dateField = typeof(CustomerLedgerEntry).GetField("<Date>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        dateField?.SetValue(entry, date);
        await ledgerRepo.AddAsync(entry);

        var handler = new ListCustomersQueryHandler(customerRepo, ledgerRepo);
        var result = await handler.Handle(new ListCustomersQuery(), CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Equal(customer.Id.Value, dto.CustomerId);
        Assert.NotNull(dto.LastTransactionDate);
        Assert.Equal(date, dto.LastTransactionDate.Value);
    }
}
