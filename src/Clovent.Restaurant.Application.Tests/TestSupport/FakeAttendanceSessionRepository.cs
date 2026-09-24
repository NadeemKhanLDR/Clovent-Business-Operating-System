using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Identity.Users;
using Clovent.Restaurant.Attendance;

namespace Clovent.Restaurant.Application.Tests.TestSupport;

internal sealed class FakeAttendanceSessionRepository : IAttendanceSessionRepository
{
    private readonly Dictionary<AttendanceSessionId, EmployeeAttendanceSession> _sessions = [];

    public Task AddAsync(EmployeeAttendanceSession session, CancellationToken cancellationToken = default)
    {
        _sessions[session.Id] = session;
        return Task.CompletedTask;
    }

    public Task<EmployeeAttendanceSession?> GetByIdAsync(AttendanceSessionId id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_sessions.GetValueOrDefault(id));
    }

    public Task<EmployeeAttendanceSession?> GetOpenSessionForUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var session = _sessions.Values
            .Where(s => s.UserId == userId && s.Status == AttendanceStatus.Open && s.PunchOutAtUtc == null)
            .OrderByDescending(s => s.PunchInAtUtc)
            .FirstOrDefault();

        return Task.FromResult(session);
    }

    public Task<IReadOnlyList<EmployeeAttendanceSession>> GetSessionsForUserAsync(
        UserId userId,
        DateTimeOffset? fromUtc = null,
        DateTimeOffset? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var query = _sessions.Values.Where(s => s.UserId == userId);
        if (fromUtc.HasValue) query = query.Where(s => s.PunchInAtUtc >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(s => s.PunchInAtUtc <= toUtc.Value);

        return Task.FromResult<IReadOnlyList<EmployeeAttendanceSession>>(query.OrderByDescending(s => s.PunchInAtUtc).ToList());
    }

    public Task<IReadOnlyList<EmployeeAttendanceSession>> GetRecentSessionsAsync(int limit, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<EmployeeAttendanceSession>>(_sessions.Values.OrderByDescending(s => s.PunchInAtUtc).Take(limit).ToList());
    }
}
