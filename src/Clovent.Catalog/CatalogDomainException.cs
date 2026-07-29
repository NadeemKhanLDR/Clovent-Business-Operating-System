using Clovent.Catalog.Barcodes;
using Clovent.Catalog.Brands;
using Clovent.Catalog.Categories;
using Clovent.Catalog.Groups;
using Clovent.Catalog.Prices;
using Clovent.Catalog.Products;
using Clovent.Catalog.UnitsOfMeasure;
using Clovent.Catalog.Variants;
using Clovent.Domain;

namespace Clovent.Catalog;

/// <summary>
/// Raised when a Catalog aggregate operation would violate one of its
/// invariants - mirrors <c>Clovent.MasterData.MasterDataDomainException</c>
/// exactly: one sealed type, one static factory method per rule.
/// </summary>
public sealed class CatalogDomainException : DomainException
{
    private CatalogDomainException(string message) : base(message)
    {
    }

    /// <summary>A category Activate() was attempted while already active.</summary>
    public static CatalogDomainException CategoryAlreadyActive(ProductCategoryId categoryId) =>
        new($"Product category '{categoryId}' is already active.");

    /// <summary>A category Deactivate() was attempted while not active.</summary>
    public static CatalogDomainException CategoryNotActive(ProductCategoryId categoryId) =>
        new($"Product category '{categoryId}' is not active.");

    /// <summary>A category's SetParent() was attempted with itself as the parent.</summary>
    public static CatalogDomainException CategoryCannotBeOwnParent(ProductCategoryId categoryId) =>
        new($"Product category '{categoryId}' cannot be its own parent.");

    /// <summary>A group Activate() was attempted while already active.</summary>
    public static CatalogDomainException GroupAlreadyActive(ProductGroupId groupId) =>
        new($"Product group '{groupId}' is already active.");

    /// <summary>A group Deactivate() was attempted while not active.</summary>
    public static CatalogDomainException GroupNotActive(ProductGroupId groupId) =>
        new($"Product group '{groupId}' is not active.");

    /// <summary>A brand Activate() was attempted while already active.</summary>
    public static CatalogDomainException BrandAlreadyActive(BrandId brandId) =>
        new($"Brand '{brandId}' is already active.");

    /// <summary>A brand Deactivate() was attempted while not active.</summary>
    public static CatalogDomainException BrandNotActive(BrandId brandId) =>
        new($"Brand '{brandId}' is not active.");

    /// <summary>A unit of measure Activate() was attempted while already active.</summary>
    public static CatalogDomainException UnitOfMeasureAlreadyActive(UnitOfMeasureId unitOfMeasureId) =>
        new($"Unit of measure '{unitOfMeasureId}' is already active.");

    /// <summary>A unit of measure Deactivate() was attempted while not active.</summary>
    public static CatalogDomainException UnitOfMeasureNotActive(UnitOfMeasureId unitOfMeasureId) =>
        new($"Unit of measure '{unitOfMeasureId}' is not active.");

    /// <summary>A product Activate() was attempted while already active.</summary>
    public static CatalogDomainException ProductAlreadyActive(ProductId productId) =>
        new($"Product '{productId}' is already active.");

    /// <summary>A product Deactivate() was attempted while not active.</summary>
    public static CatalogDomainException ProductNotActive(ProductId productId) =>
        new($"Product '{productId}' is not active.");

    /// <summary>A variant Activate() was attempted while already active.</summary>
    public static CatalogDomainException VariantAlreadyActive(ProductVariantId variantId) =>
        new($"Product variant '{variantId}' is already active.");

    /// <summary>A variant Deactivate() was attempted while not active.</summary>
    public static CatalogDomainException VariantNotActive(ProductVariantId variantId) =>
        new($"Product variant '{variantId}' is not active.");

    /// <summary>A barcode Activate() was attempted while already active.</summary>
    public static CatalogDomainException BarcodeAlreadyActive(BarcodeId barcodeId) =>
        new($"Barcode '{barcodeId}' is already active.");

    /// <summary>A barcode Deactivate() was attempted while not active.</summary>
    public static CatalogDomainException BarcodeNotActive(BarcodeId barcodeId) =>
        new($"Barcode '{barcodeId}' is not active.");

    /// <summary>A price Activate() was attempted while already active.</summary>
    public static CatalogDomainException PriceAlreadyActive(ProductPriceId priceId) =>
        new($"Product price '{priceId}' is already active.");

    /// <summary>A price Deactivate() was attempted while not active.</summary>
    public static CatalogDomainException PriceNotActive(ProductPriceId priceId) =>
        new($"Product price '{priceId}' is not active.");
}
