using System;
using System.Threading.Tasks;
using Clovent.Restaurant.Infrastructure.Repositories;
using Clovent.Restaurant.Infrastructure.Tests.TestSupport;
using Clovent.Restaurant.QuickBooks;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.QuickBooks;

public sealed class QuickBooksSyncMapRepositoryTests : SqliteTestBase
{
    [Fact]
    public async Task AddAsync_And_GetByLocalEntityAsync_ReturnsPersistedRecord()
    {
        // Arrange
        using var context = CreateContext();
        var repo = new QuickBooksSyncMapRepository(context);

        var orderId = Guid.NewGuid();
        var map = QuickBooksSyncMap.Create(
            localEntityId: orderId,
            entityType: QuickBooksSyncEntityType.Invoice,
            amount: 250.75m,
            currency: "USD",
            requestPayloadJson: "{\"OrderId\":\"" + orderId + "\"}");

        // Act
        await repo.AddAsync(map);

        // Assert
        using var readContext = CreateContext();
        var readRepo = new QuickBooksSyncMapRepository(readContext);
        var found = await readRepo.GetByLocalEntityAsync(orderId, QuickBooksSyncEntityType.Invoice);

        Assert.NotNull(found);
        Assert.Equal(map.Id, found.Id);
        Assert.Equal(orderId, found.LocalEntityId);
        Assert.Equal(QuickBooksSyncEntityType.Invoice, found.EntityType);
        Assert.Equal(250.75m, found.Amount);
        Assert.Equal("USD", found.Currency);
        Assert.Equal(QuickBooksSyncStatus.Pending, found.Status);
    }

    [Fact]
    public async Task GetByQuickBooksTxnIdAsync_ReturnsMatchingRecord()
    {
        // Arrange
        using var context = CreateContext();
        var repo = new QuickBooksSyncMapRepository(context);

        var paymentId = Guid.NewGuid();
        var map = QuickBooksSyncMap.Create(
            localEntityId: paymentId,
            entityType: QuickBooksSyncEntityType.Payment,
            amount: 100.00m);

        map.MarkSynchronized("QB-TXN-PAY-9999", "PAY-9999");
        await repo.AddAsync(map);

        // Act & Assert
        using var readContext = CreateContext();
        var readRepo = new QuickBooksSyncMapRepository(readContext);
        var found = await readRepo.GetByQuickBooksTxnIdAsync("QB-TXN-PAY-9999");

        Assert.NotNull(found);
        Assert.Equal(paymentId, found.LocalEntityId);
        Assert.Equal(QuickBooksSyncStatus.Synchronized, found.Status);
        Assert.Equal("PAY-9999", found.QuickBooksDocNumber);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMatchingRecord()
    {
        // Arrange
        using var context = CreateContext();
        var repo = new QuickBooksSyncMapRepository(context);

        var map = QuickBooksSyncMap.Create(Guid.NewGuid(), QuickBooksSyncEntityType.ShiftSummary, 500m);
        await repo.AddAsync(map);

        // Act & Assert
        using var readContext = CreateContext();
        var readRepo = new QuickBooksSyncMapRepository(readContext);
        var found = await readRepo.GetByIdAsync(map.Id);

        Assert.NotNull(found);
        Assert.Equal(map.Id, found.Id);
    }

    [Fact]
    public async Task GetRecentAsync_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        using var context = CreateContext();
        var repo = new QuickBooksSyncMapRepository(context);

        var map1 = QuickBooksSyncMap.Create(Guid.NewGuid(), QuickBooksSyncEntityType.Invoice, 100m);
        map1.MarkSynchronized("QB-1", "DOC-1");

        var map2 = QuickBooksSyncMap.Create(Guid.NewGuid(), QuickBooksSyncEntityType.Invoice, 200m);
        map2.MarkFailed("Network timeout 503");

        var map3 = QuickBooksSyncMap.Create(Guid.NewGuid(), QuickBooksSyncEntityType.Payment, 50m);

        await repo.AddAsync(map1);
        await repo.AddAsync(map2);
        await repo.AddAsync(map3);

        // Act & Assert: Filter by Failed status
        using var readContext = CreateContext();
        var readRepo = new QuickBooksSyncMapRepository(readContext);

        var failedOnly = await readRepo.GetRecentAsync(limit: 10, statusFilter: QuickBooksSyncStatus.Failed);
        Assert.Single(failedOnly);
        Assert.Equal(map2.Id, failedOnly[0].Id);

        // Filter by EntityType
        var paymentsOnly = await readRepo.GetRecentAsync(limit: 10, entityTypeFilter: QuickBooksSyncEntityType.Payment);
        Assert.Single(paymentsOnly);
        Assert.Equal(map3.Id, paymentsOnly[0].Id);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesLifecycleStatusAndLastError()
    {
        // Arrange
        using var context = CreateContext();
        var repo = new QuickBooksSyncMapRepository(context);

        var map = QuickBooksSyncMap.Create(Guid.NewGuid(), QuickBooksSyncEntityType.Invoice, 300m);
        await repo.AddAsync(map);

        // Act: mark failed and update
        map.MarkFailed("Invalid auth token");
        await repo.UpdateAsync(map);

        // Assert
        using var readContext = CreateContext();
        var readRepo = new QuickBooksSyncMapRepository(readContext);
        var found = await readRepo.GetByIdAsync(map.Id);

        Assert.NotNull(found);
        Assert.Equal(QuickBooksSyncStatus.Failed, found.Status);
        Assert.Equal("Invalid auth token", found.LastError);
        Assert.Equal(1, found.RetryCount);

        // Act: apply manager override
        var managerId = Guid.NewGuid();
        found.MarkManagerOverride(managerId, "Resolved in QB Desktop manually");
        await readRepo.UpdateAsync(found);

        using var verifyContext = CreateContext();
        var verifyRepo = new QuickBooksSyncMapRepository(verifyContext);
        var resolved = await verifyRepo.GetByIdAsync(map.Id);

        Assert.NotNull(resolved);
        Assert.Equal(QuickBooksSyncStatus.ManualReview, resolved.Status);
        Assert.Equal(managerId, resolved.ManagerOverrideUserId);
        Assert.Equal("Resolved in QB Desktop manually", resolved.ManagerOverrideNotes);
    }

    [Fact]
    public async Task AddAsync_DuplicateEntityTypeAndLocalEntityId_ThrowsDbUpdateException()
    {
        // Arrange
        using var context = CreateContext();
        var repo = new QuickBooksSyncMapRepository(context);

        var orderId = Guid.NewGuid();
        var map1 = QuickBooksSyncMap.Create(orderId, QuickBooksSyncEntityType.Invoice, 150m);
        var map2 = QuickBooksSyncMap.Create(orderId, QuickBooksSyncEntityType.Invoice, 150m);

        await repo.AddAsync(map1);

        // Act & Assert: Duplicate (EntityType, LocalEntityId) violates unique constraint
        await Assert.ThrowsAsync<DbUpdateException>(() => repo.AddAsync(map2));
    }
}
