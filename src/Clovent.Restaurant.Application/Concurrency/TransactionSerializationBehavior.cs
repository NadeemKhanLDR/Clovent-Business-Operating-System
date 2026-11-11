using Clovent.Restaurant.Application.Customers.Commands;
using Clovent.Restaurant.Application.DayClose.Commands;
using Clovent.Restaurant.Application.Discounts.Commands;
using Clovent.Restaurant.Application.OrderLines.Commands;
using Clovent.Restaurant.Application.Orders.Commands;
using Clovent.Restaurant.Application.Payments.Commands;
using Clovent.Restaurant.Application.Refunds.Commands;
using Clovent.Restaurant.Application.ServiceCharges.Commands;
using Clovent.Restaurant.Application.Shifts.Commands;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Shifts;
using Clovent.Restaurant.Tables;
using MediatR;

namespace Clovent.Restaurant.Application.Concurrency;

/// <summary>
/// MediatR pipeline behavior executing OUTER to <c>UnitOfWorkBehavior</c> so that
/// multi-entity locks (<see cref="TransactionLockScope.BusinessDay"/> &lt; <see cref="TransactionLockScope.Shift"/> &lt; <see cref="TransactionLockScope.Customer"/> &lt; <see cref="TransactionLockScope.Table"/> &lt; <see cref="TransactionLockScope.Order"/> &lt; <see cref="TransactionLockScope.Continuity"/>)
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
            RecordPaymentCommand cmd => ExecuteWithRecheckAsync(
                request,
                ct => ResolvePaymentKeysAsync(cmd.OrderId, cmd.ShiftId, cmd.IdempotencyKey, ct),
                next,
                cancellationToken,
                "Order customer changed repeatedly during lock acquisition. Refresh and retry the unchanged attempt."),

            VoidPaymentCommand cmd => ExecuteForVoidPaymentAsync(
                request,
                cmd.PaymentId,
                next,
                cancellationToken),

            VoidOrderCommand cmd => ExecuteWithRecheckAsync(
                request,
                ct => ResolveVoidOrderKeysAsync(cmd.OrderId, ct),
                next,
                cancellationToken,
                "Financial targets changed repeatedly during lock acquisition. Refresh and retry the unchanged attempt."),

            CompleteOrderCommand cmd => ExecuteWithRecheckAsync(
                request,
                ct => ResolveOrderWithOptionalTableAndBranchAsync(cmd.OrderId, cmd.BranchId, ct),
                next,
                cancellationToken,
                "Order table assignment changed repeatedly during lock acquisition. Refresh and retry the unchanged attempt."),

            SetOrderCustomerCommand cmd => ExecuteForOrderAsync(request, cmd.OrderId, next, cancellationToken),

            CreateOrderCommand cmd => cmd.TableId is { } tid
                ? ExecutePipelineWithLocksAsync(request, [TransactionLockKey.ForTable(tid)], cancellationToken, () => next())
                : next(),

            CancelOrderCommand cmd => ExecuteForOrderWithTableAsync(request, cmd.OrderId, extraTableId: null, next, cancellationToken),
            ReopenOrderCommand cmd => ExecuteForOrderWithTableAsync(request, cmd.OrderId, extraTableId: null, next, cancellationToken),
            TransferOrderTableCommand cmd => ExecuteForOrderWithTableAsync(request, cmd.OrderId, cmd.NewTableId, next, cancellationToken),
            SplitOrderCommand cmd => ExecuteForOrderWithTableAsync(request, cmd.SourceOrderId, cmd.TargetTableId, next, cancellationToken),

            HoldOrderCommand cmd => ExecuteForOrderAsync(request, cmd.OrderId, next, cancellationToken),
            ResumeOrderCommand cmd => ExecuteForOrderAsync(request, cmd.OrderId, next, cancellationToken),
            UpdateDeliveryDetailsCommand cmd => ExecuteForOrderAsync(request, cmd.OrderId, next, cancellationToken),
            AddOrderLineCommand cmd => ExecuteForOrderAsync(request, cmd.OrderId, next, cancellationToken),
            RemoveOrderLineCommand cmd => ExecuteForOrderAsync(request, cmd.OrderId, next, cancellationToken),
            ApplyDiscountToOrderCommand cmd => ExecuteForOrderAsync(request, cmd.OrderId, next, cancellationToken),
            RemoveDiscountFromOrderCommand cmd => ExecuteForOrderAsync(request, cmd.OrderId, next, cancellationToken),
            ApplyServiceChargeToOrderCommand cmd => ExecuteForOrderAsync(request, cmd.OrderId, next, cancellationToken),
            RemoveServiceChargeFromOrderCommand cmd => ExecuteForOrderAsync(request, cmd.OrderId, next, cancellationToken),

            SetOrderLineQuantityCommand cmd => ExecuteForOrderLineAsync(request, cmd.OrderLineId, next, cancellationToken),
            OverrideOrderLinePriceCommand cmd => ExecuteForOrderLineAsync(request, cmd.OrderLineId, next, cancellationToken),
            VoidOrderLineCommand cmd => ExecuteForOrderLineAsync(request, cmd.OrderLineId, next, cancellationToken),
            UnvoidOrderLineCommand cmd => ExecuteForOrderLineAsync(request, cmd.OrderLineId, next, cancellationToken),

            MergeTablesCommand cmd => ExecuteWithRecheckAsync(
                request,
                ct => ResolveMergeTableKeysAsync(cmd, ct),
                next,
                cancellationToken,
                "Table orders changed repeatedly during merge lock acquisition. Refresh and retry the unchanged attempt."),

            CloseBusinessDayCommand cmd => ExecuteWithRecheckAsync(
                request,
                ct => ResolveCloseBusinessDayKeysAsync(cmd, ct),
                next,
                cancellationToken,
                "Branch shifts changed repeatedly during business-day close lock acquisition. Refresh and retry the unchanged attempt."),

            OpenShiftCommand cmd => ExecutePipelineWithLocksAsync(
                request,
                [TransactionLockKey.ForBusinessDay(cmd.BranchId, TransactionLockAccessMode.Exclusive)],
                cancellationToken,
                () => next()),

            ProcessRefundCommand cmd => ExecuteWithRecheckAsync(
                request,
                ct => ResolveRefundKeysAsync(cmd, ct),
                next,
                cancellationToken,
                "Financial targets changed repeatedly during lock acquisition. Refresh and retry the unchanged attempt."),

            CloseShiftCommand cmd => ExecuteForShiftAsync(request, cmd.ShiftId, next, cancellationToken),
            RecordCashMovementCommand cmd => ExecuteForShiftAsync(request, cmd.ShiftId, next, cancellationToken),

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

    private async Task<TResponse> ExecuteWithRecheckAsync(
        TRequest request,
        Func<CancellationToken, Task<HashSet<TransactionLockKey>>> resolveKeys,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken,
        string instabilityMessage)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var keys = await resolveKeys(cancellationToken).ConfigureAwait(false);
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
                    var currentKeys = await resolveKeys(cancellationToken).ConfigureAwait(false);
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

        throw new InvalidOperationException(instabilityMessage);
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

    private Task<TResponse> ExecuteForOrderWithTableAsync(
        TRequest request,
        Guid orderId,
        Guid? extraTableId,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken) =>
        ExecuteWithRecheckAsync(
            request,
            async ct =>
            {
                var keys = new HashSet<TransactionLockKey> { TransactionLockKey.ForOrder(orderId) };
                if (extraTableId is { } tid)
                {
                    keys.Add(TransactionLockKey.ForTable(tid));
                }

                if (orderRepository is not null)
                {
                    var order = await orderRepository.GetByIdAsync(new OrderId(orderId), ct).ConfigureAwait(false);
                    if (order?.TableId is { } existingTableId)
                    {
                        keys.Add(TransactionLockKey.ForTable(existingTableId.Value));
                    }
                }

                return keys;
            },
            next,
            cancellationToken,
            "Order table assignment changed repeatedly during lock acquisition. Refresh and retry the unchanged attempt.");

    private async Task<HashSet<TransactionLockKey>> ResolveOrderWithOptionalTableAndBranchAsync(
        Guid orderId,
        Guid? branchId,
        CancellationToken cancellationToken)
    {
        var keys = new HashSet<TransactionLockKey> { TransactionLockKey.ForOrder(orderId) };
        if (branchId is { } bid && bid != Guid.Empty)
        {
            keys.Add(TransactionLockKey.ForBusinessDay(bid, TransactionLockAccessMode.Shared));
        }

        if (orderRepository is not null)
        {
            var order = await orderRepository.GetByIdAsync(new OrderId(orderId), cancellationToken).ConfigureAwait(false);
            if (order?.TableId is { } tid)
            {
                keys.Add(TransactionLockKey.ForTable(tid.Value));
            }
        }

        return keys;
    }

    private Task<TResponse> ExecuteForOrderLineAsync(
        TRequest request,
        Guid orderLineId,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken) =>
        ExecuteWithRecheckAsync(
            request,
            async ct =>
            {
                if (orderLineRepository is null)
                {
                    return [];
                }

                var line = await orderLineRepository.GetByIdAsync(new OrderLineId(orderLineId), ct).ConfigureAwait(false);
                return line is null
                    ? []
                    : [TransactionLockKey.ForOrder(line.OrderId.Value)];
            },
            next,
            cancellationToken,
            "Order line moved repeatedly between orders during lock acquisition. Refresh and retry the unchanged attempt.");

    private async Task<HashSet<TransactionLockKey>> ResolveMergeTableKeysAsync(
        MergeTablesCommand cmd,
        CancellationToken cancellationToken)
    {
        var keys = new HashSet<TransactionLockKey>
        {
            TransactionLockKey.ForTable(cmd.SourceTableId),
            TransactionLockKey.ForTable(cmd.TargetTableId)
        };

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

    private async Task<HashSet<TransactionLockKey>> ResolvePaymentKeysAsync(
        Guid orderId,
        Guid? shiftId,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var keys = new HashSet<TransactionLockKey> { TransactionLockKey.ForOrder(orderId) };
        await AddShiftAndBranchSharedLockAsync(keys, shiftId, cancellationToken).ConfigureAwait(false);

        if (orderRepository is not null)
        {
            var order = await orderRepository.GetByIdAsync(new OrderId(orderId), cancellationToken).ConfigureAwait(false);
            if (order?.CustomerId is { } cid)
            {
                keys.Add(TransactionLockKey.ForCustomer(cid.Value));
            }
        }

        if (TryParseEmergencyTransactionId(idempotencyKey) is { } emergencyTxId)
        {
            keys.Add(TransactionLockKey.ForContinuityTransaction(emergencyTxId));
        }

        return keys;
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

        return await ExecuteWithRecheckAsync(
            request,
            ct => ResolvePaymentKeysAsync(payment.OrderId.Value, payment.ShiftId?.Value, idempotencyKey: null, ct),
            next,
            cancellationToken,
            "Order customer changed repeatedly during lock acquisition. Refresh and retry the unchanged attempt.").ConfigureAwait(false);
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

            if (order?.TableId is { } tid)
            {
                keys.Add(TransactionLockKey.ForTable(tid.Value));
            }
        }

        if (paymentRepository is not null)
        {
            var payments = await paymentRepository.GetByOrderIdAsync(new OrderId(orderId), cancellationToken).ConfigureAwait(false);
            foreach (var p in payments)
            {
                if (!p.IsVoided && p.ShiftId is { } sid)
                {
                    await AddShiftAndBranchSharedLockAsync(keys, sid.Value, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        return keys;
    }

    private async Task<HashSet<TransactionLockKey>> ResolveRefundKeysAsync(
        ProcessRefundCommand cmd,
        CancellationToken cancellationToken)
    {
        Guid? candidateShiftId = cmd.ShiftId;
        Guid? branchIdFromShift = null;

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
            branchIdFromShift = activeShift?.BranchId.Value;
        }

        var keys = new HashSet<TransactionLockKey> { TransactionLockKey.ForOrder(cmd.OrderId) };
        if (candidateShiftId is { } sid)
        {
            if (branchIdFromShift is { } bid && bid != Guid.Empty)
            {
                keys.Add(TransactionLockKey.ForShift(sid));
                keys.Add(TransactionLockKey.ForBusinessDay(bid, TransactionLockAccessMode.Shared));
            }
            else
            {
                await AddShiftAndBranchSharedLockAsync(keys, sid, cancellationToken).ConfigureAwait(false);
            }
        }

        if (orderRepository is not null)
        {
            var order = await orderRepository.GetByIdAsync(new OrderId(cmd.OrderId), cancellationToken).ConfigureAwait(false);
            if (order?.CustomerId is { } cid)
            {
                keys.Add(TransactionLockKey.ForCustomer(cid.Value));
            }
        }

        return keys;
    }

    private async Task<TResponse> ExecuteForShiftAsync(
        TRequest request,
        Guid shiftId,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var keys = new HashSet<TransactionLockKey>();
        await AddShiftAndBranchSharedLockAsync(keys, shiftId, cancellationToken).ConfigureAwait(false);

        return await ExecutePipelineWithLocksAsync(
            request,
            keys,
            cancellationToken,
            () => next()).ConfigureAwait(false);
    }

    private async Task<TResponse> ExecuteForCustomerPaymentAsync(
        TRequest request,
        RecordCustomerPaymentCommand cmd,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var keys = new HashSet<TransactionLockKey> { TransactionLockKey.ForCustomer(cmd.CustomerId) };
        await AddShiftAndBranchSharedLockAsync(keys, cmd.ShiftId, cancellationToken).ConfigureAwait(false);

        return await ExecutePipelineWithLocksAsync(
            request,
            keys,
            cancellationToken,
            () => next()).ConfigureAwait(false);
    }

    private async Task<TResponse> ExecuteForBulkCustomerPaymentsAsync(
        TRequest request,
        RecordBulkCustomerPaymentsCommand cmd,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var keys = new HashSet<TransactionLockKey>();
        await AddShiftAndBranchSharedLockAsync(keys, cmd.ShiftId, cancellationToken).ConfigureAwait(false);

        if (cmd.Payments is not null)
        {
            foreach (var item in cmd.Payments)
            {
                keys.Add(TransactionLockKey.ForCustomer(item.CustomerId));
            }
        }

        return await ExecutePipelineWithLocksAsync(
            request,
            keys,
            cancellationToken,
            () => next()).ConfigureAwait(false);
    }

    private async Task<HashSet<TransactionLockKey>> ResolveCloseBusinessDayKeysAsync(
        CloseBusinessDayCommand cmd,
        CancellationToken cancellationToken)
    {
        var keys = new HashSet<TransactionLockKey>
        {
            TransactionLockKey.ForBusinessDay(cmd.BranchId, TransactionLockAccessMode.Exclusive)
        };

        if (shiftRepository is not null)
        {
            var fromWindow = new DateTimeOffset(cmd.BusinessDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(-1);
            var recentShifts = await shiftRepository.SearchShiftsAsync(
                fromDateUtc: fromWindow,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            foreach (var s in recentShifts)
            {
                if (s.BranchId.Value == cmd.BranchId)
                {
                    keys.Add(TransactionLockKey.ForShift(s.Id.Value));
                }
            }

            var openShifts = await shiftRepository.SearchShiftsAsync(
                status: ShiftStatus.Open,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            foreach (var s in openShifts)
            {
                if (s.BranchId.Value == cmd.BranchId)
                {
                    keys.Add(TransactionLockKey.ForShift(s.Id.Value));
                }
            }
        }

        return keys;
    }

    private async Task AddShiftAndBranchSharedLockAsync(
        HashSet<TransactionLockKey> keys,
        Guid? shiftId,
        CancellationToken cancellationToken)
    {
        if (shiftId is not { } sid || sid == Guid.Empty)
        {
            return;
        }

        keys.Add(TransactionLockKey.ForShift(sid));
        if (shiftRepository is not null)
        {
            var shift = await shiftRepository.GetByIdAsync(new ShiftId(sid), cancellationToken).ConfigureAwait(false);
            if (shift is not null && shift.BranchId.Value != Guid.Empty)
            {
                keys.Add(TransactionLockKey.ForBusinessDay(shift.BranchId.Value, TransactionLockAccessMode.Shared));
            }
        }
    }

    private static Guid? TryParseEmergencyTransactionId(string? idempotencyKey)
    {
        const string prefix = "emergency:";
        if (string.IsNullOrWhiteSpace(idempotencyKey) ||
            !idempotencyKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return Guid.TryParse(idempotencyKey.AsSpan(prefix.Length), out var txId) && txId != Guid.Empty
            ? txId
            : null;
    }
}
