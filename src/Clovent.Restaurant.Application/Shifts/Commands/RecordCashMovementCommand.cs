using System;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Identity.Users;
using Clovent.Restaurant.ActivityLogs;
using Clovent.Restaurant.Application.Shifts.Dtos;
using Clovent.Restaurant.Shifts;
using MediatR;

namespace Clovent.Restaurant.Application.Shifts.Commands;

/// <summary>Command to record cash added to (CashIn) or removed from (CashOut) the cash register drawer.</summary>
public sealed record RecordCashMovementCommand(
    Guid ShiftId,
    CashMovementType Type,
    decimal Amount,
    string Reason,
    Guid UserId,
    string? Notes = null) : IRequest<CashMovementDto>;

/// <summary>Handles <see cref="RecordCashMovementCommand"/>.</summary>
public sealed class RecordCashMovementCommandHandler(
    IShiftRepository shiftRepository,
    IActivityLogEntryRepository activityLogRepository) : IRequestHandler<RecordCashMovementCommand, CashMovementDto>
{
    /// <inheritdoc/>
    public async Task<CashMovementDto> Handle(RecordCashMovementCommand request, CancellationToken cancellationToken)
    {
        var shiftId = new ShiftId(request.ShiftId);
        var shift = await shiftRepository.GetByIdAsync(shiftId, cancellationToken)
            ?? throw new NotFoundException(nameof(Shift), request.ShiftId);

        if (shift.Status != ShiftStatus.Open)
        {
            throw new InvalidOperationException($"Cannot add cash movements to Shift #{shift.ShiftNumber} because it is {shift.Status}.");
        }

        var userId = new UserId(request.UserId);
        var movement = shift.AddCashMovement(request.Type, request.Amount, request.Reason, userId, request.Notes);

        await shiftRepository.UpdateAsync(shift, cancellationToken);

        // Audit log
        var activity = ActivityLogEntry.Record(
            request.Type == CashMovementType.CashIn ? "CashIn" : "CashOut",
            $"{request.Type}: {request.Amount:N2} - {request.Reason}",
            shift.CashierName,
            Environment.MachineName);
        await activityLogRepository.AddAsync(activity, cancellationToken);

        return CashMovementDto.FromDomain(movement);
    }
}
