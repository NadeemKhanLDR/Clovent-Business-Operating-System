using Clovent.Platform.CircuitBreakers;
using Clovent.Platform.Sync;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Clovent.Platform.Tests.Sync;

public sealed class DeltaSyncDispatcherTests
{
    private readonly InMemoryDeltaSyncTransport _transport = new();
    private readonly NetworkConnectivityProbe _probe = new(initialConnected: true);
    private readonly CircuitBreakerRegistry _cbRegistry = new();

    private DeltaSyncDispatcher CreateDispatcher()
    {
        return new DeltaSyncDispatcher(
            _transport,
            _probe,
            _cbRegistry,
            NullLogger<DeltaSyncDispatcher>.Instance);
    }

    [Fact]
    public async Task DispatchPacket_WhenOnline_DeliversSuccessfullyAndRecordsHandshake()
    {
        // Arrange
        var dispatcher = CreateDispatcher();
        var packet = SyncPacket.Create(
            Guid.NewGuid(), Guid.NewGuid(), SyncEntityKinds.InventoryStockDelta,
            "VAR-1", SyncOperations.Delta, new { Qty = -5 });

        // Act
        var result = await dispatcher.DispatchPacketAsync(packet);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.DeliveredCount);
        Assert.False(result.IsOffline);
        Assert.False(result.CircuitOpen);

        var status = dispatcher.GetHandshakeStatus();
        Assert.NotNull(status.LastHandshakeUtc);
        Assert.True(status.IsReachable);
        Assert.Equal(1, status.TotalPacketsDispatched);
        Assert.Equal("Closed", status.CircuitBreakerState);
    }

    [Fact]
    public async Task DispatchPacket_WhenOffline_ReturnsNetworkOfflineWithoutTrippingCircuitBreaker()
    {
        // Arrange
        var dispatcher = CreateDispatcher();
        _probe.SetConnected(false);

        var packet = SyncPacket.Create(
            Guid.NewGuid(), Guid.NewGuid(), SyncEntityKinds.InventoryStockDelta,
            "VAR-2", SyncOperations.Delta, new { Qty = -2 });

        // Act
        var result = await dispatcher.DispatchPacketAsync(packet);

        // Assert
        Assert.False(result.Success);
        Assert.True(result.IsOffline);
        Assert.False(result.CircuitOpen);
        Assert.Contains("offline", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        var status = dispatcher.GetHandshakeStatus();
        Assert.False(status.IsReachable);
        Assert.Equal(0, status.TotalPacketsDispatched);
        Assert.Equal("Closed", status.CircuitBreakerState);
        Assert.Equal(0, status.ConsecutiveFailures);
    }

    [Fact]
    public async Task DispatchBatch_WhenTransportFails3Times_TripsCircuitBreakerToOpen()
    {
        // Arrange
        var dispatcher = CreateDispatcher();
        _transport.SetSimulateFailure(true);

        var packet = SyncPacket.Create(
            Guid.NewGuid(), Guid.NewGuid(), SyncEntityKinds.CatalogPriceAdjustment,
            "VAR-3", SyncOperations.Adjustment, new { Price = 500m });

        // Act - 3 consecutive failures
        var res1 = await dispatcher.DispatchPacketAsync(packet);
        var res2 = await dispatcher.DispatchPacketAsync(packet);
        var res3 = await dispatcher.DispatchPacketAsync(packet);

        // Assert
        Assert.False(res1.Success);
        Assert.False(res2.Success);
        Assert.False(res3.Success);

        // 4th attempt trips circuit breaker
        var res4 = await dispatcher.DispatchPacketAsync(packet);
        Assert.False(res4.Success);
        Assert.True(res4.CircuitOpen);

        var status = dispatcher.GetHandshakeStatus();
        Assert.Equal("Open", status.CircuitBreakerState);
        Assert.False(status.IsReachable);

        // Reset circuit breaker
        dispatcher.ResetCircuitBreaker();
        var statusAfterReset = dispatcher.GetHandshakeStatus();
        Assert.Equal("Closed", statusAfterReset.CircuitBreakerState);
    }

    [Fact]
    public void SyncPacket_Create_GeneratesDeterministicIdempotencyKeyWhenNotSpecified()
    {
        var branchId = Guid.NewGuid();
        var terminalId = Guid.NewGuid();
        var packet = SyncPacket.Create(branchId, terminalId, SyncEntityKinds.ShiftSummary, "SHIFT-1", SyncOperations.Snapshot, new { Cash = 1000m });

        Assert.NotEqual(Guid.Empty, packet.PacketId);
        Assert.Contains(terminalId.ToString(), packet.IdempotencyKey);
        Assert.Contains("SHIFT-1", packet.IdempotencyKey);
        Assert.Equal(SyncEntityKinds.ShiftSummary, packet.EntityKind);
    }
}
