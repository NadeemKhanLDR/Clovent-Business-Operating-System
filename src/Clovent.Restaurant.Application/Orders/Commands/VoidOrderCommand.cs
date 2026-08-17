using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Tables;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.PaymentMethods;
using MediatR;

namespace Clovent.Restaurant.Application.Orders.Commands;

/// <summary>Voids an order. For a seated dine-in order, also vacates its table.</summary>
public sealed record VoidOrderCommand(Guid OrderId, string Reason) : IRequest<OrderDto>;

/// <summary>Handles <see cref="VoidOrderCommand"/>.</summary>
public sealed class VoidOrderCommandHandler(
    IOrderRepository orderRepository,
    ITableRepository tableRepository,
    IPaymentRepository paymentRepository,
    ICustomerRepository customerRepository,
    ICustomerLedgerEntryRepository ledgerRepository,
    IPaymentMethodRepository paymentMethodRepository)
    : IRequestHandler<VoidOrderCommand, OrderDto>
{
    /// <inheritdoc/>
    public async Task<OrderDto> Handle(VoidOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(new OrderId(request.OrderId), cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        order.Void(request.Reason);

        var payments = await paymentRepository.GetByOrderIdAsync(order.Id, cancellationToken);
        foreach (var payment in payments.Where(p => !p.IsVoided))
        {
            var paymentMethod = await paymentMethodRepository.GetByIdAsync(payment.PaymentMethodId, cancellationToken);
            if (paymentMethod is not null)
            {
                var isCredit = string.Equals(paymentMethod.Name.Value, "Credit", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(paymentMethod.Name.Value, "Customer Credit", StringComparison.OrdinalIgnoreCase);

                if (isCredit && order.CustomerId is { } customerId)
                {
                    var customer = await customerRepository.GetByIdAsync(customerId, cancellationToken);
                    if (customer is not null)
                    {
                        customer.AdjustBalance(-payment.Amount);
                        await customerRepository.UpdateAsync(customer, cancellationToken);

                        var ledgerEntry = CustomerLedgerEntry.Create(
                            customer.Id,
                            $"VOID-ORD-{order.Id.Value}",
                            $"Void Order Reversal ({order.OrderNumber.Value})",
                            0m,
                            payment.Amount,
                            customer.OutstandingBalance);
                        await ledgerRepository.AddAsync(ledgerEntry, cancellationToken);
                    }
                }
            }
            payment.Void();
        }

        if (order.TableId is { } tableId)
        {
            var table = await tableRepository.GetByIdAsync(tableId, cancellationToken);
            table?.Vacate();
        }

        return OrderDto.FromDomain(order);
    }
}
