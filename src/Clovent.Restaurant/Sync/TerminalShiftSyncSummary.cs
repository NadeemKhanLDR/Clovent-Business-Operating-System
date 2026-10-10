namespace Clovent.Restaurant.Sync;

/// <summary>
/// Replicated summary of a terminal cashier shift session, durably stored for branch-wide reporting.
/// </summary>
public sealed class TerminalShiftSyncSummary
{
    /// <summary>Unique primary identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Branch location identity.</summary>
    public Guid BranchId { get; set; }

    /// <summary>Terminal register identity.</summary>
    public Guid TerminalId { get; set; }

    /// <summary>Sequential shift number on the terminal.</summary>
    public int ShiftNumber { get; set; }

    /// <summary>Cashier user identifier.</summary>
    public Guid CashierId { get; set; }

    /// <summary>Cashier display name.</summary>
    public string CashierName { get; set; } = string.Empty;

    /// <summary>UTC opening timestamp.</summary>
    public DateTimeOffset OpenedAtUtc { get; set; }

    /// <summary>UTC closing timestamp, if closed.</summary>
    public DateTimeOffset? ClosedAtUtc { get; set; }

    /// <summary>Drawer opening cash float.</summary>
    public decimal StartingCash { get; set; }

    /// <summary>Actual cash counted at closing.</summary>
    public decimal CountedCash { get; set; }

    /// <summary>System expected cash calculated from cash sales, cash in, and cash out.</summary>
    public decimal ExpectedCash { get; set; }

    /// <summary>Discrepancy variance (Counted - Expected).</summary>
    public decimal CashVariance { get; set; }

    /// <summary>Total net sales during this shift.</summary>
    public decimal NetSales { get; set; }

    /// <summary>Total completed orders count.</summary>
    public int TotalOrdersCount { get; set; }

    /// <summary>UTC timestamp when the shift summary snapshot was generated.</summary>
    public DateTimeOffset TimestampUtc { get; set; }

    /// <summary>UTC timestamp when this record was durably replicated into the local database.</summary>
    public DateTimeOffset ReplicatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
