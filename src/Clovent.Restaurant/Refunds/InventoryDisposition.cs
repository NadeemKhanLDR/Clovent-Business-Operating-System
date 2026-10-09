namespace Clovent.Restaurant.Refunds;

/// <summary>Explicit inventory handling rule for refunded merchandise.</summary>
public enum InventoryDisposition
{
    /// <summary>Item returned into warehouse sellable inventory (generates an InventoryTransaction Receipt restock).</summary>
    Restock = 1,

    /// <summary>Item was damaged, spoiled, or non-returnable; not returned to sellable inventory.</summary>
    DamagedDiscard = 2,

    /// <summary>Not applicable (e.g. service item, delivery fee, or intangible).</summary>
    NotApplicable = 3
}
