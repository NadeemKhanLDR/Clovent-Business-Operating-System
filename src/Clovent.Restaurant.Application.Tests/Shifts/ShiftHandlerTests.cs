using System;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Identity.Branches;
using Clovent.Identity.Users;
using Clovent.MasterData.Terminals;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.ActivityLogs.Commands;
using Clovent.Restaurant.Application.Shifts.Commands;
using Clovent.Restaurant.Application.Shifts.Queries;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Shifts;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Shifts;

public class ShiftHandlerTests
{
    private readonly FakeShiftRepository _shiftRepository = new();
    private readonly FakePaymentRepository _paymentRepository = new();
    private readonly FakePaymentMethodRepository _paymentMethodRepository = new();
    private readonly FakeActivityLogEntryRepository _activityLogRepository = new();

    private static (Guid BranchId, Guid WarehouseId, Guid TerminalId, Guid CashierId) CreateGuids() =>
        (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public async Task OpenShiftCommandHandler_Valid_OpensShiftAndLogsActivity()
    {
        var (branchId, warehouseId, terminalId, cashierId) = CreateGuids();
        var handler = new OpenShiftCommandHandler(_shiftRepository, _activityLogRepository);

        var result = await handler.Handle(new OpenShiftCommand(
            branchId, warehouseId, terminalId, cashierId, "John Cashier", 150m, "Opening notes"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1001, result.ShiftNumber);
        Assert.Equal("John Cashier", result.CashierName);
        Assert.Equal(150m, result.StartingCash);
        Assert.Equal("Open", result.Status);

        var logs = _activityLogRepository.GetAll();
        var log = Assert.Single(logs);
        Assert.Equal("ShiftOpened", log.Action);
        Assert.Contains("Opened Shift #1001", log.Details);
    }

    [Fact]
    public async Task OpenShiftCommandHandler_TerminalAlreadyHasActiveShift_ThrowsInvalidOperationException()
    {
        var (branchId, warehouseId, terminalId, cashierId) = CreateGuids();
        var handler = new OpenShiftCommandHandler(_shiftRepository, _activityLogRepository);

        await handler.Handle(new OpenShiftCommand(branchId, warehouseId, terminalId, cashierId, "Cashier 1", 100m), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new OpenShiftCommand(branchId, warehouseId, terminalId, Guid.NewGuid(), "Cashier 2", 100m), CancellationToken.None));

        Assert.Contains("Terminal is already in use", ex.Message);
    }

    [Fact]
    public async Task OpenShiftCommandHandler_CashierAlreadyHasActiveShift_ThrowsInvalidOperationException()
    {
        var (branchId, warehouseId, terminalId, cashierId) = CreateGuids();
        var handler = new OpenShiftCommandHandler(_shiftRepository, _activityLogRepository);

        await handler.Handle(new OpenShiftCommand(branchId, warehouseId, terminalId, cashierId, "Cashier 1", 100m), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new OpenShiftCommand(branchId, warehouseId, Guid.NewGuid(), cashierId, "Cashier 1", 100m), CancellationToken.None));

        Assert.Contains("Cashier 'Cashier 1' already has an active Shift", ex.Message);
    }

