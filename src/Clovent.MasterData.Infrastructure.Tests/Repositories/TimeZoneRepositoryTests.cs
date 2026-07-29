using Clovent.MasterData.Infrastructure.Repositories;
using Clovent.MasterData.Infrastructure.Tests.TestSupport;
using Clovent.MasterData.Shared;
using Clovent.MasterData.TimeZones;
using Xunit;

namespace Clovent.MasterData.Infrastructure.Tests.Repositories;

public class TimeZoneRepositoryTests : SqliteTestBase
{
    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var entry = TimeZoneEntry.Create(IanaId.Create("UTC"), "(UTC) Coordinated Universal Time", 0);

        await using (var writeContext = CreateContext())
        {
            var repository = new TimeZoneRepository(writeContext);
            await repository.AddAsync(entry);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new TimeZoneRepository(readContext).GetByIdAsync(entry.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(entry.IanaId, reloaded!.IanaId);
        Assert.Equal(entry.DisplayName, reloaded.DisplayName);
        Assert.Equal(entry.UtcOffsetMinutes, reloaded.UtcOffsetMinutes);
        Assert.Equal(MasterDataStatus.Active, reloaded.Status);
    }

    [Fact]
    public async Task GetByIanaIdAsync_FindsMatch()
    {
        var entry = TimeZoneEntry.Create(IanaId.Create("America/New_York"), "(UTC-05:00) Eastern Time", -300);

        await using (var writeContext = CreateContext())
        {
            var repository = new TimeZoneRepository(writeContext);
            await repository.AddAsync(entry);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new TimeZoneRepository(readContext).GetByIanaIdAsync(IanaId.Create("America/New_York"));

        Assert.NotNull(found);
        Assert.Equal(entry.Id, found!.Id);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEveryEntry()
    {
        await using (var writeContext = CreateContext())
        {
            var repository = new TimeZoneRepository(writeContext);
            await repository.AddAsync(TimeZoneEntry.Create(IanaId.Create("UTC"), "UTC", 0));
            await repository.AddAsync(TimeZoneEntry.Create(IanaId.Create("America/Chicago"), "Central Time", -360));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var all = await new TimeZoneRepository(readContext).GetAllAsync();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new TimeZoneRepository(context).GetByIdAsync(TimeZoneEntryId.New());

        Assert.Null(result);
    }
}
