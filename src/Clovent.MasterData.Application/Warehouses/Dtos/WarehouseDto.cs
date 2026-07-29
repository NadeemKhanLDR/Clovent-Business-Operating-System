using Clovent.MasterData.Warehouses;

namespace Clovent.MasterData.Application.Warehouses.Dtos;

/// <summary>Read-model shape for a <see cref="Warehouse"/>, safe to cross a process boundary.</summary>
public sealed record WarehouseDto(
    Guid WarehouseId,
    Guid BranchId,
    string Name,
    string Code,
    string Status,
    DateTimeOffset CreatedAtUtc)
{
    /// <summary>Projects a domain <see cref="Warehouse"/> into its DTO.</summary>
    public static WarehouseDto FromDomain(Warehouse warehouse) => new(
        warehouse.Id.Value,
        warehouse.BranchId.Value,
        warehouse.Name.Value,
        warehouse.Code.Value,
        warehouse.Status.ToString(),
        warehouse.CreatedAtUtc);
}
