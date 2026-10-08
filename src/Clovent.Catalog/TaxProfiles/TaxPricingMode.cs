namespace Clovent.Catalog.TaxProfiles;

/// <summary>
/// Pricing mode defining whether catalog prices already include sales tax or exclude it.
/// </summary>
public enum TaxPricingMode
{
    /// <summary>Price is net of tax; tax is calculated and added onto the invoice total.</summary>
    Exclusive = 1,

    /// <summary>Price displayed and charged to the customer already includes tax; tax is extracted from the gross amount.</summary>
    Inclusive = 2
}
