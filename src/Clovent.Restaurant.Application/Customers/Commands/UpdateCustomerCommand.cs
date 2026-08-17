using Clovent.Restaurant.Application.Customers.Dtos;
using Clovent.Restaurant.Customers;
using MediatR;

namespace Clovent.Restaurant.Application.Customers.Commands;

/// <summary>Updates customer profile information.</summary>
public sealed record UpdateCustomerCommand(
    Guid CustomerId,
    string Name,
    string MobileNumber,
    string Address,
    string? Email,
    decimal CreditLimit,
    string? Notes) : IRequest<CustomerDto>;

/// <summary>Handles <see cref="UpdateCustomerCommand"/>.</summary>
public sealed class UpdateCustomerCommandHandler(ICustomerRepository repository) : IRequestHandler<UpdateCustomerCommand, CustomerDto>
{
    /// <inheritdoc/>
    public async Task<CustomerDto> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await repository.GetByIdAsync(new CustomerId(request.CustomerId), cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.CustomerId);

        customer.Update(
            request.Name,
            request.MobileNumber,
            request.Address,
            request.Email,
            request.CreditLimit,
            request.Notes);

        await repository.UpdateAsync(customer, cancellationToken);

        return CustomerDto.FromDomain(customer);
    }
}
