namespace Clovent.Platform.Printing;

/// <summary>
/// Specifies the intended receipt copy type and audience.
/// </summary>
public enum ReceiptCopyType
{
    /// <summary>Original customer copy.</summary>
    Customer = 0,

    /// <summary>Merchant or store accounting audit copy.</summary>
    Merchant = 1,

    /// <summary>Reprinted historical receipt copy.</summary>
    Reprint = 2,

    /// <summary>Draft pre-settlement bill or proforma check (not a finalized sales receipt).</summary>
    Draft = 3
}
