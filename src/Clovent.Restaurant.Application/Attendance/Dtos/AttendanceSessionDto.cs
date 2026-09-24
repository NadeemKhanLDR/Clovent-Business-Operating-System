using System;
using Clovent.Restaurant.Attendance;

namespace Clovent.Restaurant.Application.Attendance.Dtos;

/// <summary>
/// Data transfer object for an employee attendance session.
/// </summary>
public sealed record AttendanceSessionDto(
    Guid Id,
    Guid UserId,
    string UserName,
    Guid BranchId,
    string? BranchName,
    Guid? PunchInTerminalId,
    Guid? PunchOutTerminalId,
    DateTimeOffset PunchInAtUtc,
    DateTimeOffset? PunchOutAtUtc,
    string Status,
    string? Notes,
    TimeSpan Duration,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    /// <summary>Creates a DTO from the domain entity.</summary>
    public static AttendanceSessionDto FromDomain(EmployeeAttendanceSession session) =>
        new(
            session.Id.Value,
            session.UserId.Value,
            session.UserName,
            session.BranchId.Value,
            session.BranchName,
            session.PunchInTerminalId?.Value,
            session.PunchOutTerminalId?.Value,
            session.PunchInAtUtc,
            session.PunchOutAtUtc,
            session.Status.ToString(),
            session.Notes,
            session.Duration,
            session.CreatedAtUtc,
            session.UpdatedAtUtc);
}
