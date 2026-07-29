using System.Text.RegularExpressions;
using Clovent.Domain;

namespace Clovent.Catalog.Barcodes.ValueObjects;

/// <summary>
/// A scanned barcode value - digits only, 8-14 characters, loosely covering
/// EAN-8/UPC-A/EAN-13/ITF-14 lengths without validating any particular
/// symbology's checksum (out of scope: this models "the code a scanner
/// read," not a barcode-standards compliance checker).
/// </summary>
public sealed partial class BarcodeValue : ValueObject
{
    private const int MinLength = 8;
    private const int MaxLength = 14;

    /// <summary>The barcode digits.</summary>
    public string Value { get; }

    private BarcodeValue(string value) => Value = value;

    /// <summary>Validates <paramref name="value"/> into a <see cref="BarcodeValue"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is not <c>8</c>-<c>14</c> digits.</exception>
    public static BarcodeValue Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Barcode value is required.", nameof(value));

        var normalized = value.Trim();

        if (!BarcodePattern().IsMatch(normalized))
            throw new ArgumentException($"Barcode value must be {MinLength}-{MaxLength} digits.", nameof(value));

        return new BarcodeValue(normalized);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;

    [GeneratedRegex(@"^[0-9]{8,14}$")]
    private static partial Regex BarcodePattern();
}
