using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Restaurant.ActivityLogs;
using Clovent.Restaurant.Application.Shifts.Dtos;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Shifts;
using MediatR;

namespace Clovent.Restaurant.Application.Shifts.Commands;

/// <summary>Command to balance and close an active cash register shift session.</summary>
public sealed record CloseShiftCommand(
    Guid ShiftId,
    decimal CountedCash,
    string? VarianceReason = null,
    string? Notes = null) : IRequest<ShiftSummaryDto>;

/// <summary>Handles <see cref="CloseShiftCommand"/>.</summary>
public sealed class CloseShiftCommandHandler(
    IShiftRepository shiftRepository,
    IPaymentRepository paymentRepository,
    IPaymentMethodRepository paymentMethodRepository,
    IActivityLogEntryRepository activityLogRepository) : IRequestHandler<CloseShiftCommand, ShiftSummaryDto>
{
    /// <inheritdoc/>
    public async Task<ShiftSummaryDto> Handle(CloseShiftCommand request, CancellationToken cancellationToken)
    {
        var shiftId = new ShiftId(request.ShiftId);
        var shift = await shiftRepository.GetByIdAsync(shiftId, cancellationToken)
            ?? throw new NotFoundException(nameof(Shift), request.ShiftId);

        if (shift.Status != ShiftStatus.Open)
        {
            throw new InvalidOperationException($"Shift #{shift.ShiftNumber} cannot be closed because its status is {shift.Status}.");
        }

        // Fetch all non-voided payments recorded for this shift
        var payments = await paymentRepository.GetByShiftIdAsync(shiftId, cancellationToken);
        var validPayments = payments.Where(p => !p.IsVoided).ToList();

        // Fetch payment methods for classification
        var allPaymentMethods = await paymentMethodRepository.GetAllAsync(cancellationToken);
        var paymentMethodsById = allPaymentMethods.ToDictionary(m => m.Id);

        decimal cashSales = 0m;
        decimal cardSales = 0m;
        decimal otherSales = 0m;

        foreach (var p in validPayments)
        {
            if (paymentMethodsById.TryGetValue(p.PaymentMethodId, out var method))
            {
                var name = method.Name.Value;
                if (string.Equals(name, "Cash", StringComparison.OrdinalIgnoreCase))
                {
                    cashSales += p.Amount;
                }
                else if (string.Equals(name, "Card", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(name, "Credit Card", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(name, "Debit Card", StringComparison.OrdinalIgnoreCase))
                {
                    cardSales += p.Amount;
                }
                else
                {
                    otherSales += p.Amount;
                }
            }
            else
            {
                otherSales += p.Amount;
            }
        }

        decimal cashIn = shift.CashMovements.Where(m => m.Type == CashMovementType.CashIn).Sum(m => m.Amount);
        decimal cashOut = shift.CashMovements.Where(m => m.Type == CashMovementType.CashOut).Sum(m => m.Amount);

        decimal expectedCash = shift.StartingCash + cashIn + cashSales - cashOut;

        // Domain close handles variance calculation and validation
        shift.Close(request.CountedCash, expectedCash, request.VarianceReason, request.Notes);

        await shiftRepository.UpdateAsync(shift, cancellationToken);

        // Audit log
        var activity = ActivityLogEntry.Record(
            "ShiftClosed",
            $"Closed Shift #{shift.ShiftNumber}. Expected: {expectedCash:N2}, Counted: {request.CountedCash:N2}, Variance: {shift.CashVariance:N2}",
            shift.CashierName,
            Environment.MachineName);
        await activityLogRepository.AddAsync(activity, cancellationToken);

        var movementDtos = shift.CashMovements.Select(CashMovementDto.FromDomain).ToList();

        return new ShiftSummaryDto(
            ShiftDto.FromDomain(shift),
            shift.StartingCash,
            cashSales,
            cardSales,
            otherSales,
            cashSales + cardSales + otherSales,
            cashIn,
            cashOut,
            shift.ExpectedCash,
            shift.CountedCash,
            shift.CashVariance,
            validPayments.Select(p => p.OrderId).Distinct().Count(),
            movementDtos);
    }
}
