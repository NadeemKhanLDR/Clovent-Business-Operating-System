namespace Clovent.Catalog.TaxProfiles;

/// <summary>
/// Authoritative tax classification distinguishing taxability regimes under Pakistan sales tax statutes.
/// Prevents representing all zero-tax lines as exempt and supports statutory differentiation.
/// </summary>
public enum TaxClassification
{
    /// <summary>Standard or concessionary positive rate subject to sales tax.</summary>
    Taxable = 1,

    /// <summary>Taxable at 0% rate under Fifth Schedule (input tax deductible/creditable in statutory filing).</summary>
    ZeroRated = 2,

    /// <summary>Unconditionally or conditionally exempt under Sixth Schedule (no sales tax charged; input tax not creditable).</summary>
    Exempt = 3,

    /// <summary>Outside the statutory scope of the sales tax statute (e.g. non-supply or non-taxable jurisdiction).</summary>
    OutOfScope = 4
}
