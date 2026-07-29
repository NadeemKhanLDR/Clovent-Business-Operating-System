using Clovent.MasterData.Infrastructure.Repositories;
using Clovent.MasterData.Infrastructure.Tests.TestSupport;
using Clovent.MasterData.Languages;
using Clovent.MasterData.Shared;
using Xunit;

namespace Clovent.MasterData.Infrastructure.Tests.Repositories;

public class LanguageRepositoryTests : SqliteTestBase
{
    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var language = Language.Create(LanguageCode.Create("en"), "English", "English");

        await using (var writeContext = CreateContext())
        {
            var repository = new LanguageRepository(writeContext);
            await repository.AddAsync(language);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new LanguageRepository(readContext).GetByIdAsync(language.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(language.Code, reloaded!.Code);
        Assert.Equal(language.Name, reloaded.Name);
        Assert.Equal(language.NativeName, reloaded.NativeName);
        Assert.Equal(MasterDataStatus.Active, reloaded.Status);
    }

    [Fact]
    public async Task GetByCodeAsync_FindsMatch()
    {
        var language = Language.Create(LanguageCode.Create("es"), "Spanish", "Español");

        await using (var writeContext = CreateContext())
        {
            var repository = new LanguageRepository(writeContext);
            await repository.AddAsync(language);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new LanguageRepository(readContext).GetByCodeAsync(LanguageCode.Create("es"));

        Assert.NotNull(found);
        Assert.Equal(language.Id, found!.Id);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEveryLanguage()
    {
        await using (var writeContext = CreateContext())
        {
            var repository = new LanguageRepository(writeContext);
            await repository.AddAsync(Language.Create(LanguageCode.Create("en"), "English", "English"));
            await repository.AddAsync(Language.Create(LanguageCode.Create("fr"), "French", "Français"));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var all = await new LanguageRepository(readContext).GetAllAsync();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new LanguageRepository(context).GetByIdAsync(LanguageId.New());

        Assert.Null(result);
    }
}
