using System.Text.Json;
using Clovent.Catalog.Prices;
using Clovent.Catalog.Shared;
using Clovent.Catalog.Variants;
using Clovent.Inventory.WarehouseStocks;
using Clovent.MasterData.Currencies;
using Clovent.MasterData.Warehouses;
using Clovent.Platform.CircuitBreakers;
using Clovent.Platform.Sync;
using Clovent.Restaurant.Application.Outbox.Dtos;
using Clovent.Restaurant.Application.Outbox.Handlers;
using Clovent.Restaurant.Application.Sync;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.Outbox;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Sync;

public sealed class MultiTerminalBranchSyncIntegrationTests
{
    private readonly Guid _branchId = Guid.NewGuid();
    private readonly Guid _warehouseId = Guid.NewGuid();
    private readonly Guid _productVariantId = Guid.NewGuid();

    private readonly Guid _terminalAId = Guid.NewGuid();
    private readonly Guid _terminalBId = Guid.NewGuid();

    [Fact]
    public async Task MultiTerminal_ConcurrentOfflineSales_ReconcileWithZeroStockDrift()
    {
        // ARRANGE:
        // Initial warehouse stock = 50.00 units
        var stockRepo = new TestWarehouseStockRepository();
        var stock = WarehouseStock.Create(new WarehouseId(_warehouseId), new ProductVariantId(_productVariantId), 0, 0, allowNegativeStock: true);
        stock.Receive(50.00m);
        await stockRepo.AddAsync(stock);

        // Conflict staging & idempotency stores
        var conflictStaging = new InMemorySyncConflictStagingStore();
        var idempotencyStore = new InMemorySyncIdempotencyStore();

        // Central / peer sync ingestion engine
        var inventoryHandler = new InventoryDeltaIngestionHandler(stockRepo, conflictStaging, NullLogger<InventoryDeltaIngestionHandler>.Instance);
        var ingestionEngine = new SyncIngestionEngine([inventoryHandler], idempotencyStore, NullLogger<SyncIngestionEngine>.Instance);

        // Loopback transport routing delivered packets to the ingestion engine
        var transport = new InMemoryDeltaSyncTransport();

        // Network connectivity probe (starts OFFLINE to simulate autonomous offline retail POS)
        var networkProbe = new NetworkConnectivityProbe(initialConnected: false);
        var circuitBreakerRegistry = new CircuitBreakerRegistry();

        var dispatcher = new DeltaSyncDispatcher(transport, networkProbe, circuitBreakerRegistry, NullLogger<DeltaSyncDispatcher>.Instance);
        var outboxHandler = new InventoryDeltaSyncOutboxHandler(dispatcher, NullLogger<InventoryDeltaSyncOutboxHandler>.Instance);

        // Outbox repositories for Terminal A and Terminal B
        var outboxTerminalA = new FakeOutboxRepository();
        var outboxTerminalB = new FakeOutboxRepository();

        // ACT 1: Autonomous Offline POS Operations
        // Terminal A records 3 sales: 3 units, 2 units, 5 units = 10 units sold
        var salesTerminalA = new[] { -3.00m, -2.00m, -5.00m };
        foreach (var qty in salesTerminalA)
        {
            var payload = new InventoryDeltaSyncPayload(
                TerminalId: _terminalAId,
                BranchId: _branchId,
                WarehouseId: _warehouseId,
                ProductVariantId: _productVariantId,
                Sku: "BEV-COLDBREW-01",
                QuantityDelta: qty,
                OperationType: SyncOperations.Delta,
                ConcurrencyToken: null,
                TimestampUtc: DateTimeOffset.UtcNow);

            var msg = OutboxMessage.Create(
                messageType: OutboxMessageType.InventoryDeltaSync,
                aggregateType: "Order",
                aggregateId: Guid.NewGuid().ToString(),
                correlationId: Guid.NewGuid().ToString(),
                payload: JsonSerializer.Serialize(payload),
                idempotencyKey: $"termA:sale:{Guid.NewGuid()}");

            await outboxTerminalA.AddAsync(msg);
        }

        // Terminal B records 4 sales: 4 units, 6 units, 2 units, 3 units = 15 units sold
        var salesTerminalB = new[] { -4.00m, -6.00m, -2.00m, -3.00m };
        foreach (var qty in salesTerminalB)
        {
            var payload = new InventoryDeltaSyncPayload(
                TerminalId: _terminalBId,
                BranchId: _branchId,
                WarehouseId: _warehouseId,
                ProductVariantId: _productVariantId,
                Sku: "BEV-COLDBREW-01",
                QuantityDelta: qty,
                OperationType: SyncOperations.Delta,
                ConcurrencyToken: null,
                TimestampUtc: DateTimeOffset.UtcNow);

            var msg = OutboxMessage.Create(
                messageType: OutboxMessageType.InventoryDeltaSync,
                aggregateType: "Order",
                aggregateId: Guid.NewGuid().ToString(),
                correlationId: Guid.NewGuid().ToString(),
                payload: JsonSerializer.Serialize(payload),
                idempotencyKey: $"termB:sale:{Guid.NewGuid()}");

            await outboxTerminalB.AddAsync(msg);
        }

        // Verify that while offline, attempting to push defers safely without fatal errors
        var claimOfflineA = await outboxTerminalA.ClaimMessagesAsync(10);
        Assert.Equal(3, claimOfflineA.Count);
        foreach (var m in claimOfflineA)
        {
            // DeltaSyncOutboxHandler throws NetworkOfflineException when offline so outbox processor defers without retry penalty
            await Assert.ThrowsAsync<NetworkOfflineException>(() => outboxHandler.HandleAsync(m, CancellationToken.None));
        }

        // ACT 2: Reconnection & Delta-Sync Ingestion
        // Network connectivity restored
        networkProbe.SetConnected(true);

        // Connect transport to directly ingest into the central engine
        transport.SetPacketHandler(async (packet, ct) =>
        {
            var res = await ingestionEngine.IngestAsync(packet, ct);
            return res.Status == SyncIngestionStatus.Applied || res.Status == SyncIngestionStatus.AlreadyProcessed;
        });

        // Terminal A and Terminal B outbox messages are dispatched and ingested
        var messagesA = outboxTerminalA.AllMessages.ToList();
        var messagesB = outboxTerminalB.AllMessages.ToList();

        // Interleave packets to simulate non-deterministic concurrent arrival from both terminals
        var interleaved = new List<OutboxMessage>
        {
            messagesB[1], // B: -6
            messagesA[0], // A: -3
            messagesB[0], // B: -4
            messagesA[2], // A: -5
            messagesB[3], // B: -3
            messagesA[1], // A: -2
            messagesB[2]  // B: -2
        };

        foreach (var m in interleaved)
        {
            await outboxHandler.HandleAsync(m, CancellationToken.None);
            m.MarkCompleted();
        }

        // ASSERT:
        // Initial = 50.00
        // Terminal A sold = 10.00 (3 + 2 + 5)
        // Terminal B sold = 15.00 (4 + 6 + 2 + 3)
        // Expected = 50.00 - 10.00 - 15.00 = 25.00
        var reconciledStock = await stockRepo.GetByWarehouseAndVariantAsync(new WarehouseId(_warehouseId), new ProductVariantId(_productVariantId));
        Assert.NotNull(reconciledStock);
        Assert.Equal(25.00m, reconciledStock.QuantityOnHand);

        // Zero conflicts staged because commutative deltas do not collide
        Assert.Equal(0, await conflictStaging.GetPendingCountAsync());

        // Handshake status reflects active healthy replication
        var status = dispatcher.GetHandshakeStatus();
        Assert.True(status.IsReachable);
        Assert.Equal("Closed", status.CircuitBreakerState);
        Assert.Equal(7, status.TotalPacketsDispatched);
    }

