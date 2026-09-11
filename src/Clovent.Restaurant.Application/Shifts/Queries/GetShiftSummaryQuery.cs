using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Restaurant.Application.Shifts.Dtos;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Shifts;
using MediatR;

namespace Clovent.Restaurant.Application.Shifts.Queries;

/// <summary>Query to generate the current financial summary for a shift session.</summary>
public sealed record GetShiftSummaryQuery(Guid ShiftId) : IRequest<ShiftSummaryDto>;

/// <summary>Handles <see cref="GetShiftSummaryQuery"/>.</summary>
public sealed class GetShiftSummaryQueryHandler(
    IShiftRepository shiftRepository,
    IPaymentRepository paymentRepository,
    IPaymentMethodRepository paymentMethodRepository) : IRequestHandler<GetShiftSummaryQuery, ShiftSummaryDto>
{
    /// <inheritdoc/>
    public async Task<ShiftSummaryDto> Handle(GetShiftSummaryQuery request, CancellationToken cancellationToken)
    {
        var shiftId = new ShiftId(request.ShiftId);
        var shift = await shiftRepository.GetByIdAsync(shiftId, cancellationToken)
            ?? throw new NotFoundException(nameof(Shift), request.ShiftId);

        var payments = await paymentRepository.GetByShiftIdAsync(shiftId, cancellationToken);
        var validPayments = payments.Where(p => !p.IsVoided).ToList();

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

        decimal countedCash = shift.Status == ShiftStatus.Closed ? shift.CountedCash : 0m;
        decimal variance = shift.Status == ShiftStatus.Closed ? shift.CashVariance : (countedCash - expectedCash);

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
            expectedCash,
            countedCash,
            variance,
            validPayments.Select(p => p.OrderId).Distinct().Count(),
            movementDtos);
    }
}
