using System.Text.RegularExpressions;
using Clovent.Domain;

namespace Clovent.MasterData.Shared.ValueObjects;

/// <summary>
/// A short, human-assigned identifier for a master-data entity (e.g. a
/// warehouse code "WH-01", a terminal code "T-001") - distinct from the
/// entity's strongly-typed <c>Id</c> (system-generated, never shown to a
/// user) and from its <c>Name</c> (free text, not required to be unique).
/// Shared between <see cref="Warehouses.Warehouse"/> and <see cref="Terminals.Terminal"/>
/// since both need the identical shape; <see cref="Currencies.CurrencyCode"/>/
/// <see cref="Languages.LanguageCode"/> stay separate, ISO-format-specific
/// value objects rather than reusing this one, since their validation rules
/// are genuinely different (fixed-length, standards-defined) rather than
/// "short, human-assigned, uppercase."
/// </summary>
public sealed partial class EntityCode : ValueObject
{
    private const int MinLength = 2;
    private const int MaxLength = 20;

    /// <summary>The code, always uppercase.</summary>
    public string Value { get; }

    private EntityCode(string value) => Value = value;

    /// <summary>Validates and normalizes <paramref name="value"/> into an <see cref="EntityCode"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> does not match the required shape.</exception>
    public static EntityCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Code is required.", nameof(value));

        var normalized = value.Trim().ToUpperInvariant();

        if (!CodePattern().IsMatch(normalized))
            throw new ArgumentException(
                $"Code must be {MinLength}-{MaxLength} characters: uppercase letters, digits, or hyphens, starting with a letter or digit.",
                nameof(value));

        return new EntityCode(normalized);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;

    [GeneratedRegex(@"^[A-Z0-9][A-Z0-9-]{1,19}$")]
    private static partial Regex CodePattern();
}