    [Fact]
    public async Task MultiTerminal_DeltaSyncIngestion_IsStrictlyIdempotent()
    {
        var stockRepo = new TestWarehouseStockRepository();
        var stock = WarehouseStock.Create(new WarehouseId(_warehouseId), new ProductVariantId(_productVariantId), 0, 0, allowNegativeStock: true);
        stock.Receive(100.00m);
        await stockRepo.AddAsync(stock);

        var conflictStaging = new InMemorySyncConflictStagingStore();
        var idempotencyStore = new InMemorySyncIdempotencyStore();
        var inventoryHandler = new InventoryDeltaIngestionHandler(stockRepo, conflictStaging, NullLogger<InventoryDeltaIngestionHandler>.Instance);
        var engine = new SyncIngestionEngine([inventoryHandler], idempotencyStore, NullLogger<SyncIngestionEngine>.Instance);

        var payload = new InventoryDeltaSyncPayload(_terminalAId, _branchId, _warehouseId, _productVariantId, "SKU-1", -10.00m, SyncOperations.Delta, null, DateTimeOffset.UtcNow);
        var packet = SyncPacket.Create(_branchId, _terminalAId, SyncEntityKinds.InventoryStockDelta, _productVariantId.ToString(), SyncOperations.Delta, payload, idempotencyKey: "fixed-idempotency-key");

        // First application
        var firstResult = await engine.IngestAsync(packet);
        Assert.Equal(SyncIngestionStatus.Applied, firstResult.Status);
        Assert.Equal(90.00m, stock.QuantityOnHand);

        // Replay identical packet (simulating duplicate delivery / retry)
        var replayResult = await engine.IngestAsync(packet);
        Assert.Equal(SyncIngestionStatus.AlreadyProcessed, replayResult.Status);

        // Verify stock is still 90.00 (zero double deduction!)
        Assert.Equal(90.00m, stock.QuantityOnHand);
    }

