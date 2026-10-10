namespace Clovent.Restaurant.QuickBooks;

/// <summary>Status of synchronization between a CBOS entity and QuickBooks.</summary>
public enum QuickBooksSyncStatus
{
    /// <summary>Queued in outbox awaiting initial push to QuickBooks.</summary>
    Pending,

    /// <summary>Successfully posted and confirmed in QuickBooks with remote TxnID.</summary>
    Synchronized,

    /// <summary>Encountered remote API, connection, or validation error; pending retry.</summary>
    Failed,

    /// <summary>Flagged for store manager review or manually resolved with audit note.</summary>
    ManualReview,

    /// <summary>Deliberately ignored or bypassed by manager authorization.</summary>
    Ignored
}
