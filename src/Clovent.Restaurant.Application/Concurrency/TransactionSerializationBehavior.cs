using Clovent.Restaurant.Application.Customers.Commands;
using Clovent.Restaurant.Application.DayClose.Commands;
using Clovent.Restaurant.Application.Discounts.Commands;
using Clovent.Restaurant.Application.OrderLines.Commands;
using Clovent.Restaurant.Application.Orders.Commands;
using Clovent.Restaurant.Application.Payments.Commands;
using Clovent.Restaurant.Application.Refunds.Commands;
using Clovent.Restaurant.Application.ServiceCharges.Commands;
using Clovent.Restaurant.Application.Shifts.Commands;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Shifts;
using Clovent.Restaurant.Tables;
using MediatR;

namespace Clovent.Restaurant.Application.Concurrency;

/// <summary>
/// MediatR pipeline behavior executing OUTER to <c>UnitOfWorkBehavior</c> so that
/// multi-entity locks (<see cref="TransactionLockScope.Shift"/> &lt; <see cref="TransactionLockScope.Customer"/> &lt; <see cref="TransactionLockScope.Order"/> &lt; <see cref="TransactionLockScope.BusinessDay"/> &lt; <see cref="TransactionLockScope.Continuity"/>)
/// remain held until <c>IUnitOfWork.SaveChangesAsync</c> completes and failure cleanup (<c>DiscardTrackedState</c>) finishes.
/// </summary>
public sealed class TransactionSerializationBehavior<TRequest, TResponse>(
    IOrderRepository? orderRepository = null,
    IPaymentRepository? paymentRepository = null,
    IShiftRepository? shiftRepository = null,
    ITransactionLockProvider? persistenceLocks = null,
    IOrderLineRepository? orderLineRepository = null) : IPipelineBehavior<TRequest, TResponse>
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
                request,
                cmd.OrderId,
                cmd.ShiftId,
                next,
                cancellationToken),

            VoidPaymentCommand cmd => ExecuteForVoidPaymentAsync(
                request,
                cmd.PaymentId,
                next,
                cancellationToken),

            VoidOrderCommand cmd => ExecuteForVoidOrderAsync(
                request,
                cmd.OrderId,
                next,
                cancellationToken),

            CompleteOrderCommand cmd => ExecutePipelineWithLocksAsync(
                request,
                [TransactionLockKey.ForOrder(cmd.OrderId)],
                cancellationToken,
                () => next()),

            SetOrderCustomerCommand cmd => ExecutePipelineWithLocksAsync(
                request,
                [TransactionLockKey.ForOrder(cmd.OrderId)],
                cancellationToken,
                () => next()),

            CancelOrderCommand cmd => ExecuteForOrderAsync(request, cmd.OrderId, next, cancellationToken),
            HoldOrderCommand cmd => ExecuteForOrderAsync(request, cmd.OrderId, next, cancellationToken),
            ResumeOrderCommand cmd => ExecuteForOrderAsync(request, cmd.OrderId, next, cancellationToken),
            ReopenOrderCommand cmd => ExecuteForOrderAsync(request, cmd.OrderId, next, cancellationToken),
            TransferOrderTableCommand cmd => ExecuteForOrderAsync(request, cmd.OrderId, next, cancellationToken),
            UpdateDeliveryDetailsCommand cmd => ExecuteForOrderAsync(request, cmd.OrderId, next, cancellationToken),
            AddOrderLineCommand cmd => ExecuteForOrderAsync(request, cmd.OrderId, next, cancellationToken),
            RemoveOrderLineCommand cmd => ExecuteForOrderAsync(request, cmd.OrderId, next, cancellationToken),
            ApplyDiscountToOrderCommand cmd => ExecuteForOrderAsync(request, cmd.OrderId, next, cancellationToken),
            RemoveDiscountFromOrderCommand cmd => ExecuteForOrderAsync(request, cmd.OrderId, next, cancellationToken),
            ApplyServiceChargeToOrderCommand cmd => ExecuteForOrderAsync(request, cmd.OrderId, next, cancellationToken),
            RemoveServiceChargeFromOrderCommand cmd => ExecuteForOrderAsync(request, cmd.OrderId, next, cancellationToken),
            SplitOrderCommand cmd => ExecuteForOrderAsync(request, cmd.SourceOrderId, next, cancellationToken),

            SetOrderLineQuantityCommand cmd => ExecuteForOrderLineAsync(request, cmd.OrderLineId, next, cancellationToken),
            OverrideOrderLinePriceCommand cmd => ExecuteForOrderLineAsync(request, cmd.OrderLineId, next, cancellationToken),
            VoidOrderLineCommand cmd => ExecuteForOrderLineAsync(request, cmd.OrderLineId, next, cancellationToken),
            UnvoidOrderLineCommand cmd => ExecuteForOrderLineAsync(request, cmd.OrderLineId, next, cancellationToken),

            MergeTablesCommand cmd => ExecuteForMergeTablesAsync(request, cmd, next, cancellationToken),

            CloseBusinessDayCommand cmd => ExecutePipelineWithLocksAsync(
                request,
                [TransactionLockKey.ForBusinessDay(cmd.BranchId)],
                cancellationToken,
                () => next()),

            OpenShiftCommand cmd => ExecutePipelineWithLocksAsync(
                request,
                [TransactionLockKey.ForBusinessDay(cmd.BranchId)],
                cancellationToken,
                () => next()),

            ProcessRefundCommand cmd => ExecuteForRefundAsync(
                request,
                cmd,
                next,
                cancellationToken),

            CloseShiftCommand cmd => ExecutePipelineWithLocksAsync(
                request,
                [TransactionLockKey.ForShift(cmd.ShiftId)],
                cancellationToken,
                () => next()),

            RecordCashMovementCommand cmd => ExecutePipelineWithLocksAsync(
                request,
                [TransactionLockKey.ForShift(cmd.ShiftId)],
                cancellationToken,
                () => next()),

            RecordCustomerPaymentCommand cmd => ExecuteForCustomerPaymentAsync(
                request,
                cmd,
                next,
                cancellationToken),

            RecordBulkCustomerPaymentsCommand cmd => ExecuteForBulkCustomerPaymentsAsync(
                request,
                cmd,
                next,
                cancellationToken),

            _ => next()
        };
    }

    private Task<TResponse> ExecuteForOrderAsync(
        TRequest request,
        Guid orderId,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken) =>
        ExecutePipelineWithLocksAsync(
            request,
            [TransactionLockKey.ForOrder(orderId)],
            cancellationToken,
            () => next());

    private async Task<TResponse> ExecuteForOrderLineAsync(
        TRequest request,
        Guid orderLineId,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (orderLineRepository is null)
        {
            return await next().ConfigureAwait(false);
        }

        var line = await orderLineRepository.GetByIdAsync(new OrderLineId(orderLineId), cancellationToken).ConfigureAwait(false);
        if (line is null)
        {
            return await next().ConfigureAwait(false);
        }

        return await ExecutePipelineWithLocksAsync(
            request,
            [TransactionLockKey.ForOrder(line.OrderId.Value)],
            cancellationToken,
            () => next()).ConfigureAwait(false);
    }

    private async Task<TResponse> ExecuteForMergeTablesAsync(
        TRequest request,
        MergeTablesCommand cmd,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (orderRepository is null)
        {
            return await next().ConfigureAwait(false);
        }

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var keys = await ResolveMergeTableOrderKeysAsync(cmd, cancellationToken).ConfigureAwait(false);
            if (keys.Count == 0)
            {
                return await next().ConfigureAwait(false);
            }

            var retryNeeded = false;
            var response = await ExecutePipelineWithLocksAsync<TResponse>(
                request,
                keys,
                cancellationToken,
                async () =>
                {
                    var currentKeys = await ResolveMergeTableOrderKeysAsync(cmd, cancellationToken).ConfigureAwait(false);
                    if (!keys.SetEquals(currentKeys))
                    {
                        retryNeeded = true;
                        return default!;
                    }

                    return await next().ConfigureAwait(false);
                }).ConfigureAwait(false);

            if (!retryNeeded)
            {
                return response;
            }
        }

        throw new InvalidOperationException("Table orders changed repeatedly during merge lock acquisition. Refresh and retry the unchanged attempt.");
    }

    private async Task<HashSet<TransactionLockKey>> ResolveMergeTableOrderKeysAsync(
        MergeTablesCommand cmd,
        CancellationToken cancellationToken)
    {
        var keys = new HashSet<TransactionLockKey>();
        if (orderRepository is null)
        {
            return keys;
        }

        var sourceOrders = await orderRepository.GetOpenOrHeldByTableIdAsync(new TableId(cmd.SourceTableId), cancellationToken).ConfigureAwait(false);
        foreach (var order in sourceOrders)
        {
            keys.Add(TransactionLockKey.ForOrder(order.Id.Value));
        }

        var targetOrders = await orderRepository.GetOpenOrHeldByTableIdAsync(new TableId(cmd.TargetTableId), cancellationToken).ConfigureAwait(false);
        foreach (var order in targetOrders)
        {
            keys.Add(TransactionLockKey.ForOrder(order.Id.Value));
        }

        return keys;
    }

    private Task<T> ExecutePipelineWithLocksAsync<T>(
        object request,
        IEnumerable<TransactionLockKey> keys,
        CancellationToken cancellationToken,
        Func<Task<T>> action) =>
        TransactionLockManager.ExecutePipelineWithLocksAsync(
            request,
            keys,
            cancellationToken,
            acquiredKeys => persistenceLocks is null || acquiredKeys.Count == 0
                ? action()
                : persistenceLocks.ExecuteAsync(acquiredKeys, cancellationToken, action));

    private async Task<TResponse> ExecuteForOrderWithCustomerAsync(
        TRequest request,
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
            var response = await ExecutePipelineWithLocksAsync<TResponse>(
                request,
                keys,
                cancellationToken,
                async () =>
                {
                    if (orderRepository is not null)
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

        throw new InvalidOperationException("Order customer changed repeatedly during lock acquisition. Refresh and retry the unchanged attempt.");
    }

    private async Task<TResponse> ExecuteForVoidPaymentAsync(
        TRequest request,
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
            request,
            payment.OrderId.Value,
            payment.ShiftId?.Value,
            next,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<TResponse> ExecuteForVoidOrderAsync(
        TRequest request,
        Guid orderId,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var keys = await ResolveVoidOrderKeysAsync(orderId, cancellationToken).ConfigureAwait(false);

            var retryNeeded = false;
            var response = await ExecutePipelineWithLocksAsync<TResponse>(
                request,
                keys,
                cancellationToken,
                async () =>
                {
                    var currentKeys = await ResolveVoidOrderKeysAsync(orderId, cancellationToken).ConfigureAwait(false);
                    if (!keys.SetEquals(currentKeys))
                    {
                        retryNeeded = true;
                        return default!;
                    }

                    return await next().ConfigureAwait(false);
                }).ConfigureAwait(false);

            if (!retryNeeded)
            {
                return response;
            }
        }

        throw new InvalidOperationException("Financial targets changed repeatedly during lock acquisition. Refresh and retry the unchanged attempt.");
    }

    private async Task<HashSet<TransactionLockKey>> ResolveVoidOrderKeysAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var keys = new HashSet<TransactionLockKey> { TransactionLockKey.ForOrder(orderId) };
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

        return keys;
    }

    private async Task<TResponse> ExecuteForRefundAsync(
        TRequest request,
        ProcessRefundCommand cmd,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var (candidateShiftId, customerId) = await ResolveRefundTargetsAsync(cmd, cancellationToken).ConfigureAwait(false);

            var keys = new List<TransactionLockKey>(3) { TransactionLockKey.ForOrder(cmd.OrderId) };
            if (candidateShiftId is { } sid) keys.Add(TransactionLockKey.ForShift(sid));
            if (customerId is { } cid) keys.Add(TransactionLockKey.ForCustomer(cid));

            var retryNeeded = false;
            var response = await ExecutePipelineWithLocksAsync<TResponse>(
                request,
                keys,
                cancellationToken,
                async () =>
                {
                    var (currentShiftId, currentCustomerId) = await ResolveRefundTargetsAsync(cmd, cancellationToken).ConfigureAwait(false);
                    if (currentShiftId != candidateShiftId || currentCustomerId != customerId)
                    {
                        retryNeeded = true;
                        return default!;
                    }

                    return await next().ConfigureAwait(false);
                }).ConfigureAwait(false);

            if (!retryNeeded)
            {
                return response;
            }
        }

        throw new InvalidOperationException("Financial targets changed repeatedly during lock acquisition. Refresh and retry the unchanged attempt.");
    }

    private async Task<(Guid? ShiftId, Guid? CustomerId)> ResolveRefundTargetsAsync(
        ProcessRefundCommand cmd,
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

        Guid? customerId = null;
        if (orderRepository is not null)
        {
            var order = await orderRepository.GetByIdAsync(new OrderId(cmd.OrderId), cancellationToken).ConfigureAwait(false);
            customerId = order?.CustomerId?.Value;
        }

        return (candidateShiftId, customerId);
    }

    private Task<TResponse> ExecuteForCustomerPaymentAsync(
        TRequest request,
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

        return ExecutePipelineWithLocksAsync(
            request,
            keys,
            cancellationToken,
            () => next());
    }

    private Task<TResponse> ExecuteForBulkCustomerPaymentsAsync(
        TRequest request,
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

        return ExecutePipelineWithLocksAsync(
            request,
            keys,
            cancellationToken,
            () => next());
    }
}
