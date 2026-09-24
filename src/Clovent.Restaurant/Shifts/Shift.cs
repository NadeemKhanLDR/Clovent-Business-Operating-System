using System;
using System.Collections.Generic;
using System.Linq;
using Clovent.Domain;
using Clovent.Identity.Branches;
using Clovent.Identity.Users;
using Clovent.MasterData.Terminals;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Shifts.Events;

namespace Clovent.Restaurant.Shifts;

/// <summary>
/// A cash register / till session for a cashier on a specific terminal and branch.
/// Tracks opening cash, cash movements (In/Out), expected cash, counted cash, and variance on closure.
/// </summary>
public sealed class Shift : AggregateRoot<ShiftId>
{
    private readonly List<CashMovement> _cashMovements = new();

    /// <summary>Sequential human-readable shift number (e.g. 1001, 1002...).</summary>
    public int ShiftNumber { get; }

    /// <summary>The branch this shift session belongs to.</summary>
    public BranchId BranchId { get; }

    /// <summary>The warehouse location backing this POS shift.</summary>
    public WarehouseId WarehouseId { get; }

    /// <summary>The terminal / POS register machine for this shift session.</summary>
    public TerminalId TerminalId { get; }

    /// <summary>The cashier user operating this shift session.</summary>
    public UserId CashierId { get; }

    /// <summary>Display name of the cashier user.</summary>
    public string CashierName { get; }

    /// <summary>UTC timestamp when the shift was opened.</summary>
    public DateTimeOffset OpenedAtUtc { get; }

    /// <summary>UTC timestamp when the shift was closed, or null if open.</summary>
    public DateTimeOffset? ClosedAtUtc { get; private set; }

    /// <summary>Current lifecycle status of the shift session.</summary>
    public ShiftStatus Status { get; private set; }

    /// <summary>Starting cash float in drawer when opened.</summary>
    public decimal StartingCash { get; }

    /// <summary>Expected cash in drawer calculated at closing time.</summary>
    public decimal ExpectedCash { get; private set; }

    /// <summary>Actual cash counted by the cashier upon closing.</summary>
    public decimal CountedCash { get; private set; }

    /// <summary>Monetary variance: CountedCash - ExpectedCash.</summary>
    public decimal CashVariance { get; private set; }

    /// <summary>Required explanation if CashVariance is non-zero upon closing.</summary>
    public string? VarianceReason { get; private set; }

    /// <summary>Optional free-text notes recorded at opening or closing.</summary>
    public string? Notes { get; private set; }

    /// <summary>UTC timestamp when this record was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>UTC timestamp when this record was last modified.</summary>
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>Cash in/out movements recorded during this shift.</summary>
    public IReadOnlyCollection<CashMovement> CashMovements => _cashMovements.AsReadOnly();

