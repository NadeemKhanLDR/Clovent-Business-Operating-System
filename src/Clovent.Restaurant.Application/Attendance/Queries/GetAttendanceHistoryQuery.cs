using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Identity.Users;
using Clovent.Restaurant.Application.Attendance.Dtos;
using Clovent.Restaurant.Attendance;
using MediatR;

namespace Clovent.Restaurant.Application.Attendance.Queries;

/// <summary>
/// Query to retrieve historical attendance sessions.
/// </summary>
public sealed record GetAttendanceHistoryQuery(
    Guid? UserId = null,
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null,
    int Limit = 100) : IRequest<IReadOnlyList<AttendanceSessionDto>>;

/// <summary>Handles <see cref="GetAttendanceHistoryQuery"/>.</summary>
public sealed class GetAttendanceHistoryQueryHandler(
    IAttendanceSessionRepository attendanceRepository) : IRequestHandler<GetAttendanceHistoryQuery, IReadOnlyList<AttendanceSessionDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<AttendanceSessionDto>> Handle(GetAttendanceHistoryQuery request, CancellationToken cancellationToken)
    {
        if (request.UserId.HasValue)
        {
            var sessions = await attendanceRepository.GetSessionsForUserAsync(
                new UserId(request.UserId.Value),
                request.FromUtc,
                request.ToUtc,
                cancellationToken);

            return sessions.Take(request.Limit).Select(AttendanceSessionDto.FromDomain).ToList();
        }
        else
        {
            var sessions = await attendanceRepository.GetRecentSessionsAsync(request.Limit, cancellationToken);
            return sessions.Select(AttendanceSessionDto.FromDomain).ToList();
        }
    }
}
