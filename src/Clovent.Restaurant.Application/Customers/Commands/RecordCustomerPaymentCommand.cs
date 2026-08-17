using Clovent.Restaurant.Customers;
using MediatR;

namespace Clovent.Restaurant.Application.Customers.Commands;

/// <summary>Result breakdown returned when recording a customer payment.</summary>
public sealed record CustomerPaymentResult(
    decimal OutstandingBefore,
    decimal PaymentReceived,
    decimal AppliedAmount,
    decimal OutstandingAfter,
    decimal ChangeAmount);

/// <summary>Records money received from a customer against their outstanding balance.</summary>
public sealed record RecordCustomerPaymentCommand(
    Guid CustomerId,
    decimal AmountReceived,
    string? PaymentMethodName = null,
    string? Reference = null,
    string? Notes = null) : IRequest<CustomerPaymentResult>;

/// <summary>Handles <see cref="RecordCustomerPaymentCommand"/>.</summary>
public sealed class RecordCustomerPaymentCommandHandler(
    ICustomerRepository customerRepository,
    ICustomerLedgerEntryRepository ledgerRepository) : IRequestHandler<RecordCustomerPaymentCommand, CustomerPaymentResult>
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
        
        // Match payment against outstanding balance. Excess amount becomes change handed back.
        var appliedAmount = Math.Min(request.AmountReceived, outstandingBefore);
        var changeAmount = Math.Max(0m, request.AmountReceived - appliedAmount);

        // Adjust balance (reduce outstanding balance by the applied amount)
        customer.AdjustBalance(-appliedAmount);
        await customerRepository.UpdateAsync(customer, cancellationToken);

        if (appliedAmount > 0)
        {
            var nextRef = !string.IsNullOrWhiteSpace(request.Reference) ? request.Reference.Trim() : $"PAY-{DateTime.UtcNow:yyyyMMddHHmmss}";
            var method = !string.IsNullOrWhiteSpace(request.PaymentMethodName) ? request.PaymentMethodName.Trim() : "Cash";
            var description = !string.IsNullOrWhiteSpace(request.Notes)
                ? $"Customer Payment ({method}): {request.Notes.Trim()}"
                : $"Customer Payment ({method})";

            var entry = CustomerLedgerEntry.Create(
                customerId,
                nextRef,
                description,
                0m,
                appliedAmount,
                customer.OutstandingBalance);
            await ledgerRepository.AddAsync(entry, cancellationToken);
        }

        return new CustomerPaymentResult(
            outstandingBefore,
            request.AmountReceived,
            appliedAmount,
            customer.OutstandingBalance,
            changeAmount);
    }
}
