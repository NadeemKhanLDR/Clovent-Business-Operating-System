using Clovent.MasterData.Shared.ValueObjects;
using Clovent.Restaurant.Application.Customers.Dtos;
using Clovent.Restaurant.Customers;
using MediatR;

namespace Clovent.Restaurant.Application.Customers.Queries;

/// <summary>Queries a customer by their short code.</summary>
public sealed record GetCustomerByCodeQuery(string Code) : IRequest<CustomerDto?>;

/// <summary>Handles <see cref="GetCustomerByCodeQuery"/>.</summary>
public sealed class GetCustomerByCodeQueryHandler(ICustomerRepository repository) : IRequestHandler<GetCustomerByCodeQuery, CustomerDto?>
{
    /// <inheritdoc/>
    public async Task<CustomerDto?> Handle(GetCustomerByCodeQuery request, CancellationToken cancellationToken)
    {
        EntityCode entityCode;
        try
        {
            entityCode = EntityCode.Create(request.Code);
        }
        catch (ArgumentException)
        {
            return null;
        }

        var customer = await repository.GetByCodeAsync(entityCode, cancellationToken);
        return customer != null ? CustomerDto.FromDomain(customer) : null;
    }
}
