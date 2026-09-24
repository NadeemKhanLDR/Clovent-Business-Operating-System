using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Identity.Branches;
using Clovent.Restaurant.Application.DayClose.Dtos;
using Clovent.Restaurant.Application.Shifts.Services;
using Clovent.Restaurant.DayClose;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Shifts;
using MediatR;

namespace Clovent.Restaurant.Application.DayClose.Queries;

/// <summary>Query to retrieve the daily close summary and open shift prerequisites for a business date.</summary>
public sealed record GetBusinessDaySummaryQuery(Guid BranchId, DateOnly BusinessDate) : IRequest<BusinessDaySummaryDto>;

/// <summary>Handles <see cref="GetBusinessDaySummaryQuery"/>.</summary>
public sealed class GetBusinessDaySummaryQueryHandler(
    IBusinessDayCloseRepository dayCloseRepository,
    IShiftRepository shiftRepository,
    IPaymentRepository paymentRepository,
    IPaymentMethodRepository paymentMethodRepository,
    IBusinessDateProvider businessDateProvider) : IRequestHandler<GetBusinessDaySummaryQuery, BusinessDaySummaryDto>
{
    /// <inheritdoc/>
    public async Task<BusinessDaySummaryDto> Handle(GetBusinessDaySummaryQuery request, CancellationToken cancellationToken)
    {
        var branchId = new BranchId(request.BranchId);

        // 1. Check if already closed
        var existingClose = await dayCloseRepository.GetByBranchAndDateAsync(branchId, request.BusinessDate, cancellationToken);
        var existingCloseDto = existingClose != null ? BusinessDayCloseDto.FromDomain(existingClose) : null;

        // 2. Resolve UTC window for this operating business date
        var (startUtc, endUtc) = businessDateProvider.GetUtcRangeForBusinessDate(request.BusinessDate);

        // 3. Retrieve shifts for this branch covering this operating period
        var shifts = await shiftRepository.SearchShiftsAsync(
            fromDateUtc: startUtc,
            toDateUtc: endUtc,
            cancellationToken: cancellationToken);

        var branchShifts = shifts.Where(s => s.BranchId == branchId).ToList();

        // Also check if any open shift exists on this branch even if opened slightly earlier
        var openShiftsList = branchShifts.Where(s => s.Status == ShiftStatus.Open).ToList();
        var closedShiftsList = branchShifts.Where(s => s.Status == ShiftStatus.Closed).ToList();

        // Map to ShiftDayCloseItemDto
        var openShiftDtos = openShiftsList.Select(s => new ShiftDayCloseItemDto(
            s.Id.Value,
            s.ShiftNumber,
            s.CashierName,
            s.TerminalId.Value,
            s.OpenedAtUtc,
            s.ClosedAtUtc,
            s.Status.ToString(),
            s.StartingCash,
            s.ExpectedCash,
            s.CountedCash,
            s.CashVariance,
            s.VarianceReason)).ToList();

        var closedShiftDtos = closedShiftsList.Select(s => new ShiftDayCloseItemDto(
            s.Id.Value,
            s.ShiftNumber,
            s.CashierName,
            s.TerminalId.Value,
            s.OpenedAtUtc,
            s.ClosedAtUtc,
            s.Status.ToString(),
            s.StartingCash,
            s.ExpectedCash,
            s.CountedCash,
            s.CashVariance,
            s.VarianceReason)).ToList();

        // Payment classification
        var allPaymentMethods = await paymentMethodRepository.GetAllAsync(cancellationToken);
        var methodsById = allPaymentMethods.ToDictionary(m => m.Id);

        decimal totalCashSales = 0m;
        decimal totalCardSales = 0m;
        decimal totalOtherSales = 0m;
        decimal totalCashIn = 0m;
        decimal totalCashOut = 0m;
        decimal totalVariance = 0m;
        var distinctOrderIds = new HashSet<Guid>();

        foreach (var shift in branchShifts)
        {
            totalCashIn += shift.CashMovements.Where(m => m.Type == CashMovementType.CashIn).Sum(m => m.Amount);
            totalCashOut += shift.CashMovements.Where(m => m.Type == CashMovementType.CashOut).Sum(m => m.Amount);

            if (shift.Status == ShiftStatus.Closed)
            {
                totalVariance += shift.CashVariance;
            }

            var payments = await paymentRepository.GetByShiftIdAsync(shift.Id, cancellationToken);
            foreach (var p in payments.Where(p => !p.IsVoided))
            {
                distinctOrderIds.Add(p.OrderId.Value);
                if (methodsById.TryGetValue(p.PaymentMethodId, out var method))
                {
                    var name = method.Name.Value;
                    if (string.Equals(name, "Cash", StringComparison.OrdinalIgnoreCase))
                    {
                        totalCashSales += p.Amount;
                    }
                    else if (name.Contains("Card", StringComparison.OrdinalIgnoreCase))
                    {
                        totalCardSales += p.Amount;
                    }
                    else
                    {
                        totalOtherSales += p.Amount;
                    }
                }
                else
                {
                    totalOtherSales += p.Amount;
                }
            }
        }

        decimal totalSales = totalCashSales + totalCardSales + totalOtherSales;
        bool isAlreadyClosed = existingClose != null;
        bool canClose = !isAlreadyClosed && openShiftsList.Count == 0;

        string? blockingReason = null;
        if (isAlreadyClosed)
        {
            blockingReason = $"Business day {request.BusinessDate:yyyy-MM-dd} for this location has already been closed.";
        }
        else if (openShiftsList.Count > 0)
        {
            blockingReason = $"{openShiftsList.Count} shift(s) are still open. All cashier shifts must be closed before closing the business day.";
        }

        return new BusinessDaySummaryDto(
            request.BranchId,
            request.BusinessDate,
            isAlreadyClosed,
            existingCloseDto,
            openShiftDtos,
            closedShiftDtos,
            branchShifts.Count,
            distinctOrderIds.Count,
            totalSales,
            totalCashSales,
            totalCardSales,
            totalOtherSales,
            0m,
            0m,
            0m,
            totalCashIn,
            totalCashOut,
            totalVariance,
            canClose,
            blockingReason);
    }
}
