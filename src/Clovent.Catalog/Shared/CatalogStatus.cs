namespace Clovent.Catalog.Shared;

/// <summary>
/// Shared lifecycle state for the aggregates in this bounded context that
/// only ever need "active or not" - <see cref="Categories.ProductCategory"/>,
/// <see cref="Groups.ProductGroup"/>, <see cref="Brands.Brand"/>,
/// <see cref="UnitsOfMeasure.UnitOfMeasure"/>, <see cref="Products.Product"/>,
/// <see cref="Variants.ProductVariant"/>, <see cref="Barcodes.Barcode"/>,
/// <see cref="Prices.ProductPrice"/> - one enum rather than eight
/// structurally-identical ones, mirroring
/// <c>Clovent.MasterData.Shared.MasterDataStatus</c>'s identical reasoning.
/// </summary>
public enum CatalogStatus
{
    /// <summary>Active and usable.</summary>
    Active,

    /// <summary>Deactivated.</summary>
    Inactive
}
