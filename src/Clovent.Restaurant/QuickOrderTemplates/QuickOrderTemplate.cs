using Clovent.Catalog.Variants;
using Clovent.Domain;

namespace Clovent.Restaurant.QuickOrderTemplates;

/// <summary>
/// A named, repeatable basket (the "Breakfast Combo" / "Table of 4 usual"
/// one-tap order button) - a list of variant/quantity pairs with optional
/// per-template price overrides, ordered on screen by
/// <see cref="DisplayOrder"/>. Applying a template simply replays its items
/// through the ordinary <c>AddOrderLineCommand</c> flow at the POS, so this
/// aggregate deliberately holds nothing but the recipe.
/// </summary>
public sealed class QuickOrderTemplate : AggregateRoot<QuickOrderTemplateId>
{
    private readonly List<QuickOrderTemplateItem> _items;
    /// <summary>Optional warehouse scope; null preserves legacy globally available templates.</summary>
    public Guid? WarehouseId { get; private set; }
    public void ScopeToWarehouse(Guid? warehouseId) => WarehouseId = warehouseId;

    /// <summary>The template's display name (e.g. "Breakfast Combo").</summary>
    public string Name { get; private set; }

    /// <summary>Optional free-text description shown alongside the template button.</summary>
    public string? Description { get; private set; }

    /// <summary>Whether this template currently appears on the POS quick-order bar.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Lower values appear first on the quick-order bar.</summary>
    public int DisplayOrder { get; private set; }

    /// <summary>UTC instant this template was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>UTC instant this template was last changed.</summary>
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>The lines replayed when this template is applied.</summary>
    public IReadOnlyCollection<QuickOrderTemplateItem> Items => _items.AsReadOnly();

    /// <summary>
    /// Constructor for EF Core persistence. Deliberately has no
    /// <c>items</c> parameter: EF Core cannot bind navigation properties
    /// through constructor parameters - the <see cref="Items"/> navigation is
    /// populated through the <c>_items</c> backing field instead (see
    /// <c>QuickOrderTemplateConfiguration</c>'s <c>UsePropertyAccessMode(Field)</c>).
    /// </summary>
    private QuickOrderTemplate(
        QuickOrderTemplateId id,
        string name,
        string? description,
        bool isActive,
        int displayOrder,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        Name = name;
        Description = description;
        IsActive = isActive;
        DisplayOrder = displayOrder;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        _items = [];
    }

    /// <summary>Creates a new, active, empty quick-order template.</summary>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="displayOrder"/> is negative.</exception>
    public static QuickOrderTemplate Create(string name, string? description = null, int displayOrder = 0)
    {
        RequireValidName(name);
        RequireValidDisplayOrder(displayOrder);

        var now = DateTimeOffset.UtcNow;
        return new QuickOrderTemplate(QuickOrderTemplateId.New(), name.Trim(), description?.Trim(), true, displayOrder, now, now);
    }

    /// <summary>Updates the template's header fields (items are managed via <see cref="AddItem"/>/<see cref="RemoveItem"/>).</summary>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="displayOrder"/> is negative.</exception>
    public void Update(string name, string? description = null, int displayOrder = 0)
    {
        RequireValidName(name);
        RequireValidDisplayOrder(displayOrder);

        Name = name.Trim();
        Description = description?.Trim();
        DisplayOrder = displayOrder;
        Touch();
    }

    /// <summary>
    /// Adds an item to the template. Adding the same variant twice sums the
    /// quantities onto the existing item (keeping the first item's price
    /// override when both specify one) rather than leaving two rows that
    /// would race each other at expansion time.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="quantity"/> is not positive, or <paramref name="templateUnitPrice"/> is negative.</exception>
    public void AddItem(ProductVariantId variantId, decimal quantity, decimal? templateUnitPrice = null)
    {
        var existing = _items.FirstOrDefault(i => i.VariantId == variantId);
        if (existing is not null)
        {
            existing.AddQuantity(quantity);
            Touch();
            return;
        }

        _items.Add(QuickOrderTemplateItem.Create(Id, variantId, quantity, templateUnitPrice));
        Touch();
    }

    /// <summary>Removes the item for the given variant. A no-op if the template has no such item.</summary>
    public void RemoveItem(ProductVariantId variantId)
    {
        var existing = _items.FirstOrDefault(i => i.VariantId == variantId);
        if (existing is null) return;

        _items.Remove(existing);
        Touch();
    }

    /// <summary>Activates or deactivates the template.</summary>
    public void SetStatus(bool isActive)
    {
        if (IsActive == isActive) return;
        IsActive = isActive;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;

    private static void RequireValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Template name is required.", nameof(name));
    }

    private static void RequireValidDisplayOrder(int displayOrder)
    {
        if (displayOrder < 0)
            throw new ArgumentOutOfRangeException(nameof(displayOrder), displayOrder, "Display order cannot be negative.");
    }
}

