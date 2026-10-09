namespace Clovent.Restaurant.Refunds;

/// <summary>
/// Human-facing identifier for a refund credit note (e.g. <c>"REF-20261008-0001"</c>).
/// </summary>
public readonly record struct RefundNumber
{
    /// <summary>The display value.</summary>
    public string Value { get; }

    /// <summary>Initializes a new <see cref="RefundNumber"/>.</summary>
    public RefundNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Refund number cannot be empty.", nameof(value));
        Value = value.Trim().ToUpperInvariant();
    }

    /// <summary>Generates a collision-resistant refund number for an instant.</summary>
    public static RefundNumber Generate(DateTimeOffset instant)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return new RefundNumber($"REF-{instant:yyyyMMdd}-{suffix}");
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
