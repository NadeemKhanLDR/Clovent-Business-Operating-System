using Clovent.Domain;

namespace Clovent.Restaurant.PaymentMethods.ValueObjects;

/// <summary>A payment method's display name (e.g. "Cash", "Credit Card").</summary>
public sealed class PaymentMethodName : ValueObject
{
    private const int MinLength = 2;
    private const int MaxLength = 50;

    /// <summary>The name text.</summary>
    public string Value { get; }

    private PaymentMethodName(string value) => Value = value;

    /// <summary>Validates <paramref name="value"/> into a <see cref="PaymentMethodName"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is shorter than <c>2</c> or longer than <c>50</c> characters.</exception>
    public static PaymentMethodName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Payment method name is required.", nameof(value));

        value = value.Trim();

        if (value.Length < MinLength || value.Length > MaxLength)
            throw new ArgumentException($"Payment method name must be {MinLength}-{MaxLength} characters.", nameof(value));

        return new PaymentMethodName(value);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
