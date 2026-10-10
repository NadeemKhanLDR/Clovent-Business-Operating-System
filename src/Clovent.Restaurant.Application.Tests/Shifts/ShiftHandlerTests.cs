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
using Clovent.Restaurant.Customers;
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

    [Fact]
    public async Task GetShiftSummaryQueryHandler_ClosedShift_ReturnsExactPersistedExpectedCashAndVariance()
    {
        var (branchId, warehouseId, terminalId, cashierId) = CreateGuids();
        var openHandler = new OpenShiftCommandHandler(_shiftRepository, _activityLogRepository);
        var shiftDto = await openHandler.Handle(new OpenShiftCommand(branchId, warehouseId, terminalId, cashierId, "Hamza Cashier", 4000m), CancellationToken.None);

        var shiftId = new ShiftId(shiftDto.ShiftId);
        var shift = await _shiftRepository.GetByIdAsync(shiftId, CancellationToken.None);
        Assert.NotNull(shift);

        shift.AddCashMovement(CashMovementType.CashIn, 1000m, "Change Float Topup", new UserId(cashierId));
        shift.AddCashMovement(CashMovementType.CashOut, 500m, "Cleaning Supplies", new UserId(cashierId));
        await _shiftRepository.UpdateAsync(shift, CancellationToken.None);

        var cashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        await _paymentMethodRepository.AddAsync(cashMethod, CancellationToken.None);

        var orderId = OrderId.New();
        var payment = Payment.Create(orderId, cashMethod.Id, 13875m, shiftId);
        await _paymentRepository.AddAsync(payment, CancellationToken.None);

        var ledgerRepo = new FakeCustomerLedgerEntryRepository();
        var customerId = CustomerId.New();
        var cashCollectionEntry = CustomerLedgerEntry.Create(
            customerId,
            "PAY-001",
            "Customer Payment (Cash)",
            0m,
            2800m,
            -2800m,
            shiftId,
            "Cash");
        await ledgerRepo.AddAsync(cashCollectionEntry, CancellationToken.None);

        var closeHandler = new CloseShiftCommandHandler(_shiftRepository, _paymentRepository, _paymentMethodRepository, _activityLogRepository, ledgerRepo);
        var closeResult = await closeHandler.Handle(new CloseShiftCommand(shiftDto.ShiftId, 21175m, null), CancellationToken.None);

        Assert.Equal(21175m, closeResult.ExpectedCash);
        Assert.Equal(21175m, closeResult.CountedCash);
        Assert.Equal(0m, closeResult.Variance);
        Assert.Equal(2800m, closeResult.CashCollections);

        // Verify GetShiftSummaryQueryHandler returns the exact same financials
        var queryHandler = new GetShiftSummaryQueryHandler(_shiftRepository, _paymentRepository, _paymentMethodRepository, ledgerRepo);
        var queryResult = await queryHandler.Handle(new GetShiftSummaryQuery(shiftDto.ShiftId), CancellationToken.None);

        Assert.Equal(21175m, queryResult.ExpectedCash);
        Assert.Equal(21175m, queryResult.CountedCash);
        Assert.Equal(0m, queryResult.Variance);
        Assert.Equal(2800m, queryResult.CashCollections);
        Assert.Equal(13875m, queryResult.CashSales);
        Assert.Equal(4000m, queryResult.StartingCash);
    }

    [Fact]
    public async Task CloseShiftAndGetShiftSummary_WithRefundsAndCrossShiftRefund_ReconcilesAndPreservesClosedShift()
    {
        var (branchId, warehouseId, terminalId, cashierId) = CreateGuids();
        var openHandler = new OpenShiftCommandHandler(_shiftRepository, _activityLogRepository);
        var refundRepo = new InMemoryRefundRepository();

        // Shift 1: Float 5000, CashIn 200, CashOut 100, Cash Sale 1500, On-Account Sale 2380.80,
        // On-Account Refund 2088.00 (excluded from drawer), Cash Refund 626.40 (deducted once)
        // Expected Cash = 5000 + 200 + 1500 - 626.40 - 100 = 5973.60
        var shift1Dto = await openHandler.Handle(new OpenShiftCommand(branchId, warehouseId, terminalId, cashierId, "Usman", 5000m), CancellationToken.None);
        var shift1Id = new ShiftId(shift1Dto.ShiftId);
        var shift1 = await _shiftRepository.GetByIdAsync(shift1Id, CancellationToken.None);
        Assert.NotNull(shift1);
        shift1.AddCashMovement(CashMovementType.CashIn, 200m, "Float top-up", new UserId(cashierId));
        shift1.AddCashMovement(CashMovementType.CashOut, 100m, "Petty cash", new UserId(cashierId));
        await _shiftRepository.UpdateAsync(shift1, CancellationToken.None);

        var cashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        var onAccountMethod = PaymentMethod.Create(PaymentMethodName.Create("On Account"));
        await _paymentMethodRepository.AddAsync(cashMethod, CancellationToken.None);
        await _paymentMethodRepository.AddAsync(onAccountMethod, CancellationToken.None);

        var order1Id = OrderId.New();
        await _paymentRepository.AddAsync(Payment.Create(order1Id, cashMethod.Id, 1500m, shift1Id), CancellationToken.None);
        await _paymentRepository.AddAsync(Payment.Create(order1Id, onAccountMethod.Id, 2380.80m, shift1Id), CancellationToken.None);

        var creditRefund = Clovent.Restaurant.Refunds.Refund.Create(
            new Clovent.Restaurant.Refunds.RefundNumber("REF-20261010-0001"),
            order1Id,
            new BranchId(branchId),
            new WarehouseId(warehouseId),
            cashierId,
            "Usman",
            "On-account credit return",
            "IDEMP-CREDIT-1",
            Clovent.Restaurant.Refunds.RefundSettlementMethod.CustomerAccountCredit);
        creditRefund.FinalizeFinancials(2000m, 200m, 288m, 2088.00m, "{}");
        await refundRepo.AddAsync(creditRefund, CancellationToken.None);

        var cashRefund1 = Clovent.Restaurant.Refunds.Refund.Create(
            new Clovent.Restaurant.Refunds.RefundNumber("REF-20261010-0002"),
            order1Id,
            new BranchId(branchId),
            new WarehouseId(warehouseId),
            cashierId,
            "Usman",
            "Cash payout return in shift 1",
            "IDEMP-CASH-1",
            Clovent.Restaurant.Refunds.RefundSettlementMethod.CashPayout);
        cashRefund1.FinalizeFinancials(600m, 60m, 86.40m, 626.40m, "{}");
        await refundRepo.AddAsync(cashRefund1, CancellationToken.None);

        var queryHandler = new GetShiftSummaryQueryHandler(_shiftRepository, _paymentRepository, _paymentMethodRepository, null, refundRepo);
        var openShift1Summary = await queryHandler.Handle(new GetShiftSummaryQuery(shift1Dto.ShiftId), CancellationToken.None);
        Assert.Equal(5973.60m, openShift1Summary.ExpectedCash);

        var closeHandler = new CloseShiftCommandHandler(_shiftRepository, _paymentRepository, _paymentMethodRepository, _activityLogRepository, null, null, refundRepo);
        var closedShift1Summary = await closeHandler.Handle(new CloseShiftCommand(shift1Dto.ShiftId, 5973.60m), CancellationToken.None);
        Assert.Equal(5973.60m, closedShift1Summary.ExpectedCash);
        Assert.Equal(5973.60m, closedShift1Summary.CountedCash);
        Assert.Equal(0m, closedShift1Summary.Variance);

        // Shift 2: Opened after Shift 1 closes; processes a CashPayout refund (180.00) for order1Id from Shift 1
        // Float 3000 + CashIn 150 - CashOut 50 - CashRefund 180.00 = 2920.00
        var shift2Dto = await openHandler.Handle(new OpenShiftCommand(branchId, warehouseId, terminalId, cashierId, "Usman", 3000m), CancellationToken.None);
        var shift2Id = new ShiftId(shift2Dto.ShiftId);
        var shift2 = await _shiftRepository.GetByIdAsync(shift2Id, CancellationToken.None);
        Assert.NotNull(shift2);
        shift2.AddCashMovement(CashMovementType.CashIn, 150m, "Shift 2 CashIn", new UserId(cashierId));
        shift2.AddCashMovement(CashMovementType.CashOut, 50m, "Shift 2 CashOut", new UserId(cashierId));
        await _shiftRepository.UpdateAsync(shift2, CancellationToken.None);

        var cashRefund2 = Clovent.Restaurant.Refunds.Refund.Create(
            new Clovent.Restaurant.Refunds.RefundNumber("REF-20261010-0003"),
            order1Id,
            new BranchId(branchId),
            new WarehouseId(warehouseId),
            cashierId,
            "Usman",
            "Cross-shift cash return for Shift 1 sale",
            "IDEMP-CASH-2",
            Clovent.Restaurant.Refunds.RefundSettlementMethod.CashPayout);
        cashRefund2.FinalizeFinancials(200m, 20m, 0m, 180.00m, "{}");
        await refundRepo.AddAsync(cashRefund2, CancellationToken.None);

        // Historical Shift 1 must remain unchanged at 5973.60
        var shift1AfterShift2Refund = await queryHandler.Handle(new GetShiftSummaryQuery(shift1Dto.ShiftId), CancellationToken.None);
        Assert.Equal("Closed", shift1AfterShift2Refund.Shift.Status);
        Assert.Equal(5973.60m, shift1AfterShift2Refund.ExpectedCash);
        Assert.Equal(5973.60m, shift1AfterShift2Refund.CountedCash);
        Assert.Equal(0m, shift1AfterShift2Refund.Variance);

        // Current Shift 2 must deduct only 180.00 once -> ExpectedCash = 2920.00
        var openShift2Summary = await queryHandler.Handle(new GetShiftSummaryQuery(shift2Dto.ShiftId), CancellationToken.None);
        Assert.Equal("Open", openShift2Summary.Shift.Status);
        Assert.Equal(2920.00m, openShift2Summary.ExpectedCash);

        var closedShift2Summary = await closeHandler.Handle(new CloseShiftCommand(shift2Dto.ShiftId, 2920.00m), CancellationToken.None);
        Assert.Equal(2920.00m, closedShift2Summary.ExpectedCash);
        Assert.Equal(2920.00m, closedShift2Summary.CountedCash);
        Assert.Equal(0m, closedShift2Summary.Variance);
    }

    private sealed class InMemoryRefundRepository : Clovent.Restaurant.Refunds.IRefundRepository
    {
        private readonly System.Collections.Generic.List<Clovent.Restaurant.Refunds.Refund> _refunds = [];

        public Task<Clovent.Restaurant.Refunds.Refund?> GetByIdAsync(Clovent.Restaurant.Refunds.RefundId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_refunds.Find(r => r.Id == id));

        public Task<Clovent.Restaurant.Refunds.Refund?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(_refunds.Find(r => r.IdempotencyKey == idempotencyKey));

        public Task<System.Collections.Generic.IReadOnlyList<Clovent.Restaurant.Refunds.Refund>> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default) =>
            Task.FromResult<System.Collections.Generic.IReadOnlyList<Clovent.Restaurant.Refunds.Refund>>(_refunds.FindAll(r => r.OrderId == orderId));

        public Task<System.Collections.Generic.IReadOnlyList<Clovent.Restaurant.Refunds.Refund>> GetByDateRangeAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult<System.Collections.Generic.IReadOnlyList<Clovent.Restaurant.Refunds.Refund>>(_refunds.FindAll(r => r.RefundedAtUtc >= fromUtc && r.RefundedAtUtc <= toUtc));

        public Task<System.Collections.Generic.IReadOnlyList<Clovent.Restaurant.Refunds.Refund>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<System.Collections.Generic.IReadOnlyList<Clovent.Restaurant.Refunds.Refund>>(_refunds);

        public Task AddAsync(Clovent.Restaurant.Refunds.Refund refund, CancellationToken cancellationToken = default)
        {
            _refunds.Add(refund);
            return Task.CompletedTask;
        }
    }
}
