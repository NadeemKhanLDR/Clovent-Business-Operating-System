using Clovent.Catalog.Infrastructure.Repositories;
using Clovent.Catalog.Infrastructure.Tests.TestSupport;
using Clovent.Catalog.Shared;
using Clovent.Catalog.UnitsOfMeasure;
using Clovent.Catalog.UnitsOfMeasure.ValueObjects;
using Xunit;

namespace Clovent.Catalog.Infrastructure.Tests.Repositories;

public class UnitOfMeasureRepositoryTests : SqliteTestBase
{
    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var unit = UnitOfMeasure.Create(UnitOfMeasureCode.Create("KG"), "Kilogram");

        await using (var writeContext = CreateContext())
        {
            var repository = new UnitOfMeasureRepository(writeContext);
            await repository.AddAsync(unit);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new UnitOfMeasureRepository(readContext).GetByIdAsync(unit.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(unit.Code, reloaded!.Code);
        Assert.Equal(unit.Name, reloaded.Name);
        Assert.Equal(CatalogStatus.Active, reloaded.Status);
    }

    [Fact]
    public async Task GetByCodeAsync_FindsMatch()
    {
        var unit = UnitOfMeasure.Create(UnitOfMeasureCode.Create("PCS"), "Piece");

        await using (var writeContext = CreateContext())
        {
            var repository = new UnitOfMeasureRepository(writeContext);
            await repository.AddAsync(unit);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new UnitOfMeasureRepository(readContext).GetByCodeAsync(UnitOfMeasureCode.Create("PCS"));

        Assert.NotNull(found);
        Assert.Equal(unit.Id, found!.Id);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEveryUnit()
    {
        await using (var writeContext = CreateContext())
        {
            var repository = new UnitOfMeasureRepository(writeContext);
            await repository.AddAsync(UnitOfMeasure.Create(UnitOfMeasureCode.Create("KG"), "Kilogram"));
            await repository.AddAsync(UnitOfMeasure.Create(UnitOfMeasureCode.Create("BOX"), "Box"));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var all = await new UnitOfMeasureRepository(readContext).GetAllAsync();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new UnitOfMeasureRepository(context).GetByIdAsync(UnitOfMeasureId.New());

        Assert.Null(result);
    }
}
