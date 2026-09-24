using System;

namespace Clovent.Restaurant.Attendance;

/// <summary>
/// Strongly-typed identifier for an <see cref="EmployeeAttendanceSession"/> aggregate.
/// </summary>
public readonly record struct AttendanceSessionId(Guid Value)
{
    public AttendanceSessionId() : this(Guid.NewGuid()) { }

    /// <summary>Creates a new, unique <see cref="AttendanceSessionId"/>.</summary>
    public static AttendanceSessionId New() => new(Guid.NewGuid());
}
