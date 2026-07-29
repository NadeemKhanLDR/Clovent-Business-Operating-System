using Clovent.Restaurant.DiningAreas;

namespace Clovent.Restaurant.Application.DiningAreas.Dtos;

/// <summary>Read-model shape for a <see cref="DiningArea"/>, safe to cross a process boundary.</summary>
public sealed record DiningAreaDto(Guid DiningAreaId, Guid BranchId, string Name, string Status, DateTimeOffset CreatedAtUtc)
{
    /// <summary>Projects a domain <see cref="DiningArea"/> into its DTO.</summary>
    public static DiningAreaDto FromDomain(DiningArea area) => new(
        area.Id.Value, area.BranchId.Value, area.Name.Value, area.Status.ToString(), area.CreatedAtUtc);
}
