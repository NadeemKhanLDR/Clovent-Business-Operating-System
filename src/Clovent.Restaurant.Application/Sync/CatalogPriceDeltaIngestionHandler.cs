using System.Text.Json;
using Clovent.Catalog.Prices;
using Clovent.Catalog.Variants;
using Clovent.MasterData.Currencies;
using Clovent.Platform.Sync;
using Clovent.Restaurant.Application.Outbox.Dtos;
using Microsoft.Extensions.Logging;

namespace Clovent.Restaurant.Application.Sync;

/// <summary>
/// Ingestion handler for <see cref="SyncEntityKinds.CatalogPriceAdjustment"/>.
/// Enforces concurrency token verification, preserves currency and effective dates,
/// stages conflicting price edits into the manager-review staging table,
/// and rechecks on approval so a stale approval cannot overwrite a newer edit.
/// </summary>
public sealed class CatalogPriceDeltaIngestionHandler : ISyncIngestionHandler
{
    private readonly IProductPriceRepository _priceRepository;
    private readonly ISyncConflictStagingStore _conflictStagingStore;
    private readonly Clovent.Catalog.Application.IUnitOfWork? _unitOfWork;
    private readonly ILogger<CatalogPriceDeltaIngestionHandler> _logger;

    /// <summary>Creates a new catalog price ingestion handler.</summary>
    public CatalogPriceDeltaIngestionHandler(
        IProductPriceRepository priceRepository,
        ISyncConflictStagingStore conflictStagingStore,
        ILogger<CatalogPriceDeltaIngestionHandler> logger,
        Clovent.Catalog.Application.IUnitOfWork? unitOfWork = null)
    {
        _priceRepository = priceRepository;
        _conflictStagingStore = conflictStagingStore;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc/>
    public string EntityKind => SyncEntityKinds.CatalogPriceAdjustment;

    /// <inheritdoc/>
    public async Task<SyncIngestionResult> IngestAsync(SyncPacket packet, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(packet);

        var payload = packet.DeserializePayload<CatalogPriceAdjustmentPayload>();
        if (payload == null)
        {
            return SyncIngestionResult.Failed(packet, "Failed to deserialize CatalogPriceAdjustmentPayload.");
        }

        var variantId = new ProductVariantId(payload.ProductVariantId);
        var existingPrices = await _priceRepository.GetByProductVariantIdAsync(variantId, cancellationToken).ConfigureAwait(false);

        if (!Enum.TryParse<PriceType>(payload.PriceType, true, out var targetPriceType))
        {
            targetPriceType = PriceType.Selling;
        }

        var currentPrice = existingPrices.FirstOrDefault(p => p.PriceType == targetPriceType && p.Status == Catalog.Shared.CatalogStatus.Active);

        if (currentPrice != null)
        {
            var currentToken = $"{currentPrice.Amount:F2}:{currentPrice.CreatedAtUtc:o}";

            var isOverride = string.Equals(packet.ConcurrencyToken, "*", StringComparison.Ordinal) ||
                             string.Equals(payload.ConcurrencyToken, "*", StringComparison.Ordinal);

            if (isOverride)
            {
                // Recheck on manager approval:
                // If local price was modified to a different amount after the conflict was detected, reject stale approval!
                if (currentPrice.CreatedAtUtc > packet.TimestampUtc && currentPrice.Amount != payload.OldAmount)
                {
                    _logger.LogWarning("Stale manager approval rejected: active price has been modified to {CurrentAmount} since conflict occurred.",
                        currentPrice.Amount);
                    return SyncIngestionResult.Rejected(packet,
                        $"Stale manager approval rejected: active price has been modified to {currentPrice.Amount:F2} since the conflict occurred.");
                }
            }
            else if (!string.IsNullOrWhiteSpace(payload.ConcurrencyToken) &&
                     !string.Equals(payload.ConcurrencyToken, currentToken, StringComparison.Ordinal) &&
                     currentPrice.Amount != payload.OldAmount)
            {
                // Conflict detected! Two terminals modified prices concurrently.
                if (packet.TimestampUtc < currentPrice.CreatedAtUtc)
                {
                    _logger.LogWarning("Incoming price packet {PacketId} rejected as stale (older timestamp).", packet.PacketId);
                    return SyncIngestionResult.RejectedStale(packet,
                        $"Incoming price edit timestamp ({packet.TimestampUtc:o}) is older than active price record ({currentPrice.CreatedAtUtc:o}).");
                }

                // Stage in Manager-Review staging table
                var conflict = new SyncConflictRecord
                {
                    PacketId = packet.PacketId,
                    OriginalPacket = packet,
                    OriginalPacketJson = JsonSerializer.Serialize(packet),
                    EntityKind = EntityKind,
                    EntityId = payload.ProductVariantId.ToString(),
                    SourceBranchId = payload.BranchId,
                    SourceTerminalId = payload.TerminalId,
                    IncomingValue = payload.NewAmount.ToString("F2"),
                    CurrentValue = currentPrice.Amount.ToString("F2"),
                    IncomingConcurrencyToken = payload.ConcurrencyToken,
                    CurrentConcurrencyToken = currentToken,
                    ConflictType = SyncConflictType.OverlappingPriceEdit,
                    ConflictReason = $"Concurrent price adjustment conflict detected. Terminal proposed {payload.NewAmount:F2} while current is {currentPrice.Amount:F2}.",
                    DetectedAtUtc = DateTimeOffset.UtcNow
                };

                await _conflictStagingStore.StageConflictAsync(conflict, cancellationToken).ConfigureAwait(false);
                _logger.LogWarning("Concurrent price change staged for manager review: {ConflictId}.", conflict.ConflictId);

                return SyncIngestionResult.StagedForReview(packet, conflict.ConflictId, conflict.ConflictReason);
            }

            // Normal update: Apply new price
            currentPrice.UpdateAmount(payload.NewAmount);
            if (_unitOfWork != null)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("Updated price for variant {VariantId} to {NewAmount}.", payload.ProductVariantId, payload.NewAmount);
            return SyncIngestionResult.Applied(packet, $"Updated active price to {payload.NewAmount:F2}.");
        }
        else
        {
            // No existing active price record: Create one preserving currency and effective date
            var currencyId = payload.CurrencyId != default
                ? new CurrencyId(payload.CurrencyId)
                : (existingPrices.FirstOrDefault()?.CurrencyId ?? CurrencyId.New());

            var newPrice = ProductPrice.Create(variantId, targetPriceType, payload.NewAmount, currencyId, payload.EffectiveFromUtc);
            await _priceRepository.AddAsync(newPrice, cancellationToken).ConfigureAwait(false);
            if (_unitOfWork != null)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("Created new price for variant {VariantId}: {NewAmount}.", payload.ProductVariantId, payload.NewAmount);
            return SyncIngestionResult.Applied(packet, $"Created new active price {payload.NewAmount:F2}.");
        }
    }
}
