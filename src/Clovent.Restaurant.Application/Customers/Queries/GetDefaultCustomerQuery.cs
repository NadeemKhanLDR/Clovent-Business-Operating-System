using Clovent.Restaurant.Application.Customers.Dtos;
using Clovent.Restaurant.Customers;
using MediatR;

namespace Clovent.Restaurant.Application.Customers.Queries;

/// <summary>Queries the currently configured active default customer for POS orders.</summary>
public sealed record GetDefaultCustomerQuery : IRequest<CustomerDto?>;

/// <summary>Handles <see cref="GetDefaultCustomerQuery"/>.</summary>
public sealed class GetDefaultCustomerQueryHandler(ICustomerRepository repository) : IRequestHandler<GetDefaultCustomerQuery, CustomerDto?>
{
    /// <inheritdoc/>
    public async Task<CustomerDto?> Handle(GetDefaultCustomerQuery request, CancellationToken cancellationToken)
    {
        var customers = await repository.GetAllAsync(cancellationToken);
        var defaultCustomer = customers.FirstOrDefault(c => c.IsDefault && c.IsActive);
        return defaultCustomer is not null ? CustomerDto.FromDomain(defaultCustomer) : null;
    }
}
