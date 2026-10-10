using Clovent.Domain;
using Clovent.Identity.Branches;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.DomainServices;
using Clovent.Restaurant.Orders;

namespace Clovent.Restaurant.Refunds;

/// <summary>
/// First-class compensating Refund aggregate root (CBOS 1.3.0 Refund Domain).
/// Represents an immutable compensating financial return against an earlier completed order.
/// Strictly preserves original completed transaction immutability: never mutates, voids,
/// or deletes the original order, and never inserts negative lines into past orders.
/// </summary>
public sealed class Refund : AggregateRoot<RefundId>
{
    private readonly List<RefundLine> _lines = [];

    /// <summary>Human-facing refund credit note number (e.g. REF-20261008-0001).</summary>
    public RefundNumber RefundNumber { get; }

    /// <summary>Reference to the original completed order.</summary>
    public OrderId OrderId { get; }

    /// <summary>Branch or location where the refund was processed.</summary>
    public BranchId BranchId { get; }

    /// <summary>Warehouse stock origin where returns are processed.</summary>
    public WarehouseId WarehouseId { get; }

    /// <summary>Instant the refund was posted.</summary>
    public DateTimeOffset RefundedAtUtc { get; }

    /// <summary>Cashier identity executing the return.</summary>
    public Guid CashierId { get; }

    /// <summary>Cashier display name.</summary>
    public string CashierName { get; }

    /// <summary>Audited business return reason.</summary>
    public string Reason { get; }

    /// <summary>Approving manager identity, if required.</summary>
    public Guid? ApprovedByUserId { get; }

    /// <summary>Approving manager display name.</summary>
    public string? ApprovedByUserName { get; }

    /// <summary>Unique client request idempotency key preventing duplicate refund submissions.</summary>
    public string IdempotencyKey { get; }

    /// <summary>Collection of refunded lines.</summary>
    public IReadOnlyList<RefundLine> Lines => _lines.AsReadOnly();

    /// <summary>Gross sum of refunded items before discount/tax adjustments.</summary>
    public decimal SubtotalRefunded { get; private set; }

    /// <summary>Total original discounts reversed by this refund.</summary>
    public decimal DiscountReversedTotal { get; private set; }

    /// <summary>Total sales taxes reversed by this refund.</summary>
    public decimal TaxReversedTotal { get; private set; }

    /// <summary>Net financial amount refunded/credited to customer: Subtotal - DiscountsReversed + ExclusiveTaxReversed.</summary>
    public decimal GrandTotalRefunded { get; private set; }

    /// <summary>Tender settlement mechanism used for the refund.</summary>
    public RefundSettlementMethod SettlementMethod { get; }

    /// <summary>External tender reference (e.g. card terminal auth code or check ref).</summary>
    public string? SettlementReference { get; }

    /// <summary>Customer identity if refund is credited to customer receivable account.</summary>
    public Guid? CustomerId { get; }

    /// <summary>Immutable serialized JSON credit note snapshot captured upon completion.</summary>
    public string? ReceiptSnapshotJson { get; private set; }

    /// <summary>UTC creation instant.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Constructor for EF Core persistence.</summary>
    private Refund(
        RefundId id,
        RefundNumber refundNumber,
        OrderId orderId,
        BranchId branchId,
        WarehouseId warehouseId,
        DateTimeOffset refundedAtUtc,
        Guid cashierId,
        string cashierName,
        string reason,
        Guid? approvedByUserId,
        string? approvedByUserName,
        string idempotencyKey,
        decimal subtotalRefunded,
        decimal discountReversedTotal,
        decimal taxReversedTotal,
        decimal grandTotalRefunded,
        RefundSettlementMethod settlementMethod,
        string? settlementReference,
        Guid? customerId,
        string? receiptSnapshotJson,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        RefundNumber = refundNumber;
        OrderId = orderId;
        BranchId = branchId;
        WarehouseId = warehouseId;
        RefundedAtUtc = refundedAtUtc;
        CashierId = cashierId;
        CashierName = cashierName;
        Reason = reason;
        ApprovedByUserId = approvedByUserId;
        ApprovedByUserName = approvedByUserName;
        IdempotencyKey = idempotencyKey;
        SubtotalRefunded = subtotalRefunded;
        DiscountReversedTotal = discountReversedTotal;
        TaxReversedTotal = taxReversedTotal;
        GrandTotalRefunded = grandTotalRefunded;
        SettlementMethod = settlementMethod;
        SettlementReference = settlementReference;
        CustomerId = customerId;
        ReceiptSnapshotJson = receiptSnapshotJson;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Creates and initializes a new compensating Refund document.</summary>
    public static Refund Create(
        RefundNumber refundNumber,
        OrderId orderId,
        BranchId branchId,
        WarehouseId warehouseId,
        Guid cashierId,
        string cashierName,
        string reason,
        string idempotencyKey,
        RefundSettlementMethod settlementMethod,
        Guid? approvedByUserId = null,
        string? approvedByUserName = null,
        string? settlementReference = null,
        Guid? customerId = null,
        DateTimeOffset? refundedAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Refund reason is mandatory.", nameof(reason));
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("Idempotency key is mandatory.", nameof(idempotencyKey));
        if (string.IsNullOrWhiteSpace(cashierName))
            throw new ArgumentException("Cashier name is mandatory.", nameof(cashierName));

        var now = refundedAtUtc ?? Shifts.Shift.NextUtcNow();
        return new Refund(
            RefundId.New(),
            refundNumber,
            orderId,
            branchId,
            warehouseId,
            now,
            cashierId,
            cashierName.Trim(),
            reason.Trim(),
            approvedByUserId,
            approvedByUserName?.Trim(),
            idempotencyKey.Trim(),
            0.00m,
            0.00m,
            0.00m,
            0.00m,
            settlementMethod,
            settlementReference?.Trim(),
            customerId,
            null,
            now);
    }

    /// <summary>Adds a refund line item to this aggregate.</summary>
    public void AddLine(RefundLine line)
    {
        ArgumentNullException.ThrowIfNull(line);
        _lines.Add(line);
    }

    /// <summary>Freezes financial totals and immutable credit note receipt snapshot.</summary>
    public void FinalizeFinancials(
        decimal subtotal,
        decimal discountReversed,
        decimal taxReversed,
        decimal grandTotal,
        string creditNoteSnapshotJson)
    {
        SubtotalRefunded = MoneyRoundingPolicy.RoundMoney(subtotal);
        DiscountReversedTotal = MoneyRoundingPolicy.RoundMoney(discountReversed);
        TaxReversedTotal = MoneyRoundingPolicy.RoundMoney(taxReversed);
        GrandTotalRefunded = MoneyRoundingPolicy.RoundMoney(grandTotal);
        ReceiptSnapshotJson = creditNoteSnapshotJson;
    }
}
