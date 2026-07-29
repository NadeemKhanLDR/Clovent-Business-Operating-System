using Clovent.Catalog.Brands;
using Clovent.Catalog.Categories;
using Clovent.Catalog.Groups;
using Clovent.Catalog.Products.Events;
using Clovent.Catalog.Products.ValueObjects;
using Clovent.Catalog.Shared;
using Clovent.Catalog.Shared.ValueObjects;
using Clovent.Catalog.UnitsOfMeasure;
using Clovent.Domain;

namespace Clovent.Catalog.Products;

/// <summary>
/// A sellable item's catalog record - name, SKU, classification
/// (Category/Group/Brand, each optional), base unit of measure, and tax
/// treatment. Deliberately holds no price or stock: those vary per
/// <see cref="Variants.ProductVariant"/> (a bare product with no variants
/// would be unsellable - see <c>CatalogArchitecture.md</c> for the
/// Product/Variant split rationale).
/// </summary>
public sealed class Product : AggregateRoot<ProductId>
{
    /// <summary>The product's display name.</summary>
    public ProductName Name { get; private set; }

    /// <summary>The product's stock-keeping unit code, fixed at creation.</summary>
    public Sku Sku { get; }

    /// <summary>The product's category, if classified.</summary>
    public ProductCategoryId? CategoryId { get; private set; }

    /// <summary>The product's group, if classified.</summary>
    public ProductGroupId? GroupId { get; private set; }

    /// <summary>The product's brand, if any.</summary>
    public BrandId? BrandId { get; private set; }

    /// <summary>The product's base unit of measure, fixed at creation.</summary>
    public UnitOfMeasureId BaseUnitOfMeasureId { get; }

    /// <summary>The product's tax treatment.</summary>
    public TaxConfiguration TaxConfiguration { get; private set; }

    /// <summary>The product's current lifecycle state.</summary>
    public CatalogStatus Status { get; private set; }

    /// <summary>UTC instant this product was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private Product(
        ProductId id,
        ProductName name,
        Sku sku,
        ProductCategoryId? categoryId,
        ProductGroupId? groupId,
        BrandId? brandId,
        UnitOfMeasureId baseUnitOfMeasureId,
        TaxConfiguration taxConfiguration,
        CatalogStatus status,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        Name = name;
        Sku = sku;
        CategoryId = categoryId;
        GroupId = groupId;
        BrandId = brandId;
        BaseUnitOfMeasureId = baseUnitOfMeasureId;
        TaxConfiguration = taxConfiguration;
        Status = status;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Creates a new, active product.</summary>
    public static Product Create(
        ProductName name,
        Sku sku,
        UnitOfMeasureId baseUnitOfMeasureId,
        TaxConfiguration? taxConfiguration = null,
        ProductCategoryId? categoryId = null,
        ProductGroupId? groupId = null,
        BrandId? brandId = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(sku);

        var now = DateTimeOffset.UtcNow;
        var product = new Product(
            ProductId.New(), name, sku, categoryId, groupId, brandId, baseUnitOfMeasureId,
            taxConfiguration ?? TaxConfiguration.None, CatalogStatus.Active, now);
        product.AddDomainEvent(new ProductCreated(product.Id, product.Name, product.Sku, product.BaseUnitOfMeasureId, now));
        return product;
    }

    /// <summary>Renames the product. A no-op (no event raised) if unchanged.</summary>
    public void Rename(ProductName name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (Name == name) return;

        Name = name;
        AddDomainEvent(new ProductRenamed(Id, name, DateTimeOffset.UtcNow));
    }

    /// <summary>Sets or clears the product's category.</summary>
    public void SetCategory(ProductCategoryId? categoryId)
    {
        if (CategoryId == categoryId) return;

        CategoryId = categoryId;
        AddDomainEvent(new ProductCategoryAssigned(Id, categoryId, DateTimeOffset.UtcNow));
    }

    /// <summary>Sets or clears the product's group.</summary>
    public void SetGroup(ProductGroupId? groupId)
    {
        if (GroupId == groupId) return;

        GroupId = groupId;
        AddDomainEvent(new ProductGroupAssigned(Id, groupId, DateTimeOffset.UtcNow));
    }

    /// <summary>Sets or clears the product's brand.</summary>
    public void SetBrand(BrandId? brandId)
    {
        if (BrandId == brandId) return;

        BrandId = brandId;
        AddDomainEvent(new ProductBrandAssigned(Id, brandId, DateTimeOffset.UtcNow));
    }

    /// <summary>Changes the product's tax configuration.</summary>
    public void SetTaxConfiguration(TaxConfiguration taxConfiguration)
    {
        ArgumentNullException.ThrowIfNull(taxConfiguration);
        if (TaxConfiguration == taxConfiguration) return;

        TaxConfiguration = taxConfiguration;
        AddDomainEvent(new ProductTaxConfigurationChanged(Id, taxConfiguration, DateTimeOffset.UtcNow));
    }

    /// <summary>Activates the product.</summary>
    /// <exception cref="CatalogDomainException">The product is already active.</exception>
    public void Activate()
    {
        if (Status == CatalogStatus.Active)
            throw CatalogDomainException.ProductAlreadyActive(Id);

        Status = CatalogStatus.Active;
        AddDomainEvent(new ProductActivated(Id, DateTimeOffset.UtcNow));
    }

    /// <summary>Deactivates the product.</summary>
    /// <exception cref="CatalogDomainException">The product is not active.</exception>
    public void Deactivate()
    {
        if (Status != CatalogStatus.Active)
            throw CatalogDomainException.ProductNotActive(Id);

        Status = CatalogStatus.Inactive;
        AddDomainEvent(new ProductDeactivated(Id, DateTimeOffset.UtcNow));
    }
}
