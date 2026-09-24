using System;
using Clovent.Domain;
using Clovent.Identity.Branches;
using Clovent.Identity.Users;
using Clovent.Restaurant.DayClose.Events;

namespace Clovent.Restaurant.DayClose;

/// <summary>
/// A persistent, immutable auditable snapshot created when a manager closes a business operating day
/// for a restaurant branch/location. Aggregates all closed cashier shifts, payments, movements, and variances.
/// </summary>
public sealed class BusinessDayClose : AggregateRoot<BusinessDayCloseId>
{
    /// <summary>The branch or location this business day closing belongs to.</summary>
    public BranchId BranchId { get; }

    /// <summary>The restaurant operating business date (in the configured business timezone).</summary>
    public DateOnly BusinessDate { get; }

    /// <summary>UTC timestamp when this business day was closed.</summary>
    public DateTimeOffset ClosedAtUtc { get; }

    /// <summary>The manager/user who closed this business day.</summary>
    public UserId ClosedByUserId { get; }

    /// <summary>Display name of the manager/user who closed this business day.</summary>
    public string ClosedByUserName { get; }

    /// <summary>Current lifecycle status of the business day closure (always Closed).</summary>
    public BusinessDayCloseStatus Status { get; private set; }

    /// <summary>Total sales across all payment tenders for this business date.</summary>
    public decimal TotalSales { get; }

    /// <summary>Total cash sales collected in cashier drawers during this business date.</summary>
    public decimal CashSales { get; }

    /// <summary>Total card sales tendered during this business date.</summary>
    public decimal CardSales { get; }

    /// <summary>Total sales tendered via mobile wallet or other non-cash/non-card methods.</summary>
    public decimal OtherPayments { get; }

    /// <summary>Total refunds processed during this business date.</summary>
    public decimal Refunds { get; }

    /// <summary>Total discounts applied across completed orders during this business date.</summary>
    public decimal Discounts { get; }

    /// <summary>Total tax collected across completed orders during this business date.</summary>
    public decimal Tax { get; }

    /// <summary>Total cash added to drawers via Cash In movements during this business date.</summary>
    public decimal CashIn { get; }

    /// <summary>Total cash removed from drawers via Cash Out movements during this business date.</summary>
    public decimal CashOut { get; }

    /// <summary>Total number of cashier shifts contained within this business date.</summary>
    public int ShiftCount { get; }

    /// <summary>Cumulative variance across all cashier shifts (CountedCash - ExpectedCash).</summary>
    public decimal TotalShiftVariance { get; }

    /// <summary>Total number of completed orders during this business date.</summary>
    public int OrderCount { get; }

    /// <summary>Optional manager notes recorded at the time of business day close.</summary>
    public string? Notes { get; }

    /// <summary>UTC timestamp when this record was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>EF Core constructor for materialization.</summary>
    private BusinessDayClose(
        BusinessDayCloseId id,
        BranchId branchId,
        DateOnly businessDate,
        DateTimeOffset closedAtUtc,
        UserId closedByUserId,
        string closedByUserName,
        BusinessDayCloseStatus status,
        decimal totalSales,
        decimal cashSales,
        decimal cardSales,
        decimal otherPayments,
        decimal refunds,
        decimal discounts,
        decimal tax,
        decimal cashIn,
        decimal cashOut,
        int shiftCount,
        decimal totalShiftVariance,
        int orderCount,
        string? notes,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        BranchId = branchId;
        BusinessDate = businessDate;
        ClosedAtUtc = closedAtUtc;
        ClosedByUserId = closedByUserId;
        ClosedByUserName = closedByUserName;
        Status = status;
        TotalSales = totalSales;
        CashSales = cashSales;
        CardSales = cardSales;
        OtherPayments = otherPayments;
        Refunds = refunds;
        Discounts = discounts;
        Tax = tax;
        CashIn = cashIn;
        CashOut = cashOut;
        ShiftCount = shiftCount;
        TotalShiftVariance = totalShiftVariance;
        OrderCount = orderCount;
        Notes = notes;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Creates a new closed business day snapshot record.</summary>
    public static BusinessDayClose Close(
        BranchId branchId,
        DateOnly businessDate,
        UserId closedByUserId,
        string closedByUserName,
        decimal totalSales,
        decimal cashSales,
        decimal cardSales,
        decimal otherPayments,
        decimal refunds,
        decimal discounts,
        decimal tax,
        decimal cashIn,
        decimal cashOut,
        int shiftCount,
        decimal totalShiftVariance,
        int orderCount,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(closedByUserName))
            throw new ArgumentException("ClosedBy user name is required.", nameof(closedByUserName));

        if (shiftCount < 0)
            throw new ArgumentOutOfRangeException(nameof(shiftCount), shiftCount, "Shift count cannot be negative.");

        if (orderCount < 0)
            throw new ArgumentOutOfRangeException(nameof(orderCount), orderCount, "Order count cannot be negative.");

        var now = DateTimeOffset.UtcNow;
        var entity = new BusinessDayClose(
            BusinessDayCloseId.New(),
            branchId,
            businessDate,
            now,
            closedByUserId,
            closedByUserName.Trim(),
            BusinessDayCloseStatus.Closed,
            totalSales,
            cashSales,
            cardSales,
            otherPayments,
            refunds,
            discounts,
            tax,
            cashIn,
            cashOut,
            shiftCount,
            totalShiftVariance,
            orderCount,
            notes?.Trim(),
            now);

        entity.AddDomainEvent(new BusinessDayClosed(
            entity.Id,
            entity.BranchId,
            entity.BusinessDate,
            entity.TotalSales,
            entity.CashSales,
            entity.CardSales,
            entity.ShiftCount,
            entity.TotalShiftVariance,
            entity.ClosedByUserId,
            now));

        return entity;
    }
}
