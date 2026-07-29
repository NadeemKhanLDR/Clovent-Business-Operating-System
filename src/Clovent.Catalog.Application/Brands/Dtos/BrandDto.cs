using Clovent.Catalog.Brands;

namespace Clovent.Catalog.Application.Brands.Dtos;

/// <summary>Read-model shape for a <see cref="Brand"/>, safe to cross a process boundary.</summary>
public sealed record BrandDto(Guid BrandId, string Name, string Status, DateTimeOffset CreatedAtUtc)
{
    /// <summary>Projects a domain <see cref="Brand"/> into its DTO.</summary>
    public static BrandDto FromDomain(Brand brand) => new(brand.Id.Value, brand.Name.Value, brand.Status.ToString(), brand.CreatedAtUtc);
}
