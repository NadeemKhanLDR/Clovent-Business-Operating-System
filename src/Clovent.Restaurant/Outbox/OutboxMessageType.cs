namespace Clovent.Restaurant.Outbox;

/// <summary>Standard outbox message type names across the solution.</summary>
public static class OutboxMessageType
{
    /// <summary>Warehouse stock deduction movement for completed orders.</summary>
    public const string InventoryPosting = "InventoryPosting";

    /// <summary>QuickBooks Online / Desktop invoice and sales synchronization.</summary>
    public const string QuickBooksSync = "QuickBooksSync";

    /// <summary>QuickBooks Online / Desktop individual sales invoice synchronization.</summary>
    public const string QuickBooksInvoiceSync = "QuickBooksInvoiceSync";

    /// <summary>QuickBooks Online / Desktop customer payment synchronization.</summary>
    public const string QuickBooksPaymentSync = "QuickBooksPaymentSync";

    /// <summary>QuickBooks Online / Desktop shift / daily close summary synchronization.</summary>
    public const string QuickBooksShiftSync = "QuickBooksShiftSync";

    /// <summary>General accounting ledger / journal synchronization.</summary>
    public const string AccountingSync = "AccountingSync";

    /// <summary>Asynchronous receipt rendering and printing dispatch.</summary>
    public const string ReceiptPrint = "ReceiptPrint";

    /// <summary>Receipt customer email dispatch.</summary>
    public const string ReceiptEmail = "ReceiptEmail";

    /// <summary>Receipt customer SMS dispatch.</summary>
    public const string ReceiptSms = "ReceiptSms";

    /// <summary>POS operational event and telemetry recording.</summary>
    public const string AnalyticsEvent = "AnalyticsEvent";

    /// <summary>Recommendation engine frequency and model updating.</summary>
    public const string RecommendationLearning = "RecommendationLearning";

    /// <summary>Cloud multi-store sales aggregation synchronization.</summary>
    public const string CloudSync = "CloudSync";

    /// <summary>General notification dispatch.</summary>
    public const string Notification = "Notification";

    /// <summary>Multi-terminal delta synchronization packet envelope.</summary>
    public const string DeltaSyncPacket = "DeltaSyncPacket";

    /// <summary>Multi-terminal local inventory stock delta synchronization.</summary>
    public const string InventoryDeltaSync = "InventoryDeltaSync";

    /// <summary>Multi-terminal catalog price adjustment synchronization.</summary>
    public const string CatalogPriceDeltaSync = "CatalogPriceDeltaSync";

    /// <summary>Multi-terminal cashier shift opening/closing summary synchronization.</summary>
    public const string ShiftSummaryDeltaSync = "ShiftSummaryDeltaSync";

    /// <summary>Cashier audit anomaly alert notification for store managers.</summary>
    public const string CashierAuditAlert = "CashierAuditAlert";
}
