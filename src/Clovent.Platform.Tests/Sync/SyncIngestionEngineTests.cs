using Clovent.Platform.Sync;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Clovent.Platform.Tests.Sync;

public sealed class SyncIngestionEngineTests
{
    private sealed class TestSyncHandler : ISyncIngestionHandler
    {
        public string EntityKind => "TestEntity";
        public int CallCount { get; private set; }

        public Task<SyncIngestionResult> IngestAsync(SyncPacket packet, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(SyncIngestionResult.Applied(packet, "Applied test entity."));
        }
    }

    [Fact]
    public async Task IngestAsync_WhenNewPacket_AppliesAndRecordsIdempotency()
    {
        // Arrange
        var handler = new TestSyncHandler();
        var idempStore = new InMemorySyncIdempotencyStore();
        var stagingStore = new InMemorySyncConflictStagingStore();
        var engine = new SyncIngestionEngine([handler], idempStore, NullLogger<SyncIngestionEngine>.Instance, stagingStore);

        var packet = SyncPacket.Create(Guid.NewGuid(), Guid.NewGuid(), "TestEntity", "E-1", SyncOperations.Delta, new { Val = 10 });

        // Act
        var result = await engine.IngestAsync(packet);

        // Assert
        Assert.Equal(SyncIngestionStatus.Applied, result.Status);
        Assert.Equal(1, handler.CallCount);
        Assert.True(await idempStore.HasBeenProcessedAsync(packet.IdempotencyKey));
    }

    [Fact]
    public async Task IngestAsync_WhenDuplicatePacket_SkipsProcessingIdempotently()
    {
        // Arrange
        var handler = new TestSyncHandler();
        var idempStore = new InMemorySyncIdempotencyStore();
        var stagingStore = new InMemorySyncConflictStagingStore();
        var engine = new SyncIngestionEngine([handler], idempStore, NullLogger<SyncIngestionEngine>.Instance, stagingStore);

        var packet = SyncPacket.Create(Guid.NewGuid(), Guid.NewGuid(), "TestEntity", "E-2", SyncOperations.Delta, new { Val = 20 });

        // Act - Ingest twice
        var res1 = await engine.IngestAsync(packet);
        var res2 = await engine.IngestAsync(packet);

        // Assert
        Assert.Equal(SyncIngestionStatus.Applied, res1.Status);
        Assert.Equal(SyncIngestionStatus.AlreadyProcessed, res2.Status);
        Assert.Equal(1, handler.CallCount); // Handler only called once!
    }

    [Fact]
    public async Task ResolveStagedConflictAsync_WhenApproved_ReappliesPacketAndResolvesConflict()
    {
        // Arrange
        var handler = new TestSyncHandler();
        var idempStore = new InMemorySyncIdempotencyStore();
        var stagingStore = new InMemorySyncConflictStagingStore();
        var engine = new SyncIngestionEngine([handler], idempStore, NullLogger<SyncIngestionEngine>.Instance, stagingStore);

        var packet = SyncPacket.Create(Guid.NewGuid(), Guid.NewGuid(), "TestEntity", "E-3", SyncOperations.Adjustment, new { Val = 30 });
        var conflict = new SyncConflictRecord
        {
            PacketId = packet.PacketId,
            OriginalPacket = packet,
            EntityKind = "TestEntity",
            EntityId = "E-3",
            SourceBranchId = packet.SourceBranchId,
            SourceTerminalId = packet.SourceTerminalId,
            ConflictType = SyncConflictType.ConcurrencyTokenMismatch,
            ConflictReason = "Simulated concurrency conflict"
        };
        await stagingStore.StageConflictAsync(conflict);

        // Act
        var reviewerId = Guid.NewGuid();
        var resolveResult = await engine.ResolveStagedConflictAsync(
            conflict.ConflictId,
            SyncConflictStatus.ApprovedManagerLww,
            reviewerId,
            "Manager override approved.");

        // Assert
        Assert.Equal(SyncIngestionStatus.Applied, resolveResult.Status);
        Assert.Equal(1, handler.CallCount);

        var stored = await stagingStore.GetConflictByIdAsync(conflict.ConflictId);
        Assert.NotNull(stored);
        Assert.Equal(SyncConflictStatus.ApprovedManagerLww, stored.Status);
        Assert.Equal(reviewerId, stored.ReviewedByUserId);
        Assert.True(await idempStore.HasBeenProcessedAsync(packet.IdempotencyKey));
    }
}