    [Fact]
    public async Task MultiTerminal_ConcurrentPriceEdits_DetectsConflictAndStagesForManagerReview()
    {
        var priceRepo = new TestProductPriceRepository();
        var variantId = new ProductVariantId(_productVariantId);
        var initialPrice = ProductPrice.Create(variantId, PriceType.Selling, 10.00m, CurrencyId.New());
        await priceRepo.AddAsync(initialPrice);

        var conflictStaging = new InMemorySyncConflictStagingStore();
        var idempotencyStore = new InMemorySyncIdempotencyStore();
        var priceHandler = new CatalogPriceDeltaIngestionHandler(priceRepo, conflictStaging, NullLogger<CatalogPriceDeltaIngestionHandler>.Instance);
        var engine = new SyncIngestionEngine([priceHandler], idempotencyStore, NullLogger<SyncIngestionEngine>.Instance);

        var baseToken = $"{initialPrice.Amount:F2}:{initialPrice.CreatedAtUtc:o}";

        // Terminal A adjusts price to 12.00 based on baseToken
        var payloadA = new CatalogPriceAdjustmentPayload(
            TerminalId: _terminalAId,
            BranchId: _branchId,
            ProductVariantId: _productVariantId,
            PriceType: "Selling",
            NewAmount: 12.00m,
            OldAmount: 10.00m,
            ConcurrencyToken: baseToken,
            EffectiveFromUtc: DateTimeOffset.UtcNow,
            TimestampUtc: DateTimeOffset.UtcNow);

        var packetA = SyncPacket.Create(_branchId, _terminalAId, SyncEntityKinds.CatalogPriceAdjustment, _productVariantId.ToString(), SyncOperations.Adjustment, payloadA, concurrencyToken: baseToken);

        // Terminal B concurrently adjusts price to 11.00 based on the SAME baseToken
        var payloadB = new CatalogPriceAdjustmentPayload(
            TerminalId: _terminalBId,
            BranchId: _branchId,
            ProductVariantId: _productVariantId,
            PriceType: "Selling",
            NewAmount: 11.00m,
            OldAmount: 10.00m,
            ConcurrencyToken: baseToken,
            EffectiveFromUtc: DateTimeOffset.UtcNow,
            TimestampUtc: DateTimeOffset.UtcNow);

        var packetB = SyncPacket.Create(_branchId, _terminalBId, SyncEntityKinds.CatalogPriceAdjustment, _productVariantId.ToString(), SyncOperations.Adjustment, payloadB, concurrencyToken: baseToken);

        // Ingest Terminal A first
        var resultA = await engine.IngestAsync(packetA);
        Assert.Equal(SyncIngestionStatus.Applied, resultA.Status);
        Assert.Equal(12.00m, initialPrice.Amount);

        // Ingest Terminal B -> Token mismatch detected because Terminal A already changed it!
        var resultB = await engine.IngestAsync(packetB);
        Assert.Equal(SyncIngestionStatus.StagedForManagerReview, resultB.Status);

        // Verify conflict was durably staged for Manager Review
        var pendingConflicts = await conflictStaging.GetPendingConflictsAsync();
        Assert.Single(pendingConflicts);
        var conflict = pendingConflicts[0];
        Assert.Equal(SyncConflictType.OverlappingPriceEdit, conflict.ConflictType);
        Assert.Equal("11.00", conflict.IncomingValue);
        Assert.Equal("12.00", conflict.CurrentValue);
        Assert.Equal(_terminalBId, conflict.SourceTerminalId);

        // Manager reviews and resolves conflict
        var managerId = Guid.NewGuid();
        await conflictStaging.ResolveConflictAsync(conflict.ConflictId, SyncConflictStatus.ApprovedManagerLww, managerId, "Manager reviewed price divergence.");

        Assert.Equal(0, await conflictStaging.GetPendingCountAsync());
    }

