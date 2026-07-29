using Clovent.Domain;

namespace Clovent.Catalog.Products.ValueObjects;

/// <summary>
/// A product's tax treatment: the rate applied and whether the amounts on
/// <c>Prices.ProductPrice</c> records already include that tax or exclude
/// it. Modeled as one value object
/// rather than two independent fields since "rate" and "inclusive/exclusive"
/// are only ever meaningful together - a rate with no inclusive/exclusive
/// answer is incomplete, and vice versa.
/// </summary>
public sealed class TaxConfiguration : ValueObject
{
    /// <summary>The tax rate as a percentage (e.g. <c>15.0</c> for 15%).</summary>
    public decimal RatePercentage { get; }

    /// <summary>Whether prices for this product already include tax.</summary>
    public bool IsInclusive { get; }

    private TaxConfiguration(decimal ratePercentage, bool isInclusive)
    {
        RatePercentage = ratePercentage;
        IsInclusive = isInclusive;
    }

    /// <summary>Creates a <see cref="TaxConfiguration"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="ratePercentage"/> is negative or greater than 100.</exception>
    public static TaxConfiguration Create(decimal ratePercentage, bool isInclusive)
    {
        if (ratePercentage is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(ratePercentage), ratePercentage, "Tax rate must be between 0 and 100.");

        return new TaxConfiguration(ratePercentage, isInclusive);
    }

    /// <summary>A convenience default: no tax, exclusive.</summary>
    public static TaxConfiguration None => new(0m, false);

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RatePercentage;
        yield return IsInclusive;
    }

    /// <inheritdoc/>
    public override string ToString() => $"{RatePercentage}% ({(IsInclusive ? "inclusive" : "exclusive")})";
}
