using Clovent.Platform.CircuitBreakers;
using Clovent.Platform.Sync;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Clovent.Platform.Tests.Sync;

public sealed class DeltaSyncEngineTests
{
    [Fact]
    public void SyncPacket_Create_GeneratesValidEnvelopeAndPayload()
    {
        var branchId = Guid.NewGuid();
        var terminalId = Guid.NewGuid();
        var entityId = Guid.NewGuid().ToString();

        var packet = SyncPacket.Create(
            sourceBranchId: branchId,
            sourceTerminalId: terminalId,
            entityKind: SyncEntityKinds.InventoryStockDelta,
            entityId: entityId,
            operation: SyncOperations.Delta,
            payload: new { Sku = "BEV-001", QuantityDelta = -2.5m },
            concurrencyToken: "v1-stamp",
            idempotencyKey: "custom-key-123");

        Assert.NotEqual(Guid.Empty, packet.PacketId);
        Assert.Equal(branchId, packet.SourceBranchId);
        Assert.Equal(terminalId, packet.SourceTerminalId);
        Assert.Equal(SyncEntityKinds.InventoryStockDelta, packet.EntityKind);
        Assert.Equal(entityId, packet.EntityId);
        Assert.Equal(SyncOperations.Delta, packet.Operation);
        Assert.Equal("v1-stamp", packet.ConcurrencyToken);
        Assert.Equal("custom-key-123", packet.IdempotencyKey);
        Assert.Contains("BEV-001", packet.PayloadJson);

        var deserialized = packet.DeserializePayload<TestPayload>();
        Assert.NotNull(deserialized);
        Assert.Equal("BEV-001", deserialized.Sku);
        Assert.Equal(-2.5m, deserialized.QuantityDelta);
    }

    [Fact]
    public void NetworkConnectivityProbe_TogglingState_TriggersEvent()
    {
        var probe = new NetworkConnectivityProbe(initialConnected: true);
        Assert.True(probe.IsConnected);

        bool? eventArg = null;
        probe.ConnectivityChanged += (_, isConnected) => eventArg = isConnected;

        probe.SetConnected(false);
        Assert.False(probe.IsConnected);
        Assert.False(eventArg);

        probe.SetConnected(true);
        Assert.True(probe.IsConnected);
        Assert.True(eventArg);
    }

    [Fact]
    public async Task DeltaSyncDispatcher_WhenOffline_DefersPacketsWithoutTrippingCircuitBreaker()
    {
        var probe = new NetworkConnectivityProbe(initialConnected: false);
        var transport = new InMemoryDeltaSyncTransport();
        var registry = new CircuitBreakerRegistry();
        var dispatcher = new DeltaSyncDispatcher(transport, probe, registry, NullLogger<DeltaSyncDispatcher>.Instance);

        var packet = SyncPacket.Create(Guid.NewGuid(), Guid.NewGuid(), SyncEntityKinds.ShiftSummary, "1001", SyncOperations.Snapshot, new { Shift = 1 });

        var result = await dispatcher.DispatchPacketAsync(packet);

        Assert.False(result.Success);
        Assert.False(result.CircuitOpen);
        Assert.Contains("inactive", result.ErrorMessage);

        var status = dispatcher.GetHandshakeStatus();
        Assert.False(status.IsReachable);
        Assert.Equal("Closed", status.CircuitBreakerState);
        Assert.Equal(0, status.ConsecutiveFailures);
    }

    [Fact]
    public async Task DeltaSyncDispatcher_WhenOnline_DispatchesAndRecordsHandshake()
    {
        var probe = new NetworkConnectivityProbe(initialConnected: true);
        var transport = new InMemoryDeltaSyncTransport();
        var registry = new CircuitBreakerRegistry();
        var dispatcher = new DeltaSyncDispatcher(transport, probe, registry, NullLogger<DeltaSyncDispatcher>.Instance);

        var packet = SyncPacket.Create(Guid.NewGuid(), Guid.NewGuid(), SyncEntityKinds.ShiftSummary, "1001", SyncOperations.Snapshot, new { Shift = 1 });

        var result = await dispatcher.DispatchPacketAsync(packet);

        Assert.True(result.Success);
        Assert.Equal(1, result.DeliveredCount);
        Assert.NotEqual(default, result.HandshakeTimestampUtc);

        var status = dispatcher.GetHandshakeStatus();
        Assert.True(status.IsReachable);
        Assert.NotNull(status.LastHandshakeUtc);
        Assert.Equal(1, status.TotalPacketsDispatched);
        Assert.Single(transport.DeliveredPackets);
    }

