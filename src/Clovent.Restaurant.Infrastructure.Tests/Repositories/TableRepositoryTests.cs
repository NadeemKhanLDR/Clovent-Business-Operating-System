using Clovent.MasterData.Shared.ValueObjects;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.Infrastructure.Repositories;
using Clovent.Restaurant.Infrastructure.Tests.TestSupport;
using Clovent.Restaurant.Tables;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Repositories;

public class TableRepositoryTests : SqliteTestBase
{
    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var diningAreaId = DiningAreaId.New();
        var table = Table.Create(diningAreaId, EntityCode.Create("T-01"), 4);
        table.Occupy();

        await using (var writeContext = CreateContext())
        {
            var repository = new TableRepository(writeContext);
            await repository.AddAsync(table);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new TableRepository(readContext).GetByIdAsync(table.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(diningAreaId, reloaded!.DiningAreaId);
        Assert.Equal(table.Code, reloaded.Code);
        Assert.Equal(4, reloaded.Capacity);
        Assert.Equal(TableOccupancyStatus.Occupied, reloaded.OccupancyStatus);
    }

    [Fact]
    public async Task GetByDiningAreaIdAsync_FiltersToOwningArea()
    {
        var areaId = DiningAreaId.New();
        var otherAreaId = DiningAreaId.New();

        await using (var writeContext = CreateContext())
        {
            var repository = new TableRepository(writeContext);
            await repository.AddAsync(Table.Create(areaId, EntityCode.Create("T-01"), 4));
            await repository.AddAsync(Table.Create(areaId, EntityCode.Create("T-02"), 4));
            await repository.AddAsync(Table.Create(otherAreaId, EntityCode.Create("T-03"), 4));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new TableRepository(readContext).GetByDiningAreaIdAsync(areaId);

        Assert.Equal(2, found.Count);
        Assert.All(found, t => Assert.Equal(areaId, t.DiningAreaId));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEveryTable()
    {
        await using (var writeContext = CreateContext())
        {
            var repository = new TableRepository(writeContext);
            await repository.AddAsync(Table.Create(DiningAreaId.New(), EntityCode.Create("T-01"), 4));
            await repository.AddAsync(Table.Create(DiningAreaId.New(), EntityCode.Create("T-02"), 4));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var all = await new TableRepository(readContext).GetAllAsync();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new TableRepository(context).GetByIdAsync(TableId.New());

        Assert.Null(result);
    }
}
