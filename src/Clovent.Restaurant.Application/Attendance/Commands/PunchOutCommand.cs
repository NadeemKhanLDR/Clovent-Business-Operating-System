using System;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Identity.Users;
using Clovent.MasterData.Terminals;
using Clovent.Restaurant.ActivityLogs;
using Clovent.Restaurant.Application.Attendance.Dtos;
using Clovent.Restaurant.Attendance;
using Clovent.Restaurant.Shifts;
using MediatR;

namespace Clovent.Restaurant.Application.Attendance.Commands;

/// <summary>
/// Command to punch out (clock out) an employee from their working-time attendance.
/// Blocked if the employee still owns an active open cash shift.
/// </summary>
public sealed record PunchOutCommand(
    Guid UserId,
    string UserName,
    Guid? TerminalId = null,
    string? Notes = null) : IRequest<AttendanceSessionDto>;

/// <summary>Handles <see cref="PunchOutCommand"/>.</summary>
public sealed class PunchOutCommandHandler(
    IAttendanceSessionRepository attendanceRepository,
    IShiftRepository shiftRepository,
    IActivityLogEntryRepository activityLogRepository) : IRequestHandler<PunchOutCommand, AttendanceSessionDto>
{
    /// <inheritdoc/>
    public async Task<AttendanceSessionDto> Handle(PunchOutCommand request, CancellationToken cancellationToken)
    {
        var userId = new UserId(request.UserId);

        // 1. Validate open attendance session exists
        var session = await attendanceRepository.GetOpenSessionForUserAsync(userId, cancellationToken);
        if (session == null)
        {
            throw new InvalidOperationException(
                $"No active attendance session found for employee '{request.UserName}'. Cannot punch out.");
        }

        // 2. Invariant: User CANNOT punch out while owning an open cash shift!
        var activeShift = await shiftRepository.GetActiveShiftForCashierAsync(userId, cancellationToken);
        if (activeShift != null)
        {
            throw new InvalidOperationException(
                $"CLOSE SHIFT FIRST: You still have an open POS shift (Shift #{activeShift.ShiftNumber}). Close your cash shift before punching out.");
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var terminalId = request.TerminalId.HasValue ? new TerminalId(request.TerminalId.Value) : (TerminalId?)null;

        session.PunchOut(nowUtc, terminalId, request.Notes);

        // Canonical audit log
        var activity = ActivityLogEntry.Record(
            "Employee Punched Out",
            $"Employee '{request.UserName}' punched out at {nowUtc:yyyy-MM-dd HH:mm:ss} UTC. Duration: {session.Duration:hh\\:mm}" + (string.IsNullOrWhiteSpace(request.Notes) ? "" : $" (Notes: {request.Notes})"),
            request.UserName,
            Environment.MachineName);
        await activityLogRepository.AddAsync(activity, cancellationToken);

        return AttendanceSessionDto.FromDomain(session);
    }
}
