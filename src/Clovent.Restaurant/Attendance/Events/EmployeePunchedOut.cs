using System;
using Clovent.Domain;
using Clovent.Identity.Branches;
using Clovent.Identity.Users;
using Clovent.MasterData.Terminals;

namespace Clovent.Restaurant.Attendance.Events;

/// <summary>
/// Domain event raised when an employee punches out.
/// </summary>
public sealed record EmployeePunchedOut(
    AttendanceSessionId SessionId,
    UserId UserId,
    BranchId BranchId,
    TerminalId? TerminalId,
    DateTimeOffset PunchOutAtUtc,
    TimeSpan Duration,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;
