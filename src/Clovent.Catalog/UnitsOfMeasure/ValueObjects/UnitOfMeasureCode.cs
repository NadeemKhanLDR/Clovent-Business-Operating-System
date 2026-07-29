using System.Text.RegularExpressions;
using Clovent.Domain;

namespace Clovent.Catalog.UnitsOfMeasure.ValueObjects;

/// <summary>
/// A unit of measure's short code (e.g. "KG", "PCS", "BOX") - not reusing
/// <c>Clovent.MasterData.Shared.ValueObjects.EntityCode</c> since that would
/// pull this bounded context into a dependency it doesn't otherwise need for
/// what is, structurally, an identical short-code shape; see
/// <c>Clovent.Catalog.Shared.CatalogStatus</c>'s doc comment for the same
/// "avoid an unnecessary cross-project dependency" reasoning applied to
/// enums instead of value objects.
/// </summary>
public sealed partial class UnitOfMeasureCode : ValueObject
{
    private const int MinLength = 1;
    private const int MaxLength = 10;

    /// <summary>The code, always uppercase.</summary>
    public string Value { get; }

    private UnitOfMeasureCode(string value) => Value = value;

    /// <summary>Validates and normalizes <paramref name="value"/> into a <see cref="UnitOfMeasureCode"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> does not match the required shape.</exception>
    public static UnitOfMeasureCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Unit of measure code is required.", nameof(value));

        var normalized = value.Trim().ToUpperInvariant();

        if (!CodePattern().IsMatch(normalized))
            throw new ArgumentException(
                $"Unit of measure code must be {MinLength}-{MaxLength} uppercase letters or digits.",
                nameof(value));

        return new UnitOfMeasureCode(normalized);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;

    [GeneratedRegex(@"^[A-Z0-9]{1,10}$")]
    private static partial Regex CodePattern();
}
