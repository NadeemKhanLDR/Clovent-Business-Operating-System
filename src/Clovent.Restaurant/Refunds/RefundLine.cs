using Clovent.Catalog.Variants;
using Clovent.Domain;
using Clovent.Restaurant.DomainServices;
using Clovent.Restaurant.OrderLines;

namespace Clovent.Restaurant.Refunds;

/// <summary>
/// A single item line on a compensating refund document.
/// Explicitly links back to the original <see cref="OrderLineId"/>, consuming snapshotted
/// unit prices, discounts, and tax rates without retroactively recalculating from current catalog tables.
/// </summary>
public sealed class RefundLine : Entity<RefundLineId>
{
    /// <summary>Identity of the parent refund aggregate.</summary>
    public RefundId RefundId { get; private set; }

    /// <summary>Identity of the original order line being refunded.</summary>
    public OrderLineId OrderLineId { get; }

    /// <summary>Variant identity of the product refunded.</summary>
    public ProductVariantId ProductVariantId { get; }

    /// <summary>Snapshotted SKU text.</summary>
    public string Sku { get; }

    /// <summary>Snapshotted product name.</summary>
    public string Name { get; }

    /// <summary>Quantity refunded (> 0).</summary>
    public decimal Quantity { get; }

    /// <summary>Original unit price snapshotted from the order line.</summary>
    public decimal UnitPrice { get; }

    /// <summary>Gross value refunded: <see cref="Quantity"/> * <see cref="UnitPrice"/>.</summary>
    public decimal GrossAmount => MoneyRoundingPolicy.RoundMoney(Quantity * UnitPrice);

    /// <summary>Allocated discount reversed by this refund line.</summary>
    public decimal DiscountReversed { get; }

    /// <summary>Sales tax amount reversed by this refund line.</summary>
    public decimal TaxReversed { get; }

    /// <summary>Net line amount payable back to the customer.</summary>
    public decimal LineTotalRefunded { get; }

    /// <summary>Statutory tax classification of the original line.</summary>
    public string TaxClassification { get; }

    /// <summary>Statutory tax code of the original line.</summary>
    public string TaxCode { get; }

    /// <summary>Tax rate percentage of the original line.</summary>
    public decimal TaxRatePercentage { get; }

    /// <summary>Whether original price was tax inclusive.</summary>
    public bool TaxIsInclusive { get; }

    /// <summary>Disposition for returned inventory.</summary>
    public InventoryDisposition InventoryDisposition { get; }

    /// <summary>Constructor for EF Core persistence.</summary>
    private RefundLine(
        RefundLineId id,
        RefundId refundId,
        OrderLineId orderLineId,
        ProductVariantId productVariantId,
        string sku,
        string name,
        decimal quantity,
        decimal unitPrice,
        decimal discountReversed,
        decimal taxReversed,
        decimal lineTotalRefunded,
        string taxClassification,
        string taxCode,
        decimal taxRatePercentage,
        bool taxIsInclusive,
        InventoryDisposition inventoryDisposition)
    {
        Id = id;
        RefundId = refundId;
        OrderLineId = orderLineId;
        ProductVariantId = productVariantId;
        Sku = sku;
        Name = name;
        Quantity = quantity;
        UnitPrice = unitPrice;
        DiscountReversed = discountReversed;
        TaxReversed = taxReversed;
        LineTotalRefunded = lineTotalRefunded;
        TaxClassification = taxClassification;
        TaxCode = taxCode;
        TaxRatePercentage = taxRatePercentage;
        TaxIsInclusive = taxIsInclusive;
        InventoryDisposition = inventoryDisposition;
    }

    /// <summary>Creates a new refund line.</summary>
    public static RefundLine Create(
        RefundId refundId,
        OrderLineId orderLineId,
        ProductVariantId productVariantId,
        string sku,
        string name,
        decimal quantity,
        decimal unitPrice,
        decimal discountReversed,
        decimal taxReversed,
        decimal lineTotalRefunded,
        string taxClassification,
        string taxCode,
        decimal taxRatePercentage,
        bool taxIsInclusive,
        InventoryDisposition inventoryDisposition)
    {
        if (quantity <= 0m)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Refund quantity must be positive.");
        if (unitPrice < 0m)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), unitPrice, "Unit price cannot be negative.");

        return new RefundLine(
            RefundLineId.New(),
            refundId,
            orderLineId,
            productVariantId,
            sku.Trim(),
            name.Trim(),
            quantity,
            unitPrice,
            discountReversed,
            taxReversed,
            lineTotalRefunded,
            taxClassification,
            taxCode,
            taxRatePercentage,
            taxIsInclusive,
            inventoryDisposition);
    }
}
