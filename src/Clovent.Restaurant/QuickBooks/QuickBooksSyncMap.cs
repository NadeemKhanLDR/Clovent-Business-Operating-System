namespace Clovent.Restaurant.QuickBooks;

/// <summary>
/// Durable bidirectional mapping ledger linking CBOS transactions (Orders, Payments, Shifts)
/// to remote QuickBooks Desktop / Online transaction entities (TxnID, DocNumber, EditSequence).
/// Enforces idempotent synchronization preventing duplicate invoices or double-counted payments.
/// </summary>
public sealed class QuickBooksSyncMap
{
    /// <summary>Unique mapping record identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Local CBOS entity primary identifier (OrderId, PaymentId, or ShiftId).</summary>
    public Guid LocalEntityId { get; set; }

    /// <summary>Entity discriminator (<see cref="QuickBooksSyncEntityType"/>: Invoice, Payment, ShiftSummary).</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Remote QuickBooks transaction identifier (TxnID returned by QBSDK or QBO API).</summary>
    public string? QuickBooksTxnId { get; set; }

    /// <summary>Remote document number (e.g. Invoice #, Receipt #) assigned in QuickBooks.</summary>
    public string? QuickBooksDocNumber { get; set; }

    /// <summary>Remote QuickBooks EditSequence token used for optimistic update checks.</summary>
    public string? QuickBooksEditSequence { get; set; }

    /// <summary>Financial amount synchronized to QuickBooks.</summary>
    public decimal Amount { get; set; }

    /// <summary>ISO-4217 currency code (e.g. USD).</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>Current synchronization lifecycle status.</summary>
    public QuickBooksSyncStatus Status { get; set; } = QuickBooksSyncStatus.Pending;

    /// <summary>Most recent failure message or HTTP/COM exception details.</summary>
    public string? LastError { get; set; }

    /// <summary>Number of times transmission has been attempted.</summary>
    public int RetryCount { get; set; }

    /// <summary>UTC instant the local transaction mapping was queued.</summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>UTC instant QuickBooks confirmed successful transaction recording.</summary>
    public DateTimeOffset? SyncedAtUtc { get; set; }

    /// <summary>Serialized JSON request payload sent to the QuickBooks gateway.</summary>
    public string? RequestPayloadJson { get; set; }

    /// <summary>Serialized JSON or XML response payload received from QuickBooks.</summary>
    public string? ResponsePayloadJson { get; set; }

    /// <summary>Branch where the transaction originated.</summary>
    public Guid? BranchId { get; set; }

    /// <summary>Terminal register where the transaction was finalized.</summary>
    public Guid? TerminalId { get; set; }

    /// <summary>Manager audit notes when resolving or overriding synchronization manually.</summary>
    public string? ManagerOverrideNotes { get; set; }

    /// <summary>User ID of the manager who performed manual resolution.</summary>
    public Guid? ManagerOverrideUserId { get; set; }

    /// <summary>UTC timestamp when manual manager override was recorded.</summary>
    public DateTimeOffset? ManagerOverrideAtUtc { get; set; }

    /// <summary>Creates a new pending synchronization mapping ledger entry.</summary>
    public static QuickBooksSyncMap Create(
        Guid localEntityId,
        string entityType,
        decimal amount,
        string currency = "USD",
        Guid? branchId = null,
        Guid? terminalId = null,
        string? requestPayloadJson = null)
    {
        return new QuickBooksSyncMap
        {
            Id = Guid.NewGuid(),
            LocalEntityId = localEntityId,
            EntityType = entityType,
            Amount = amount,
            Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency,
            Status = QuickBooksSyncStatus.Pending,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            BranchId = branchId,
            TerminalId = terminalId,
            RequestPayloadJson = requestPayloadJson
        };
    }

    /// <summary>Marks this entity as durably confirmed and synchronized in QuickBooks.</summary>
    public void MarkSynchronized(
        string qbTxnId,
        string? docNumber = null,
        string? editSequence = null,
        string? responsePayloadJson = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(qbTxnId);

        QuickBooksTxnId = qbTxnId;
        QuickBooksDocNumber = docNumber;
        QuickBooksEditSequence = editSequence;
        ResponsePayloadJson = responsePayloadJson;
        Status = QuickBooksSyncStatus.Synchronized;
        SyncedAtUtc = DateTimeOffset.UtcNow;
        LastError = null;
    }

    /// <summary>Records an error and increments the retry count.</summary>
    public void MarkFailed(string errorMessage)
    {
        LastError = errorMessage;
        RetryCount++;
        Status = QuickBooksSyncStatus.Failed;
    }

    /// <summary>Applies store manager override or manual reconciliation.</summary>
    public void MarkManagerOverride(Guid managerUserId, string notes)
    {
        ManagerOverrideUserId = managerUserId;
        ManagerOverrideNotes = notes;
        ManagerOverrideAtUtc = DateTimeOffset.UtcNow;
        Status = QuickBooksSyncStatus.ManualReview;
    }

    /// <summary>Resets status to Pending for an immediate manual retry.</summary>
    public void ResetForRetry()
    {
        Status = QuickBooksSyncStatus.Pending;
        LastError = null;
    }
}
