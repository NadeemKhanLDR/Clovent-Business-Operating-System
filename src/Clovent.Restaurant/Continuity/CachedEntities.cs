namespace Clovent.Restaurant.Continuity;

/// <summary>Cached catalog category record.</summary>
public sealed record CachedCategory(
    Guid ProductCategoryId,
    string Name,
    Guid? ParentCategoryId,
    string? ColorHex,
    int SortOrder,
    string Status);

/// <summary>Cached product record with tax treatment and item classification.</summary>
public sealed record CachedProduct(
    Guid ProductId,
    string Name,
    string Sku,
    Guid? CategoryId,
    string ItemType,
    decimal TaxRatePercentage,
    bool TaxIsInclusive,
    string Status);

/// <summary>Cached variant record representing a sellable menu item with price and availability.</summary>
public sealed record CachedVariant(
    Guid ProductVariantId,
    Guid ProductId,
    string Name,
    string Sku,
    decimal SellingPrice,
    Guid UnitOfMeasureId,
    Guid? ProductCategoryId,
    int SortOrder,
    string Status,
    bool IsAvailable,
    string ItemType,
    decimal? LastKnownStock,
    string? ProductName = null,
    decimal TaxRatePercentage = 0m,
    bool TaxIsInclusive = false);

/// <summary>Cached quick order template line item.</summary>
public sealed record CachedQuickOrderTemplateItem(
    Guid ProductVariantId,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal);

/// <summary>Cached quick order template.</summary>
public sealed record CachedQuickOrderTemplate(
    Guid TemplateId,
    string Name,
    string? Description,
    decimal TotalPrice,
    Guid? WarehouseId,
    IReadOnlyList<CachedQuickOrderTemplateItem> Items);

/// <summary>Cached dining area for table layout in restaurant POS.</summary>
public sealed record CachedDiningArea(
    Guid AreaId,
    string Name);

/// <summary>Cached restaurant table.</summary>
public sealed record CachedTable(
    Guid TableId,
    Guid DiningAreaId,
    string TableNumber,
    int Capacity,
    string Status,
    string? OccupancyStatus = null)
{
    public string Code => TableNumber;
    public string EffectiveOccupancyStatus => OccupancyStatus ?? Status;
}

/// <summary>Cached discount policy for offline validation.</summary>
public sealed record CachedDiscountPolicy(
    Guid DiscountId,
    string Name,
    string DiscountType,
    decimal Value,
    decimal MaxDiscountAmount,
    bool RequireManagerPin);

/// <summary>Cached payment method.</summary>
public sealed record CachedPaymentMethod(
    Guid PaymentMethodId,
    string Name,
    string Status,
    bool IsOfflineAllowed);

/// <summary>Cached operator authorization claim for already-authenticated cashier sessions.</summary>
public sealed record CachedOperatorClaim(
    Guid CashierId,
    string CashierName,
    string RoleName,
    bool CanOverridePrice,
    bool CanApplyDiscount,
    DateTimeOffset? SessionExpiryUtc);

/// <summary>Minimized cached customer record for basic customer tagging during offline checkout.</summary>
public sealed record CachedCustomer(
    Guid CustomerId,
    string Name,
    string? PhoneNumber,
    string? AccountNumber,
    bool IsWalkIn,
    bool IsActive = true,
    bool IsDefault = false,
    decimal OutstandingBalance = 0m,
    bool IsCreditAllowed = false)
{
    public string? Code => AccountNumber;
    public string? MobileNumber => PhoneNumber;
}
