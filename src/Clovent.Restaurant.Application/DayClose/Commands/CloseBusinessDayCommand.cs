using System;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Identity.Branches;
using Clovent.Identity.Users;
using Clovent.Restaurant.ActivityLogs;
using Clovent.Restaurant.Application.DayClose.Dtos;
using Clovent.Restaurant.Application.DayClose.Queries;
using Clovent.Restaurant.DayClose;
using MediatR;

namespace Clovent.Restaurant.Application.DayClose.Commands;

/// <summary>Command to balance, aggregate, and permanently close a restaurant operating business day.</summary>
public sealed record CloseBusinessDayCommand(
    Guid BranchId,
    DateOnly BusinessDate,
    Guid ClosedByUserId,
    string ClosedByUserName,
    string? Notes = null) : IRequest<BusinessDayCloseDto>;

/// <summary>Handles <see cref="CloseBusinessDayCommand"/>.</summary>
public sealed class CloseBusinessDayCommandHandler(
    IBusinessDayCloseRepository dayCloseRepository,
    IMediator mediator,
    IActivityLogEntryRepository activityLogRepository) : IRequestHandler<CloseBusinessDayCommand, BusinessDayCloseDto>
{
    /// <inheritdoc/>
    public async Task<BusinessDayCloseDto> Handle(CloseBusinessDayCommand request, CancellationToken cancellationToken)
    {
        var branchId = new BranchId(request.BranchId);

        // 1. Idempotency Check: Prevent duplicate day closes for same branch and business date
        var existingClose = await dayCloseRepository.GetByBranchAndDateAsync(branchId, request.BusinessDate, cancellationToken);
        if (existingClose != null)
        {
            throw new InvalidOperationException($"Business day {request.BusinessDate:yyyy-MM-dd} for this location has already been closed.");
        }

        // 2. Query summary to verify preconditions (open shifts, totals)
        var summary = await mediator.Send(new GetBusinessDaySummaryQuery(request.BranchId, request.BusinessDate), cancellationToken);

        // 3. Precondition Check: Must NOT have any open cashier shifts
        if (summary.OpenShifts.Count > 0)
        {
            var openShiftDetails = string.Join(", ", summary.OpenShifts.Select(s => $"Shift #{s.ShiftNumber} ({s.CashierName})"));
            throw new InvalidOperationException(
                $"Cannot close business day {request.BusinessDate:yyyy-MM-dd}. {summary.OpenShifts.Count} shift(s) are still open: {openShiftDetails}. All cashier shifts must be closed first.");
        }

        // 4. Create persistent auditable BusinessDayClose aggregate
        var entity = BusinessDayClose.Close(
            branchId,
            request.BusinessDate,
            new UserId(request.ClosedByUserId),
            request.ClosedByUserName,
            summary.TotalSales,
            summary.CashSales,
            summary.CardSales,
            summary.OtherSales,
            summary.Refunds,
            summary.Discounts,
            summary.Tax,
            summary.CashIn,
            summary.CashOut,
            summary.TotalShifts,
            summary.TotalShiftVariance,
            summary.TotalOrders,
            request.Notes);

        await dayCloseRepository.AddAsync(entity, cancellationToken);

        // 5. Activity Log (Audit trail)
        var activity = ActivityLogEntry.Record(
            "BusinessDayClosed",
            $"Closed business day {request.BusinessDate:yyyy-MM-dd}. Shifts: {entity.ShiftCount}, Orders: {entity.OrderCount}, Total Sales: {entity.TotalSales:N2}, Cash: {entity.CashSales:N2}, Card: {entity.CardSales:N2}, Total Variance: {entity.TotalShiftVariance:N2}",
            request.ClosedByUserName,
            Environment.MachineName);
        await activityLogRepository.AddAsync(activity, cancellationToken);

        return BusinessDayCloseDto.FromDomain(entity);
    }
}
