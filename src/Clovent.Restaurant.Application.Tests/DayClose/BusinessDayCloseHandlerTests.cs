using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Identity.Branches;
using Clovent.Identity.Users;
using Clovent.MasterData.Terminals;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.DayClose.Commands;
using Clovent.Restaurant.Application.DayClose.Queries;
using Clovent.Restaurant.Application.Shifts.Services;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.DayClose;
using Clovent.Restaurant.Shifts;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.DayClose;

public class BusinessDayCloseHandlerTests
{
    private readonly FakeBusinessDayCloseRepository _dayCloseRepo = new();
    private readonly FakeShiftRepository _shiftRepo = new();
    private readonly FakePaymentRepository _paymentRepo = new();
    private readonly FakePaymentMethodRepository _paymentMethodRepo = new();
    private readonly FakeActivityLogEntryRepository _activityLogRepo = new();
    private readonly BusinessDateProvider _dateProvider = new();

    private readonly Guid _branchId = Guid.NewGuid();
    private readonly Guid _warehouseId = Guid.NewGuid();
    private readonly Guid _terminalId = Guid.NewGuid();
    private readonly Guid _cashierId = Guid.NewGuid();
    private readonly DateOnly _businessDate = DateOnly.FromDateTime(DateTime.Today);

    [Fact]
    public async Task GetBusinessDaySummary_WithOpenShift_SetsCanCloseFalse_AndBlockingReason()
    {
        // Arrange
        var shift = Shift.Open(
            1001,
            new BranchId(_branchId),
            new WarehouseId(_warehouseId),
            new TerminalId(_terminalId),
            new UserId(_cashierId),
            "Alice Cashier",
            100m);
        await _shiftRepo.AddAsync(shift);

        var queryHandler = new GetBusinessDaySummaryQueryHandler(
            _dayCloseRepo,
            _shiftRepo,
            _paymentRepo,
            _paymentMethodRepo,
            _dateProvider);

        // Act
        var summary = await queryHandler.Handle(new GetBusinessDaySummaryQuery(_branchId, _businessDate), CancellationToken.None);

        // Assert
        Assert.False(summary.CanClose);
        Assert.Single(summary.OpenShifts);
        Assert.Empty(summary.ClosedShifts);
        Assert.Contains("open", summary.BlockingReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetBusinessDaySummary_WithAllClosedShifts_SetsCanCloseTrue()
    {
        // Arrange
        var shift = Shift.Open(
            1001,
            new BranchId(_branchId),
            new WarehouseId(_warehouseId),
            new TerminalId(_terminalId),
            new UserId(_cashierId),
            "Alice Cashier",
            100m);
        shift.Close(120m, 120m, null, null);
        await _shiftRepo.AddAsync(shift);

        var queryHandler = new GetBusinessDaySummaryQueryHandler(
            _dayCloseRepo,
            _shiftRepo,
            _paymentRepo,
            _paymentMethodRepo,
            _dateProvider);

        // Act
        var summary = await queryHandler.Handle(new GetBusinessDaySummaryQuery(_branchId, _businessDate), CancellationToken.None);

        // Assert
        Assert.True(summary.CanClose);
        Assert.Empty(summary.OpenShifts);
        Assert.Single(summary.ClosedShifts);
        Assert.Equal(1, summary.TotalShifts);
    }

    [Fact]
    public async Task CloseBusinessDay_ThrowsWhenOpenShiftsExist()
    {
        // Arrange
        var shift = Shift.Open(
            1001,
            new BranchId(_branchId),
            new WarehouseId(_warehouseId),
            new TerminalId(_terminalId),
            new UserId(_cashierId),
            "Alice Cashier",
            100m);
        await _shiftRepo.AddAsync(shift);

        var queryHandler = new GetBusinessDaySummaryQueryHandler(
            _dayCloseRepo,
            _shiftRepo,
            _paymentRepo,
            _paymentMethodRepo,
            _dateProvider);

        var fakeMediator = new FakeMediator(async req =>
        {
            if (req is GetBusinessDaySummaryQuery q)
                return await queryHandler.Handle(q, CancellationToken.None);
            return null;
        });

        var commandHandler = new CloseBusinessDayCommandHandler(_dayCloseRepo, fakeMediator, _activityLogRepo);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            commandHandler.Handle(new CloseBusinessDayCommand(_branchId, _businessDate, _cashierId, "Manager"), CancellationToken.None));

        Assert.Contains("still open", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CloseBusinessDay_Succeeds_WhenAllShiftsClosed()
    {
        // Arrange
        var shift = Shift.Open(
            1001,
            new BranchId(_branchId),
            new WarehouseId(_warehouseId),
            new TerminalId(_terminalId),
            new UserId(_cashierId),
            "Alice Cashier",
            100m);
        shift.Close(120m, 120m, null, "All balanced");
        await _shiftRepo.AddAsync(shift);

        var queryHandler = new GetBusinessDaySummaryQueryHandler(
            _dayCloseRepo,
            _shiftRepo,
            _paymentRepo,
            _paymentMethodRepo,
            _dateProvider);

        var fakeMediator = new FakeMediator(async req =>
        {
            if (req is GetBusinessDaySummaryQuery q)
                return await queryHandler.Handle(q, CancellationToken.None);
            return null;
        });

        var commandHandler = new CloseBusinessDayCommandHandler(_dayCloseRepo, fakeMediator, _activityLogRepo);

        // Act
        var result = await commandHandler.Handle(new CloseBusinessDayCommand(_branchId, _businessDate, _cashierId, "Manager", "Shift finished"), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_businessDate, result.BusinessDate);
        Assert.Equal("Closed", result.Status);
        Assert.Equal(1, result.ShiftCount);

        // Verify persisted in repo
        var persisted = await _dayCloseRepo.GetByBranchAndDateAsync(new BranchId(_branchId), _businessDate);
        Assert.NotNull(persisted);
        Assert.Equal(BusinessDayCloseStatus.Closed, persisted.Status);
    }

    [Fact]
    public async Task CloseBusinessDay_ThrowsWhenAlreadyClosed()
    {
        // Arrange
        var existingClose = BusinessDayClose.Close(
            new BranchId(_branchId),
            _businessDate,
            new UserId(_cashierId),
            "Manager",
            500m,
            300m,
            200m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            1,
            0m,
            5);
        await _dayCloseRepo.AddAsync(existingClose);

        var fakeMediator = new FakeMediator(req => Task.FromResult<object?>(null));
        var commandHandler = new CloseBusinessDayCommandHandler(_dayCloseRepo, fakeMediator, _activityLogRepo);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            commandHandler.Handle(new CloseBusinessDayCommand(_branchId, _businessDate, _cashierId, "Manager"), CancellationToken.None));

        Assert.Contains("already been closed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
