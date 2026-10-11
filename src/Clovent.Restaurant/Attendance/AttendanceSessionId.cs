using System;

namespace Clovent.Restaurant.Attendance;

/// <summary>
/// Strongly-typed identifier for an <see cref="EmployeeAttendanceSession"/> aggregate.
/// </summary>
public readonly record struct AttendanceSessionId(Guid Value)
{
    /// <summary>Initializes a new <see cref="AttendanceSessionId"/> with a generated <see cref="Guid"/>.</summary>
    public AttendanceSessionId() : this(Guid.NewGuid()) { }

    /// <summary>Creates a new, unique <see cref="AttendanceSessionId"/>.</summary>
    public static AttendanceSessionId New() => new(Guid.NewGuid());
}
