using System.Text.Json;
using Clovent.Catalog.Variants;
using Clovent.Inventory.Transactions;
using Clovent.Inventory.WarehouseStocks;
using Clovent.MasterData.Warehouses;
using Clovent.Platform.Sync;
using Clovent.Restaurant.Application.Outbox.Dtos;
using Microsoft.Extensions.Logging;

namespace Clovent.Restaurant.Application.Sync;

/// <summary>
/// Ingestion handler for <see cref="SyncEntityKinds.InventoryStockDelta"/>.
/// Applies commutative relative stock deltas to mathematically eliminate stock drift,
/// persists immutable <see cref="InventoryTransaction"/> ledger entries for all stock mutations,
/// safely handles SQL Server concurrency collisions, and durably stages overselling discrepancies.
/// </summary>
public sealed class InventoryDeltaIngestionHandler : ISyncIngestionHandler
{
    private readonly IWarehouseStockRepository _stockRepository;
    private readonly IInventoryTransactionRepository? _transactionRepository;
    private readonly ISyncConflictStagingStore _conflictStagingStore;
    private readonly Clovent.Inventory.Application.IUnitOfWork? _unitOfWork;
    private readonly ILogger<InventoryDeltaIngestionHandler> _logger;

    /// <summary>Creates a new inventory delta ingestion handler.</summary>
    public InventoryDeltaIngestionHandler(
        IWarehouseStockRepository stockRepository,
        ISyncConflictStagingStore conflictStagingStore,
        ILogger<InventoryDeltaIngestionHandler> logger,
        IInventoryTransactionRepository? transactionRepository = null,
        Clovent.Inventory.Application.IUnitOfWork? unitOfWork = null)
    {
        _stockRepository = stockRepository;
        _transactionRepository = transactionRepository;
        _conflictStagingStore = conflictStagingStore;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc/>
    public string EntityKind => SyncEntityKinds.InventoryStockDelta;

    /// <inheritdoc/>
    public async Task<SyncIngestionResult> IngestAsync(SyncPacket packet, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(packet);

        var payload = packet.DeserializePayload<InventoryDeltaSyncPayload>();
        if (payload == null)
        {
            return SyncIngestionResult.Failed(packet, "Failed to deserialize InventoryDeltaSyncPayload.");
        }

        var warehouseId = new WarehouseId(payload.WarehouseId);
        var variantId = new ProductVariantId(payload.ProductVariantId);

        // Case 1: Commutative Relative Delta (Sales, Returns, Normal Issue/Receive)
        if (string.Equals(packet.Operation, SyncOperations.Delta, StringComparison.OrdinalIgnoreCase))
        {
            const int maxRetries = 3;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var stock = await _stockRepository.GetByWarehouseAndVariantAsync(warehouseId, variantId, cancellationToken).ConfigureAwait(false);

                    if (payload.QuantityDelta < 0)
                    {
                        var absDelta = Math.Abs(payload.QuantityDelta);

                        // Overselling policy verification:
                        // If stock is missing or insufficient and negative stock is disallowed, stage for review.
                        // The completed financial transaction on the origin terminal stands, but the inventory effect is staged.
                        if (stock == null || (!stock.AllowNegativeStock && stock.QuantityOnHand - absDelta < 0))
                        {
                            var currentQty = stock?.QuantityOnHand ?? 0m;
                            var conflict = new SyncConflictRecord
                            {
                                PacketId = packet.PacketId,
                                OriginalPacket = packet,
                                OriginalPacketJson = JsonSerializer.Serialize(packet),
                                EntityKind = EntityKind,
                                EntityId = payload.ProductVariantId.ToString(),
                                SourceBranchId = payload.BranchId,
                                SourceTerminalId = payload.TerminalId,
                                IncomingValue = payload.QuantityDelta.ToString("G"),
                                CurrentValue = currentQty.ToString("G"),
                                ConflictType = SyncConflictType.NegativeStockDiscrepancy,
                                ConflictReason = $"Overselling discrepancy: Insufficient stock on hand ({currentQty}) to issue {absDelta} on variant {payload.ProductVariantId}. Retaining completed sale with pending inventory discrepancy.",
                                DetectedAtUtc = DateTimeOffset.UtcNow
                            };

                            await _conflictStagingStore.StageConflictAsync(conflict, cancellationToken).ConfigureAwait(false);
                            _logger.LogWarning("Overselling discrepancy staged for manager review for packet {PacketId}: {Reason}",
                                packet.PacketId, conflict.ConflictReason);

                            return SyncIngestionResult.StagedForReview(packet, conflict.ConflictId, conflict.ConflictReason);
                        }

                        stock.Issue(absDelta);

                        if (_transactionRepository != null)
                        {
                            var tx = InventoryTransaction.Create(
                                warehouseId,
                                variantId,
                                InventoryTransactionType.Issue,
                                absDelta,
                                referenceType: "DeltaSync",
                                referenceId: packet.PacketId,
                                notes: $"Replicated stock issue from Terminal {payload.TerminalId}");

                            await _transactionRepository.AddAsync(tx, cancellationToken).ConfigureAwait(false);
                        }

                        _logger.LogInformation("Applied incoming negative stock delta (-{Delta}) on variant {VariantId}. Balance: {Balance}.",
                            absDelta, payload.ProductVariantId, stock.QuantityOnHand);
                    }
                    else if (payload.QuantityDelta > 0)
                    {
                        if (stock == null)
                        {
                            stock = WarehouseStock.Create(warehouseId, variantId, minimumStock: 0, maximumStock: 0, allowNegativeStock: false);
                            await _stockRepository.AddAsync(stock, cancellationToken).ConfigureAwait(false);
                        }

                        stock.Receive(payload.QuantityDelta);

                        if (_transactionRepository != null)
                        {
                            var tx = InventoryTransaction.Create(
                                warehouseId,
                                variantId,
                                InventoryTransactionType.Receipt,
                                payload.QuantityDelta,
                                referenceType: "DeltaSync",
                                referenceId: packet.PacketId,
                                notes: $"Replicated stock receipt from Terminal {payload.TerminalId}");

                            await _transactionRepository.AddAsync(tx, cancellationToken).ConfigureAwait(false);
                        }

                        _logger.LogInformation("Applied incoming positive stock delta (+{Delta}) on variant {VariantId}. Balance: {Balance}.",
                            payload.QuantityDelta, payload.ProductVariantId, stock.QuantityOnHand);
                    }

                    if (_unitOfWork != null)
                    {
                        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    }

                    var finalQty = stock?.QuantityOnHand ?? 0m;
                    return SyncIngestionResult.Applied(packet, $"Stock delta {payload.QuantityDelta} applied with immutable ledger movement. Balance: {finalQty}.");
                }
                catch (Exception ex) when (ex.GetType().Name.Contains("Concurrency") && attempt < maxRetries)
                {
                    _logger.LogWarning("Optimistic concurrency collision on stock variant {VariantId}. Retrying attempt {Attempt} of {Max}.",
                        payload.ProductVariantId, attempt, maxRetries);
                    await Task.Delay(25 * attempt, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        // Case 2: Absolute Stock Snapshot / Adjustment (Physical Inventory Count Override)
        if (string.Equals(packet.Operation, SyncOperations.Snapshot, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(packet.Operation, SyncOperations.Adjustment, StringComparison.OrdinalIgnoreCase))
        {
            var stock = await _stockRepository.GetByWarehouseAndVariantAsync(warehouseId, variantId, cancellationToken).ConfigureAwait(false);

            if (stock != null && !string.IsNullOrWhiteSpace(payload.ConcurrencyToken))
            {
                var currentToken = stock.UpdatedAtUtc.ToString("o");
                bool isOverride = string.Equals(packet.ConcurrencyToken, "*", StringComparison.Ordinal) ||
                                  string.Equals(payload.ConcurrencyToken, "*", StringComparison.Ordinal);
                bool tokenMatches = isOverride ||
                                    string.Equals(payload.ConcurrencyToken, currentToken, StringComparison.Ordinal) ||
                                    string.Equals(payload.ConcurrencyToken, stock.Id.Value.ToString(), StringComparison.Ordinal);

                if (!tokenMatches)
                {
                    if (packet.TimestampUtc <= stock.UpdatedAtUtc)
                    {
                        return SyncIngestionResult.RejectedStale(packet,
                            $"Incoming stock adjustment timestamp ({packet.TimestampUtc:o}) is older than current updated state ({stock.UpdatedAtUtc:o}).");
                    }

                    var conflict = new SyncConflictRecord
                    {
                        PacketId = packet.PacketId,
                        OriginalPacket = packet,
                        OriginalPacketJson = JsonSerializer.Serialize(packet),
                        EntityKind = EntityKind,
                        EntityId = payload.ProductVariantId.ToString(),
                        SourceBranchId = payload.BranchId,
                        SourceTerminalId = payload.TerminalId,
                        IncomingValue = payload.QuantityDelta.ToString("G"),
                        CurrentValue = stock.QuantityOnHand.ToString("G"),
                        IncomingConcurrencyToken = payload.ConcurrencyToken,
                        CurrentConcurrencyToken = currentToken,
                        ConflictType = SyncConflictType.ConcurrencyTokenMismatch,
                        ConflictReason = "Concurrent terminal stock adjustment detected with diverging concurrency token.",
                        DetectedAtUtc = DateTimeOffset.UtcNow
                    };

                    await _conflictStagingStore.StageConflictAsync(conflict, cancellationToken).ConfigureAwait(false);
                    return SyncIngestionResult.StagedForReview(packet, conflict.ConflictId, conflict.ConflictReason);
                }
            }

            if (stock == null)
            {
                stock = WarehouseStock.Create(warehouseId, variantId, minimumStock: 0, maximumStock: 0, allowNegativeStock: false);
                await _stockRepository.AddAsync(stock, cancellationToken).ConfigureAwait(false);
            }

            var difference = payload.QuantityDelta - stock.QuantityOnHand;
            if (difference > 0)
            {
                stock.Receive(difference);
                if (_transactionRepository != null)
                {
                    var tx = InventoryTransaction.Create(warehouseId, variantId, InventoryTransactionType.Adjustment, difference, "DeltaSyncAdjustment", packet.PacketId, "Replicated stock adjustment increase");
                    await _transactionRepository.AddAsync(tx, cancellationToken).ConfigureAwait(false);
                }
            }
            else if (difference < 0)
            {
                var absDiff = Math.Abs(difference);
                stock.Issue(absDiff);
                if (_transactionRepository != null)
                {
                    var tx = InventoryTransaction.Create(warehouseId, variantId, InventoryTransactionType.Adjustment, absDiff, "DeltaSyncAdjustment", packet.PacketId, "Replicated stock adjustment decrease");
                    await _transactionRepository.AddAsync(tx, cancellationToken).ConfigureAwait(false);
                }
            }

            if (_unitOfWork != null)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            return SyncIngestionResult.Applied(packet, $"Stock level set to {payload.QuantityDelta} with ledger movement.");
        }

        return SyncIngestionResult.Failed(packet, $"Unsupported operation '{packet.Operation}' for inventory delta.");
    }
}
