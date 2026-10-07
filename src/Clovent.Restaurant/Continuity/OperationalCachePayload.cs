namespace Clovent.Restaurant.Continuity;

/// <summary>
/// Immutable operational cache payload containing only the menu, pricing, tax,
/// and terminal data required for safe offline Continuity Cash selling.
/// </summary>
public sealed class OperationalCachePayload
{
    /// <summary>Cached catalog categories.</summary>
    public IReadOnlyList<CachedCategory> Categories { get; init; } = Array.Empty<CachedCategory>();

    /// <summary>Cached products with tax configurations.</summary>
    public IReadOnlyList<CachedProduct> Products { get; init; } = Array.Empty<CachedProduct>();

    /// <summary>Cached variants with selling prices.</summary>
    public IReadOnlyList<CachedVariant> Variants { get; init; } = Array.Empty<CachedVariant>();

    /// <summary>Cached quick order templates.</summary>
    public IReadOnlyList<CachedQuickOrderTemplate> QuickOrderTemplates { get; init; } = Array.Empty<CachedQuickOrderTemplate>();

    /// <summary>Cached dining areas.</summary>
    public IReadOnlyList<CachedDiningArea> DiningAreas { get; init; } = Array.Empty<CachedDiningArea>();

    /// <summary>Cached tables.</summary>
    public IReadOnlyList<CachedTable> Tables { get; init; } = Array.Empty<CachedTable>();

    /// <summary>Cached discount policies.</summary>
    public IReadOnlyList<CachedDiscountPolicy> Discounts { get; init; } = Array.Empty<CachedDiscountPolicy>();

    /// <summary>Cached payment methods.</summary>
    public IReadOnlyList<CachedPaymentMethod> PaymentMethods { get; init; } = Array.Empty<CachedPaymentMethod>();

    /// <summary>Cached operator claims.</summary>
    public IReadOnlyList<CachedOperatorClaim> Operators { get; init; } = Array.Empty<CachedOperatorClaim>();

    /// <summary>Minimized cached customers.</summary>
    public IReadOnlyList<CachedCustomer> Customers { get; init; } = Array.Empty<CachedCustomer>();
}
