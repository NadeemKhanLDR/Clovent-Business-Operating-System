using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.Orders;
using MediatR;

namespace Clovent.Restaurant.Application.Orders.Commands;

/// <summary>Associates a customer (or null for Walk-in) with an order.</summary>
public sealed record SetOrderCustomerCommand(Guid OrderId, Guid? CustomerId) : IRequest<OrderDto>;

/// <summary>Handles <see cref="SetOrderCustomerCommand"/>.</summary>
public sealed class SetOrderCustomerCommandHandler(
    IOrderRepository orderRepository,
    ICustomerRepository customerRepository) : IRequestHandler<SetOrderCustomerCommand, OrderDto>
{
    /// <inheritdoc/>
    public async Task<OrderDto> Handle(SetOrderCustomerCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(new OrderId(request.OrderId), cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        if (request.CustomerId is { } custId)
        {
            var customer = await customerRepository.GetByIdAsync(new CustomerId(custId), cancellationToken)
                ?? throw new NotFoundException(nameof(Customer), custId);

            if (!customer.IsActive)
                throw new InvalidOperationException("Cannot select an inactive customer.");

            order.SetCustomer(customer.Id);
        }
        else
        {
            order.SetCustomer(null);
        }

        return OrderDto.FromDomain(order);
    }
}
