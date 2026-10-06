namespace Clovent.Desktop.Restaurant.Services;

/// <summary>Represents a single line item in an active cart crash-recovery checkpoint.</summary>
public sealed record CartCheckpointLine(
    Guid ProductVariantId,
    string Sku,
    string Name,
    string PortionName,
    decimal Quantity,
    decimal UnitPrice,
    string? Notes);

/// <summary>Snapshot of an in-progress cart preserved to protect against sudden terminal power loss or crashes.</summary>
public sealed record CartCheckpoint(
    Guid CheckpointId,
    string TerminalId,
    Guid? CashierId,
    string? CashierName,
    string OrderType,
    Guid? TableId,
    string? TableCode,
    DateTimeOffset SavedAtUtc,
    IReadOnlyList<CartCheckpointLine> Lines,
    string? Notes,
    string? CustomerNotes);

/// <summary>Local durable storage for preserving uncommitted cart items across application crashes.</summary>
public interface IActiveOrderCheckpointStore
{
    /// <summary>Asynchronously saves or updates the active cart checkpoint.</summary>
    Task SaveCheckpointAsync(CartCheckpoint checkpoint, CancellationToken cancellationToken = default);

    /// <summary>Loads the most recent active cart checkpoint for this terminal, if any exists.</summary>
    Task<CartCheckpoint?> LoadCheckpointAsync(string terminalId, CancellationToken cancellationToken = default);

    /// <summary>Clears the active cart checkpoint when an order is successfully committed or intentionally discarded.</summary>
    Task ClearCheckpointAsync(string terminalId, CancellationToken cancellationToken = default);
}
