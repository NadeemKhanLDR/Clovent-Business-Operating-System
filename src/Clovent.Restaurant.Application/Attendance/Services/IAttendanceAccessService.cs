using System;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Restaurant.Application.Attendance.Dtos;
using Clovent.Restaurant.Application.Shifts.Dtos;

namespace Clovent.Restaurant.Application.Attendance.Services;

/// <summary>
/// Service contract to evaluate employee attendance and shift relationships.
/// </summary>
public interface IAttendanceAccessService
{
    /// <summary>Retrieves the active open attendance session for an employee, or null.</summary>
    Task<AttendanceSessionDto?> GetOpenSessionAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the active open cash shift for an employee, or null.</summary>
    Task<ShiftDto?> GetActiveShiftForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
