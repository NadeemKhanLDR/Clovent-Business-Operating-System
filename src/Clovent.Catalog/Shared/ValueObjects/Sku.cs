using System.Text.RegularExpressions;
using Clovent.Domain;

namespace Clovent.Catalog.Shared.ValueObjects;

/// <summary>
/// A stock-keeping unit code - the human-assigned identifier a
/// <see cref="Products.Product"/> or <see cref="Variants.ProductVariant"/>
/// is looked up and referenced by outside the system (purchase orders,
/// receipts, barcodes-adjacent paperwork), distinct from the aggregate's
/// system-generated <c>Id</c>. Shared between <c>Product</c> and
/// <c>ProductVariant</c> since both need the identical shape and each
/// enforces its own uniqueness independently (a product's own SKU and its
/// variants' SKUs live in different uniqueness scopes).
/// </summary>
public sealed partial class Sku : ValueObject
{
    private const int MinLength = 2;
    private const int MaxLength = 40;

    /// <summary>The code, always uppercase.</summary>
    public string Value { get; }

    private Sku(string value) => Value = value;

    /// <summary>Validates and normalizes <paramref name="value"/> into a <see cref="Sku"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> does not match the required shape.</exception>
    public static Sku Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("SKU is required.", nameof(value));

        var normalized = value.Trim().ToUpperInvariant();

        if (!SkuPattern().IsMatch(normalized))
            throw new ArgumentException(
                $"SKU must be {MinLength}-{MaxLength} characters: uppercase letters, digits, or hyphens, starting with a letter or digit.",
                nameof(value));

        return new Sku(normalized);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;

    [GeneratedRegex(@"^[A-Z0-9][A-Z0-9-]{1,39}$")]
    private static partial Regex SkuPattern();
}
