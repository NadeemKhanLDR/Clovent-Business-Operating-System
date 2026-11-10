using Clovent.Restaurant.Application.Orders;
using Clovent.Restaurant.Application.Payments.Dtos;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.DomainServices;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.Payments;
using MediatR;

namespace Clovent.Restaurant.Application.Payments.Commands;

/// <summary>
/// Voids a payment recorded in error on an open, held, voided, or cancelled order.
/// Strictly prohibited on <see cref="OrderStatus.Completed"/> orders under PDR-0003
/// (Completed Financial Transaction Immutability): completed orders must use the
/// compensating refund workflow (<c>ProcessRefundCommand</c>).
/// </summary>
public sealed record VoidPaymentCommand(Guid PaymentId) : IRequest<PaymentDto>;

/// <summary>Handles <see cref="VoidPaymentCommand"/>.</summary>
public sealed class VoidPaymentCommandHandler(
    IPaymentRepository repository,
    IOrderRepository orderRepository,
    ICustomerRepository customerRepository,
    ICustomerLedgerEntryRepository ledgerRepository,
    IPaymentMethodRepository paymentMethodRepository,
    ICustomerPaymentAllocationRepository allocationRepository) : IRequestHandler<VoidPaymentCommand, PaymentDto>
{
    /// <inheritdoc/>
    public async Task<PaymentDto> Handle(VoidPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await repository.GetByIdAsync(new PaymentId(request.PaymentId), cancellationToken)
            ?? throw new NotFoundException(nameof(Payment), request.PaymentId);

        var orderLock = OrderTransactionLocks.GetLock(payment.OrderId.Value);
        await orderLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await HandleCoreAsync(payment.Id, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            orderLock.Release();
        }
    }

    private async Task<PaymentDto> HandleCoreAsync(PaymentId paymentId, CancellationToken cancellationToken)
    {
        var payment = await repository.GetByIdAsync(paymentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Payment), paymentId.Value);

        if (payment.IsVoided)
        {
            throw new InvalidOperationException("Payment is already voided.");
        }

        var order = await orderRepository.GetByIdAsync(payment.OrderId, cancellationToken);
        if (order is not null && order.Status == OrderStatus.Completed)
        {
            throw RestaurantDomainException.CompletedOrderPaymentCannotBeVoided(order.Id, payment.Id);
        }

        var paymentMethod = await paymentMethodRepository.GetByIdAsync(payment.PaymentMethodId, cancellationToken);
        if (paymentMethod is not null)
        {
            var isCredit = PaymentMethodClassifier.IsOnAccountCredit(paymentMethod.Name.Value);
            var isAdvance = PaymentMethodClassifier.IsCustomerAdvance(paymentMethod.Name.Value);

            if ((isCredit || isAdvance) && order?.CustomerId is { } customerId)
            {
                var customer = await customerRepository.GetByIdAsync(customerId, cancellationToken);
                if (customer is not null)
                {
                    await CleanupOrderAllocationsForVoidedPaymentAsync(
                        order.Id,
                        payment,
                        isAdvance,
                        cancellationToken);

                    customer.AdjustBalance(-payment.Amount);
                    await customerRepository.UpdateAsync(customer, cancellationToken);

                    var desc = isAdvance
                        ? $"Void Advance Settlement ({order.OrderNumber.Value})"
                        : $"Void Credit Sale ({order.OrderNumber.Value})";

                    var ledgerEntry = CustomerLedgerEntry.Create(
                        customer.Id,
                        $"VOID-{payment.Id.Value}",
                        desc,
                        0m,
                        payment.Amount,
                        customer.OutstandingBalance);
                    await ledgerRepository.AddAsync(ledgerEntry, cancellationToken);
                }
            }
        }

        payment.Void();
        return PaymentDto.FromDomain(payment);
    }

    private async Task CleanupOrderAllocationsForVoidedPaymentAsync(
        OrderId orderId,
        Payment voidedPayment,
        bool isAdvancePayment,
        CancellationToken cancellationToken)
    {
        var allocations = (await allocationRepository.GetByOrderIdAsync(orderId, cancellationToken)).ToList();
        if (allocations.Count == 0)
        {
            return;
        }

        if (isAdvancePayment)
        {
            var matchingAdvance = allocations.FirstOrDefault(a =>
                a.Amount == voidedPayment.Amount &&
                a.Notes is not null &&
                a.Notes.Contains("Customer advance applied", StringComparison.OrdinalIgnoreCase))
                ?? allocations.FirstOrDefault(a => a.Amount == voidedPayment.Amount);

            if (matchingAdvance is not null)
            {
                await allocationRepository.RemoveAsync(matchingAdvance, cancellationToken);
                allocations.Remove(matchingAdvance);
            }
        }

        // Compute remaining active credit/advance payment coverage on this order after voiding voidedPayment.
        var orderPayments = await repository.GetByOrderIdAsync(orderId, cancellationToken);
        var methods = (await paymentMethodRepository.GetAllAsync(cancellationToken)).ToDictionary(m => m.Id);

        decimal remainingCoveredPayments = 0m;
        foreach (var p in orderPayments)
        {
            if (p.IsVoided || p.Id == voidedPayment.Id)
            {
                continue;
            }

            if (methods.TryGetValue(p.PaymentMethodId, out var pm) &&
                (PaymentMethodClassifier.IsOnAccountCredit(pm.Name.Value) ||
                 PaymentMethodClassifier.IsCustomerAdvance(pm.Name.Value)))
            {
                remainingCoveredPayments += p.Amount;
            }
        }

        var allocatedTotal = allocations.Sum(a => a.Amount);
        if (allocatedTotal > remainingCoveredPayments)
        {
            // Remove newest / advance-applied allocations first so orphaned receivables allocations
            // never exceed remaining active On Account + Customer Advance tenders on the order.
            var candidates = allocations
                .OrderByDescending(a => a.Notes != null && a.Notes.Contains("Customer advance applied", StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(a => a.AllocatedAtUtc)
                .ToList();

            foreach (var alloc in candidates)
            {
                if (allocatedTotal <= remainingCoveredPayments)
                {
                    break;
                }

                await allocationRepository.RemoveAsync(alloc, cancellationToken);
                allocatedTotal -= alloc.Amount;
            }
        }
    }
}
