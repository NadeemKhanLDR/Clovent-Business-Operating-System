using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Shifts;
using MediatR;

namespace Clovent.Restaurant.Application.Customers.Commands;

/// <summary>Individual collection item in a bulk customer payment batch.</summary>
public sealed record BulkCustomerPaymentItem(
    Guid CustomerId,
    decimal Amount,
    string PaymentMethodName,
    string? Reference = null,
    string? Notes = null);

/// <summary>Batch command to record multiple customer collections atomically.</summary>
public sealed record RecordBulkCustomerPaymentsCommand(
    IReadOnlyList<BulkCustomerPaymentItem> Payments,
    Guid? ShiftId = null,
    string? BatchReference = null) : IRequest<BulkCustomerPaymentBatchResult>;

/// <summary>Batch result returned after committing multiple customer payments.</summary>
public sealed record BulkCustomerPaymentBatchResult(
    string BatchReference,
    int TotalPayments,
    decimal TotalReceived,
    decimal TotalAppliedToReceivables,
    decimal TotalNewAdvances,
    IReadOnlyList<CustomerPaymentResult> Results);

/// <summary>Handles <see cref="RecordBulkCustomerPaymentsCommand"/>.</summary>
public sealed class RecordBulkCustomerPaymentsCommandHandler(
    ICustomerRepository customerRepository,
    ICustomerLedgerEntryRepository ledgerRepository,
    IOrderRepository orderRepository,
    IPaymentRepository paymentRepository,
    IPaymentMethodRepository paymentMethodRepository,
    ICustomerPaymentAllocationRepository allocationRepository)
    : IRequestHandler<RecordBulkCustomerPaymentsCommand, BulkCustomerPaymentBatchResult>
{
    /// <inheritdoc/>
    public async Task<BulkCustomerPaymentBatchResult> Handle(RecordBulkCustomerPaymentsCommand request, CancellationToken cancellationToken)
    {
        if (request.Payments == null || request.Payments.Count == 0)
        {
            throw new ArgumentException("At least one payment row is required for bulk collection.", nameof(request.Payments));
        }

        // Validate all rows before committing anything
        var hasCash = request.Payments.Any(p => string.Equals(p.PaymentMethodName, "Cash", StringComparison.OrdinalIgnoreCase));
        if (hasCash && !request.ShiftId.HasValue)
        {
            throw new InvalidOperationException("A cashier shift must be active to record cash collections.");
        }

        foreach (var item in request.Payments)
        {
            if (item.Amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(item.Amount), "Payment amount must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(item.PaymentMethodName))
            {
                throw new ArgumentException("Payment method is required for every row.", nameof(item.PaymentMethodName));
            }

            var cust = await customerRepository.GetByIdAsync(new CustomerId(item.CustomerId), cancellationToken);
            if (cust == null)
            {
                throw new NotFoundException(nameof(Customer), item.CustomerId);
            }

            if (!cust.IsActive)
            {
                throw new InvalidOperationException($"Cannot record payment for inactive customer '{cust.Name}'.");
            }
        }

        var batchRef = !string.IsNullOrWhiteSpace(request.BatchReference)
            ? request.BatchReference.Trim()
            : $"RCV-BATCH-{DateTime.UtcNow:yyyyMMddHHmmss}";

        var singleHandler = new RecordCustomerPaymentCommandHandler(
            customerRepository,
            ledgerRepository,
            orderRepository,
            paymentRepository,
            paymentMethodRepository,
            allocationRepository);

        var results = new List<CustomerPaymentResult>();
        decimal totalReceived = 0m;
        decimal totalApplied = 0m;
        decimal totalAdvances = 0m;

        foreach (var item in request.Payments)
        {
            var notePrefix = $"[Batch {batchRef}]";
            var combinedNotes = string.IsNullOrWhiteSpace(item.Notes)
                ? notePrefix
                : $"{notePrefix} {item.Notes.Trim()}";

            var refVal = !string.IsNullOrWhiteSpace(item.Reference)
                ? item.Reference.Trim()
                : batchRef;

            var res = await singleHandler.Handle(new RecordCustomerPaymentCommand(
                item.CustomerId,
                item.Amount,
                item.PaymentMethodName,
                refVal,
                combinedNotes,
                request.ShiftId), cancellationToken);

            results.Add(res);
            totalReceived += res.PaymentReceived;
            totalApplied += res.AppliedAmount;
            totalAdvances += res.AdvanceCreated;
        }

        return new BulkCustomerPaymentBatchResult(
            batchRef,
            results.Count,
            totalReceived,
            totalApplied,
            totalAdvances,
            results);
    }
}