    [Fact]
    public async Task DeltaSyncDispatcher_WhenTransportFailsRepeatedly_TripsCircuitBreaker()
    {
        var probe = new NetworkConnectivityProbe(initialConnected: true);
        var transport = new InMemoryDeltaSyncTransport();
        transport.SetSimulateFailure(true);

        var registry = new CircuitBreakerRegistry();
        var dispatcher = new DeltaSyncDispatcher(transport, probe, registry, NullLogger<DeltaSyncDispatcher>.Instance);

        var packet = SyncPacket.Create(Guid.NewGuid(), Guid.NewGuid(), SyncEntityKinds.InventoryStockDelta, "P1", SyncOperations.Delta, new { Qty = 1 });

        // 3 failures to trip breaker
        await dispatcher.DispatchPacketAsync(packet);
        await dispatcher.DispatchPacketAsync(packet);
        await dispatcher.DispatchPacketAsync(packet);

        var status = dispatcher.GetHandshakeStatus();
        Assert.Equal("Open", status.CircuitBreakerState);
        Assert.False(status.IsReachable);

        // Fourth call immediately returns CircuitBreakerOpen
        var blockedResult = await dispatcher.DispatchPacketAsync(packet);
        Assert.False(blockedResult.Success);
        Assert.True(blockedResult.CircuitOpen);

        // Manual reset clears circuit
        dispatcher.ResetCircuitBreaker();
        Assert.Equal("Closed", dispatcher.GetHandshakeStatus().CircuitBreakerState);
    }

    [Fact]
    public async Task InMemorySyncIdempotencyStore_DeduplicatesExactPackets()
    {
        var store = new InMemorySyncIdempotencyStore();
        var key = "term1:inv:v1:1001";
        var packetId = Guid.NewGuid();

        Assert.False(await store.HasBeenProcessedAsync(key));

        await store.RecordProcessedAsync(key, packetId, SyncEntityKinds.InventoryStockDelta, DateTimeOffset.UtcNow);

        Assert.True(await store.HasBeenProcessedAsync(key));
        Assert.Equal(1, await store.GetProcessedCountAsync());

        // Second check remains true
        Assert.True(await store.HasBeenProcessedAsync(key));
    }

    [Fact]
    public async Task InMemorySyncConflictStagingStore_StageAndResolve_MaintainsAuditDetails()
    {
        var store = new InMemorySyncConflictStagingStore();
        var conflictId = Guid.NewGuid();
        var managerId = Guid.NewGuid();

        var conflict = new SyncConflictRecord
        {
            ConflictId = conflictId,
            PacketId = Guid.NewGuid(),
            EntityKind = SyncEntityKinds.CatalogPriceAdjustment,
            EntityId = "SKU-999",
            SourceBranchId = Guid.NewGuid(),
            SourceTerminalId = Guid.NewGuid(),
            IncomingValue = "15.00",
            CurrentValue = "12.00",
            ConflictType = SyncConflictType.OverlappingPriceEdit,
            ConflictReason = "Concurrent terminal price collision.",
            Status = SyncConflictStatus.PendingReview
        };

        await store.StageConflictAsync(conflict);

        var pending = await store.GetPendingConflictsAsync();
        Assert.Single(pending);
        Assert.Equal(conflictId, pending[0].ConflictId);

        await store.ResolveConflictAsync(conflictId, SyncConflictStatus.ApprovedManagerLww, managerId, "Approved newer price.");

        var updated = await store.GetConflictByIdAsync(conflictId);
        Assert.NotNull(updated);
        Assert.Equal(SyncConflictStatus.ApprovedManagerLww, updated.Status);
        Assert.Equal(managerId, updated.ReviewedByUserId);
        Assert.NotNull(updated.ReviewedAtUtc);
        Assert.Equal("Approved newer price.", updated.ResolutionNotes);

        Assert.Empty(await store.GetPendingConflictsAsync());
    }

    [Fact]
    public async Task SyncIngestionEngine_EnforcesIdempotencyAcrossHandlers()
    {
        var idempotencyStore = new InMemorySyncIdempotencyStore();
        var handler = new FakeSyncIngestionHandler(SyncEntityKinds.InventoryStockDelta);
        var engine = new SyncIngestionEngine([handler], idempotencyStore, NullLogger<SyncIngestionEngine>.Instance);

        var packet = SyncPacket.Create(Guid.NewGuid(), Guid.NewGuid(), SyncEntityKinds.InventoryStockDelta, "SKU-1", SyncOperations.Delta, new { Qty = 1 });

        // First ingestion
        var firstResult = await engine.IngestAsync(packet);
        Assert.Equal(SyncIngestionStatus.Applied, firstResult.Status);
        Assert.Equal(1, handler.InvocationCount);

        // Duplicate ingestion
        var secondResult = await engine.IngestAsync(packet);
        Assert.Equal(SyncIngestionStatus.AlreadyProcessed, secondResult.Status);
        Assert.Equal(1, handler.InvocationCount); // Handler not invoked a second time!
    }

    private sealed record TestPayload(string Sku, decimal QuantityDelta);

    private sealed class FakeSyncIngestionHandler(string entityKind) : ISyncIngestionHandler
    {
        public string EntityKind => entityKind;
        public int InvocationCount { get; private set; }

        public Task<SyncIngestionResult> IngestAsync(SyncPacket packet, CancellationToken cancellationToken = default)
        {
            InvocationCount++;
            return Task.FromResult(SyncIngestionResult.Applied(packet));
        }
    }
}