    [Fact]
    public async Task MultiTerminal_ShiftSummaries_AggregatedWithoutCollision()
    {
        var shiftRegistry = new InMemoryShiftSyncRegistry();
        var conflictStaging = new InMemorySyncConflictStagingStore();
        var idempotencyStore = new InMemorySyncIdempotencyStore();
        var shiftHandler = new ShiftSummaryDeltaIngestionHandler(shiftRegistry, conflictStaging, NullLogger<ShiftSummaryDeltaIngestionHandler>.Instance);
        var engine = new SyncIngestionEngine([shiftHandler], idempotencyStore, NullLogger<SyncIngestionEngine>.Instance);

        var shiftA = new ShiftSummaryDeltaSyncPayload(
            TerminalId: _terminalAId,
            BranchId: _branchId,
            ShiftNumber: 101,
            CashierId: Guid.NewGuid(),
            CashierName: "Alice Cashier",
            OpenedAtUtc: DateTimeOffset.UtcNow.AddHours(-8),
            ClosedAtUtc: DateTimeOffset.UtcNow,
            StartingCash: 100m,
            CountedCash: 450m,
            ExpectedCash: 450m,
            CashVariance: 0m,
            NetSales: 350m,
            TotalOrdersCount: 25,
            TimestampUtc: DateTimeOffset.UtcNow);

        var shiftB = new ShiftSummaryDeltaSyncPayload(
            TerminalId: _terminalBId,
            BranchId: _branchId,
            ShiftNumber: 201,
            CashierId: Guid.NewGuid(),
            CashierName: "Bob Cashier",
            OpenedAtUtc: DateTimeOffset.UtcNow.AddHours(-6),
            ClosedAtUtc: DateTimeOffset.UtcNow,
            StartingCash: 150m,
            CountedCash: 720m,
            ExpectedCash: 720m,
            CashVariance: 0m,
            NetSales: 570m,
            TotalOrdersCount: 42,
            TimestampUtc: DateTimeOffset.UtcNow);

        var packetA = SyncPacket.Create(_branchId, _terminalAId, SyncEntityKinds.ShiftSummary, "101", SyncOperations.Snapshot, shiftA);
        var packetB = SyncPacket.Create(_branchId, _terminalBId, SyncEntityKinds.ShiftSummary, "201", SyncOperations.Snapshot, shiftB);

        var resA = await engine.IngestAsync(packetA);
        var resB = await engine.IngestAsync(packetB);

        Assert.Equal(SyncIngestionStatus.Applied, resA.Status);
        Assert.Equal(SyncIngestionStatus.Applied, resB.Status);

        var branchShifts = await shiftRegistry.GetBranchShiftSummariesAsync(_branchId);
        Assert.Equal(2, branchShifts.Count);
        Assert.Contains(branchShifts, s => s.CashierName == "Alice Cashier" && s.ShiftNumber == 101);
        Assert.Contains(branchShifts, s => s.CashierName == "Bob Cashier" && s.ShiftNumber == 201);
    }

