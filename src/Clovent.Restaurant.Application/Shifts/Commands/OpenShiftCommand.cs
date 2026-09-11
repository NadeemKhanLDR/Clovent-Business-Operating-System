using System;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Identity.Branches;
using Clovent.Identity.Users;
using Clovent.MasterData.Terminals;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.ActivityLogs;
using Clovent.Restaurant.Application.Shifts.Dtos;
using Clovent.Restaurant.Shifts;
using MediatR;

namespace Clovent.Restaurant.Application.Shifts.Commands;

/// <summary>Command to open a new cash register shift session.</summary>
public sealed record OpenShiftCommand(
    Guid BranchId,
    Guid WarehouseId,
    Guid TerminalId,
    Guid CashierId,
    string CashierName,
    decimal StartingCash,
    string? Notes = null) : IRequest<ShiftDto>;

/// <summary>Handles <see cref="OpenShiftCommand"/>.</summary>
public sealed class OpenShiftCommandHandler(
    IShiftRepository shiftRepository,
    IActivityLogEntryRepository activityLogRepository) : IRequestHandler<OpenShiftCommand, ShiftDto>
{
    /// <inheritdoc/>
    public async Task<ShiftDto> Handle(OpenShiftCommand request, CancellationToken cancellationToken)
    {
        var terminalId = new TerminalId(request.TerminalId);
        var cashierId = new UserId(request.CashierId);

        // Guard: Check if terminal already has an active open shift
        var activeTerminalShift = await shiftRepository.GetActiveShiftForTerminalAsync(terminalId, cancellationToken);
        if (activeTerminalShift != null)
        {
            throw new InvalidOperationException(
                $"Terminal is already in use by active Shift #{activeTerminalShift.ShiftNumber} opened by {activeTerminalShift.CashierName}.");
        }

        // Guard: Check if cashier already has an active open shift
        var activeCashierShift = await shiftRepository.GetActiveShiftForCashierAsync(cashierId, cancellationToken);
        if (activeCashierShift != null)
        {
            throw new InvalidOperationException(
                $"Cashier '{request.CashierName}' already has an active Shift #{activeCashierShift.ShiftNumber} open on another terminal.");
        }

        var shiftNumber = await shiftRepository.GetNextShiftNumberAsync(cancellationToken);

        var shift = Shift.Open(
            shiftNumber,
            new BranchId(request.BranchId),
            new WarehouseId(request.WarehouseId),
            terminalId,
            cashierId,
            request.CashierName,
            request.StartingCash,
            request.Notes);

        await shiftRepository.AddAsync(shift, cancellationToken);

        // Audit log
        var activity = ActivityLogEntry.Record(
            "ShiftOpened",
            $"Opened Shift #{shift.ShiftNumber} with starting cash of {request.StartingCash:N2}",
            request.CashierName,
            Environment.MachineName);
        await activityLogRepository.AddAsync(activity, cancellationToken);

        return ShiftDto.FromDomain(shift);
    }
}
