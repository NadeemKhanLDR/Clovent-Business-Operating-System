using Clovent.Restaurant.Application.Customers.Dtos;
using Clovent.Restaurant.Customers;
using MediatR;

namespace Clovent.Restaurant.Application.Customers.Queries;

/// <summary>Queries a customer by their unique identifier.</summary>
public sealed record GetCustomerByIdQuery(Guid CustomerId) : IRequest<CustomerDto>;

/// <summary>Handles <see cref="GetCustomerByIdQuery"/>.</summary>
public sealed class GetCustomerByIdQueryHandler(ICustomerRepository repository) : IRequestHandler<GetCustomerByIdQuery, CustomerDto>
{
    /// <inheritdoc/>
    public async Task<CustomerDto> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var customer = await repository.GetByIdAsync(new CustomerId(request.CustomerId), cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.CustomerId);

        return CustomerDto.FromDomain(customer);
    }
}
