using System;
using System.Linq;
using Clovent.Identity.Branches;
using Clovent.Identity.Users;
using Clovent.MasterData.Terminals;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Shifts;
using Clovent.Restaurant.Shifts.Events;
using Xunit;

namespace Clovent.Restaurant.Tests.Shifts;

public class ShiftTests
{
    private static Shift CreateDefaultOpenShift(decimal startingCash = 100m) =>
        Shift.Open(
            1001,
            BranchId.New(),
            WarehouseId.New(),
            TerminalId.New(),
            UserId.New(),
            "Jane Cashier",
            startingCash,
            "Morning shift");

    [Fact]
    public void Open_ValidParameters_CreatesOpenShiftAndRaisesShiftOpened()
    {
        var branchId = BranchId.New();
        var warehouseId = WarehouseId.New();
        var terminalId = TerminalId.New();
        var cashierId = UserId.New();

        var shift = Shift.Open(1001, branchId, warehouseId, terminalId, cashierId, "Jane Cashier", 150m, "Opening drawer");

        Assert.NotNull(shift);
        Assert.Equal(1001, shift.ShiftNumber);
        Assert.Equal(branchId, shift.BranchId);
        Assert.Equal(warehouseId, shift.WarehouseId);
        Assert.Equal(terminalId, shift.TerminalId);
        Assert.Equal(cashierId, shift.CashierId);
        Assert.Equal("Jane Cashier", shift.CashierName);
        Assert.Equal(150m, shift.StartingCash);
        Assert.Equal(ShiftStatus.Open, shift.Status);
        Assert.Null(shift.ClosedAtUtc);
        Assert.Equal("Opening drawer", shift.Notes);

        var domainEvent = Assert.Single(shift.DomainEvents);
        var openedEvent = Assert.IsType<ShiftOpened>(domainEvent);
        Assert.Equal(shift.Id, openedEvent.ShiftId);
        Assert.Equal(1001, openedEvent.ShiftNumber);
        Assert.Equal(150m, openedEvent.StartingCash);
    }

    [Fact]
    public void Open_InvalidShiftNumber_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Shift.Open(0, BranchId.New(), WarehouseId.New(), TerminalId.New(), UserId.New(), "Jane", 100m));
    }

    [Fact]
    public void Open_NegativeStartingCash_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Shift.Open(1001, BranchId.New(), WarehouseId.New(), TerminalId.New(), UserId.New(), "Jane", -50m));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Open_NullOrWhitespaceCashierName_ThrowsArgumentException(string? invalidCashierName)
    {
        Assert.Throws<ArgumentException>(() =>
            Shift.Open(1001, BranchId.New(), WarehouseId.New(), TerminalId.New(), UserId.New(), invalidCashierName!, 100m));
    }

    [Fact]
    public void AddCashMovement_WhenShiftIsOpen_AddsMovementAndRaisesCashMovementRecorded()
    {
        var shift = CreateDefaultOpenShift();
        shift.ClearDomainEvents();

        var userId = UserId.New();
        var movement = shift.AddCashMovement(CashMovementType.CashIn, 50m, "Petty cash top-up", userId, "Note");

        Assert.NotNull(movement);
        Assert.Equal(shift.Id, movement.ShiftId);
        Assert.Equal(CashMovementType.CashIn, movement.Type);
        Assert.Equal(50m, movement.Amount);
        Assert.Equal("Petty cash top-up", movement.Reason);
        Assert.Equal(userId, movement.UserId);
        Assert.Equal("Note", movement.Notes);

        Assert.Single(shift.CashMovements);

        var domainEvent = Assert.Single(shift.DomainEvents);
        var recordedEvent = Assert.IsType<CashMovementRecorded>(domainEvent);
        Assert.Equal(shift.Id, recordedEvent.ShiftId);
        Assert.Equal(movement.Id, recordedEvent.CashMovementId);
        Assert.Equal(50m, recordedEvent.Amount);
    }

    [Fact]
    public void AddCashMovement_WhenShiftIsClosed_ThrowsInvalidOperationException()
    {
        var shift = CreateDefaultOpenShift();
        shift.Close(100m, 100m, null);

        Assert.Throws<InvalidOperationException>(() =>
            shift.AddCashMovement(CashMovementType.CashIn, 50m, "Top-up", UserId.New()));
    }

    [Fact]
    public void AddCashMovement_NegativeOrZeroAmount_ThrowsArgumentOutOfRangeException()
    {
        var shift = CreateDefaultOpenShift();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            shift.AddCashMovement(CashMovementType.CashIn, 0m, "Top-up", UserId.New()));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            shift.AddCashMovement(CashMovementType.CashOut, -20m, "Drop", UserId.New()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddCashMovement_NullOrWhitespaceReason_ThrowsArgumentException(string? invalidReason)
    {
        var shift = CreateDefaultOpenShift();

        Assert.Throws<ArgumentException>(() =>
            shift.AddCashMovement(CashMovementType.CashIn, 50m, invalidReason!, UserId.New()));
    }

    [Fact]
    public void Close_ValidCountedCashMatchingExpected_ClosesShiftAndCalculatesZeroVariance()
    {
        var shift = CreateDefaultOpenShift(100m);
        shift.ClearDomainEvents();

        shift.Close(100m, 100m, null, "All balanced");

        Assert.Equal(ShiftStatus.Closed, shift.Status);
        Assert.NotNull(shift.ClosedAtUtc);
        Assert.Equal(100m, shift.CountedCash);
        Assert.Equal(100m, shift.ExpectedCash);
        Assert.Equal(0m, shift.CashVariance);
        Assert.Null(shift.VarianceReason);

        var domainEvent = Assert.Single(shift.DomainEvents);
        var closedEvent = Assert.IsType<ShiftClosed>(domainEvent);
        Assert.Equal(shift.Id, closedEvent.ShiftId);
        Assert.Equal(0m, closedEvent.Variance);
    }

    [Fact]
    public void Close_ValidCountedCashWithDifference_CalculatesVarianceAndRequiresReason()
    {
        var shift = CreateDefaultOpenShift(100m);

        shift.Close(95m, 100m, "Shortage due to wrong change given", "Closing notes");

        Assert.Equal(ShiftStatus.Closed, shift.Status);
        Assert.Equal(95m, shift.CountedCash);
        Assert.Equal(100m, shift.ExpectedCash);
        Assert.Equal(-5m, shift.CashVariance);
        Assert.Equal("Shortage due to wrong change given", shift.VarianceReason);
    }

    [Fact]
    public void Close_MissingVarianceReasonWhenVarianceExists_ThrowsArgumentException()
    {
        var shift = CreateDefaultOpenShift(100m);

        Assert.Throws<ArgumentException>(() =>
            shift.Close(90m, 100m, null));

        Assert.Throws<ArgumentException>(() =>
            shift.Close(110m, 100m, "  "));
    }

    [Fact]
    public void Close_NegativeCountedCash_ThrowsArgumentOutOfRangeException()
    {
        var shift = CreateDefaultOpenShift(100m);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            shift.Close(-10m, 100m, "Reason"));
    }

    [Fact]
    public void Close_WhenShiftIsAlreadyClosed_ThrowsInvalidOperationException()
    {
        var shift = CreateDefaultOpenShift(100m);
        shift.Close(100m, 100m, null);

        Assert.Throws<InvalidOperationException>(() =>
            shift.Close(100m, 100m, null));
    }
}
