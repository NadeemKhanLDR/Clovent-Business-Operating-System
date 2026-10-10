using System.Text.Json;
using Clovent.Catalog.Prices;
using Clovent.Catalog.Shared;
using Clovent.Catalog.Variants;
using Clovent.Inventory.WarehouseStocks;
using Clovent.MasterData.Currencies;
using Clovent.MasterData.Warehouses;
using Clovent.Platform.CircuitBreakers;
using Clovent.Platform.Sync;
using Clovent.Restaurant.Application.Outbox;
using Clovent.Restaurant.Application.Outbox.Dtos;
using Clovent.Restaurant.Application.Outbox.Handlers;
using Clovent.Restaurant.Application.Sync;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.Outbox;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Sync;

/// <summary>
/// Comprehensive integration testing suite simulating multi-terminal concurrent offline
/// retail sales transactions and subsequent automated delta-sync reconciliation without
/// stock or pricing drift.
/// </summary>
public sealed class MultiTerminalDeltaSyncIntegrationTests
{
    private readonly WarehouseId _warehouseId = new(Guid.NewGuid());
    private readonly ProductVariantId _variantId = new(Guid.NewGuid());
    private readonly CurrencyId _currencyId = CurrencyId.New();
    private readonly Guid _branchId = Guid.NewGuid();
    private readonly Guid _terminal1Id = Guid.NewGuid();
    private readonly Guid _terminal2Id = Guid.NewGuid();

    private readonly InMemoryWarehouseStockRepository _stockRepository = new();
    private readonly InMemoryProductPriceRepository _priceRepository = new();
    private readonly InMemoryShiftSyncRegistry _shiftRegistry = new();
    private readonly InMemorySyncIdempotencyStore _idempotencyStore = new();
    private readonly InMemorySyncConflictStagingStore _conflictStagingStore = new();
    private readonly NetworkConnectivityProbe _connectivityProbe = new(initialConnected: false);
    private readonly InMemoryDeltaSyncTransport _transport = new();
    private readonly CircuitBreakerRegistry _cbRegistry = new();

    private readonly DeltaSyncDispatcher _dispatcher;
    private readonly SyncIngestionEngine _ingestionEngine;

    public MultiTerminalDeltaSyncIntegrationTests()
    {
        _dispatcher = new DeltaSyncDispatcher(
            _transport,
            _connectivityProbe,
            _cbRegistry,
            NullLogger<DeltaSyncDispatcher>.Instance);

        var inventoryHandler = new InventoryDeltaIngestionHandler(
            _stockRepository,
            _conflictStagingStore,
            NullLogger<InventoryDeltaIngestionHandler>.Instance);

        var priceHandler = new CatalogPriceDeltaIngestionHandler(
            _priceRepository,
            _conflictStagingStore,
            NullLogger<CatalogPriceDeltaIngestionHandler>.Instance);

        var shiftHandler = new ShiftSummaryDeltaIngestionHandler(
            _shiftRegistry,
            _conflictStagingStore,
            NullLogger<ShiftSummaryDeltaIngestionHandler>.Instance);

        _ingestionEngine = new SyncIngestionEngine(
            [inventoryHandler, priceHandler, shiftHandler],
            _idempotencyStore,
            NullLogger<SyncIngestionEngine>.Instance,
            _conflictStagingStore);
    }