    [Fact]
    public async Task RecordCashMovementCommandHandler_Valid_AddsMovementAndLogsActivity()
    {
        var (branchId, warehouseId, terminalId, cashierId) = CreateGuids();
        var openHandler = new OpenShiftCommandHandler(_shiftRepository, _activityLogRepository);
        var shiftDto = await openHandler.Handle(new OpenShiftCommand(branchId, warehouseId, terminalId, cashierId, "Cashier 1", 100m), CancellationToken.None);

        var movementHandler = new RecordCashMovementCommandHandler(_shiftRepository, _activityLogRepository);
        var result = await movementHandler.Handle(new RecordCashMovementCommand(
            shiftDto.ShiftId, CashMovementType.CashIn, 50m, "Petty cash top up", cashierId, "Note"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("CashIn", result.Type);
        Assert.Equal(50m, result.Amount);
        Assert.Equal("Petty cash top up", result.Reason);

        var shift = await _shiftRepository.GetByIdAsync(new ShiftId(shiftDto.ShiftId));
        Assert.NotNull(shift);
        Assert.Single(shift.CashMovements);
    }

    [Fact]
    public async Task RecordCashMovementCommandHandler_ClosedShift_ThrowsInvalidOperationException()
    {
        var (branchId, warehouseId, terminalId, cashierId) = CreateGuids();
        var openHandler = new OpenShiftCommandHandler(_shiftRepository, _activityLogRepository);
        var shiftDto = await openHandler.Handle(new OpenShiftCommand(branchId, warehouseId, terminalId, cashierId, "Cashier 1", 100m), CancellationToken.None);

        var closeHandler = new CloseShiftCommandHandler(_shiftRepository, _paymentRepository, _paymentMethodRepository, _activityLogRepository);
        await closeHandler.Handle(new CloseShiftCommand(shiftDto.ShiftId, 100m), CancellationToken.None);

        var movementHandler = new RecordCashMovementCommandHandler(_shiftRepository, _activityLogRepository);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            movementHandler.Handle(new RecordCashMovementCommand(shiftDto.ShiftId, CashMovementType.CashIn, 50m, "Top up", cashierId), CancellationToken.None));
    }

    [Fact]
    public async Task RecordCashMovementCommandHandler_NotFound_ThrowsNotFoundException()
    {
        var movementHandler = new RecordCashMovementCommandHandler(_shiftRepository, _activityLogRepository);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            movementHandler.Handle(new RecordCashMovementCommand(Guid.NewGuid(), CashMovementType.CashIn, 50m, "Top up", Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task CloseShiftCommandHandler_Valid_CalculatesExpectedCashAndClosesShift()
    {
        var (branchId, warehouseId, terminalId, cashierId) = CreateGuids();
        var openHandler = new OpenShiftCommandHandler(_shiftRepository, _activityLogRepository);
        var shiftDto = await openHandler.Handle(new OpenShiftCommand(branchId, warehouseId, terminalId, cashierId, "Cashier 1", 200m), CancellationToken.None);

        // Record a CashIn movement of 50
        var movementHandler = new RecordCashMovementCommandHandler(_shiftRepository, _activityLogRepository);
        await movementHandler.Handle(new RecordCashMovementCommand(shiftDto.ShiftId, CashMovementType.CashIn, 50m, "Top up", cashierId), CancellationToken.None);

        // Record cash sales payment of 100
        var cashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        _paymentMethodRepository.Add(cashMethod);

        var orderId = OrderId.New();
        var payment = Payment.Create(orderId, cashMethod.Id, 100m, new ShiftId(shiftDto.ShiftId));
        await _paymentRepository.AddAsync(payment);

        var closeHandler = new CloseShiftCommandHandler(_shiftRepository, _paymentRepository, _paymentMethodRepository, _activityLogRepository);
        var summary = await closeHandler.Handle(new CloseShiftCommand(shiftDto.ShiftId, 350m, null, "Closing note"), CancellationToken.None);

        Assert.NotNull(summary);
        Assert.Equal(200m, summary.StartingCash);
        Assert.Equal(100m, summary.CashSales);
        Assert.Equal(50m, summary.CashIn);
        Assert.Equal(0m, summary.CashOut);
        Assert.Equal(350m, summary.ExpectedCash); // 200 starting + 50 cash in + 100 cash sales
        Assert.Equal(350m, summary.CountedCash);
        Assert.Equal(0m, summary.Variance);
        Assert.Equal("Closed", summary.Shift.Status);
    }

    [Fact]
    public async Task CloseShiftCommandHandler_WithVariance_RequiresAndSavesVarianceReason()
    {
        var (branchId, warehouseId, terminalId, cashierId) = CreateGuids();
        var openHandler = new OpenShiftCommandHandler(_shiftRepository, _activityLogRepository);
        var shiftDto = await openHandler.Handle(new OpenShiftCommand(branchId, warehouseId, terminalId, cashierId, "Cashier 1", 100m), CancellationToken.None);

        var closeHandler = new CloseShiftCommandHandler(_shiftRepository, _paymentRepository, _paymentMethodRepository, _activityLogRepository);

        // Counted 90 vs Expected 100 requires reason
        await Assert.ThrowsAsync<ArgumentException>(() =>
            closeHandler.Handle(new CloseShiftCommand(shiftDto.ShiftId, 90m, null), CancellationToken.None));

        var summary = await closeHandler.Handle(new CloseShiftCommand(shiftDto.ShiftId, 90m, "Missing 10 dollar bill"), CancellationToken.None);
        Assert.Equal(-10m, summary.Variance);
        Assert.Equal("Missing 10 dollar bill", summary.Shift.VarianceReason);
    }

    [Fact]
    public async Task GetActiveShiftQueryHandler_ReturnsShiftWhenOpen()
    {
        var (branchId, warehouseId, terminalId, cashierId) = CreateGuids();
        var openHandler = new OpenShiftCommandHandler(_shiftRepository, _activityLogRepository);
        await openHandler.Handle(new OpenShiftCommand(branchId, warehouseId, terminalId, cashierId, "Cashier 1", 100m), CancellationToken.None);

        var queryHandler = new GetActiveShiftQueryHandler(_shiftRepository);
        var activeByTerminal = await queryHandler.Handle(new GetActiveShiftQuery(TerminalId: terminalId), CancellationToken.None);
        var activeByCashier = await queryHandler.Handle(new GetActiveShiftQuery(CashierId: cashierId), CancellationToken.None);

        Assert.NotNull(activeByTerminal);
        Assert.NotNull(activeByCashier);
        Assert.Equal(activeByTerminal.ShiftId, activeByCashier.ShiftId);
    }

    [Fact]
    public async Task GetShiftByIdQueryHandler_ReturnsShiftOrThrowsNotFound()
    {
        var (branchId, warehouseId, terminalId, cashierId) = CreateGuids();
        var openHandler = new OpenShiftCommandHandler(_shiftRepository, _activityLogRepository);
        var shiftDto = await openHandler.Handle(new OpenShiftCommand(branchId, warehouseId, terminalId, cashierId, "Cashier 1", 100m), CancellationToken.None);

        var queryHandler = new GetShiftByIdQueryHandler(_shiftRepository);
        var found = await queryHandler.Handle(new GetShiftByIdQuery(shiftDto.ShiftId), CancellationToken.None);
        Assert.Equal(shiftDto.ShiftId, found.ShiftId);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            queryHandler.Handle(new GetShiftByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task ListShiftsQueryHandler_FiltersCorrectly()
    {
        var (branchId, warehouseId, terminalId1, cashierId1) = CreateGuids();
        var terminalId2 = Guid.NewGuid();
        var openHandler = new OpenShiftCommandHandler(_shiftRepository, _activityLogRepository);
        
        await openHandler.Handle(new OpenShiftCommand(branchId, warehouseId, terminalId1, cashierId1, "Cashier 1", 100m), CancellationToken.None);
        
        var listHandler = new ListShiftsQueryHandler(_shiftRepository);
        var t1Shifts = await listHandler.Handle(new ListShiftsQuery(TerminalId: terminalId1), CancellationToken.None);
        var t2Shifts = await listHandler.Handle(new ListShiftsQuery(TerminalId: terminalId2), CancellationToken.None);

        Assert.Single(t1Shifts);
        Assert.Empty(t2Shifts);
    }
}
