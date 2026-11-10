using Clovent.Restaurant.Application.Customers.Commands;
using Clovent.Restaurant.Application.Orders.Commands;
using Clovent.Restaurant.Application.Payments.Commands;
using Clovent.Restaurant.Application.Refunds.Commands;
using Clovent.Restaurant.Application.Shifts.Commands;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Shifts;
using MediatR;

namespace Clovent.Restaurant.Application.Concurrency;

/// <summary>
/// MediatR pipeline behavior executing OUTER to <c>UnitOfWorkBehavior</c> so that
/// multi-entity locks (<see cref="TransactionLockScope.Shift"/> &lt; <see cref="TransactionLockScope.Customer"/> &lt; <see cref="TransactionLockScope.Order"/>)
/// remain held until <c>IUnitOfWork.SaveChangesAsync</c> completes or aborts.
/// </summary>
public sealed class TransactionSerializationBehavior<TRequest, TResponse>(
    IOrderRepository? orderRepository = null,
    IPaymentRepository? paymentRepository = null,
    IShiftRepository? shiftRepository = null) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <inheritdoc/>
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        return request switch
        {
            RecordPaymentCommand cmd => ExecuteForOrderWithCustomerAsync(
                cmd.OrderId,
                cmd.ShiftId,
                next,
                cancellationToken),

            VoidPaymentCommand cmd => ExecuteForVoidPaymentAsync(
                cmd.PaymentId,
                next,
                cancellationToken),

            VoidOrderCommand cmd => ExecuteForVoidOrderAsync(
                cmd.OrderId,
                next,
                cancellationToken),

            CompleteOrderCommand cmd => TransactionLockManager.ExecuteWithLocksAsync(
                [TransactionLockKey.ForOrder(cmd.OrderId)],
                cancellationToken,
                () => next()),

            SetOrderCustomerCommand cmd => TransactionLockManager.ExecuteWithLocksAsync(
                [TransactionLockKey.ForOrder(cmd.OrderId)],
                cancellationToken,
                () => next()),

            ProcessRefundCommand cmd => ExecuteForRefundAsync(
                cmd,
                next,
                cancellationToken),

            CloseShiftCommand cmd => TransactionLockManager.ExecuteWithLocksAsync(
                [TransactionLockKey.ForShift(cmd.ShiftId)],
                cancellationToken,
                () => next()),

            RecordCashMovementCommand cmd => TransactionLockManager.ExecuteWithLocksAsync(
                [TransactionLockKey.ForShift(cmd.ShiftId)],
                cancellationToken,
                () => next()),

            RecordCustomerPaymentCommand cmd => ExecuteForCustomerPaymentAsync(
                cmd,
                next,
                cancellationToken),

            RecordBulkCustomerPaymentsCommand cmd => ExecuteForBulkCustomerPaymentsAsync(
                cmd,
                next,
                cancellationToken),

            _ => next()
        };
    }

    private async Task<TResponse> ExecuteForOrderWithCustomerAsync(
        Guid orderId,
        Guid? shiftId,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            Guid? preReadCustomerId = null;
            if (orderRepository is not null)
            {
                var order = await orderRepository.GetByIdAsync(new OrderId(orderId), cancellationToken).ConfigureAwait(false);
                preReadCustomerId = order?.CustomerId?.Value;
            }

            var keys = new List<TransactionLockKey>(3);
            if (shiftId is { } sid) keys.Add(TransactionLockKey.ForShift(sid));
            if (preReadCustomerId is { } cid) keys.Add(TransactionLockKey.ForCustomer(cid));
            keys.Add(TransactionLockKey.ForOrder(orderId));

            var retryNeeded = false;
            var response = await TransactionLockManager.ExecuteWithLocksAsync<TResponse>(
                keys,
                cancellationToken,
                async () =>
                {
                    if (attempt == 0 && orderRepository is not null)
                    {
                        var rechecked = await orderRepository.GetByIdAsync(new OrderId(orderId), cancellationToken).ConfigureAwait(false);
                        if (rechecked?.CustomerId?.Value != preReadCustomerId)
                        {
                            retryNeeded = true;
                            return default!;
                        }
                    }

                    return await next().ConfigureAwait(false);
                }).ConfigureAwait(false);

            if (!retryNeeded)
            {
                return response;
            }
        }

        return await next().ConfigureAwait(false);
    }

    private async Task<TResponse> ExecuteForVoidPaymentAsync(
        Guid paymentId,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (paymentRepository is null)
        {
            return await next().ConfigureAwait(false);
        }

        var payment = await paymentRepository.GetByIdAsync(new PaymentId(paymentId), cancellationToken).ConfigureAwait(false);
        if (payment is null)
        {
            return await next().ConfigureAwait(false);
        }

        return await ExecuteForOrderWithCustomerAsync(
            payment.OrderId.Value,
            payment.ShiftId?.Value,
            next,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<TResponse> ExecuteForVoidOrderAsync(
        Guid orderId,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var keys = new List<TransactionLockKey> { TransactionLockKey.ForOrder(orderId) };
        if (orderRepository is not null)
        {
            var order = await orderRepository.GetByIdAsync(new OrderId(orderId), cancellationToken).ConfigureAwait(false);
            if (order?.CustomerId is { } cid)
            {
                keys.Add(TransactionLockKey.ForCustomer(cid.Value));
            }
        }

        if (paymentRepository is not null)
        {
            var payments = await paymentRepository.GetByOrderIdAsync(new OrderId(orderId), cancellationToken).ConfigureAwait(false);
            foreach (var p in payments)
            {
                if (!p.IsVoided && p.ShiftId is { } sid)
                {
                    keys.Add(TransactionLockKey.ForShift(sid.Value));
                }
            }
        }

        return await TransactionLockManager.ExecuteWithLocksAsync(
            keys,
            cancellationToken,
            () => next()).ConfigureAwait(false);
    }

    private async Task<TResponse> ExecuteForRefundAsync(
        ProcessRefundCommand cmd,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        Guid? candidateShiftId = cmd.ShiftId;
        if (!candidateShiftId.HasValue && shiftRepository is not null)
        {
            Shift? activeShift = null;
            if (cmd.TerminalId.HasValue)
            {
                activeShift = await shiftRepository.GetActiveShiftForTerminalAsync(
                    new Clovent.MasterData.Terminals.TerminalId(cmd.TerminalId.Value),
                    cancellationToken).ConfigureAwait(false);
            }

            if (activeShift is null && cmd.CashierId != Guid.Empty)
            {
                activeShift = await shiftRepository.GetActiveShiftForCashierAsync(
                    new Clovent.Identity.Users.UserId(cmd.CashierId),
                    cancellationToken).ConfigureAwait(false);
            }

            candidateShiftId = activeShift?.Id.Value;
        }

        return await ExecuteForOrderWithCustomerAsync(
            cmd.OrderId,
            candidateShiftId,
            next,
            cancellationToken).ConfigureAwait(false);
    }

    private static Task<TResponse> ExecuteForCustomerPaymentAsync(
        RecordCustomerPaymentCommand cmd,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var keys = new List<TransactionLockKey>(2)
        {
            TransactionLockKey.ForCustomer(cmd.CustomerId)
        };
        if (cmd.ShiftId.HasValue)
        {
            keys.Add(TransactionLockKey.ForShift(cmd.ShiftId.Value));
        }

        return TransactionLockManager.ExecuteWithLocksAsync(
            keys,
            cancellationToken,
            () => next());
    }

    private static Task<TResponse> ExecuteForBulkCustomerPaymentsAsync(
        RecordBulkCustomerPaymentsCommand cmd,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var keys = new List<TransactionLockKey>();
        if (cmd.ShiftId.HasValue)
        {
            keys.Add(TransactionLockKey.ForShift(cmd.ShiftId.Value));
        }

        if (cmd.Payments is not null)
        {
            foreach (var item in cmd.Payments)
            {
                keys.Add(TransactionLockKey.ForCustomer(item.CustomerId));
            }
        }

        return TransactionLockManager.ExecuteWithLocksAsync(
            keys,
            cancellationToken,
            () => next());
    }
}
