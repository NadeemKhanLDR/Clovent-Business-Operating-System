using Clovent.Catalog.Prices.Events;
using Clovent.Catalog.Shared;
using Clovent.Catalog.Variants;
using Clovent.Domain;
using Clovent.MasterData.Currencies;

namespace Clovent.Catalog.Prices;

/// <summary>
/// A priced record for one <see cref="ProductVariant"/> - either what the
/// business pays (<see cref="PriceType.Cost"/>) or charges
/// (<see cref="PriceType.Selling"/>). "Multiple prices" per variant is a
/// natural consequence of this being its own aggregate with its own
/// repository rather than a single field on the variant: a variant can have
/// several active/inactive <c>ProductPrice</c> records over time (a price
/// history), the Application layer reading whichever is currently active
/// for a given <see cref="PriceType"/> - see <c>CatalogArchitecture.md</c>.
/// </summary>
public sealed class ProductPrice : AggregateRoot<ProductPriceId>
{
    /// <summary>The variant this price applies to, fixed at creation.</summary>
    public ProductVariantId ProductVariantId { get; }

    /// <summary>Whether this is a cost or selling price, fixed at creation.</summary>
    public PriceType PriceType { get; }

    /// <summary>The price amount.</summary>
    public decimal Amount { get; private set; }

    /// <summary>The currency this amount is denominated in, fixed at creation.</summary>
    public CurrencyId CurrencyId { get; }

    /// <summary>UTC instant this price becomes effective.</summary>
    public DateTimeOffset EffectiveFromUtc { get; }

    /// <summary>The price's current lifecycle state.</summary>
    public CatalogStatus Status { get; private set; }

    /// <summary>UTC instant this price record was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private ProductPrice(
        ProductPriceId id,
        ProductVariantId productVariantId,
        PriceType priceType,
        decimal amount,
        CurrencyId currencyId,
        DateTimeOffset effectiveFromUtc,
        CatalogStatus status,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        ProductVariantId = productVariantId;
        PriceType = priceType;
        Amount = amount;
        CurrencyId = currencyId;
        EffectiveFromUtc = effectiveFromUtc;
        Status = status;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Creates a new, active price record for the given variant.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="amount"/> is negative.</exception>
    public static ProductPrice Create(ProductVariantId productVariantId, PriceType priceType, decimal amount, CurrencyId currencyId, DateTimeOffset? effectiveFromUtc = null)
    {
        RequireNonNegative(amount);

        var now = DateTimeOffset.UtcNow;
        var price = new ProductPrice(ProductPriceId.New(), productVariantId, priceType, amount, currencyId, effectiveFromUtc ?? now, CatalogStatus.Active, now);
        price.AddDomainEvent(new ProductPriceCreated(price.Id, price.ProductVariantId, price.PriceType, price.Amount, price.CurrencyId, now));
        return price;
    }

    /// <summary>Updates the price amount.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="amount"/> is negative.</exception>
    public void UpdateAmount(decimal amount)
    {
        RequireNonNegative(amount);
        if (Amount == amount) return;

        Amount = amount;
        AddDomainEvent(new ProductPriceAmountChanged(Id, amount, DateTimeOffset.UtcNow));
    }

    /// <summary>Activates the price.</summary>
    /// <exception cref="CatalogDomainException">The price is already active.</exception>
    public void Activate()
    {
        if (Status == CatalogStatus.Active)
            throw CatalogDomainException.PriceAlreadyActive(Id);

        Status = CatalogStatus.Active;
        AddDomainEvent(new ProductPriceActivated(Id, DateTimeOffset.UtcNow));
    }

    /// <summary>Deactivates the price.</summary>
    /// <exception cref="CatalogDomainException">The price is not active.</exception>
    public void Deactivate()
    {
        if (Status != CatalogStatus.Active)
            throw CatalogDomainException.PriceNotActive(Id);

        Status = CatalogStatus.Inactive;
        AddDomainEvent(new ProductPriceDeactivated(Id, DateTimeOffset.UtcNow));
    }

    private static void RequireNonNegative(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Price amount cannot be negative.");
    }
}
