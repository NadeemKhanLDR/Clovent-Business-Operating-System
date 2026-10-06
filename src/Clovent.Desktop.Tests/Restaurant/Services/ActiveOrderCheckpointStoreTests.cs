using Clovent.Desktop.Restaurant.Services;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Services;

public sealed class ActiveOrderCheckpointStoreTests : IDisposable
{
    private readonly string _tempDir;

    public ActiveOrderCheckpointStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "CloventTest_Cart_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
            // Best effort
        }
    }

    [Fact]
    public async Task ScenarioF_SaveAndRestoreCheckpoint_RecoversCartAfterRestart()
    {
        var store = new ActiveOrderCheckpointStore(_tempDir);
        var terminalId = "POS-TERMINAL-01";

        var lines = new List<CartCheckpointLine>
        {
            new(Guid.NewGuid(), "SKU-BURGER", "Double Cheeseburger", "Standard", 2, 450.00m, "No onions")
        };

        var checkpointId = Guid.NewGuid();
        var checkpoint = new CartCheckpoint(
            checkpointId,
            terminalId,
            Guid.NewGuid(),
            "Cashier 1",
            "DineIn",
            Guid.NewGuid(),
            "Table 4",
            DateTimeOffset.UtcNow,
            lines,
            "VIP guest",
            null);

        // Act 1: Save checkpoint during order composition
        await store.SaveCheckpointAsync(checkpoint);

        // Act 2: Simulate process crash and restart with fresh store instance
        var freshStore = new ActiveOrderCheckpointStore(_tempDir);
        var restored = await freshStore.LoadCheckpointAsync(terminalId);

        // Assert
        Assert.NotNull(restored);
        Assert.Equal(checkpoint.CheckpointId, restored!.CheckpointId);
        Assert.Equal("Table 4", restored.TableCode);
        Assert.Single(restored.Lines);
        Assert.Equal("Double Cheeseburger", restored.Lines[0].Name);
        Assert.Equal(900.00m, restored.Lines[0].Quantity * restored.Lines[0].UnitPrice);
    }

    [Fact]
    public async Task ScenarioG_CorruptedCheckpoint_DiscardedSafelyWithoutCrashing()
    {
        var store = new ActiveOrderCheckpointStore(_tempDir);
        var terminalId = "POS-TERMINAL-02";
        var path = Path.Combine(_tempDir, $"terminal_{terminalId}.json");

        // Write corrupt JSON
        await File.WriteAllTextAsync(path, "{ corrupted json payload !!!");

        // Act: Loading corrupted checkpoint does not crash, deletes corrupted file, and returns null
        var restored = await store.LoadCheckpointAsync(terminalId);

        Assert.Null(restored);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task ClearCheckpointAsync_RemovesCheckpointOnSuccessfulOrderCompletion()
    {
        var store = new ActiveOrderCheckpointStore(_tempDir);
        var terminalId = "POS-TERMINAL-03";

        var checkpoint = new CartCheckpoint(
            Guid.NewGuid(),
            terminalId,
            Guid.NewGuid(),
            "Cashier 1",
            "TakeAway",
            null,
            null,
            DateTimeOffset.UtcNow,
            [],
            null,
            null);

        await store.SaveCheckpointAsync(checkpoint);
        Assert.NotNull(await store.LoadCheckpointAsync(terminalId));

        await store.ClearCheckpointAsync(terminalId);
        Assert.Null(await store.LoadCheckpointAsync(terminalId));
    }
}
