namespace Clovent.Restaurant.Attendance;

/// <summary>
/// Status lifecycle of an employee attendance session.
/// </summary>
public enum AttendanceStatus
{
    /// <summary>Active attendance session. Employee is currently clocked in.</summary>
    Open = 1,

    /// <summary>Completed attendance session. Employee has clocked out.</summary>
    Closed = 2
}
