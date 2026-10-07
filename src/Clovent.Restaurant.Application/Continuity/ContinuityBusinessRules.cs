namespace Clovent.Restaurant.Application.Continuity;

/// <summary>
/// Domain business rules governing what operations may be performed safely during
/// emergency offline Continuity Mode without risking irreversible financial corruption.
/// Enforces the CBOS Continuity Mode Capability Matrix.
/// </summary>
public static class ContinuityBusinessRules
{
    /// <summary>Default maximum percentage discount permitted by a standard cashier offline without manager override.</summary>
    public const decimal DefaultMaxCashierDiscountPercentage = 15m;

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
    /// Checks whether card payments can be accepted offline.
    /// Blocked unless the payment gateway integration explicitly supports approved offline store-and-forward.
    /// </summary>
    public static bool CanAcceptCardPayment(bool isOfflineStoreAndForwardApproved, out string rejectionReason)
    {
        if (isOfflineStoreAndForwardApproved)
        {
            rejectionReason = string.Empty;
            return true;
        }

        rejectionReason = "Card payments are disabled in Continuity Mode. Simulated offline card authorizations are strictly prohibited.";
        return false;
    }

    /// <summary>
    /// Checks whether customer credit / on-account tender can be accepted in Continuity Mode.
    /// </summary>
    public static bool CanAcceptOnAccount(out string rejectionReason)
    {
        rejectionReason = "On-Account / Credit sales are disabled in Continuity Mode because real-time accounts receivable ledger and credit limit validation require the primary database.";
        return false;
    }

    /// <summary>
    /// Checks whether a refund operation can be performed in Continuity Mode.
    /// </summary>
    public static bool CanPerformRefund(out string rejectionReason)
    {
        rejectionReason = "Refunds require primary database durability and audit ledger verification. They are strictly prohibited in Emergency Continuity Mode.";
        return false;
    }

    /// <summary>
    /// Checks whether a void of a previously settled online transaction can be performed.
    /// </summary>
    public static bool CanVoidSettledTransaction(bool isPriorOnlineTransaction, out string rejectionReason)
    {
        if (isPriorOnlineTransaction)
        {
            rejectionReason = "Voiding previously settled online transactions requires real-time database ledger updates and is prohibited in Continuity Mode.";
            return false;
        }

        rejectionReason = string.Empty;
        return true;
    }

    /// <summary>
    /// Checks whether a price override can be performed in Continuity Mode.
    /// Requires cached manager authorization claim.
    /// </summary>
    public static bool CanOverridePrice(bool hasManagerPrivilege, out string rejectionReason)
    {
        if (hasManagerPrivilege)
        {
            rejectionReason = string.Empty;
            return true;
        }

        rejectionReason = "Price override in Continuity Mode requires cached Manager authorization.";
        return false;
    }

    /// <summary>
    /// Checks whether a discount can be applied in Continuity Mode based on cached policy and operator claims.
    /// </summary>
    public static bool CanApplyDiscount(
        decimal discountPercentage,
        bool hasManagerPrivilege,
        decimal maxCashierDiscountPercentage,
        out string rejectionReason)
    {
        if (discountPercentage <= 0)
        {
            rejectionReason = string.Empty;
            return true;
        }

        if (discountPercentage <= maxCashierDiscountPercentage)
        {
            rejectionReason = string.Empty;
            return true;
        }

        if (hasManagerPrivilege)
        {
            rejectionReason = string.Empty;
            return true;
        }

        rejectionReason = $"Discounts above {maxCashierDiscountPercentage:F0}% require Manager authorization in Continuity Mode.";
        return false;
    }

    /// <summary>
    /// Checks whether a new customer record can be created in Continuity Mode.
    /// </summary>
    public static bool CanCreateCustomer(out string rejectionReason)
    {
        rejectionReason = "New customer creation is disabled in Continuity Mode to prevent duplicate identity collisions upon database restoration.";
        return false;
    }

    /// <summary>
    /// Checks whether customer master information can be edited in Continuity Mode.
    /// </summary>
    public static bool CanEditCustomer(out string rejectionReason)
    {
        rejectionReason = "Customer master editing is disabled in Continuity Mode.";
        return false;
    }

    /// <summary>
    /// Checks whether inventory adjustments can be performed in Continuity Mode.
    /// </summary>
    public static bool CanAdjustInventory(out string rejectionReason)
    {
        rejectionReason = "Inventory adjustments cannot be performed in Continuity Mode. Warehouse stock mutations require authoritative database ledger posting.";
        return false;
    }

    /// <summary>
    /// Checks whether configuration or master data changes can be performed in Continuity Mode.
    /// </summary>
    public static bool CanModifyMasterData(out string rejectionReason)
    {
        rejectionReason = "Catalog, product, and configuration updates cannot be performed while operating in Continuity Mode.";
        return false;
    }
}
