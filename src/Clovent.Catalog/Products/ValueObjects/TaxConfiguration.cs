using Clovent.Catalog.TaxProfiles;
using Clovent.Domain;

namespace Clovent.Catalog.Products.ValueObjects;

/// <summary>
/// A product's tax treatment: rate, inclusive/exclusive pricing mode, statutory tax classification
/// (Taxable, Zero-Rated, Exempt, Out of Scope), authority, tax code, and profile reference.
/// Preserves backward compatibility with legacy 2-parameter instantiation while enriching
/// statutory Pakistan sales tax metadata.
/// </summary>
public sealed class TaxConfiguration : ValueObject
{
    /// <summary>The tax rate as a percentage (e.g. <c>16.0</c> for 16%).</summary>
    public decimal RatePercentage { get; }

    /// <summary>Whether prices for this product already include tax.</summary>
    public bool IsInclusive { get; }

    /// <summary>Statutory taxability classification (Taxable, ZeroRated, Exempt, OutOfScope).</summary>
    public TaxClassification TaxClassification { get; }

    /// <summary>Statutory tax code (e.g. "PK-PRA-16", "PK-FBR-18").</summary>
    public string? TaxCode { get; }

    /// <summary>Revenue authority (e.g. "PRA", "SRB", "FBR").</summary>
    public string? Authority { get; }

    /// <summary>Associated TaxProfile ID, if configured.</summary>
    public TaxProfileId? TaxProfileId { get; }

    private TaxConfiguration(
        decimal ratePercentage,
        bool isInclusive,
        TaxClassification taxClassification,
        string? taxCode,
        string? authority,
        TaxProfileId? taxProfileId)
    {
        RatePercentage = ratePercentage;
        IsInclusive = isInclusive;
        TaxClassification = taxClassification;
        TaxCode = taxCode;
        Authority = authority;
        TaxProfileId = taxProfileId;
    }

    /// <summary>Creates a <see cref="TaxConfiguration"/> with backward compatibility.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="ratePercentage"/> is negative or greater than 100.</exception>
    public static TaxConfiguration Create(
        decimal ratePercentage,
        bool isInclusive,
        TaxClassification? taxClassification = null,
        string? taxCode = null,
        string? authority = null,
        TaxProfileId? taxProfileId = null)
    {
        if (ratePercentage is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(ratePercentage), ratePercentage, "Tax rate must be between 0 and 100.");

        var classification = taxClassification ?? (ratePercentage > 0m ? TaxClassification.Taxable : TaxClassification.OutOfScope);
        return new TaxConfiguration(ratePercentage, isInclusive, classification, taxCode, authority, taxProfileId);
    }

    /// <summary>Creates a <see cref="TaxConfiguration"/> directly from a <see cref="TaxProfiles.TaxProfile"/>.</summary>
    public static TaxConfiguration FromProfile(TaxProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return new TaxConfiguration(
            profile.RatePercentage,
            profile.IsInclusive,
            profile.TaxClassification,
            profile.Code,
            profile.Authority,
            profile.Id);
    }

    /// <summary>A convenience default: out of scope, 0% exclusive.</summary>
    public static TaxConfiguration None => new(0m, false, TaxClassification.OutOfScope, null, null, null);

    /// <summary>Convenience helper for zero-rated items.</summary>
    public static TaxConfiguration ZeroRated(string? taxCode = "PK-ZERO", string? authority = "FBR") =>
        new(0m, false, TaxClassification.ZeroRated, taxCode, authority, null);

    /// <summary>Convenience helper for statutory exempt items.</summary>
    public static TaxConfiguration Exempt(string? taxCode = "PK-EXEMPT", string? authority = "FBR") =>
        new(0m, false, TaxClassification.Exempt, taxCode, authority, null);

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RatePercentage;
        yield return IsInclusive;
        yield return TaxClassification;
        yield return TaxCode;
        yield return Authority;
        yield return TaxProfileId;
    }

    /// <inheritdoc/>
    public override string ToString() =>
        $"{TaxClassification}: {RatePercentage}% ({(IsInclusive ? "inclusive" : "exclusive")}) [Code: {TaxCode ?? "N/A"}]";
}
