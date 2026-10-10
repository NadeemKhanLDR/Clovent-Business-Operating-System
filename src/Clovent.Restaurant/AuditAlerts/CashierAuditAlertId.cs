namespace Clovent.Restaurant.AuditAlerts;

/// <summary>Strongly-typed identifier for a <see cref="CashierAuditAlert"/> aggregate.</summary>
public readonly record struct CashierAuditAlertId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("CashierAuditAlertId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="CashierAuditAlertId"/>.</summary>
    public static CashierAuditAlertId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