    /// <summary>
    /// Constructor for EF Core persistence. Deliberately has no
    /// <c>cashMovements</c> parameter: EF Core cannot bind navigation
    /// properties through constructor parameters, and a parameter named after
    /// a mapped navigation makes every <c>Shift</c> query fail at model
    /// validation ("No suitable constructor was found"). The
    /// <see cref="CashMovements"/> navigation is populated through the
    /// <c>_cashMovements</c> backing field instead (see
    /// <c>ShiftConfiguration</c>'s <c>UsePropertyAccessMode(Field)</c>).
    /// </summary>
    private Shift(
        ShiftId id,
        int shiftNumber,
        BranchId branchId,
        WarehouseId warehouseId,
        TerminalId terminalId,
        UserId cashierId,
        string cashierName,
        DateTimeOffset openedAtUtc,
        DateTimeOffset? closedAtUtc,
        ShiftStatus status,
        decimal startingCash,
        decimal expectedCash,
        decimal countedCash,
        decimal cashVariance,
        string? varianceReason,
        string? notes,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        ShiftNumber = shiftNumber;
        BranchId = branchId;
        WarehouseId = warehouseId;
        TerminalId = terminalId;
        CashierId = cashierId;
        CashierName = cashierName;
        OpenedAtUtc = openedAtUtc;
        ClosedAtUtc = closedAtUtc;
        Status = status;
        StartingCash = startingCash;
        ExpectedCash = expectedCash;
        CountedCash = countedCash;
        CashVariance = cashVariance;
        VarianceReason = varianceReason;
        Notes = notes;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>Opens a new active shift session.</summary>
    public static Shift Open(
        int shiftNumber,
        BranchId branchId,
        WarehouseId warehouseId,
        TerminalId terminalId,
        UserId cashierId,
        string cashierName,
        decimal startingCash,
        string? notes = null)
    {
        if (shiftNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(shiftNumber), shiftNumber, "Shift number must be positive.");

        if (startingCash < 0)
            throw new ArgumentOutOfRangeException(nameof(startingCash), startingCash, "Starting cash cannot be negative.");

        if (string.IsNullOrWhiteSpace(cashierName))
            throw new ArgumentException("Cashier name is required.", nameof(cashierName));

        var now = DateTimeOffset.UtcNow;
        var shift = new Shift(
            ShiftId.New(),
            shiftNumber,
            branchId,
            warehouseId,
            terminalId,
            cashierId,
            cashierName.Trim(),
            now,
            null,
            ShiftStatus.Open,
            startingCash,
            0m,
            0m,
            0m,
            null,
            notes?.Trim(),
            now,
            now);

        shift.AddDomainEvent(new ShiftOpened(
            shift.Id,
            shift.ShiftNumber,
            shift.BranchId,
            shift.WarehouseId,
            shift.TerminalId,
            shift.CashierId,
            shift.StartingCash,
            now));

        return shift;
    }

    /// <summary>Records a cash in or cash out drawer movement during an active shift.</summary>
    public CashMovement AddCashMovement(
        CashMovementType type,
        decimal amount,
        string reason,
        UserId userId,
        string? notes = null)
    {
        if (Status != ShiftStatus.Open)
            throw new InvalidOperationException($"Cannot record cash movement on a shift with status {Status}.");

        var movement = CashMovement.Create(Id, type, amount, reason, userId, notes);
        _cashMovements.Add(movement);
        UpdatedAtUtc = DateTimeOffset.UtcNow;

        AddDomainEvent(new CashMovementRecorded(
            Id,
            movement.Id,
            type,
            amount,
            reason,
            userId,
            UpdatedAtUtc));

        return movement;
    }

    /// <summary>Closes and reconciles the active shift.</summary>
    public void Close(decimal countedCash, decimal expectedCash, string? varianceReason, string? notes = null)
    {
        if (Status != ShiftStatus.Open)
            throw new InvalidOperationException($"Cannot close a shift with status {Status}.");

        if (countedCash < 0)
            throw new ArgumentOutOfRangeException(nameof(countedCash), countedCash, "Counted cash cannot be negative.");

        var variance = countedCash - expectedCash;

        if (variance != 0 && string.IsNullOrWhiteSpace(varianceReason))
            throw new ArgumentException("Variance reason is required when counted cash differs from expected cash.", nameof(varianceReason));

        var now = DateTimeOffset.UtcNow;
        ClosedAtUtc = now;
        Status = ShiftStatus.Closed;
        ExpectedCash = expectedCash;
        CountedCash = countedCash;
        CashVariance = variance;
        VarianceReason = variance != 0 ? varianceReason?.Trim() : null;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            Notes = string.IsNullOrWhiteSpace(Notes) ? notes.Trim() : $"{Notes}\n{notes.Trim()}";
        }
        UpdatedAtUtc = now;

        AddDomainEvent(new ShiftClosed(
            Id,
            ShiftNumber,
            ExpectedCash,
            CountedCash,
            CashVariance,
            VarianceReason,
            now));
    }
}
