namespace Clovent.Restaurant.QuickBooks;

/// <summary>Standard QuickBooks target entity discriminators.</summary>
public static class QuickBooksSyncEntityType
{
    /// <summary>Sales invoice created in QuickBooks for completed orders.</summary>
    public const string Invoice = "Invoice";

    /// <summary>Customer payment received in QuickBooks against an order invoice.</summary>
    public const string Payment = "Payment";

    /// <summary>Daily sales summary or cash drawer close journal entry.</summary>
    public const string ShiftSummary = "ShiftSummary";

    /// <summary>Item service/product mapping in QuickBooks.</summary>
    public const string CatalogItem = "CatalogItem";
}
