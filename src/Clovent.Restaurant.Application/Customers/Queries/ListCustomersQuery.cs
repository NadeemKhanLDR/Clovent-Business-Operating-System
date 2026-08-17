using Clovent.Restaurant.Application.Customers.Dtos;
using Clovent.Restaurant.Customers;
using MediatR;

namespace Clovent.Restaurant.Application.Customers.Queries;

/// <summary>Queries the list of all registered customers.</summary>
public sealed record ListCustomersQuery : IRequest<IReadOnlyList<CustomerDto>>;

/// <summary>Handles <see cref="ListCustomersQuery"/>.</summary>
public sealed class ListCustomersQueryHandler(
    ICustomerRepository repository,
    ICustomerLedgerEntryRepository ledgerRepository) : IRequestHandler<ListCustomersQuery, IReadOnlyList<CustomerDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<CustomerDto>> Handle(ListCustomersQuery request, CancellationToken cancellationToken)
    {
        var customers = await repository.GetAllAsync(cancellationToken);
        var lastDates = await ledgerRepository.GetLastTransactionDatesAsync(cancellationToken);

        return [.. customers.Select(c =>
        {
            var dto = CustomerDto.FromDomain(c);
            lastDates.TryGetValue(c.Id, out var lastDate);
            return dto with { LastTransactionDate = lastDate == default ? null : lastDate };
        })];
    }
}
