using System.Security.Cryptography;
using Clovent.Restaurant.Continuity;
using Clovent.Restaurant.Infrastructure.Continuity;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests;

public sealed class OperationalCacheStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ProtectedOperationalCacheStore _store;

    public OperationalCacheStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "CBOS_CacheTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _store = new ProtectedOperationalCacheStore(_tempDir, DataProtectionScope.CurrentUser);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }

    [Fact]
    public async Task SaveAndLoadSnapshot_ValidData_SucceedsAndMatches()
    {
        var terminalId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        var snapshot = CreateSampleSnapshot(companyId, branchId, terminalId, warehouseId);

        await _store.SaveSnapshotAsync(snapshot);

        var loaded = await _store.LoadSnapshotAsync(terminalId);

        Assert.NotNull(loaded);
        Assert.Equal(snapshot.Metadata.CacheVersion, loaded.Metadata.CacheVersion);
        Assert.Equal(snapshot.Metadata.TerminalCode, loaded.Metadata.TerminalCode);
        Assert.Equal(snapshot.Payload.Categories.Count, loaded.Payload.Categories.Count);
        Assert.Equal(snapshot.Payload.Products.Count, loaded.Payload.Products.Count);
        Assert.Equal(snapshot.Payload.Variants.Count, loaded.Payload.Variants.Count);
        Assert.True(loaded.VerifyIntegrity());
    }

    [Fact]
    public async Task LoadSnapshot_MissingFile_ReturnsNull()
    {
        var nonExistentTerminal = Guid.NewGuid();
        var loaded = await _store.LoadSnapshotAsync(nonExistentTerminal);
        Assert.Null(loaded);
    }

    [Fact]
    public async Task SaveSnapshot_TamperedHmac_ThrowsContinuityTamperException()
    {
        var terminalId = Guid.NewGuid();
        var snapshot = CreateSampleSnapshot(Guid.NewGuid(), Guid.NewGuid(), terminalId, Guid.NewGuid());

        var tamperedMetadata = snapshot.Metadata with { HmacSignature = "INVALID_HMAC_SIGNATURE" };
        var tamperedSnapshot = new OperationalCacheSnapshot(tamperedMetadata, snapshot.Payload);

        await Assert.ThrowsAsync<ContinuityTamperException>(() => _store.SaveSnapshotAsync(tamperedSnapshot));
    }

    [Fact]
    public async Task LoadSnapshot_CorruptedCiphertext_ThrowsContinuitySecurityException()
    {
        var terminalId = Guid.NewGuid();
        var snapshot = CreateSampleSnapshot(Guid.NewGuid(), Guid.NewGuid(), terminalId, Guid.NewGuid());
        await _store.SaveSnapshotAsync(snapshot);

        var files = Directory.GetFiles(_tempDir, "*.dat");
        Assert.Single(files);
        // Overwrite file with invalid ciphertext
        await File.WriteAllTextAsync(files[0], "CORRUPTED_CIPHERTEXT_NOT_DPAPI");

        await Assert.ThrowsAsync<ContinuitySecurityException>(() => _store.LoadSnapshotAsync(terminalId));
    }

    [Fact]
    public async Task HasValidCacheAsync_CorrectTerminalAndBranch_ReturnsTrue()
    {
        var terminalId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var snapshot = CreateSampleSnapshot(Guid.NewGuid(), branchId, terminalId, Guid.NewGuid());
        await _store.SaveSnapshotAsync(snapshot);

        var isValid = await _store.HasValidCacheAsync(branchId, terminalId, CacheFreshnessPolicy.Default);
        Assert.True(isValid);
    }

    [Fact]
    public async Task HasValidCacheAsync_CrossBranchOrTerminalMismatch_ReturnsFalse()
    {
        var terminalId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var snapshot = CreateSampleSnapshot(Guid.NewGuid(), branchId, terminalId, Guid.NewGuid());
        await _store.SaveSnapshotAsync(snapshot);

        var otherTerminal = Guid.NewGuid();
        var otherBranch = Guid.NewGuid();

        var wrongTerm = await _store.HasValidCacheAsync(branchId, otherTerminal, CacheFreshnessPolicy.Default);
        var wrongBranch = await _store.HasValidCacheAsync(otherBranch, terminalId, CacheFreshnessPolicy.Default);

        Assert.False(wrongTerm);
        Assert.False(wrongBranch);
    }

    private static OperationalCacheSnapshot CreateSampleSnapshot(
        Guid companyId, Guid branchId, Guid terminalId, Guid warehouseId)
    {
        var catId = Guid.NewGuid();
        var prodId = Guid.NewGuid();
        var varId = Guid.NewGuid();

        var categories = new List<CachedCategory>
        {
            new(catId, "Hot Beverages", null, null, 1, "Active")
        };

        var products = new List<CachedProduct>
        {
            new(prodId, "Karak Chai", "HOT-CHAI", catId, "Prepared", 16.0m, false, "Active")
        };

        var variants = new List<CachedVariant>
        {
            new(varId, prodId, "Regular", "HOT-CHAI-REG", 120m, Guid.NewGuid(), catId, 1, "Active", true, "Prepared", null, "Karak Chai", 16.0m, false)
        };

        var payload = new OperationalCachePayload
        {
            Categories = categories,
            Products = products,
            Variants = variants,
            QuickOrderTemplates = [],
            DiningAreas = [],
            Tables = [],
            Discounts = [],
            PaymentMethods = [],
            Operators = [],
            Customers = []
        };

        var checksum = OperationalCacheSnapshot.ComputePayloadChecksum(payload);
        var now = DateTimeOffset.UtcNow;
        var hmac = OperationalCacheSnapshot.ComputeHmacSignature(
            1, "1.2.0", now, companyId, branchId, terminalId, warehouseId, checksum);

        var metadata = new OperationalCacheMetadata(
            SchemaVersion: 1,
            CacheVersion: "1.2.0",
            GeneratedAtUtc: now,
            LastSuccessfulSyncUtc: now,
            CompanyId: companyId,
            CompanyName: "Test Cafe",
            BranchId: branchId,
            BranchName: "Main Branch",
            TerminalId: terminalId,
            TerminalName: "Counter 1",
            TerminalCode: "POS01",
            WarehouseId: warehouseId,
            WarehouseName: "Kitchen Stock",
            CurrencyCode: "PKR",
            CurrencySymbol: "Rs.",
            CurrencyDecimalPlaces: 2,
            SourceDatabaseIdentity: "CBOS_Production",
            SourceRevision: "REV-100",
            TotalCategories: 1,
            TotalProducts: 1,
            TotalVariants: 1,
            TotalTemplates: 0,
            PayloadChecksum: checksum,
            HmacSignature: hmac);

        return new OperationalCacheSnapshot(metadata, payload);
    }
}
