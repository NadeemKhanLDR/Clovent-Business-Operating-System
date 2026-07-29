using Clovent.Identity.Branches;
using Clovent.MasterData.Infrastructure.Repositories;
using Clovent.MasterData.Infrastructure.Tests.TestSupport;
using Clovent.MasterData.Shared;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Warehouses;
using Clovent.MasterData.Warehouses.ValueObjects;
using Xunit;

namespace Clovent.MasterData.Infrastructure.Tests.Repositories;

public class WarehouseRepositoryTests : SqliteTestBase
{
    private static Warehouse CreateWarehouse(BranchId branchId, string name = "Main Warehouse", string code = "WH-01") =>
        Warehouse.Create(branchId, WarehouseName.Create(name), EntityCode.Create(code));

    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var branchId = BranchId.New();
        var warehouse = CreateWarehouse(branchId);

        await using (var writeContext = CreateContext())
        {
            var repository = new WarehouseRepository(writeContext);
            await repository.AddAsync(warehouse);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new WarehouseRepository(readContext).GetByIdAsync(warehouse.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(branchId, reloaded!.BranchId);
        Assert.Equal(warehouse.Name, reloaded.Name);
        Assert.Equal(warehouse.Code, reloaded.Code);
        Assert.Equal(MasterDataStatus.Active, reloaded.Status);
    }

    [Fact]
    public async Task GetByBranchIdAsync_FiltersToOwningBranch()
    {
        var branchId = BranchId.New();
        var otherBranchId = BranchId.New();

        await using (var writeContext = CreateContext())
        {
            var repository = new WarehouseRepository(writeContext);
            await repository.AddAsync(CreateWarehouse(branchId, "Warehouse A", "WH-01"));
            await repository.AddAsync(CreateWarehouse(branchId, "Warehouse B", "WH-02"));
            await repository.AddAsync(CreateWarehouse(otherBranchId, "Warehouse C", "WH-03"));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new WarehouseRepository(readContext).GetByBranchIdAsync(branchId);

        Assert.Equal(2, found.Count);
        Assert.All(found, w => Assert.Equal(branchId, w.BranchId));
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new WarehouseRepository(context).GetByIdAsync(WarehouseId.New());

        Assert.Null(result);
    }
}
