using System;
using System.Linq;
using Clovent.Identity.Branches;
using Clovent.Identity.Users;
using Clovent.MasterData.Terminals;
using Clovent.Restaurant.Attendance;
using Clovent.Restaurant.Attendance.Events;
using Xunit;

namespace Clovent.Restaurant.Tests.Attendance;

public class EmployeeAttendanceSessionTests
{
    [Fact]
    public void Start_ValidParameters_CreatesOpenSessionAndRaisesEmployeePunchedIn()
    {
        var userId = UserId.New();
        var branchId = BranchId.New();
        var terminalId = TerminalId.New();
        var now = DateTimeOffset.UtcNow;

        var session = EmployeeAttendanceSession.Start(
            userId,
            "Ahmed Cashier",
            branchId,
            "Main Branch",
            terminalId,
            now,
            "Morning shift punch in");

        Assert.NotNull(session);
        Assert.Equal(userId, session.UserId);
        Assert.Equal("Ahmed Cashier", session.UserName);
        Assert.Equal(branchId, session.BranchId);
        Assert.Equal("Main Branch", session.BranchName);
        Assert.Equal(terminalId, session.PunchInTerminalId);
        Assert.Null(session.PunchOutTerminalId);
        Assert.Equal(now, session.PunchInAtUtc);
        Assert.Null(session.PunchOutAtUtc);
        Assert.Equal(AttendanceStatus.Open, session.Status);
        Assert.Equal("Morning shift punch in", session.Notes);

        var domainEvent = Assert.Single(session.DomainEvents);
        var inEvent = Assert.IsType<EmployeePunchedIn>(domainEvent);
        Assert.Equal(session.Id, inEvent.SessionId);
        Assert.Equal(userId, inEvent.UserId);
        Assert.Equal(branchId, inEvent.BranchId);
        Assert.Equal(terminalId, inEvent.TerminalId);
        Assert.Equal(now, inEvent.PunchInAtUtc);
    }

    [Fact]
    public void Start_EmptyUserId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            EmployeeAttendanceSession.Start(new UserId(Guid.Empty), "Ahmed", BranchId.New(), "Branch", null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Start_EmptyBranchId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            EmployeeAttendanceSession.Start(UserId.New(), "Ahmed", new BranchId(Guid.Empty), "Branch", null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void PunchOut_ValidParameters_ClosesSessionAndCalculatesDuration()
    {
        var punchInTime = DateTimeOffset.UtcNow.AddHours(-4);
        var session = EmployeeAttendanceSession.Start(
            UserId.New(),
            "Ahmed Cashier",
            BranchId.New(),
            "Main Branch",
            null,
            punchInTime);

        var punchOutTime = DateTimeOffset.UtcNow;
        var outTerminalId = TerminalId.New();

        session.PunchOut(punchOutTime, outTerminalId, "Finished day");

        Assert.Equal(AttendanceStatus.Closed, session.Status);
        Assert.Equal(punchOutTime, session.PunchOutAtUtc);
        Assert.Equal(outTerminalId, session.PunchOutTerminalId);
        Assert.True(session.Duration.TotalHours >= 3.9);

        var outEvent = session.DomainEvents.OfType<EmployeePunchedOut>().Single();
        Assert.Equal(session.Id, outEvent.SessionId);
        Assert.Equal(punchOutTime, outEvent.PunchOutAtUtc);
    }

    [Fact]
    public void PunchOut_AlreadyClosed_ThrowsRestaurantDomainException()
    {
        var session = EmployeeAttendanceSession.Start(
            UserId.New(),
            "Ahmed Cashier",
            BranchId.New(),
            "Main Branch",
            null,
            DateTimeOffset.UtcNow.AddHours(-2));

        session.PunchOut(DateTimeOffset.UtcNow);

        Assert.Throws<RestaurantDomainException>(() => session.PunchOut(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void PunchOut_EarlierThanPunchIn_ThrowsRestaurantDomainException()
    {
        var punchInTime = DateTimeOffset.UtcNow;
        var session = EmployeeAttendanceSession.Start(
            UserId.New(),
            "Ahmed Cashier",
            BranchId.New(),
            "Main Branch",
            null,
            punchInTime);

        Assert.Throws<RestaurantDomainException>(() => session.PunchOut(punchInTime.AddMinutes(-5)));
    }

    [Fact]
    public void DifferentUsers_HaveIndependentAttendanceSessions()
    {
        var userA = UserId.New();
        var userB = UserId.New();

        var sessionA = EmployeeAttendanceSession.Start(userA, "User A", BranchId.New(), "Branch", null, DateTimeOffset.UtcNow.AddHours(-2));
        var sessionB = EmployeeAttendanceSession.Start(userB, "User B", BranchId.New(), "Branch", null, DateTimeOffset.UtcNow.AddHours(-1));

        Assert.NotEqual(sessionA.Id, sessionB.Id);
        Assert.NotEqual(sessionA.UserId, sessionB.UserId);
        Assert.Equal(AttendanceStatus.Open, sessionA.Status);
        Assert.Equal(AttendanceStatus.Open, sessionB.Status);

        sessionA.PunchOut(DateTimeOffset.UtcNow);

        Assert.Equal(AttendanceStatus.Closed, sessionA.Status);
        Assert.Equal(AttendanceStatus.Open, sessionB.Status);
    }

    [Fact]
    public void MultipleSequentialAttendanceSessions_AreAllowed()
    {
        var user = UserId.New();
        var branch = BranchId.New();

        var session1 = EmployeeAttendanceSession.Start(user, "User", branch, "Branch", null, DateTimeOffset.UtcNow.AddHours(-8));
        session1.PunchOut(DateTimeOffset.UtcNow.AddHours(-4));

        var session2 = EmployeeAttendanceSession.Start(user, "User", branch, "Branch", null, DateTimeOffset.UtcNow.AddHours(-3));
        session2.PunchOut(DateTimeOffset.UtcNow);

        Assert.Equal(AttendanceStatus.Closed, session1.Status);
        Assert.Equal(AttendanceStatus.Closed, session2.Status);
        Assert.NotEqual(session1.Id, session2.Id);
    }
}
