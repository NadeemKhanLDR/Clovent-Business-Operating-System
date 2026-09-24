using System;
using Clovent.Domain;
using Clovent.Identity.Branches;
using Clovent.Identity.Users;
using Clovent.MasterData.Terminals;
using Clovent.Restaurant.Attendance.Events;

namespace Clovent.Restaurant.Attendance;

/// <summary>
/// Represents an employee's working-time attendance session (Punch In to Punch Out).
/// This is distinct from a cash register shift (which tracks till drawer money).
/// One attendance session can encompass zero, one, or multiple cash shifts.
/// </summary>
public sealed class EmployeeAttendanceSession : AggregateRoot<AttendanceSessionId>
{
    private const int MaxUserNameLength = 150;
    private const int MaxBranchNameLength = 150;
    private const int MaxNotesLength = 1000;

    /// <summary>The employee user identity.</summary>
    public UserId UserId { get; }

    /// <summary>Display name of the employee at time of punch-in.</summary>
    public string UserName { get; }

    /// <summary>The restaurant branch where the employee is working.</summary>
    public BranchId BranchId { get; }

    /// <summary>The display name of the branch.</summary>
    public string? BranchName { get; }

    /// <summary>The terminal workstation where punch-in occurred, if applicable.</summary>
    public TerminalId? PunchInTerminalId { get; }

    /// <summary>The terminal workstation where punch-out occurred, if applicable.</summary>
    public TerminalId? PunchOutTerminalId { get; private set; }

    /// <summary>System-generated UTC timestamp when the employee punched in.</summary>
    public DateTimeOffset PunchInAtUtc { get; }

    /// <summary>System-generated UTC timestamp when the employee punched out, or null if session is active.</summary>
    public DateTimeOffset? PunchOutAtUtc { get; private set; }

    /// <summary>Current lifecycle status of the attendance session (Open or Closed).</summary>
    public AttendanceStatus Status { get; private set; }

    /// <summary>Optional notes recorded at punch-in or punch-out.</summary>
    public string? Notes { get; private set; }

    /// <summary>UTC timestamp when this record was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>UTC timestamp when this record was last updated.</summary>
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>Calculates the worked duration of this session (or time worked so far if still open).</summary>
    public TimeSpan Duration => (PunchOutAtUtc ?? DateTimeOffset.UtcNow) - PunchInAtUtc;

    /// <summary>EF Core parameterless/explicit mapping constructor.</summary>
    private EmployeeAttendanceSession(
        AttendanceSessionId id,
        UserId userId,
        string userName,
        BranchId branchId,
        string? branchName,
        TerminalId? punchInTerminalId,
        TerminalId? punchOutTerminalId,
        DateTimeOffset punchInAtUtc,
        DateTimeOffset? punchOutAtUtc,
        AttendanceStatus status,
        string? notes,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        UserId = userId;
        UserName = userName;
        BranchId = branchId;
        BranchName = branchName;
        PunchInTerminalId = punchInTerminalId;
        PunchOutTerminalId = punchOutTerminalId;
        PunchInAtUtc = punchInAtUtc;
        PunchOutAtUtc = punchOutAtUtc;
        Status = status;
        Notes = notes;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>
    /// Starts a new employee attendance session (Punch In).
    /// </summary>
    public static EmployeeAttendanceSession Start(
        UserId userId,
        string userName,
        BranchId branchId,
        string? branchName,
        TerminalId? punchInTerminalId,
        DateTimeOffset punchInAtUtc,
        string? notes = null)
    {
        if (userId.Value == Guid.Empty)
            throw new ArgumentException("UserId is required for attendance.", nameof(userId));

        if (branchId.Value == Guid.Empty)
            throw new ArgumentException("BranchId is required for attendance.", nameof(branchId));

        userName = RequireField(userName, nameof(userName), MaxUserNameLength);

        if (notes is { Length: > MaxNotesLength })
            notes = notes[..MaxNotesLength];

        if (branchName is { Length: > MaxBranchNameLength })
            branchName = branchName[..MaxBranchNameLength];

        var sessionId = AttendanceSessionId.New();
        var session = new EmployeeAttendanceSession(
            sessionId,
            userId,
            userName,
            branchId,
            branchName,
            punchInTerminalId,
            null,
            punchInAtUtc,
            null,
            AttendanceStatus.Open,
            notes,
            punchInAtUtc,
            punchInAtUtc);

        session.AddDomainEvent(new EmployeePunchedIn(sessionId, userId, branchId, punchInTerminalId, punchInAtUtc, punchInAtUtc));
        return session;
    }

    /// <summary>
    /// Closes an open attendance session (Punch Out).
    /// </summary>
    public void PunchOut(
        DateTimeOffset punchOutAtUtc,
        TerminalId? punchOutTerminalId = null,
        string? notes = null)
    {
        if (Status == AttendanceStatus.Closed)
        {
            throw RestaurantDomainException.AttendanceSessionAlreadyClosed();
        }

        if (punchOutAtUtc < PunchInAtUtc)
        {
            throw RestaurantDomainException.InvalidPunchOutTimestamp();
        }

        PunchOutAtUtc = punchOutAtUtc;
        PunchOutTerminalId = punchOutTerminalId ?? PunchOutTerminalId;
        Status = AttendanceStatus.Closed;
        UpdatedAtUtc = punchOutAtUtc;

        if (!string.IsNullOrWhiteSpace(notes))
        {
            if (notes.Length > MaxNotesLength)
                notes = notes[..MaxNotesLength];

            Notes = string.IsNullOrWhiteSpace(Notes) ? notes : $"{Notes}\n{notes}";
        }

        AddDomainEvent(new EmployeePunchedOut(Id, UserId, BranchId, punchOutTerminalId, punchOutAtUtc, Duration, punchOutAtUtc));
    }

    private static string RequireField(string value, string paramName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{paramName} is required.", paramName);

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new ArgumentException($"{paramName} cannot exceed {maxLength} characters.", paramName);

        return trimmed;
    }
}
