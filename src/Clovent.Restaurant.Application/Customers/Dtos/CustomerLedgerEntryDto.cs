namespace Clovent.Restaurant.Application.Customers.Dtos;

/// <summary>Read-model shape for a customer ledger entry.</summary>
public sealed record CustomerLedgerEntryDto(
    Guid Id,
    Guid CustomerId,
    DateTimeOffset Date,
    string Reference,
    string Description,
    decimal Debit,
    decimal Credit,
    decimal RunningBalance)
{
    /// <summary>Projects a domain <see cref="Clovent.Restaurant.Customers.CustomerLedgerEntry"/> into its DTO.</summary>
    public static CustomerLedgerEntryDto FromDomain(Clovent.Restaurant.Customers.CustomerLedgerEntry entry) => new(
        entry.Id.Value,
        entry.CustomerId.Value,
        entry.Date,
        entry.Reference,
        entry.Description,
        entry.Debit,
        entry.Credit,
        entry.RunningBalance);
}
