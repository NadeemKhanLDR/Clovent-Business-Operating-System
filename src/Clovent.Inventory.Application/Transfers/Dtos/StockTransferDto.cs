using Clovent.Inventory.Transfers;

namespace Clovent.Inventory.Application.Transfers.Dtos;

/// <summary>Read-model shape for a <see cref="StockTransfer"/>, safe to cross a process boundary.</summary>
public sealed record StockTransferDto(
    Guid StockTransferId,
    Guid SourceWarehouseId,
    Guid DestinationWarehouseId,
    Guid ProductVariantId,
    decimal Quantity,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc)
{
    /// <summary>Projects a domain <see cref="StockTransfer"/> into its DTO.</summary>
    public static StockTransferDto FromDomain(StockTransfer transfer) => new(
        transfer.Id.Value,
        transfer.SourceWarehouseId.Value,
        transfer.DestinationWarehouseId.Value,
        transfer.ProductVariantId.Value,
        transfer.Quantity,
        transfer.Status.ToString(),
        transfer.CreatedAtUtc,
        transfer.CompletedAtUtc);
}
