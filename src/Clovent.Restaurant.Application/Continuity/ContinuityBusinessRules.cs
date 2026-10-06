namespace Clovent.Restaurant.Application.Continuity;

/// <summary>
/// Domain business rules governing what operations may be performed safely during
/// emergency offline continuity mode without risking irreversible financial corruption.
/// </summary>
public static class ContinuityBusinessRules
{
    /// <summary>
    /// Checks whether the specified tender type is permitted in Continuity Mode.
    /// Only Cash tender is permitted offline by default to protect ledger accuracy.
    /// </summary>
    public static bool CanAcceptTender(string? paymentMethod, out string rejectionReason)
    {
        if (string.IsNullOrWhiteSpace(paymentMethod))
        {
            rejectionReason = "Payment method was not specified.";
            return false;
        }

        var normalized = paymentMethod.Trim().ToLowerInvariant();
        if (normalized is "cash" or "cash payment" or "exact cash")
        {
            rejectionReason = string.Empty;
            return true;
        }

        if (normalized.Contains("card") || normalized.Contains("credit") || normalized.Contains("debit") || normalized.Contains("visa") || normalized.Contains("mastercard"))
        {
            rejectionReason = "Card payments require real-time payment gateway verification and are disabled during Offline Continuity Mode.";
            return false;
        }

        if (normalized.Contains("account") || normalized.Contains("credit limit") || normalized.Contains("customer"))
        {
            rejectionReason = "On-Account / Credit ledger transactions require real-time balance verification and are disabled during Offline Continuity Mode.";
            return false;
        }

        rejectionReason = $"Tender method '{paymentMethod}' is not permitted during Offline Continuity Mode. Only Cash is accepted.";
        return false;
    }

    /// <summary>
    /// Checks whether a refund or void operation can be performed in Continuity Mode.
    /// Always returns false because reversing settled funds requires primary ledger durability.
    /// </summary>
    public static bool CanPerformRefund(out string rejectionReason)
    {
        rejectionReason = "Refunds and voids require primary database durability and audit verification. They are strictly prohibited in Emergency Continuity Mode.";
        return false;
    }

    /// <summary>
    /// Checks whether pricing or catalog changes can be performed in Continuity Mode.
    /// </summary>
    public static bool CanModifyMasterData(out string rejectionReason)
    {
        rejectionReason = "Catalog, product, and pricing updates cannot be performed while operating in Continuity Mode.";
        return false;
    }
}