    [Fact]
    public async Task Scenario1_MultiTerminalOfflineSales_CommutativeDeltaReconciliation_ZeroStockDrift()
    {
        // 1. Arrange Baseline: Central store warehouse starts with 100 units of Burger
        var initialStock = WarehouseStock.Create(_warehouseId, _variantId, minimumStock: 10, maximumStock: 200, allowNegativeStock: true);
        initialStock.Receive(100m);
        _stockRepository.Add(initialStock);

        // Terminals are completely offline (simulating WAN disconnect / local-first standalone operation)
        _connectivityProbe.SetConnected(false);

        // 2. Terminal 1 conducts two offline sales: -5 units and -3 units
        var t1Sale1Payload = new InventoryDeltaSyncPayload(
            _terminal1Id, _branchId, _warehouseId.Value, _variantId.Value, "BURGER-01",
            -5m, SyncOperations.Delta, null, DateTimeOffset.UtcNow);

        var t1Sale2Payload = new InventoryDeltaSyncPayload(
            _terminal1Id, _branchId, _warehouseId.Value, _variantId.Value, "BURGER-01",
            -3m, SyncOperations.Delta, null, DateTimeOffset.UtcNow);

        var t1Msg1 = OutboxMessage.Create(
            OutboxMessageType.InventoryDeltaSync, "WarehouseStock", _variantId.Value.ToString(), "ORD-T1-1",
            JsonSerializer.Serialize(t1Sale1Payload), $"inv-t1-1:{_terminal1Id}");
        var t1Msg2 = OutboxMessage.Create(
            OutboxMessageType.InventoryDeltaSync, "WarehouseStock", _variantId.Value.ToString(), "ORD-T1-2",
            JsonSerializer.Serialize(t1Sale2Payload), $"inv-t1-2:{_terminal1Id}");

        // 3. Terminal 2 conducts an offline sale: -7 units
        var t2SalePayload = new InventoryDeltaSyncPayload(
            _terminal2Id, _branchId, _warehouseId.Value, _variantId.Value, "BURGER-01",
            -7m, SyncOperations.Delta, null, DateTimeOffset.UtcNow);

        var t2Msg = OutboxMessage.Create(
            OutboxMessageType.InventoryDeltaSync, "WarehouseStock", _variantId.Value.ToString(), "ORD-T2-1",
            JsonSerializer.Serialize(t2SalePayload), $"inv-t2-1:{_terminal2Id}");

        // 4. Offline Outbox Dispatch attempt: Both terminals attempt push, which must throw NetworkOfflineException
        // and safely defer without consuming retry budget or dead-lettering
        var invOutboxHandler = new InventoryDeltaSyncOutboxHandler(_dispatcher, NullLogger<InventoryDeltaSyncOutboxHandler>.Instance);

        t1Msg1.ClaimForProcessing();
        var offlineEx1 = await Assert.ThrowsAsync<NetworkOfflineException>(() => invOutboxHandler.HandleAsync(t1Msg1, CancellationToken.None));
        Assert.Contains("offline", offlineEx1.Message, StringComparison.OrdinalIgnoreCase);
        t1Msg1.DeferForOffline(DateTimeOffset.UtcNow.AddSeconds(5), offlineEx1.Message);

        Assert.Equal(OutboxMessageStatus.RetryScheduled, t1Msg1.Status);
        Assert.Equal(0, t1Msg1.AttemptCount); // No retry penalties while offline!

        // 5. Network Connectivity Restored!
        _connectivityProbe.SetConnected(true);

        // Terminal 1 pushes packets
        var packetT1_1 = SyncPacket.Create(_branchId, _terminal1Id, SyncEntityKinds.InventoryStockDelta, _variantId.Value.ToString(), SyncOperations.Delta, t1Sale1Payload, idempotencyKey: t1Msg1.IdempotencyKey);
        var packetT1_2 = SyncPacket.Create(_branchId, _terminal1Id, SyncEntityKinds.InventoryStockDelta, _variantId.Value.ToString(), SyncOperations.Delta, t1Sale2Payload, idempotencyKey: t1Msg2.IdempotencyKey);

        var dispatchRes1 = await _dispatcher.DispatchPacketAsync(packetT1_1);
        var dispatchRes2 = await _dispatcher.DispatchPacketAsync(packetT1_2);
        Assert.True(dispatchRes1.Success);
        Assert.True(dispatchRes2.Success);

        // Terminal 2 pushes packet
        var packetT2 = SyncPacket.Create(_branchId, _terminal2Id, SyncEntityKinds.InventoryStockDelta, _variantId.Value.ToString(), SyncOperations.Delta, t2SalePayload, idempotencyKey: t2Msg.IdempotencyKey);
        var dispatchRes3 = await _dispatcher.DispatchPacketAsync(packetT2);
        Assert.True(dispatchRes3.Success);

        // 6. Hub / Target Server Ingests Delta Packets
        var ingestRes1 = await _ingestionEngine.IngestAsync(packetT1_1);
        var ingestRes2 = await _ingestionEngine.IngestAsync(packetT1_2);
        var ingestRes3 = await _ingestionEngine.IngestAsync(packetT2);

        Assert.Equal(SyncIngestionStatus.Applied, ingestRes1.Status);
        Assert.Equal(SyncIngestionStatus.Applied, ingestRes2.Status);
        Assert.Equal(SyncIngestionStatus.Applied, ingestRes3.Status);

        // 7. Verify Mathematical Stock Balance Reconciliation:
        // Expected Stock: 100 - 5 - 3 - 7 = 85. Exactly ZERO stock drift!
        var reconciledStock = await _stockRepository.GetByWarehouseAndVariantAsync(_warehouseId, _variantId);
        Assert.NotNull(reconciledStock);
        Assert.Equal(85m, reconciledStock.QuantityOnHand);

        // 8. Strict Idempotency Verification: Replay Terminal 1 packets (simulating network duplicate / retry)
        var replayRes1 = await _ingestionEngine.IngestAsync(packetT1_1);
        var replayRes2 = await _ingestionEngine.IngestAsync(packetT1_2);

        Assert.Equal(SyncIngestionStatus.AlreadyProcessed, replayRes1.Status);
        Assert.Equal(SyncIngestionStatus.AlreadyProcessed, replayRes2.Status);

        // Stock balance must remain exactly 85 without double-deduction!
        var verifiedStock = await _stockRepository.GetByWarehouseAndVariantAsync(_warehouseId, _variantId);
        Assert.NotNull(verifiedStock);
        Assert.Equal(85m, verifiedStock.QuantityOnHand);
    }

