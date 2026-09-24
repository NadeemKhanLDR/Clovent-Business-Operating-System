using Clovent.Restaurant.Application.Customers.Dtos;
using Clovent.Restaurant.Customers;
using MediatR;

namespace Clovent.Restaurant.Application.Customers.Commands;

/// <summary>Designates a customer as the default customer for new POS orders.</summary>
public sealed record SetDefaultCustomerCommand(Guid CustomerId) : IRequest<CustomerDto>;

/// <summary>Handles <see cref="SetDefaultCustomerCommand"/>.</summary>
public sealed class SetDefaultCustomerCommandHandler(ICustomerRepository repository)
    : IRequestHandler<SetDefaultCustomerCommand, CustomerDto>
{
    /// <inheritdoc/>
    public async Task<CustomerDto> Handle(SetDefaultCustomerCommand request, CancellationToken cancellationToken)
    {
        var targetCustomer = await repository.GetByIdAsync(new CustomerId(request.CustomerId), cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.CustomerId);

        if (!targetCustomer.IsActive)
            throw RestaurantDomainException.CustomerCannotBeDefaultWhileInactive();

        var allCustomers = await repository.GetAllAsync(cancellationToken);
        foreach (var c in allCustomers.Where(c => c.IsDefault && c.Id != targetCustomer.Id))
        {
            var tracked = await repository.GetByIdAsync(c.Id, cancellationToken);
            if (tracked is not null && tracked.IsDefault)
            {
                tracked.SetDefault(false);
                await repository.UpdateAsync(tracked, cancellationToken);
            }
        }

        targetCustomer.SetDefault(true);
        await repository.UpdateAsync(targetCustomer, cancellationToken);

        return CustomerDto.FromDomain(targetCustomer);
    }
}
