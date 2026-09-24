using System;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Identity.Branches;
using Clovent.Identity.Users;
using Clovent.MasterData.Terminals;
using Clovent.Restaurant.ActivityLogs;
using Clovent.Restaurant.Application.Attendance.Dtos;
using Clovent.Restaurant.Attendance;
using MediatR;

namespace Clovent.Restaurant.Application.Attendance.Commands;

/// <summary>
/// Command to punch in (clock in) an employee for their working-time attendance.
/// Does NOT open a cash drawer or shift session.
/// </summary>
public sealed record PunchInCommand(
    Guid UserId,
    string UserName,
    Guid BranchId,
    string? BranchName = null,
    Guid? TerminalId = null,
    string? Notes = null) : IRequest<AttendanceSessionDto>;

/// <summary>Handles <see cref="PunchInCommand"/>.</summary>
public sealed class PunchInCommandHandler(
    IAttendanceSessionRepository attendanceRepository,
    IActivityLogEntryRepository activityLogRepository) : IRequestHandler<PunchInCommand, AttendanceSessionDto>
{
    /// <inheritdoc/>
    public async Task<AttendanceSessionDto> Handle(PunchInCommand request, CancellationToken cancellationToken)
    {
        var userId = new UserId(request.UserId);

        // Invariant: User cannot punch in twice concurrently
        var openSession = await attendanceRepository.GetOpenSessionForUserAsync(userId, cancellationToken);
        if (openSession != null)
        {
            throw new InvalidOperationException(
                $"Employee '{request.UserName}' already has an active attendance session (Punched in at {openSession.PunchInAtUtc:u}).");
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var terminalId = request.TerminalId.HasValue ? new TerminalId(request.TerminalId.Value) : (TerminalId?)null;

        var session = EmployeeAttendanceSession.Start(
            userId,
            request.UserName,
            new BranchId(request.BranchId),
            request.BranchName,
            terminalId,
            nowUtc,
            request.Notes);

        await attendanceRepository.AddAsync(session, cancellationToken);

        // Canonical audit log
        var activity = ActivityLogEntry.Record(
            "Employee Punched In",
            $"Employee '{request.UserName}' punched in at {nowUtc:yyyy-MM-dd HH:mm:ss} UTC" + (string.IsNullOrWhiteSpace(request.Notes) ? "" : $" (Notes: {request.Notes})"),
            request.UserName,
            Environment.MachineName);
        await activityLogRepository.AddAsync(activity, cancellationToken);

        return AttendanceSessionDto.FromDomain(session);
    }
}
