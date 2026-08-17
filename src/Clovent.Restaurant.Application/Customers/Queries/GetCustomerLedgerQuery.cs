using Clovent.Restaurant.Application.Customers.Dtos;
using Clovent.Restaurant.Customers;
using MediatR;

namespace Clovent.Restaurant.Application.Customers.Queries;

/// <summary>Queries the ledger transaction history for a customer.</summary>
public sealed record GetCustomerLedgerQuery(Guid CustomerId) : IRequest<IReadOnlyList<CustomerLedgerEntryDto>>;

/// <summary>Handles <see cref="GetCustomerLedgerQuery"/>.</summary>
public sealed class GetCustomerLedgerQueryHandler(ICustomerLedgerEntryRepository repository)
    : IRequestHandler<GetCustomerLedgerQuery, IReadOnlyList<CustomerLedgerEntryDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<CustomerLedgerEntryDto>> Handle(GetCustomerLedgerQuery request, CancellationToken cancellationToken)
    {
        var entries = await repository.GetByCustomerIdAsync(new CustomerId(request.CustomerId), cancellationToken);
        return [.. entries.Select(CustomerLedgerEntryDto.FromDomain)];
    }
}
