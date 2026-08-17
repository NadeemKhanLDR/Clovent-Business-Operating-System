using Clovent.Domain;
using Clovent.Restaurant.Orders.ValueObjects;

namespace Clovent.Restaurant.Orders;

/// <summary>
/// A single, global counter backing <see cref="Order.OrderNumber"/> - a
/// restaurant owner-configurable prefix (e.g. "ORD-") plus the next number
/// to issue (e.g. "ORD-3453", then "ORD-3454", ...), replacing the earlier
/// timestamp-derived value <see cref="OrderNumber.Generate"/> still
/// documents. Exactly one row exists (see
/// <c>IOrderNumberSequenceRepository.GetSingletonAsync</c>), created with
/// sensible defaults on first use - the same "get-or-create" shape
/// <c>Clovent.Restaurant.Sales.DailySalesSequence</c> already established
/// for a different per-day counter, just without the per-(warehouse, date)
/// partitioning since order numbers are meant to be restaurant-wide and
/// never reset.
/// </summary>
public sealed class OrderNumberSequence : AggregateRoot<OrderNumberSequenceId>
{
    private const int MaxPrefixLength = 20;

    /// <summary>The default prefix used before an owner configures one.</summary>
    public const string DefaultPrefix = "ORD-";

    /// <summary>The default first number used before an owner configures one.</summary>
    public const int DefaultStartingNumber = 1;

    /// <summary>Text prepended to every generated number (e.g. "ORD-").</summary>
    public string Prefix { get; private set; }

    /// <summary>The number that will be issued by the next call to <see cref="Next"/>.</summary>
    public int NextNumber { get; private set; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private OrderNumberSequence(OrderNumberSequenceId id, string prefix, int nextNumber)
    {
        Id = id;
        Prefix = prefix;
        NextNumber = nextNumber;
    }

    /// <summary>Creates the sequence with its default prefix/starting number, before a restaurant owner has configured either.</summary>
    public static OrderNumberSequence CreateDefault() =>
        new(OrderNumberSequenceId.New(), DefaultPrefix, DefaultStartingNumber);

    /// <summary>
    /// Re-baselines the sequence from the Restaurant Setup screen - future
    /// calls to <see cref="Next"/> use the new prefix and start counting
    /// from <paramref name="startingNumber"/>. Does not touch numbers
    /// already issued (already-created orders keep their existing
    /// <see cref="Order.OrderNumber"/>).
    /// </summary>
    /// <exception cref="RestaurantDomainException"><paramref name="prefix"/> is empty/too long, or <paramref name="startingNumber"/> is below 1.</exception>
    public void Configure(string prefix, int startingNumber)
    {
        if (string.IsNullOrWhiteSpace(prefix) || prefix.Trim().Length > MaxPrefixLength)
            throw RestaurantDomainException.InvalidOrderNumberSequencePrefix();

        if (startingNumber < 1)
            throw RestaurantDomainException.InvalidOrderNumberSequenceStartingNumber();

        Prefix = prefix.Trim();
        NextNumber = startingNumber;
    }

    /// <summary>Issues the next <see cref="OrderNumber"/> in the sequence and advances the counter.</summary>
    public OrderNumber Next()
    {
        // OrderNumber requires at least 3 characters; a restaurant owner is
        // free to configure a 1-character Prefix, so the numeric part is
        // zero-padded just enough to keep the combined value valid (e.g.
        // Prefix "O" + NextNumber 1 => "O01", not the invalid "O1"). Normal
        // prefixes (2+ chars, like the default "ORD-") are long enough on
        // their own and this never changes their formatting.
        var minDigits = Math.Max(1, 3 - Prefix.Length);
        var issued = OrderNumber.Create($"{Prefix}{NextNumber.ToString("D" + minDigits)}");
        NextNumber++;
        return issued;
    }
}
