using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Identity.Users;
using Clovent.Restaurant.Attendance;
using Clovent.Restaurant.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Restaurant.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAttendanceSessionRepository"/>.
/// </summary>
public sealed class AttendanceSessionRepository(RestaurantDbContext dbContext) : IAttendanceSessionRepository
{
    /// <inheritdoc/>
    public async Task AddAsync(EmployeeAttendanceSession session, CancellationToken cancellationToken = default)
    {
        await dbContext.AttendanceSessions.AddAsync(session, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<EmployeeAttendanceSession?> GetByIdAsync(AttendanceSessionId id, CancellationToken cancellationToken = default)
    {
        return await dbContext.AttendanceSessions
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<EmployeeAttendanceSession?> GetOpenSessionForUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.AttendanceSessions
            .Where(s => s.UserId == userId && s.Status == AttendanceStatus.Open && s.PunchOutAtUtc == null)
            .OrderByDescending(s => s.PunchInAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<EmployeeAttendanceSession>> GetSessionsForUserAsync(
        UserId userId,
        DateTimeOffset? fromUtc = null,
        DateTimeOffset? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.AttendanceSessions
            .Where(s => s.UserId == userId);

        if (fromUtc.HasValue)
            query = query.Where(s => s.PunchInAtUtc >= fromUtc.Value);

        if (toUtc.HasValue)
            query = query.Where(s => s.PunchInAtUtc <= toUtc.Value);

        return await query
            .OrderByDescending(s => s.PunchInAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<EmployeeAttendanceSession>> GetRecentSessionsAsync(int limit, CancellationToken cancellationToken = default)
    {
        return await dbContext.AttendanceSessions
            .OrderByDescending(s => s.PunchInAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
