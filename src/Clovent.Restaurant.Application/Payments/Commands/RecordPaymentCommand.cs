using Clovent.Restaurant.Application.Discounts.Dtos;
using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.Application.Orders;
using Clovent.Restaurant.Application.Payments.Dtos;
using Clovent.Restaurant.Application.ServiceCharges.Dtos;
using Clovent.Restaurant.Discounts;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.ServiceCharges;
using Clovent.Restaurant.Customers;
using MediatR;

namespace Clovent.Restaurant.Application.Payments.Commands;

/// <summary>
/// Records a payment against an order. Partial payments, multiple tenders,
/// and split-bill scenarios are all a natural consequence of an order
/// accumulating several of these - see <see cref="Payment"/>'s
/// doc comment - so this one command covers all three, not a special
/// "split bill" command.
/// </summary>
public sealed record RecordPaymentCommand(Guid OrderId, Guid PaymentMethodId, decimal Amount, bool ExceedCreditLimitApproved = false) : IRequest<PaymentDto>;

/// <summary>Handles <see cref="RecordPaymentCommand"/>.</summary>
public sealed class RecordPaymentCommandHandler(
    IOrderRepository orderRepository,
    IPaymentRepository paymentRepository,
    ICustomerRepository customerRepository,
    ICustomerLedgerEntryRepository ledgerRepository,
    IPaymentMethodRepository paymentMethodRepository,
    IOrderLineRepository orderLineRepository,
    IDiscountRepository discountRepository,
    IServiceChargeRepository serviceChargeRepository)
    : IRequestHandler<RecordPaymentCommand, PaymentDto>
{
    /// <summary>
    /// Half-cent tolerance for the outstanding-balance ceiling, matching the
    /// slack <c>CompleteOrderCommandHandler</c> and the POS screen already
    /// apply whenever money is compared against zero.
    /// </summary>
    private const decimal BalanceEpsilon = 0.005m;

    /// <inheritdoc/>
    public async Task<PaymentDto> Handle(RecordPaymentCommand request, CancellationToken cancellationToken)
    {
        var orderId = new OrderId(request.OrderId);
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        var paymentMethodId = new PaymentMethodId(request.PaymentMethodId);
        var paymentMethod = await paymentMethodRepository.GetByIdAsync(paymentMethodId, cancellationToken)
            ?? throw new NotFoundException(nameof(PaymentMethod), request.PaymentMethodId);

        await RequireWithinOutstandingBalanceAsync(orderId, request.Amount, cancellationToken);

        var isCredit = string.Equals(paymentMethod.Name.Value, "Credit", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(paymentMethod.Name.Value, "Customer Credit", StringComparison.OrdinalIgnoreCase);

        if (isCredit)
        {
            if (order.CustomerId is null)
            {
                throw new InvalidOperationException("A customer must be selected for Credit / Pay Later sales.");
            }

            var customer = await customerRepository.GetByIdAsync(order.CustomerId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(Customer), order.CustomerId.Value.Value);

            if (!customer.IsActive)
            {
                throw new InvalidOperationException($"The customer '{customer.Name}' is inactive.");
            }

            if (customer.OutstandingBalance + request.Amount > customer.CreditLimit && !request.ExceedCreditLimitApproved)
            {
                throw new InvalidOperationException(
                    $"Credit limit exceeded.\n\n" +
                    $"This customer currently owes {customer.OutstandingBalance:N2}.\n" +
                    $"The new sale would increase the balance to {(customer.OutstandingBalance + request.Amount):N2}, " +
                    $"but the credit limit is {customer.CreditLimit:N2}.\n\n" +
                    $"Collect a payment or ask an authorized manager to approve the credit sale.");
            }

            // Adjust customer balance
            customer.AdjustBalance(request.Amount);
            await customerRepository.UpdateAsync(customer, cancellationToken);

            // Add ledger entry
            var ledgerEntry = CustomerLedgerEntry.Create(
                customer.Id,
                order.OrderNumber.Value,
                "Credit Sale",
                request.Amount,
                0m,
                customer.OutstandingBalance);
            await ledgerRepository.AddAsync(ledgerEntry, cancellationToken);
        }

        var payment = Payment.Create(orderId, paymentMethodId, request.Amount);
        order.RecordPayment(payment.Id);

        await paymentRepository.AddAsync(payment, cancellationToken);

        return PaymentDto.FromDomain(payment);
    }

    /// <summary>
    /// Rejects a payment larger than what the order still owes, computing that
    /// balance here and now from the order's own lines, discounts, service
    /// charges and already-recorded payments via the same
    /// <see cref="OrderTotalsCalculator"/> <c>GetOrderSummaryQuery</c> and
    /// <c>CompleteOrderCommandHandler</c> use - never from an amount the
    /// caller supplied.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Root cause this closes:</b> the POS screen's own
    /// <c>Math.Min(tendered, _balance)</c> cap was the only over-payment
    /// defense in the system, and it reads a <em>snapshot</em> taken when the
    /// screen last refreshed. Two clicks on Record Payment before the first
    /// finished both read the same pre-payment snapshot and both recorded a
    /// full-balance payment, settling a $280 bill twice as $560 - and
    /// <c>CompleteOrderCommandHandler</c>'s <c>Balance &gt; 0.005m</c> check
    /// accepts the resulting negative balance without complaint, so the order
    /// closed over-paid and silently.
    /// </para>
    /// <para>
    /// <b>Why re-reading here is sound:</b> a screen's <c>SerializedMediator</c>
    /// gate wraps the <em>whole</em> request - handler plus
    /// <c>UnitOfWorkBehavior</c>'s <c>SaveChangesAsync</c> - so a queued second
    /// command does not begin until the first has committed. These repository
    /// calls therefore query a database that already contains the first
    /// payment row, and the second command sees a balance of zero and is
    /// refused. Partial payments are unaffected: each one only ever shrinks
    /// the balance the next is measured against, so $200 then $300 on a $500
    /// bill both pass.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException"><paramref name="amount"/> exceeds the order's outstanding balance.</exception>
    private async Task RequireWithinOutstandingBalanceAsync(OrderId orderId, decimal amount, CancellationToken cancellationToken)
    {
        var lines = await orderLineRepository.GetByOrderIdAsync(orderId, cancellationToken);
        var discounts = await discountRepository.GetByOrderIdAsync(orderId, cancellationToken);
        var serviceCharges = await serviceChargeRepository.GetByOrderIdAsync(orderId, cancellationToken);
        var payments = await paymentRepository.GetByOrderIdAsync(orderId, cancellationToken);

        var totals = OrderTotalsCalculator.Calculate(
            lines.Select(OrderLineDto.FromDomain).ToList(),
            discounts.Select(DiscountDto.FromDomain).ToList(),
            serviceCharges.Select(ServiceChargeDto.FromDomain).ToList(),
            payments.Select(PaymentDto.FromDomain).ToList());

        if (amount > totals.Balance + BalanceEpsilon)
        {
            throw new InvalidOperationException(
                $"This payment of {amount:N2} is more than the {Math.Max(totals.Balance, 0m):N2} still outstanding on this bill.\n\n" +
                $"The bill totals {totals.GrandTotal:N2} and {totals.PaidTotal:N2} has already been paid. " +
                $"If a payment was just recorded, the bill may already be settled - check the payment history before trying again.");
        }
    }
}
