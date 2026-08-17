using Clovent.Catalog.Categories;

namespace Clovent.Catalog.Application.Categories.Dtos;

/// <summary>Read-model shape for a <see cref="ProductCategory"/>, safe to cross a process boundary.</summary>
public sealed record ProductCategoryDto(
    Guid ProductCategoryId,
    string Name,
    Guid? ParentCategoryId,
    string Status,
    string? ColorHex,
    int SortOrder,
    DateTimeOffset CreatedAtUtc)
{
    /// <summary>Projects a domain <see cref="ProductCategory"/> into its DTO.</summary>
    public static ProductCategoryDto FromDomain(ProductCategory category) => new(
        category.Id.Value,
        category.Name.Value,
        category.ParentCategoryId?.Value,
        category.Status.ToString(),
        category.ColorHex,
        category.SortOrder,
        category.CreatedAtUtc);
}
