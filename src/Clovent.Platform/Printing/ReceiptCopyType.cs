namespace Clovent.Platform.Printing;

/// <summary>
/// Identifies the intended recipient or purpose of a printed receipt slip.
/// </summary>
public enum ReceiptCopyType
{
    /// <summary>Standard generic customer receipt.</summary>
    Standard = 0,

    /// <summary>Dedicated customer receipt copy.</summary>
    CustomerCopy = 1,

    /// <summary>Merchant / store audit copy, typically with signature line.</summary>
    MerchantCopy = 2,

    /// <summary>Historical reprint slip.</summary>
    Reprint = 3
}
