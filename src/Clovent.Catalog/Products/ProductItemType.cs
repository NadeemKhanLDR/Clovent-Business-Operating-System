namespace Clovent.Catalog.Products;

/// <summary>
/// The fundamental classification of a sellable item in the catalog.
/// </summary>
public enum ProductItemType
{
    /// <summary>Prepared in-house from recipe ingredients (e.g. Chicken Karahi, Biryani).</summary>
    Prepared = 0,

    /// <summary>Purchased from an external vendor and resold (e.g. Naan from an external tandoor, canned soda).</summary>
    PurchasedResale = 1,

    /// <summary>A non-tangible charge or service (e.g. Food Heating, Delivery Service). Does not track stock or deduct recipe inventory.</summary>
    Service = 2
}
