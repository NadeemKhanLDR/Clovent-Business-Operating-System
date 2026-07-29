using Clovent.Identity.Branches;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.DiningAreas.ValueObjects;
using Clovent.Restaurant.Infrastructure.Repositories;
using Clovent.Restaurant.Infrastructure.Tests.TestSupport;
using Clovent.Restaurant.Shared;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Repositories;

public class DiningAreaRepositoryTests : SqliteTestBase
{
    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var branchId = BranchId.New();
        var area = DiningArea.Create(branchId, DiningAreaName.Create("Patio"));

        await using (var writeContext = CreateContext())
        {
            var repository = new DiningAreaRepository(writeContext);
            await repository.AddAsync(area);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new DiningAreaRepository(readContext).GetByIdAsync(area.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(branchId, reloaded!.BranchId);
        Assert.Equal(area.Name, reloaded.Name);
        Assert.Equal(RestaurantStatus.Active, reloaded.Status);
    }

    [Fact]
    public async Task GetByBranchIdAsync_FiltersToOwningBranch()
    {
        var branchId = BranchId.New();
        var otherBranchId = BranchId.New();

        await using (var writeContext = CreateContext())
        {
            var repository = new DiningAreaRepository(writeContext);
            await repository.AddAsync(DiningArea.Create(branchId, DiningAreaName.Create("Patio")));
            await repository.AddAsync(DiningArea.Create(branchId, DiningAreaName.Create("Main Hall")));
            await repository.AddAsync(DiningArea.Create(otherBranchId, DiningAreaName.Create("Other Branch Area")));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new DiningAreaRepository(readContext).GetByBranchIdAsync(branchId);

        Assert.Equal(2, found.Count);
        Assert.All(found, a => Assert.Equal(branchId, a.BranchId));
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new DiningAreaRepository(context).GetByIdAsync(DiningAreaId.New());

        Assert.Null(result);
    }
}
