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
    string? Notes,
    string? ShopNo = null,
    string? Mobile2 = null,
    string? Phone = null,
    bool? IsDefault = null) : IRequest<CustomerDto>;

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
            request.Notes,
            request.ShopNo,
            request.Mobile2,
            request.Phone);

        if (request.IsDefault is { } makeDefault)
        {
            if (makeDefault && !customer.IsDefault)
            {
                var allCustomers = await repository.GetAllAsync(cancellationToken);
                foreach (var c in allCustomers.Where(c => c.IsDefault && c.Id != customer.Id))
                {
                    var tracked = await repository.GetByIdAsync(c.Id, cancellationToken);
                    if (tracked is not null && tracked.IsDefault)
                    {
                        tracked.SetDefault(false);
                        await repository.UpdateAsync(tracked, cancellationToken);
                    }
                }
                customer.SetDefault(true);
            }
            else if (!makeDefault && customer.IsDefault)
            {
                customer.SetDefault(false);
            }
        }

        await repository.UpdateAsync(customer, cancellationToken);

        return CustomerDto.FromDomain(customer);
    }
}
