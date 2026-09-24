using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Identity.Branches;
using Clovent.Identity.Users;
using Clovent.MasterData.Terminals;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.Attendance.Commands;
using Clovent.Restaurant.Application.Attendance.Queries;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.Shifts;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Attendance;

public class AttendanceHandlerTests
{
    private readonly FakeAttendanceSessionRepository _attendanceRepository = new();
    private readonly FakeShiftRepository _shiftRepository = new();
    private readonly FakeActivityLogEntryRepository _activityRepository = new();

    [Fact]
    public async Task PunchIn_NoOpenSession_CreatesSessionAndLogsActivity()
    {
        var handler = new PunchInCommandHandler(_attendanceRepository, _activityRepository);
        var userId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var terminalId = Guid.NewGuid();

        var result = await handler.Handle(
            new PunchInCommand(userId, "Ahmed", branchId, "Main Branch", terminalId, "Starting morning shift"),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal("Ahmed", result.UserName);
        Assert.Equal(branchId, result.BranchId);
        Assert.Equal("Main Branch", result.BranchName);
        Assert.Equal(terminalId, result.PunchInTerminalId);
        Assert.Equal("Open", result.Status);
        Assert.Null(result.PunchOutAtUtc);

        // Verify audit log
        var activity = _activityRepository.GetAll().Single();
        Assert.Equal("Employee Punched In", activity.Action);
        Assert.Equal("Ahmed", activity.PerformedBy);
        Assert.Contains("punched in", activity.Details);
    }

    [Fact]
    public async Task PunchIn_DoublePunchIn_ThrowsInvalidOperationException()
    {
        var handler = new PunchInCommandHandler(_attendanceRepository, _activityRepository);
        var userId = Guid.NewGuid();
        var branchId = Guid.NewGuid();

        await handler.Handle(new PunchInCommand(userId, "Ahmed", branchId), CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new PunchInCommand(userId, "Ahmed", branchId), CancellationToken.None));
    }

