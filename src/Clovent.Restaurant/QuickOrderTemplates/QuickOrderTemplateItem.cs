using Clovent.Catalog.Variants;
using Clovent.Domain;

namespace Clovent.Restaurant.QuickOrderTemplates;

/// <summary>
/// One line of a <see cref="QuickOrderTemplate"/> - a quantity of one
/// <see cref="ProductVariantId"/> with an optional fixed unit price. When
/// <see cref="TemplateUnitPrice"/> is <see langword="null"/>, expanding the
/// template charges the variant's current catalog selling price; when set,
/// that override wins (the "Rs. 500 lunch combo, whatever the menu says"
/// case).
/// </summary>
public sealed class QuickOrderTemplateItem : Entity<QuickOrderTemplateItemId>
{
    /// <summary>The template this item belongs to, fixed at creation.</summary>
    public QuickOrderTemplateId TemplateId { get; }

    /// <summary>The variant ordered when the template is applied.</summary>
    public ProductVariantId VariantId { get; private set; }

    /// <summary>The quantity ordered.</summary>
    public decimal Quantity { get; private set; }

    /// <summary>Optional per-template unit-price override; <see langword="null"/> resolves the current selling price at expansion time.</summary>
    public decimal? TemplateUnitPrice { get; private set; }

    /// <summary>Constructor for EF Core persistence.</summary>
    private QuickOrderTemplateItem(
        QuickOrderTemplateItemId id,
        QuickOrderTemplateId templateId,
        ProductVariantId variantId,
        decimal quantity,
        decimal? templateUnitPrice)
    {
        Id = id;
        TemplateId = templateId;
        VariantId = variantId;
        Quantity = quantity;
        TemplateUnitPrice = templateUnitPrice;
    }

    /// <summary>Creates a new template item.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="quantity"/> is not positive, or <paramref name="templateUnitPrice"/> is negative.</exception>
    public static QuickOrderTemplateItem Create(QuickOrderTemplateId templateId, ProductVariantId variantId, decimal quantity, decimal? templateUnitPrice = null)
    {
        RequirePositiveQuantity(quantity);
        RequireNonNegativePrice(templateUnitPrice);

        return new QuickOrderTemplateItem(QuickOrderTemplateItemId.New(), templateId, variantId, quantity, templateUnitPrice);
    }

    /// <summary>Adds to this item's quantity - used by <see cref="QuickOrderTemplate.AddItem"/> to merge a duplicate variant into one row.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The resulting quantity is not positive.</exception>
    public void AddQuantity(decimal quantity)
    {
        var updated = Quantity + quantity;
        RequirePositiveQuantity(updated);
        Quantity = updated;
    }

    private static void RequirePositiveQuantity(decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be positive.");
    }

    private static void RequireNonNegativePrice(decimal? templateUnitPrice)
    {
        if (templateUnitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(templateUnitPrice), templateUnitPrice, "Template unit price cannot be negative.");
    }
}