    [Fact]
    public async Task Scenario2_ConcurrentPriceOverrides_DetectsConcurrencyTokenMismatch_StagesAndResolvesViaManagerReview()
    {
        // 1. Arrange Baseline Price: 450.00 PKR
        var initialPrice = ProductPrice.Create(_variantId, PriceType.Selling, 450.00m, _currencyId, DateTimeOffset.UtcNow);
        _priceRepository.Add(initialPrice);
        var baselineToken = $"{initialPrice.Amount:F2}:{initialPrice.CreatedAtUtc:o}";

        // 2. Terminal 1 edits price to 480.00 with baseline token
        var t1PricePayload = new CatalogPriceAdjustmentPayload(
            _terminal1Id, _branchId, _variantId.Value, "Selling", 480.00m, 450.00m, baselineToken,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var packetT1 = SyncPacket.Create(
            _branchId, _terminal1Id, SyncEntityKinds.CatalogPriceAdjustment, _variantId.Value.ToString(),
            SyncOperations.Adjustment, t1PricePayload, concurrencyToken: baselineToken);

        // 3. Terminal 2 concurrently edits price to 520.00 with the SAME baseline token
        var t2PricePayload = new CatalogPriceAdjustmentPayload(
            _terminal2Id, _branchId, _variantId.Value, "Selling", 520.00m, 450.00m, 520.00m != 450.00m ? baselineToken : null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var packetT2 = SyncPacket.Create(
            _branchId, _terminal2Id, SyncEntityKinds.CatalogPriceAdjustment, _variantId.Value.ToString(),
            SyncOperations.Adjustment, t2PricePayload, concurrencyToken: baselineToken);

        // 4. Ingest Terminal 1: First update succeeds
        var res1 = await _ingestionEngine.IngestAsync(packetT1);
        Assert.Equal(SyncIngestionStatus.Applied, res1.Status);

        var activePrices = await _priceRepository.GetActiveByPriceTypeAsync(PriceType.Selling);
        Assert.Equal(480.00m, activePrices.First().Amount);

        // 5. Ingest Terminal 2: Concurrency conflict detected!
        // Local price is now 480.00, but Terminal 2 expected 450.00 with diverging concurrency token
        var res2 = await _ingestionEngine.IngestAsync(packetT2);
        Assert.Equal(SyncIngestionStatus.StagedForManagerReview, res2.Status);
        Assert.NotNull(res2.StagedConflictId);

        // Verify conflict recorded in staging store
        var pendingConflicts = await _conflictStagingStore.GetPendingConflictsAsync();
        Assert.Single(pendingConflicts);
        var conflict = pendingConflicts[0];
        Assert.Equal(SyncConflictType.OverlappingPriceEdit, conflict.ConflictType);
        Assert.Equal(SyncConflictStatus.PendingReview, conflict.Status);
        Assert.Equal("520.00", conflict.IncomingValue);
        Assert.Equal("480.00", conflict.CurrentValue);

        // 6. Store Manager Reviews and Approves Incoming Override
        var managerUserId = Guid.NewGuid();
        var resolveResult = await _ingestionEngine.ResolveStagedConflictAsync(
            conflict.ConflictId,
            SyncConflictStatus.ApprovedManagerLww,
            managerUserId,
            "Manager approved higher pricing override from drive-thru terminal.");

        Assert.Equal(SyncIngestionStatus.Applied, resolveResult.Status);

        // 7. Verify that manager-approved price is now active in catalog
        var finalPrices = await _priceRepository.GetActiveByPriceTypeAsync(PriceType.Selling);
        Assert.Equal(520.00m, finalPrices.First().Amount);

        // Verify conflict is resolved
        var remainingPending = await _conflictStagingStore.GetPendingCountAsync();
        Assert.Equal(0, remainingPending);
    }

    [Fact]
    public async Task Scenario3_MultiTerminalShiftSummaries_ReplicateWithoutCollision()
    {
        var shift1Payload = new ShiftSummaryDeltaSyncPayload(
            _terminal1Id, _branchId, 101, Guid.NewGuid(), "Ali Cashier",
            DateTimeOffset.UtcNow.AddHours(-8), DateTimeOffset.UtcNow,
            5000m, 18500m, 18500m, 0m, 13500m, 42, DateTimeOffset.UtcNow);

        var shift2Payload = new ShiftSummaryDeltaSyncPayload(
            _terminal2Id, _branchId, 101, Guid.NewGuid(), "Sara Cashier",
            DateTimeOffset.UtcNow.AddHours(-8), DateTimeOffset.UtcNow,
            5000m, 24200m, 24200m, 0m, 19200m, 58, DateTimeOffset.UtcNow);

        var packet1 = SyncPacket.Create(_branchId, _terminal1Id, SyncEntityKinds.ShiftSummary, "101", SyncOperations.Snapshot, shift1Payload);
        var packet2 = SyncPacket.Create(_branchId, _terminal2Id, SyncEntityKinds.ShiftSummary, "101", SyncOperations.Snapshot, shift2Payload);

        var res1 = await _ingestionEngine.IngestAsync(packet1);
        var res2 = await _ingestionEngine.IngestAsync(packet2);

        Assert.Equal(SyncIngestionStatus.Applied, res1.Status);
        Assert.Equal(SyncIngestionStatus.Applied, res2.Status);

        var recordedShift1 = await _shiftRegistry.GetLatestTerminalShiftAsync(_branchId, _terminal1Id);
        var recordedShift2 = await _shiftRegistry.GetLatestTerminalShiftAsync(_branchId, _terminal2Id);

        Assert.NotNull(recordedShift1);
        Assert.Equal("Ali Cashier", recordedShift1.CashierName);
        Assert.Equal(13500m, recordedShift1.NetSales);

        Assert.NotNull(recordedShift2);
        Assert.Equal("Sara Cashier", recordedShift2.CashierName);
        Assert.Equal(19200m, recordedShift2.NetSales);
    }
}
