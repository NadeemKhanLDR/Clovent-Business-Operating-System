namespace Clovent.Restaurant.Outbox;

/// <summary>Standard outbox message type names across the solution.</summary>
public static class OutboxMessageType
{
    /// <summary>Warehouse stock deduction movement for completed orders.</summary>
    public const string InventoryPosting = "InventoryPosting";

    /// <summary>QuickBooks Online / Desktop invoice and sales synchronization.</summary>
    public const string QuickBooksSync = "QuickBooksSync";

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
}
