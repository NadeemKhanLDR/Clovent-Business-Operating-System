using Clovent.Catalog.Products;
using Clovent.Catalog.Shared;
using Clovent.Catalog.Shared.ValueObjects;
using Clovent.Catalog.UnitsOfMeasure;
using Clovent.Catalog.Variants.Events;
using Clovent.Catalog.Variants.ValueObjects;
using Clovent.Domain;

namespace Clovent.Catalog.Variants;

/// <summary>
/// A sellable variation of a <see cref="Products.Product"/> (e.g. "Size:
/// Large, Color: Red") - the unit <see cref="Barcodes.Barcode"/>,
/// <see cref="Prices.ProductPrice"/>, and warehouse stock all attach to
/// ("Barcode per variant", "Multiple prices" - see <c>CatalogArchitecture.md</c>).
/// References its owning product by id only, the same pattern
/// <c>Clovent.MasterData</c> uses for its own parent/child relationships.
/// </summary>
public sealed class ProductVariant : AggregateRoot<ProductVariantId>
{
    /// <summary>The product this variant belongs to, fixed at creation.</summary>
    public ProductId ProductId { get; }

    /// <summary>The variant's display name/attribute summary.</summary>
    public VariantName Name { get; private set; }

    /// <summary>The variant's own stock-keeping unit code, fixed at creation.</summary>
    public Sku Sku { get; }

    /// <summary>The variant's unit of measure - may differ from the product's base unit (e.g. a "case" variant of an "each" product).</summary>
    public UnitOfMeasureId UnitOfMeasureId { get; private set; }

    /// <summary>The variant's current lifecycle state.</summary>
    public CatalogStatus Status { get; private set; }

    /// <summary>Manual display position for owner-driven drag-drop reordering (lower sorts first) - e.g. Menu Items/the POS tile wall. Defaults to 0 - every variant starts equally-ordered until an owner reorders them.</summary>
    public int SortOrder { get; private set; }

    /// <summary>UTC instant this variant was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private ProductVariant(ProductVariantId id, ProductId productId, VariantName name, Sku sku, UnitOfMeasureId unitOfMeasureId, CatalogStatus status, int sortOrder, DateTimeOffset createdAtUtc)
    {
        Id = id;
        ProductId = productId;
        Name = name;
        Sku = sku;
        UnitOfMeasureId = unitOfMeasureId;
        Status = status;
        SortOrder = sortOrder;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Creates a new, active variant under the given product.</summary>
    public static ProductVariant Create(ProductId productId, VariantName name, Sku sku, UnitOfMeasureId unitOfMeasureId)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(sku);

        var now = DateTimeOffset.UtcNow;
        var variant = new ProductVariant(ProductVariantId.New(), productId, name, sku, unitOfMeasureId, CatalogStatus.Active, 0, now);
        variant.AddDomainEvent(new ProductVariantCreated(variant.Id, variant.ProductId, variant.Name, variant.Sku, now));
        return variant;
    }

    /// <summary>Renames the variant. A no-op (no event raised) if unchanged.</summary>
    public void Rename(VariantName name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (Name == name) return;

        Name = name;
        AddDomainEvent(new ProductVariantRenamed(Id, name, DateTimeOffset.UtcNow));
    }

    /// <summary>Changes the variant's unit of measure.</summary>
    public void SetUnitOfMeasure(UnitOfMeasureId unitOfMeasureId)
    {
        if (UnitOfMeasureId.Equals(unitOfMeasureId)) return;

        UnitOfMeasureId = unitOfMeasureId;
        AddDomainEvent(new ProductVariantUnitOfMeasureChanged(Id, unitOfMeasureId, DateTimeOffset.UtcNow));
    }

    /// <summary>Sets this variant's manual display position (owner-driven drag-drop reordering). A no-op (no event raised) if unchanged.</summary>
    public void SetSortOrder(int sortOrder)
    {
        if (SortOrder == sortOrder) return;

        SortOrder = sortOrder;
        AddDomainEvent(new ProductVariantSortOrderChanged(Id, sortOrder, DateTimeOffset.UtcNow));
    }

    /// <summary>Activates the variant.</summary>
    /// <exception cref="CatalogDomainException">The variant is already active.</exception>
    public void Activate()
    {
        if (Status == CatalogStatus.Active)
            throw CatalogDomainException.VariantAlreadyActive(Id);

        Status = CatalogStatus.Active;
        AddDomainEvent(new ProductVariantActivated(Id, DateTimeOffset.UtcNow));
    }

    /// <summary>Deactivates the variant.</summary>
    /// <exception cref="CatalogDomainException">The variant is not active.</exception>
    public void Deactivate()
    {
        if (Status != CatalogStatus.Active)
            throw CatalogDomainException.VariantNotActive(Id);

        Status = CatalogStatus.Inactive;
        AddDomainEvent(new ProductVariantDeactivated(Id, DateTimeOffset.UtcNow));
    }
}
