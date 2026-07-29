using Clovent.Inventory.Transactions;

namespace Clovent.Inventory.Application.Transactions.Dtos;

/// <summary>Read-model shape for an <see cref="InventoryTransaction"/> ledger entry, safe to cross a process boundary.</summary>
public sealed record InventoryTransactionDto(
    Guid InventoryTransactionId,
    Guid WarehouseId,
    Guid ProductVariantId,
    string TransactionType,
    decimal Quantity,
    string? ReferenceType,
    Guid? ReferenceId,
    string? Notes,
    DateTimeOffset OccurredAtUtc)
{
    /// <summary>Projects a domain <see cref="InventoryTransaction"/> into its DTO.</summary>
    public static InventoryTransactionDto FromDomain(InventoryTransaction transaction) => new(
        transaction.Id.Value,
        transaction.WarehouseId.Value,
        transaction.ProductVariantId.Value,
        transaction.TransactionType.ToString(),
        transaction.Quantity,
        transaction.ReferenceType,
        transaction.ReferenceId,
        transaction.Notes,
        transaction.OccurredAtUtc);
}
