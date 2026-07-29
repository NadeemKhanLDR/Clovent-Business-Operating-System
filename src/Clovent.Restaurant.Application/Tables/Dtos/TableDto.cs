using Clovent.Restaurant.Tables;

namespace Clovent.Restaurant.Application.Tables.Dtos;

/// <summary>Read-model shape for a <see cref="Table"/>, safe to cross a process boundary.</summary>
public sealed record TableDto(
    Guid TableId,
    Guid DiningAreaId,
    string Code,
    int Capacity,
    string Status,
    string OccupancyStatus,
    DateTimeOffset CreatedAtUtc)
{
    /// <summary>Projects a domain <see cref="Table"/> into its DTO.</summary>
    public static TableDto FromDomain(Table table) => new(
        table.Id.Value,
        table.DiningAreaId.Value,
        table.Code.Value,
        table.Capacity,
        table.Status.ToString(),
        table.OccupancyStatus.ToString(),
        table.CreatedAtUtc);
}
