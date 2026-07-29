namespace Clovent.Catalog.Prices;

/// <summary>Which side of a transaction a <see cref="ProductPrice"/> represents.</summary>
public enum PriceType
{
    /// <summary>What the business pays to acquire the variant.</summary>
    Cost,

    /// <summary>What the business charges a customer for the variant.</summary>
    Selling
}
