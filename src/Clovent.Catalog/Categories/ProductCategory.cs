using Clovent.Catalog.Categories.Events;
using Clovent.Catalog.Categories.ValueObjects;
using Clovent.Catalog.Shared;
using Clovent.Domain;

namespace Clovent.Catalog.Categories;

/// <summary>
/// A hierarchical grouping for <see cref="Products.Product"/>s (e.g.
/// "Beverages" -&gt; "Soft Drinks"). Self-referencing via
/// <see cref="ParentCategoryId"/> rather than a separate tree structure -
/// the same "reference by id only" shape as every other Milestone 13/14
/// parent/child relationship, just pointing at its own aggregate type.
/// </summary>
public sealed class ProductCategory : AggregateRoot<ProductCategoryId>
{
    /// <summary>The category's display name.</summary>
    public ProductCategoryName Name { get; private set; }

    /// <summary>The parent category, if this is a sub-category.</summary>
    public ProductCategoryId? ParentCategoryId { get; private set; }

    /// <summary>The category's current lifecycle state.</summary>
    public CatalogStatus Status { get; private set; }

    /// <summary>UTC instant this category was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private ProductCategory(ProductCategoryId id, ProductCategoryName name, ProductCategoryId? parentCategoryId, CatalogStatus status, DateTimeOffset createdAtUtc)
    {
        Id = id;
        Name = name;
        ParentCategoryId = parentCategoryId;
        Status = status;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Creates a new, active category.</summary>
    /// <exception cref="CatalogDomainException"><paramref name="parentCategoryId"/> would make the category its own parent - impossible for a brand-new id, kept for symmetry with <see cref="SetParent"/>.</exception>
    public static ProductCategory Create(ProductCategoryName name, ProductCategoryId? parentCategoryId = null)
    {
        ArgumentNullException.ThrowIfNull(name);

        var now = DateTimeOffset.UtcNow;
        var category = new ProductCategory(ProductCategoryId.New(), name, parentCategoryId, CatalogStatus.Active, now);
        category.AddDomainEvent(new ProductCategoryCreated(category.Id, category.Name, now));
        return category;
    }

    /// <summary>Renames the category. A no-op (no event raised) if unchanged.</summary>
    public void Rename(ProductCategoryName name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (Name == name) return;

        Name = name;
        AddDomainEvent(new ProductCategoryRenamed(Id, name, DateTimeOffset.UtcNow));
    }

    /// <summary>Sets or clears the parent category.</summary>
    /// <exception cref="CatalogDomainException">The category would become its own parent.</exception>
    public void SetParent(ProductCategoryId? parentCategoryId)
    {
        if (parentCategoryId == Id)
            throw CatalogDomainException.CategoryCannotBeOwnParent(Id);

        if (ParentCategoryId == parentCategoryId) return;

        ParentCategoryId = parentCategoryId;
        AddDomainEvent(new ProductCategoryParentChanged(Id, parentCategoryId, DateTimeOffset.UtcNow));
    }

    /// <summary>Activates the category.</summary>
    /// <exception cref="CatalogDomainException">The category is already active.</exception>
    public void Activate()
    {
        if (Status == CatalogStatus.Active)
            throw CatalogDomainException.CategoryAlreadyActive(Id);

        Status = CatalogStatus.Active;
        AddDomainEvent(new ProductCategoryActivated(Id, DateTimeOffset.UtcNow));
    }

    /// <summary>Deactivates the category.</summary>
    /// <exception cref="CatalogDomainException">The category is not active.</exception>
    public void Deactivate()
    {
        if (Status != CatalogStatus.Active)
            throw CatalogDomainException.CategoryNotActive(Id);

        Status = CatalogStatus.Inactive;
        AddDomainEvent(new ProductCategoryDeactivated(Id, DateTimeOffset.UtcNow));
    }
}
