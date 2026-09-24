using Clovent.Catalog.Variants;
using Clovent.Domain;

namespace Clovent.Restaurant.SmartRecommendations;

/// <summary>
/// The kind of cashier interaction a smart suggestion produced. Only
/// <see cref="Accepted"/> carries attributed revenue, and it is written
/// exclusively when the cashier added the item through the suggestion
/// strip - a normal menu addition never writes an event.
/// </summary>
public enum SuggestionEventKind
{
    /// <summary>The suggestion was displayed to the cashier.</summary>
    Offered,

    /// <summary>The cashier tapped the suggestion and the item was added.</summary>
    Accepted,

    /// <summary>The cashier dismissed the suggestion strip.</summary>
    Dismissed
}

/// <summary>
/// An immutable analytics fact about one smart-suggestion interaction on
/// the POS: offered, accepted, or dismissed. Written fire-and-forget by the
/// POS (never blocking checkout), aggregated by
/// <c>GetUpsellPerformanceQuery</c> in the Back Office. Accepted events
/// snapshot the added line's quantity and unit amount so upsell revenue is
/// attributed exactly to the interaction that caused it, without re-deriving
/// it later from mutable order data.
/// </summary>
public sealed class SuggestionEvent : Entity<SuggestionEventId>
{
    /// <summary>The order whose basket drove the suggestion.</summary>
    public Guid OrderId { get; }

    /// <summary>The suggested (recommended) variant.</summary>
    public ProductVariantId VariantId { get; }

    /// <summary>The basket variant that triggered the rule, when known.</summary>
    public Guid? TriggerVariantId { get; }

    /// <summary>What the cashier did with the suggestion.</summary>
    public SuggestionEventKind Kind { get; }

    /// <summary>
    /// The order line created (or incremented) by accepting the suggestion;
    /// <see langword="null"/> for Offered/Dismissed events.
    /// </summary>
    public Guid? OrderLineId { get; }

    /// <summary>Quantity added through the accepted suggestion; 0 otherwise.</summary>
    public decimal AcceptedQuantity { get; }

    /// <summary>
    /// Unit amount of the added line at accept time (the order pipeline's
    /// own price, not catalog data duplicated here); 0 otherwise.
    /// </summary>
    public decimal AcceptedUnitAmount { get; }

    /// <summary>UTC instant the interaction occurred.</summary>
    public DateTimeOffset OccurredAtUtc { get; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private SuggestionEvent(
        SuggestionEventId id,
        Guid orderId,
        ProductVariantId variantId,
        Guid? triggerVariantId,
        SuggestionEventKind kind,
        Guid? orderLineId,
        decimal acceptedQuantity,
        decimal acceptedUnitAmount,
        DateTimeOffset occurredAtUtc)
    {
        Id = id;
        OrderId = orderId;
        VariantId = variantId;
        TriggerVariantId = triggerVariantId;
        Kind = kind;
        OrderLineId = orderLineId;
        AcceptedQuantity = acceptedQuantity;
        AcceptedUnitAmount = acceptedUnitAmount;
        OccurredAtUtc = occurredAtUtc;
    }

    /// <summary>Records that a suggestion was offered.</summary>
    public static SuggestionEvent Offered(Guid orderId, ProductVariantId variantId, Guid? triggerVariantId) =>
        new(SuggestionEventId.New(), orderId, variantId, triggerVariantId, SuggestionEventKind.Offered, null, 0m, 0m, DateTimeOffset.UtcNow);

    /// <summary>Records that the cashier accepted a suggestion by adding the item.</summary>
    public static SuggestionEvent Accepted(Guid orderId, ProductVariantId variantId, Guid? triggerVariantId, Guid? orderLineId, decimal quantity, decimal unitAmount)
    {
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity cannot be negative.");
        if (unitAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(unitAmount), unitAmount, "Unit amount cannot be negative.");

        return new SuggestionEvent(SuggestionEventId.New(), orderId, variantId, triggerVariantId, SuggestionEventKind.Accepted, orderLineId, quantity, unitAmount, DateTimeOffset.UtcNow);
    }

    /// <summary>Records that the cashier dismissed a suggestion.</summary>
    public static SuggestionEvent Dismissed(Guid orderId, ProductVariantId variantId, Guid? triggerVariantId) =>
        new(SuggestionEventId.New(), orderId, variantId, triggerVariantId, SuggestionEventKind.Dismissed, null, 0m, 0m, DateTimeOffset.UtcNow);
}

/// <summary>Strongly-typed identifier for a <see cref="SuggestionEvent"/> entity.</summary>
public readonly record struct SuggestionEventId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("SuggestionEventId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="SuggestionEventId"/>.</summary>
    public static SuggestionEventId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