    [Fact]
    public async Task MultiTerminal_CircuitBreaker_TripsOnRepeatedFailureAndResetsOnRecovery()
    {
        var transport = new InMemoryDeltaSyncTransport();
        transport.SetSimulateFailure(true);

        var probe = new NetworkConnectivityProbe(initialConnected: true);
        var registry = new CircuitBreakerRegistry();
        var dispatcher = new DeltaSyncDispatcher(transport, probe, registry, NullLogger<DeltaSyncDispatcher>.Instance);
        var handler = new InventoryDeltaSyncOutboxHandler(dispatcher, NullLogger<InventoryDeltaSyncOutboxHandler>.Instance);

        var payload = new InventoryDeltaSyncPayload(_terminalAId, _branchId, _warehouseId, _productVariantId, "SKU-1", -1m, SyncOperations.Delta, null, DateTimeOffset.UtcNow);
        var msg = OutboxMessage.Create(OutboxMessageType.InventoryDeltaSync, "Order", "1", "1", JsonSerializer.Serialize(payload));

        // Attempt 1, 2, 3 fail and trip circuit breaker
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(msg, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(msg, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(msg, CancellationToken.None));

        // 4th call encounters tripped circuit breaker
        await Assert.ThrowsAsync<CircuitBreakerOpenException>(() => handler.HandleAsync(msg, CancellationToken.None));

        var status = dispatcher.GetHandshakeStatus();
        Assert.Equal("Open", status.CircuitBreakerState);

        // Network/Transport recovers and circuit is reset
        transport.SetSimulateFailure(false);
        dispatcher.ResetCircuitBreaker();

        // 5th call succeeds immediately
        await handler.HandleAsync(msg, CancellationToken.None);
        Assert.Equal("Closed", dispatcher.GetHandshakeStatus().CircuitBreakerState);
    }

    private sealed class TestWarehouseStockRepository : IWarehouseStockRepository
    {
        private readonly Dictionary<WarehouseStockId, WarehouseStock> _stocks = [];

        public Task<WarehouseStock?> GetByIdAsync(WarehouseStockId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_stocks.GetValueOrDefault(id));

        public Task<WarehouseStock?> GetByWarehouseAndVariantAsync(WarehouseId warehouseId, ProductVariantId productVariantId, CancellationToken cancellationToken = default)
            => Task.FromResult(_stocks.Values.FirstOrDefault(s => s.WarehouseId == warehouseId && s.ProductVariantId == productVariantId));

        public Task<IReadOnlyCollection<WarehouseStock>> GetByWarehouseIdAsync(WarehouseId warehouseId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<WarehouseStock>>([.. _stocks.Values.Where(s => s.WarehouseId == warehouseId)]);

        public Task<IReadOnlyCollection<WarehouseStock>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<WarehouseStock>>([.. _stocks.Values]);

        public Task AddAsync(WarehouseStock stock, CancellationToken cancellationToken = default)
        {
            _stocks[stock.Id] = stock;
            return Task.CompletedTask;
        }
    }

    private sealed class TestProductPriceRepository : IProductPriceRepository
    {
        private readonly Dictionary<ProductPriceId, ProductPrice> _prices = [];

        public Task<ProductPrice?> GetByIdAsync(ProductPriceId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_prices.GetValueOrDefault(id));

        public Task<IReadOnlyCollection<ProductPrice>> GetByProductVariantIdAsync(ProductVariantId productVariantId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<ProductPrice>>([.. _prices.Values.Where(p => p.ProductVariantId == productVariantId)]);

        public Task<IReadOnlyCollection<ProductPrice>> GetActiveByPriceTypeAsync(PriceType priceType, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<ProductPrice>>([.. _prices.Values.Where(p => p.PriceType == priceType && p.Status == CatalogStatus.Active)]);

        public Task AddAsync(ProductPrice price, CancellationToken cancellationToken = default)
        {
            _prices[price.Id] = price;
            return Task.CompletedTask;
        }
    }
}
