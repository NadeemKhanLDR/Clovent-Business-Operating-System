using Clovent.Restaurant.Customers;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Shifts;
using MediatR;

namespace Clovent.Restaurant.Application.Customers.Commands;

/// <summary>Result breakdown returned when recording a customer payment.</summary>
public sealed record CustomerPaymentResult(
    decimal OutstandingBefore,
    decimal PaymentReceived,
    decimal AppliedAmount,
    decimal OutstandingAfter,
    decimal ChangeAmount,
    decimal AdvanceCreated = 0m,
    decimal ExistingAdvance = 0m,
    decimal AdvanceAfter = 0m);

/// <summary>Records money received from a customer against their outstanding balance.</summary>
public sealed record RecordCustomerPaymentCommand(
    Guid CustomerId,
    decimal AmountReceived,
    string? PaymentMethodName = null,
    string? Reference = null,
    string? Notes = null,
    Guid? ShiftId = null) : IRequest<CustomerPaymentResult>;

/// <summary>Handles <see cref="RecordCustomerPaymentCommand"/>.</summary>
public sealed class RecordCustomerPaymentCommandHandler(
    ICustomerRepository customerRepository,
    ICustomerLedgerEntryRepository ledgerRepository,
    IOrderRepository? orderRepository = null,
    IPaymentRepository? paymentRepository = null,
    IPaymentMethodRepository? paymentMethodRepository = null,
    ICustomerPaymentAllocationRepository? allocationRepository = null) : IRequestHandler<RecordCustomerPaymentCommand, CustomerPaymentResult>
{
    /// <inheritdoc/>
    public async Task<CustomerPaymentResult> Handle(RecordCustomerPaymentCommand request, CancellationToken cancellationToken)
    {
        if (request.AmountReceived <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.AmountReceived), "Payment amount must be positive.");

        var customerId = new CustomerId(request.CustomerId);
        var customer = await customerRepository.GetByIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.CustomerId);

        if (!customer.IsActive)
            throw new InvalidOperationException("Cannot record payment for an inactive customer.");

        var outstandingBefore = customer.OutstandingBalance;
        var receivableBefore = Math.Max(0m, outstandingBefore);
        var existingAdvance = Math.Max(0m, -outstandingBefore);

        // Apply payment against receivable first; excess becomes unapplied advance credit
        var appliedAmount = Math.Min(request.AmountReceived, receivableBefore);
        var advanceCreated = request.AmountReceived - appliedAmount;
        var changeAmount = 0m;

        // Adjust balance (reduces net balance by full amount received)
        customer.AdjustBalance(-request.AmountReceived);
        await customerRepository.UpdateAsync(customer, cancellationToken);

        var advanceAfter = Math.Max(0m, -customer.OutstandingBalance);

        var nextRef = !string.IsNullOrWhiteSpace(request.Reference) ? request.Reference.Trim() : $"PAY-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var method = !string.IsNullOrWhiteSpace(request.PaymentMethodName) ? request.PaymentMethodName.Trim() : "Cash";

        string description;
        if (receivableBefore == 0m)
        {
            description = !string.IsNullOrWhiteSpace(request.Notes)
                ? $"Customer Advance Payment ({method}): {request.Notes.Trim()}"
                : $"Customer Advance Payment ({method})";
        }
        else if (advanceCreated > 0m)
        {
            description = !string.IsNullOrWhiteSpace(request.Notes)
                ? $"Customer Payment ({method}) [{appliedAmount:N2} applied, {advanceCreated:N2} advance]: {request.Notes.Trim()}"
                : $"Customer Payment ({method}) [{appliedAmount:N2} applied, {advanceCreated:N2} advance]";
        }
        else
        {
            description = !string.IsNullOrWhiteSpace(request.Notes)
                ? $"Customer Payment ({method}): {request.Notes.Trim()}"
                : $"Customer Payment ({method})";
        }

        ShiftId? shiftId = request.ShiftId.HasValue ? new ShiftId(request.ShiftId.Value) : null;

        var entry = CustomerLedgerEntry.Create(
            customerId,
            nextRef,
            description,
            0m,
            request.AmountReceived,
            customer.OutstandingBalance,
            shiftId,
            method);
        await ledgerRepository.AddAsync(entry, cancellationToken);

        if (appliedAmount > 0 && orderRepository is not null && paymentRepository is not null && paymentMethodRepository is not null && allocationRepository is not null)
        {
            // FIFO allocation across open On Account orders (oldest order first)
            var customerOrders = await orderRepository.GetByCustomerIdAsync(customerId, cancellationToken);
            var sortedOrders = customerOrders.OrderBy(o => o.CreatedAtUtc).ToList();

            var paymentMethods = await paymentMethodRepository.GetAllAsync(cancellationToken);
            var onAccountMethodIds = paymentMethods
                .Where(m => string.Equals(m.Name.Value, "On Account", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(m.Name.Value, "Customer Account", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(m.Name.Value, "Credit", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(m.Name.Value, "Customer Credit", StringComparison.OrdinalIgnoreCase))
                .Select(m => m.Id)
                .ToHashSet();

            var remainingToAllocate = appliedAmount;

            foreach (var order in sortedOrders)
            {
                if (remainingToAllocate <= 0) break;

                // Find On Account payments on this order
                var payments = await paymentRepository.GetByOrderIdAsync(order.Id, cancellationToken);
                var onAccountPaid = payments
                    .Where(p => !p.IsVoided && onAccountMethodIds.Contains(p.PaymentMethodId))
                    .Sum(p => p.Amount);

                if (onAccountPaid <= 0) continue;

                // Check existing allocations against this order
                var existingAllocations = await allocationRepository.GetByOrderIdAsync(order.Id, cancellationToken);
                var alreadyAllocated = existingAllocations.Sum(a => a.Amount);
                var orderReceivableRemaining = Math.Max(0m, onAccountPaid - alreadyAllocated);

                if (orderReceivableRemaining > 0)
                {
                    var allocateAmount = Math.Min(remainingToAllocate, orderReceivableRemaining);
                    var allocation = CustomerPaymentAllocation.Create(
                        customerId,
                        order.Id,
                        allocateAmount,
                        entry.Id,
                        $"FIFO allocation from payment {nextRef}");
                    await allocationRepository.AddAsync(allocation, cancellationToken);
                    remainingToAllocate -= allocateAmount;
                }
            }
        }

        return new CustomerPaymentResult(
            outstandingBefore,
            request.AmountReceived,
            appliedAmount,
            customer.OutstandingBalance,
            changeAmount,
            advanceCreated,
            existingAdvance,
            advanceAfter);
    }
}
