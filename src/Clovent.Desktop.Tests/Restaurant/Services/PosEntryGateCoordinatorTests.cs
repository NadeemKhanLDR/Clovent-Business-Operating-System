using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Shell;
using Clovent.Desktop.Navigation;
using Clovent.Desktop.Restaurant.Services;
using Clovent.Desktop.Sessions;
using Clovent.Restaurant.Application.Shifts.Dtos;
using Clovent.Restaurant.Application.Shifts.Services;
using MediatR;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Services;

public class PosEntryGateCoordinatorTests
{
    private sealed class FakeTerminalResolutionService : ITerminalResolutionService
    {
        public TerminalResolutionResult ResultToReturn { get; set; } = new(IsConfigured: false, ErrorMessage: "No terminal");

        public Task<TerminalResolutionResult> ResolveCurrentTerminalAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ResultToReturn);
        }
    }

    private sealed class FakePosShiftAccessService : IPosShiftAccessService
    {
        public PosShiftAccessResult ResultToReturn { get; set; } = new(PosShiftAccessStatus.AccessDenied);

        public Task<PosShiftAccessResult> EvaluateAccessAsync(
            Guid cashierId,
            string cashierName,
            Guid? terminalId,
            string? terminalName = null,
            string? terminalCode = null,
            Guid? branchId = null,
            string? branchName = null,
            Guid? warehouseId = null,
            string? warehouseName = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ResultToReturn);
        }
    }

    private sealed class FakeAttendanceAccessService : Clovent.Restaurant.Application.Attendance.Services.IAttendanceAccessService
    {
        public Clovent.Restaurant.Application.Attendance.Dtos.AttendanceSessionDto? OpenSessionToReturn { get; set; }
        public ShiftDto? ActiveShiftToReturn { get; set; }

        public Task<Clovent.Restaurant.Application.Attendance.Dtos.AttendanceSessionDto?> GetOpenSessionAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(OpenSessionToReturn);
        }

        public Task<ShiftDto?> GetActiveShiftForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ActiveShiftToReturn);
        }
    }

    private sealed class FakeApplicationModeNavigator : IApplicationModeNavigator
    {
        public ShiftDto? LastOpenedShift { get; private set; }
        public int OpenPosCallCount { get; private set; }

        public Form? CurrentForm => null;
        public IWorkspaceHost? CurrentWorkspaceHost => null;
        public bool IsTransitioning => false;
        public ApplicationContext ApplicationContext { get; } = new CbosApplicationContext();

        public Task OpenPosAsync(ShiftDto? activeShift = null)
        {
            OpenPosCallCount++;
            LastOpenedShift = activeShift;
            return Task.CompletedTask;
        }

        public Task OpenBackOfficeAsync(string? initialViewKey = "dashboard", string? initialCaption = "Dashboard") => Task.CompletedTask;
        public void ExitApplication() { }
        public void ExitApplication(string initiator) { }
    }

    private sealed class FakeCurrentSession : ICurrentSession
    {
        public Guid? UserId { get; set; }
        public Guid? SessionId { get; set; }
        public string? DisplayName { get; set; }
        public bool IsAuthenticated => UserId.HasValue;

        public void SignIn(Guid userId, Guid sessionId, string displayName)
        {
            UserId = userId;
            SessionId = sessionId;
            DisplayName = displayName;
        }

        public void SignOut()
        {
            UserId = null;
            SessionId = null;
            DisplayName = null;
        }

#pragma warning disable CS0067
        public event EventHandler? Changed;
#pragma warning restore CS0067
    }

    private sealed class DummyMediator : IMediator
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest => throw new NotImplementedException();
        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;
    }

    private readonly FakeTerminalResolutionService _terminalService = new();
    private readonly FakePosShiftAccessService _shiftAccessService = new();
    private readonly FakeAttendanceAccessService _attendanceService = new();
    private readonly FakeApplicationModeNavigator _navigator = new();
    private readonly FakeCurrentSession _session = new();
    private readonly DummyMediator _mediator = new();

    private readonly Guid _terminalId = Guid.NewGuid();
    private readonly Guid _branchId = Guid.NewGuid();
    private readonly Guid _warehouseId = Guid.NewGuid();
    private readonly Guid _cashierId = Guid.NewGuid();

    public PosEntryGateCoordinatorTests()
    {
        _session.UserId = _cashierId;
        _session.DisplayName = "Test Cashier";
        _attendanceService.OpenSessionToReturn = new Clovent.Restaurant.Application.Attendance.Dtos.AttendanceSessionDto(
            Guid.NewGuid(),
            _cashierId,
            "Test Cashier",
            _branchId,
            "Main Branch",
            _terminalId,
            null,
            DateTimeOffset.UtcNow,
            null,
            "Open",
            null,
            TimeSpan.FromHours(1),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task EnsureShiftAndOpenPosAsync_WhenTerminalNotConfigured_ReturnsFalse_AndDoesNotOpenPos()
    {
        _terminalService.ResultToReturn = new(IsConfigured: false, ErrorMessage: "No terminal configured.");

        var coordinator = new PosEntryGateCoordinator(
            _terminalService,
            _shiftAccessService,
            _navigator,
            _mediator,
            _session,
            _attendanceService);

        var result = await coordinator.EnsureShiftAndOpenPosAsync();

        Assert.False(result);
        Assert.Equal(0, _navigator.OpenPosCallCount);
    }

    [Fact]
    public async Task EnsureShiftAndOpenPosAsync_WhenExistingOwnShift_ResumesShiftDirectly_AndReturnsTrue()
    {
        var existingShift = new ShiftDto(
            Guid.NewGuid(),
            42,
            _branchId,
            _warehouseId,
            _terminalId,
            _cashierId,
            "Test Cashier",
            DateTimeOffset.UtcNow,
            null,
            "Open",
            100m,
            100m,
            100m,
            0m,
            null,
            null,
            DateTimeOffset.UtcNow);

        _terminalService.ResultToReturn = new(
            IsConfigured: true,
            TerminalId: _terminalId,
            TerminalName: "Main POS",
            TerminalCode: "TERM-01",
            BranchId: _branchId,
            BranchName: "Main Branch",
            WarehouseId: _warehouseId,
            WarehouseName: "Main WH");

        _shiftAccessService.ResultToReturn = new(
            Status: PosShiftAccessStatus.ExistingOwnShift,
            CurrentShift: existingShift);

        var coordinator = new PosEntryGateCoordinator(
            _terminalService,
            _shiftAccessService,
            _navigator,
            _mediator,
            _session,
            _attendanceService);

        var result = await coordinator.EnsureShiftAndOpenPosAsync();

        Assert.True(result);
        Assert.Equal(1, _navigator.OpenPosCallCount);
        Assert.Same(existingShift, _navigator.LastOpenedShift);
    }

    [Fact]
    public async Task EnsureShiftAndOpenPosAsync_WhenTerminalOccupiedByAnotherUser_BlocksAccess_AndReturnsFalse()
    {
        _terminalService.ResultToReturn = new(
            IsConfigured: true,
            TerminalId: _terminalId,
            TerminalName: "Main POS",
            TerminalCode: "TERM-01",
            BranchId: _branchId,
            BranchName: "Main Branch",
            WarehouseId: _warehouseId,
            WarehouseName: "Main WH");

        _shiftAccessService.ResultToReturn = new(
            Status: PosShiftAccessStatus.TerminalOccupiedByAnotherUser,
            Message: "Terminal occupied by another cashier.");

        var coordinator = new PosEntryGateCoordinator(
            _terminalService,
            _shiftAccessService,
            _navigator,
            _mediator,
            _session,
            _attendanceService);

        var result = await coordinator.EnsureShiftAndOpenPosAsync();

        Assert.False(result);
        Assert.Equal(0, _navigator.OpenPosCallCount);
    }

    [Fact]
    public async Task EnsureShiftAndOpenPosAsync_WhenUserHasShiftOnAnotherTerminal_BlocksAccess_AndReturnsFalse()
    {
        _terminalService.ResultToReturn = new(
            IsConfigured: true,
            TerminalId: _terminalId,
            TerminalName: "Main POS",
            TerminalCode: "TERM-01",
            BranchId: _branchId,
            BranchName: "Main Branch",
            WarehouseId: _warehouseId,
            WarehouseName: "Main WH");

        _shiftAccessService.ResultToReturn = new(
            Status: PosShiftAccessStatus.UserHasShiftOnAnotherTerminal,
            Message: "User has shift on another terminal.");

        var coordinator = new PosEntryGateCoordinator(
            _terminalService,
            _shiftAccessService,
            _navigator,
            _mediator,
            _session,
            _attendanceService);

        var result = await coordinator.EnsureShiftAndOpenPosAsync();

        Assert.False(result);
        Assert.Equal(0, _navigator.OpenPosCallCount);
    }

    [Fact]
    public async Task EnsureShiftAndOpenPosAsync_WhenNotPunchedIn_WithoutOwner_ReturnsFalse_AndDoesNotOpenPos()
    {
        _attendanceService.OpenSessionToReturn = null; // Not punched in!

        _terminalService.ResultToReturn = new(
            IsConfigured: true,
            TerminalId: _terminalId,
            TerminalName: "Main POS",
            TerminalCode: "TERM-01",
            BranchId: _branchId,
            BranchName: "Main Branch",
            WarehouseId: _warehouseId,
            WarehouseName: "Main WH");

        var coordinator = new PosEntryGateCoordinator(
            _terminalService,
            _shiftAccessService,
            _navigator,
            _mediator,
            _session,
            _attendanceService);

        var result = await coordinator.EnsureShiftAndOpenPosAsync();

        Assert.False(result);
        Assert.Equal(0, _navigator.OpenPosCallCount);
    }
}
