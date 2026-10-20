namespace Clovent.Platform.Printing;

/// <summary>
/// Defines payment conditions that trigger printing of a merchant receipt copy.
/// </summary>
public enum MerchantCopyTrigger
{
    /// <summary>Print merchant copy only when payment includes Card, Mobile, or Digital tender.</summary>
    CardAndDigitalOnly = 0,

    /// <summary>Print merchant copy on all completed sales regardless of payment method.</summary>
    Always = 1
}
