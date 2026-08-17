namespace Clovent.Restaurant.Application.Customers.Dtos;

/// <summary>Read-model shape for a <see cref="Clovent.Restaurant.Customers.Customer"/> aggregate.</summary>
public sealed record CustomerDto(
    Guid CustomerId,
    string Code,
    string Name,
    string MobileNumber,
    string Address,
    string? Email,
    decimal OpeningBalance,
    decimal CreditLimit,
    decimal OutstandingBalance,
    bool IsActive,
    string? Notes,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? LastTransactionDate = null)
{
    /// <summary>Projects a domain <see cref="Clovent.Restaurant.Customers.Customer"/> into its DTO.</summary>
    public static CustomerDto FromDomain(Clovent.Restaurant.Customers.Customer customer) => new(
        customer.Id.Value,
        customer.Code.Value,
        customer.Name,
        customer.MobileNumber,
        customer.Address,
        customer.Email,
        customer.OpeningBalance,
        customer.CreditLimit,
        customer.OutstandingBalance,
        customer.IsActive,
        customer.Notes,
        customer.CreatedAtUtc,
        customer.UpdatedAtUtc);
}