    [Fact]
    public async Task PunchOut_WithoutPunchIn_ThrowsInvalidOperationException()
    {
        var handler = new PunchOutCommandHandler(_attendanceRepository, _shiftRepository, _activityRepository);
        var userId = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new PunchOutCommand(userId, "Ahmed"), CancellationToken.None));
    }

    [Fact]
    public async Task PunchOut_WithOpenCashShift_IsBlocked()
    {
        var punchInHandler = new PunchInCommandHandler(_attendanceRepository, _activityRepository);
        var punchOutHandler = new PunchOutCommandHandler(_attendanceRepository, _shiftRepository, _activityRepository);
        var userId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var terminalId = Guid.NewGuid();

        // 1. Employee Punches In
        await punchInHandler.Handle(new PunchInCommand(userId, "Ahmed", branchId, "Main Branch", terminalId), CancellationToken.None);

        // 2. Open Cash Shift #1001 for this employee
        var shift = Shift.Open(
            1001,
            new BranchId(branchId),
            WarehouseId.New(),
            new TerminalId(terminalId),
            new UserId(userId),
            "Ahmed",
            5000m,
            "Register 1");
        await _shiftRepository.AddAsync(shift);

        // 3. Attempt to Punch Out while shift is open -> MUST BLOCK
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            punchOutHandler.Handle(new PunchOutCommand(userId, "Ahmed"), CancellationToken.None));

        Assert.Contains("CLOSE SHIFT FIRST", ex.Message);
        Assert.Contains("1001", ex.Message);

        // Verify attendance session is STILL open
        var activeSession = await _attendanceRepository.GetOpenSessionForUserAsync(new UserId(userId));
        Assert.NotNull(activeSession);
        Assert.Null(activeSession.PunchOutAtUtc);
    }

    [Fact]
    public async Task PunchOut_AfterClosingShift_Succeeds()
    {
        var punchInHandler = new PunchInCommandHandler(_attendanceRepository, _activityRepository);
        var punchOutHandler = new PunchOutCommandHandler(_attendanceRepository, _shiftRepository, _activityRepository);
        var userId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var terminalId = Guid.NewGuid();

        // 1. Punch In
        await punchInHandler.Handle(new PunchInCommand(userId, "Ahmed", branchId, "Main Branch", terminalId), CancellationToken.None);

        // 2. Open Cash Shift #1001
        var shift = Shift.Open(
            1001,
            new BranchId(branchId),
            WarehouseId.New(),
            new TerminalId(terminalId),
            new UserId(userId),
            "Ahmed",
            5000m);
        await _shiftRepository.AddAsync(shift);

        // 3. Close Cash Shift #1001
        shift.Close(5000m, 5000m, null, "Day shift done");

        // 4. Now Punch Out -> MUST SUCCEED
        var result = await punchOutHandler.Handle(new PunchOutCommand(userId, "Ahmed", terminalId, "End of day"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Closed", result.Status);
        Assert.NotNull(result.PunchOutAtUtc);

        var punchOutActivity = _activityRepository.GetAll().First(a => a.Action == "Employee Punched Out");
        Assert.Equal("Ahmed", punchOutActivity.PerformedBy);
        Assert.Contains("punched out", punchOutActivity.Details);
    }

    [Fact]
    public async Task MultipleShifts_WithinOneAttendanceSession_PreservesSingleAttendance()
    {
        var punchInHandler = new PunchInCommandHandler(_attendanceRepository, _activityRepository);
        var punchOutHandler = new PunchOutCommandHandler(_attendanceRepository, _shiftRepository, _activityRepository);
        var userId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var terminalId = Guid.NewGuid();

        // 08:55 Punch In
        var attendanceResult = await punchInHandler.Handle(
            new PunchInCommand(userId, "Ahmed", branchId, "Main Branch", terminalId), CancellationToken.None);

        // Shift 1: 09:00 - 13:00
        var shift1 = Shift.Open(1001, new BranchId(branchId), WarehouseId.New(), new TerminalId(terminalId), new UserId(userId), "Ahmed", 5000m);
        await _shiftRepository.AddAsync(shift1);
        shift1.Close(5500m, 5500m, null, "Shift 1 close");

        // Break: 13:00 - 14:00 (still punched in)
        var stillOpenAttendance = await _attendanceRepository.GetOpenSessionForUserAsync(new UserId(userId));
        Assert.NotNull(stillOpenAttendance);
        Assert.Equal(attendanceResult.Id, stillOpenAttendance.Id.Value);

        // Shift 2: 14:00 - 18:00
        var shift2 = Shift.Open(1002, new BranchId(branchId), WarehouseId.New(), new TerminalId(terminalId), new UserId(userId), "Ahmed", 4500m);
        await _shiftRepository.AddAsync(shift2);
        shift2.Close(4800m, 4800m, null, "Shift 2 close");

        // 18:10 Punch Out
        var punchOutResult = await punchOutHandler.Handle(new PunchOutCommand(userId, "Ahmed", terminalId), CancellationToken.None);

        Assert.Equal(attendanceResult.Id, punchOutResult.Id);
        Assert.Equal("Closed", punchOutResult.Status);

        // Exactly 1 attendance session exists for Ahmed
        var userSessions = await _attendanceRepository.GetSessionsForUserAsync(new UserId(userId));
        Assert.Single(userSessions);
    }

    [Fact]
    public async Task GetActiveAttendanceSessionQuery_ReturnsActiveSessionWhenPunchedIn()
    {
        var punchInHandler = new PunchInCommandHandler(_attendanceRepository, _activityRepository);
        var queryHandler = new GetActiveAttendanceSessionQueryHandler(_attendanceRepository);
        var userId = Guid.NewGuid();

        var queryBefore = await queryHandler.Handle(new GetActiveAttendanceSessionQuery(userId), CancellationToken.None);
        Assert.Null(queryBefore);

        await punchInHandler.Handle(new PunchInCommand(userId, "Ahmed", Guid.NewGuid()), CancellationToken.None);

        var queryAfter = await queryHandler.Handle(new GetActiveAttendanceSessionQuery(userId), CancellationToken.None);
        Assert.NotNull(queryAfter);
        Assert.Equal(userId, queryAfter.UserId);
        Assert.Equal("Open", queryAfter.Status);
    }
}
