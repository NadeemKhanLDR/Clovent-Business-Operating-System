using Clovent.Catalog.Groups;

namespace Clovent.Catalog.Application.Groups.Dtos;

/// <summary>Read-model shape for a <see cref="ProductGroup"/>, safe to cross a process boundary.</summary>
public sealed record ProductGroupDto(Guid ProductGroupId, string Name, string Status, DateTimeOffset CreatedAtUtc)
{
    /// <summary>Projects a domain <see cref="ProductGroup"/> into its DTO.</summary>
    public static ProductGroupDto FromDomain(ProductGroup group) => new(
        group.Id.Value, group.Name.Value, group.Status.ToString(), group.CreatedAtUtc);
}
