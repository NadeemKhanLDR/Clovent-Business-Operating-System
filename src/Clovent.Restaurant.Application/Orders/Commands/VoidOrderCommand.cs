using Clovent.Platform.Execution;
using Clovent.Restaurant.Application.Concurrency;
using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.DomainServices;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Tables;
using MediatR;

namespace Clovent.Restaurant.Application.Orders.Commands;

/// <summary>Voids an order. For a seated dine-in order, also vacates its table.</summary>
public sealed record VoidOrderCommand(Guid OrderId, string Reason) : IRequest<OrderDto>, IRequireBranchScope;

/// <summary>Handles <see cref="VoidOrderCommand"/>.</summary>
public sealed class VoidOrderCommandHandler(
    IOrderRepository orderRepository,
    ITableRepository tableRepository,
    IPaymentRepository paymentRepository,
    ICustomerRepository customerRepository,
    ICustomerLedgerEntryRepository ledgerRepository,
    IPaymentMethodRepository paymentMethodRepository,
    ICustomerPaymentAllocationRepository allocationRepository,
    Clovent.Restaurant.Application.Security.IOrderScopeValidator? orderScopeValidator = null)
    : IRequestHandler<VoidOrderCommand, OrderDto>
{
    /// <inheritdoc/>
    public async Task<OrderDto> Handle(VoidOrderCommand request, CancellationToken cancellationToken)
    {
        var orderId = new OrderId(request.OrderId);
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken);
        var payments = await paymentRepository.GetByOrderIdAsync(orderId, cancellationToken);

        var keys = new List<TransactionLockKey> { TransactionLockKey.ForOrder(request.OrderId) };
        if (order?.CustomerId is { } cid) keys.Add(TransactionLockKey.ForCustomer(cid.Value));
        foreach (var p in payments)
        {
            if (!p.IsVoided && p.ShiftId is { } sid)
            {
                keys.Add(TransactionLockKey.ForShift(sid.Value));
            }
        }

        return await TransactionLockManager.ExecuteWithLocksAsync(
            keys,
            cancellationToken,
            () => HandleCoreAsync(request, cancellationToken)).ConfigureAwait(false);
    }

    private async Task<OrderDto> HandleCoreAsync(VoidOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(new OrderId(request.OrderId), cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        if (orderScopeValidator != null)
        {
            await orderScopeValidator.ValidateOrderAccessAsync(order, cancellationToken).ConfigureAwait(false);
        }

        order.Void(request.Reason);

        var payments = await paymentRepository.GetByOrderIdAsync(order.Id, cancellationToken);
        foreach (var payment in payments.Where(p => !p.IsVoided))
        {
            var paymentMethod = await paymentMethodRepository.GetByIdAsync(payment.PaymentMethodId, cancellationToken);
            if (paymentMethod is not null)
            {
                var isCredit = PaymentMethodClassifier.IsOnAccountCredit(paymentMethod.Name.Value);
                var isAdvance = PaymentMethodClassifier.IsCustomerAdvance(paymentMethod.Name.Value);

                if ((isCredit || isAdvance) && order.CustomerId is { } customerId)
                {
                    var customer = await customerRepository.GetByIdAsync(customerId, cancellationToken);
                    if (customer is not null)
                    {
                        customer.AdjustBalance(-payment.Amount);
                        await customerRepository.UpdateAsync(customer, cancellationToken);

                        var desc = isAdvance
                            ? $"Void Advance Settlement ({order.OrderNumber.Value})"
                            : $"Void Order Reversal ({order.OrderNumber.Value})";

                        var ledgerEntry = CustomerLedgerEntry.Create(
                            customer.Id,
                            $"VOID-ORD-{order.Id.Value}",
                            desc,
                            0m,
                            payment.Amount,
                            customer.OutstandingBalance);
                        await ledgerRepository.AddAsync(ledgerEntry, cancellationToken);
                    }
                }
            }
            payment.Void();
        }

        var allocations = await allocationRepository.GetByOrderIdAsync(order.Id, cancellationToken);
        foreach (var alloc in allocations)
        {
            await allocationRepository.RemoveAsync(alloc, cancellationToken);
        }

        if (order.TableId is { } tableId)
        {
            var activeOrders = await orderRepository.GetOpenOrHeldByTableIdAsync(tableId, cancellationToken);
            if (!activeOrders.Any(o => o.Id != order.Id))
            {
                var table = await tableRepository.GetByIdAsync(tableId, cancellationToken);
                table?.Vacate();
            }
        }

        return OrderDto.FromDomain(order);
    }
}
