using Clovent.Domain;

namespace Clovent.Identity.Shared.ValueObjects;

/// <summary>
/// A business's tax registration identifier (e.g. EIN, VAT number) - deliberately
/// a loosely-validated opaque string rather than a jurisdiction-specific
/// format, since this milestone models the multi-tenant hierarchy generically
/// rather than any one country's tax authority rules. Shared between
/// <see cref="Organizations.Organization"/> and <see cref="Companies.Company"/> -
/// both are legal entities that may register one, and the shape is identical.
/// </summary>
public sealed class TaxId : ValueObject
{
    private const int MaxLength = 50;

    /// <summary>The tax identifier text, as registered.</summary>
    public string Value { get; }

    private TaxId(string value) => Value = value;

    /// <summary>Validates <paramref name="value"/> into a <see cref="TaxId"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is empty or exceeds <see cref="MaxLength"/> characters.</exception>
    public static TaxId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Tax id is required.", nameof(value));

        value = value.Trim();

        if (value.Length > MaxLength)
            throw new ArgumentException($"Tax id cannot exceed {MaxLength} characters.", nameof(value));

        return new TaxId(value);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
