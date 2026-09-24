using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Identity.Users;

namespace Clovent.Restaurant.Attendance;

/// <summary>
/// Persistence contract for <see cref="EmployeeAttendanceSession"/> aggregates.
/// </summary>
public interface IAttendanceSessionRepository
{
    /// <summary>Adds a new attendance session.</summary>
    Task AddAsync(EmployeeAttendanceSession session, CancellationToken cancellationToken = default);

    /// <summary>Finds an attendance session by its strongly typed ID.</summary>
    Task<EmployeeAttendanceSession?> GetByIdAsync(AttendanceSessionId id, CancellationToken cancellationToken = default);

    /// <summary>Finds the currently open attendance session for an employee, or null if none.</summary>
    Task<EmployeeAttendanceSession?> GetOpenSessionForUserAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>Lists attendance sessions for an employee within an optional time range.</summary>
    Task<IReadOnlyList<EmployeeAttendanceSession>> GetSessionsForUserAsync(
        UserId userId,
        DateTimeOffset? fromUtc = null,
        DateTimeOffset? toUtc = null,
        CancellationToken cancellationToken = default);

    /// <summary>Lists recent attendance sessions across all employees.</summary>
    Task<IReadOnlyList<EmployeeAttendanceSession>> GetRecentSessionsAsync(int limit, CancellationToken cancellationToken = default);
}
