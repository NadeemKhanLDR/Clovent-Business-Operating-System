using System;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Identity.Users;
using Clovent.Restaurant.Application.Attendance.Dtos;
using Clovent.Restaurant.Attendance;
using MediatR;

namespace Clovent.Restaurant.Application.Attendance.Queries;

/// <summary>
/// Query to find the currently active attendance session for an employee.
/// </summary>
public sealed record GetActiveAttendanceSessionQuery(Guid UserId) : IRequest<AttendanceSessionDto?>;

/// <summary>Handles <see cref="GetActiveAttendanceSessionQuery"/>.</summary>
public sealed class GetActiveAttendanceSessionQueryHandler(
    IAttendanceSessionRepository attendanceRepository) : IRequestHandler<GetActiveAttendanceSessionQuery, AttendanceSessionDto?>
{
    /// <inheritdoc/>
    public async Task<AttendanceSessionDto?> Handle(GetActiveAttendanceSessionQuery request, CancellationToken cancellationToken)
    {
        var session = await attendanceRepository.GetOpenSessionForUserAsync(new UserId(request.UserId), cancellationToken);
        return session == null ? null : AttendanceSessionDto.FromDomain(session);
    }
}
