using System;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Identity.Users;
using Clovent.Restaurant.Application.Attendance.Dtos;
using Clovent.Restaurant.Application.Shifts.Dtos;
using Clovent.Restaurant.Attendance;
using Clovent.Restaurant.Shifts;

namespace Clovent.Restaurant.Application.Attendance.Services;

/// <summary>
/// Canonical implementation of <see cref="IAttendanceAccessService"/>.
/// </summary>
public sealed class AttendanceAccessService(
    IAttendanceSessionRepository attendanceRepository,
    IShiftRepository shiftRepository) : IAttendanceAccessService
{
    /// <inheritdoc/>
    public async Task<AttendanceSessionDto?> GetOpenSessionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var session = await attendanceRepository.GetOpenSessionForUserAsync(new UserId(userId), cancellationToken);
        return session == null ? null : AttendanceSessionDto.FromDomain(session);
    }

    /// <inheritdoc/>
    public async Task<ShiftDto?> GetActiveShiftForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var shift = await shiftRepository.GetActiveShiftForCashierAsync(new UserId(userId), cancellationToken);
        return shift == null ? null : ShiftDto.FromDomain(shift);
    }
}
