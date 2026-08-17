using Clovent.Restaurant.Application.Customers.Dtos;
using Clovent.Restaurant.Customers;
using MediatR;

namespace Clovent.Restaurant.Application.Customers.Commands;

/// <summary>Activates or deactivates a customer account.</summary>
public sealed record SetCustomerStatusCommand(Guid CustomerId, bool IsActive) : IRequest<CustomerDto>;

/// <summary>Handles <see cref="SetCustomerStatusCommand"/>.</summary>
public sealed class SetCustomerStatusCommandHandler(ICustomerRepository repository) : IRequestHandler<SetCustomerStatusCommand, CustomerDto>
{
    /// <inheritdoc/>
    public async Task<CustomerDto> Handle(SetCustomerStatusCommand request, CancellationToken cancellationToken)
    {
        var customer = await repository.GetByIdAsync(new CustomerId(request.CustomerId), cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.CustomerId);

        customer.SetStatus(request.IsActive);

        // UpdateStatusAsync, not UpdateAsync: activating or deactivating an
        // account must not be able to persist a balance at all - see
        // ICustomerRepository.UpdateStatusAsync for why (defect D23).
        await repository.UpdateStatusAsync(customer, cancellationToken);

        return CustomerDto.FromDomain(customer);
    }
}
