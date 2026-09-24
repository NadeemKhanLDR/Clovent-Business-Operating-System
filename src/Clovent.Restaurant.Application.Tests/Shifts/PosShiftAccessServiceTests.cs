using System;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Identity.Branches;
using Clovent.Identity.Users;
using Clovent.MasterData.Terminals;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.Shifts.Services;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.Shifts;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Shifts;

public class PosShiftAccessServiceTests
{
    private readonly FakeShiftRepository _shiftRepo = new();
    private readonly BusinessDateProvider _dateProvider = new();

    private readonly Guid _branchId = Guid.NewGuid();
    private readonly Guid _warehouseId = Guid.NewGuid();
    private readonly Guid _terminalId = Guid.NewGuid();
    private readonly Guid _cashierId = Guid.NewGuid();
    private readonly DateOnly _businessDate = DateOnly.FromDateTime(DateTime.Today);

    [Fact]
    public async Task EvaluateAccess_WhenCashierEmpty_ReturnsAccessDenied()
    {
        var service = new PosShiftAccessService(_shiftRepo, _dateProvider);

        var result = await service.EvaluateAccessAsync(
            Guid.Empty,
            "Unknown",
            _terminalId,
            cancellationToken: CancellationToken.None);

        Assert.Equal(PosShiftAccessStatus.AccessDenied, result.Status);
    }

    [Fact]
    public async Task EvaluateAccess_WhenTerminalNull_ReturnsNoTerminalConfigured()
    {
        var service = new PosShiftAccessService(_shiftRepo, _dateProvider);

        var result = await service.EvaluateAccessAsync(
            _cashierId,
            "Cashier",
            null,
            cancellationToken: CancellationToken.None);

        Assert.Equal(PosShiftAccessStatus.NoTerminalConfigured, result.Status);
    }

    [Fact]
    public async Task EvaluateAccess_WhenExistingOwnShift_ReturnsExistingOwnShift()
    {
        var shift = Shift.Open(
            1001,
            new BranchId(_branchId),
            new WarehouseId(_warehouseId),
            new TerminalId(_terminalId),
            new UserId(_cashierId),
            "Cashier 1",
            100m);
        await _shiftRepo.AddAsync(shift);

        var service = new PosShiftAccessService(_shiftRepo, _dateProvider);

        var result = await service.EvaluateAccessAsync(
            _cashierId,
            "Cashier 1",
            _terminalId,
            cancellationToken: CancellationToken.None);

        Assert.Equal(PosShiftAccessStatus.ExistingOwnShift, result.Status);
        Assert.NotNull(result.CurrentShift);
        Assert.Equal(1001, result.CurrentShift.ShiftNumber);
    }

    [Fact]
    public async Task EvaluateAccess_WhenTerminalOccupiedByAnotherCashier_ReturnsTerminalOccupied()
    {
        var otherCashierId = Guid.NewGuid();
        var shift = Shift.Open(
            1001,
            new BranchId(_branchId),
            new WarehouseId(_warehouseId),
            new TerminalId(_terminalId),
            new UserId(otherCashierId),
            "Other Cashier",
            100m);
        await _shiftRepo.AddAsync(shift);

        var service = new PosShiftAccessService(_shiftRepo, _dateProvider);

        var result = await service.EvaluateAccessAsync(
            _cashierId,
            "Cashier 1",
            _terminalId,
            cancellationToken: CancellationToken.None);

        Assert.Equal(PosShiftAccessStatus.TerminalOccupiedByAnotherUser, result.Status);
        Assert.Contains("Other Cashier", result.Message);
    }

    [Fact]
    public async Task EvaluateAccess_WhenUserHasShiftOnAnotherTerminal_ReturnsUserHasShiftOnAnotherTerminal()
    {
        var otherTerminalId = Guid.NewGuid();
        var shift = Shift.Open(
            1001,
            new BranchId(_branchId),
            new WarehouseId(_warehouseId),
            new TerminalId(otherTerminalId),
            new UserId(_cashierId),
            "Cashier 1",
            100m);
        await _shiftRepo.AddAsync(shift);

        var service = new PosShiftAccessService(_shiftRepo, _dateProvider);

        var result = await service.EvaluateAccessAsync(
            _cashierId,
            "Cashier 1",
            _terminalId,
            cancellationToken: CancellationToken.None);

        Assert.Equal(PosShiftAccessStatus.UserHasShiftOnAnotherTerminal, result.Status);
    }

    [Fact]
    public async Task EvaluateAccess_WhenNoActiveShift_ReturnsShiftRequired()
    {
        var service = new PosShiftAccessService(_shiftRepo, _dateProvider);

        var result = await service.EvaluateAccessAsync(
            _cashierId,
            "Cashier 1",
            _terminalId,
            cancellationToken: CancellationToken.None);

        Assert.Equal(PosShiftAccessStatus.ShiftRequired, result.Status);
        Assert.Equal(_businessDate, result.BusinessDate);
    }

    [Fact]
    public async Task EvaluateAccess_WhenUserHasShiftOnAnotherTerminal_WithTerminalRepo_ResolvesOccupyingTerminalDetails()
    {
        var otherTerminal = Terminal.Create(
            new BranchId(_branchId),
            Clovent.MasterData.Terminals.ValueObjects.TerminalName.Create("Drive-Thru"),
            Clovent.MasterData.Shared.ValueObjects.EntityCode.Create("T-DRIVE"));

        var stubTermRepo = new StubTerminalRepository();
        stubTermRepo.Add(otherTerminal);

        var shift = Shift.Open(
            1005,
            new BranchId(_branchId),
            new WarehouseId(_warehouseId),
            otherTerminal.Id,
            new UserId(_cashierId),
            "Cashier 1",
            150m);
        await _shiftRepo.AddAsync(shift);

        var service = new PosShiftAccessService(_shiftRepo, _dateProvider, stubTermRepo);

        var result = await service.EvaluateAccessAsync(
            _cashierId,
            "Cashier 1",
            _terminalId,
            terminalName: "Front Counter",
            terminalCode: "T-001",
            cancellationToken: CancellationToken.None);

        Assert.Equal(PosShiftAccessStatus.UserHasShiftOnAnotherTerminal, result.Status);
        Assert.Equal(otherTerminal.Id.Value, result.OccupyingTerminalId);
        Assert.Equal("Drive-Thru", result.OccupyingTerminalName);
        Assert.Equal("T-DRIVE", result.OccupyingTerminalCode);
        Assert.Contains("Drive-Thru (T-DRIVE)", result.Message);
        Assert.Contains("Front Counter (T-001)", result.Message);
    }

    private sealed class StubTerminalRepository : ITerminalRepository
    {
        private readonly System.Collections.Generic.Dictionary<TerminalId, Terminal> _terminals = [];
        public void Add(Terminal terminal) => _terminals[terminal.Id] = terminal;
        public Task<Terminal?> GetByIdAsync(TerminalId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_terminals.GetValueOrDefault(id));
        public Task<System.Collections.Generic.IReadOnlyCollection<Terminal>> GetByBranchIdAsync(BranchId branchId, CancellationToken cancellationToken = default) =>
            Task.FromResult<System.Collections.Generic.IReadOnlyCollection<Terminal>>([.. _terminals.Values.Where(t => t.BranchId == branchId)]);
        public Task AddAsync(Terminal terminal, CancellationToken cancellationToken = default)
        {
            _terminals[terminal.Id] = terminal;
            return Task.CompletedTask;
        }
    }
}
