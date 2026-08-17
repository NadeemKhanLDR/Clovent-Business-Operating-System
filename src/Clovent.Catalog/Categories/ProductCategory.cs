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

    /// <summary>
    /// Optional display color as a 6-digit hex string ("#RRGGBB"), e.g. for
    /// the Restaurant POS category rail's owner-configurable color-coding -
    /// purely presentational, never used for filtering/matching.
    /// <see langword="null"/> means "no color chosen, use the screen's
    /// default".
    /// </summary>
    public string? ColorHex { get; private set; }

    /// <summary>Manual display position for owner-driven drag-drop reordering (lower sorts first). Defaults to 0 - every category starts equally-ordered until an owner reorders them.</summary>
    public int SortOrder { get; private set; }

    /// <summary>UTC instant this category was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private ProductCategory(ProductCategoryId id, ProductCategoryName name, ProductCategoryId? parentCategoryId, CatalogStatus status, string? colorHex, int sortOrder, DateTimeOffset createdAtUtc)
    {
        Id = id;
        Name = name;
        ParentCategoryId = parentCategoryId;
        Status = status;
        ColorHex = colorHex;
        SortOrder = sortOrder;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Creates a new, active category.</summary>
    /// <exception cref="CatalogDomainException"><paramref name="parentCategoryId"/> would make the category its own parent - impossible for a brand-new id, kept for symmetry with <see cref="SetParent"/>.</exception>
    public static ProductCategory Create(ProductCategoryName name, ProductCategoryId? parentCategoryId = null)
    {
        ArgumentNullException.ThrowIfNull(name);

        var now = DateTimeOffset.UtcNow;
        var category = new ProductCategory(ProductCategoryId.New(), name, parentCategoryId, CatalogStatus.Active, null, 0, now);
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

    /// <summary>Sets or clears this category's display color. A no-op (no event raised) if unchanged.</summary>
    /// <exception cref="CatalogDomainException"><paramref name="colorHex"/> is not <see langword="null"/> and not a valid "#RRGGBB" hex color.</exception>
    public void SetColor(string? colorHex)
    {
        if (colorHex is not null && !IsValidHexColor(colorHex))
            throw CatalogDomainException.InvalidCategoryColor(Id);

        if (ColorHex == colorHex) return;

        ColorHex = colorHex;
        AddDomainEvent(new ProductCategoryColorChanged(Id, colorHex, DateTimeOffset.UtcNow));
    }

    /// <summary>Sets this category's manual display position (owner-driven drag-drop reordering). A no-op (no event raised) if unchanged.</summary>
    public void SetSortOrder(int sortOrder)
    {
        if (SortOrder == sortOrder) return;

        SortOrder = sortOrder;
        AddDomainEvent(new ProductCategorySortOrderChanged(Id, sortOrder, DateTimeOffset.UtcNow));
    }

    private static bool IsValidHexColor(string value) =>
        value.Length == 7 && value[0] == '#' && value[1..].All(Uri.IsHexDigit);

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
