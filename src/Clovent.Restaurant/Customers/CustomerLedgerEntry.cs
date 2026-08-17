using Clovent.Domain;

namespace Clovent.Restaurant.Customers;

/// <summary>
/// A single transaction/ledger entry in a customer's credit account history.
/// Represents debits (credit sales/purchases) or credits (payments received).
/// </summary>
public sealed class CustomerLedgerEntry : AggregateRoot<CustomerLedgerEntryId>
{
    /// <summary>The customer this ledger entry belongs to.</summary>
    public CustomerId CustomerId { get; }

    /// <summary>UTC date and time the entry was recorded.</summary>
    public DateTimeOffset Date { get; }

    /// <summary>Reference document number (e.g. order number ORD-XXXX or payment PAY-XXXX).</summary>
    public string Reference { get; }

    /// <summary>Brief description of the transaction.</summary>
    public string Description { get; }

    /// <summary>The debit amount (increases outstanding balance, e.g. credit sales).</summary>
    public decimal Debit { get; }

    /// <summary>The credit amount (decreases outstanding balance, e.g. payments received).</summary>
    public decimal Credit { get; }

    /// <summary>The customer's running outstanding balance after this entry.</summary>
    public decimal RunningBalance { get; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private CustomerLedgerEntry(
        CustomerLedgerEntryId id,
        CustomerId customerId,
        DateTimeOffset date,
        string reference,
        string description,
        decimal debit,
        decimal credit,
        decimal runningBalance)
    {
        Id = id;
        CustomerId = customerId;
        Date = date;
        Reference = reference;
        Description = description;
        Debit = debit;
        Credit = credit;
        RunningBalance = runningBalance;
    }

    /// <summary>Creates a new CustomerLedgerEntry.</summary>
    public static CustomerLedgerEntry Create(
        CustomerId customerId,
        string reference,
        string description,
        decimal debit,
        decimal credit,
        decimal runningBalance)
    {
        if (string.IsNullOrWhiteSpace(reference))
            throw new ArgumentException("Reference is required.", nameof(reference));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        return new CustomerLedgerEntry(
            CustomerLedgerEntryId.New(),
            customerId,
            DateTimeOffset.UtcNow,
            reference.Trim(),
            description.Trim(),
            debit,
            credit,
            runningBalance);
    }
}
