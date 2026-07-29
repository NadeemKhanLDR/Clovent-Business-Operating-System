using Clovent.MasterData.Currencies;
using Clovent.MasterData.Infrastructure.Repositories;
using Clovent.MasterData.Infrastructure.Tests.TestSupport;
using Clovent.MasterData.Shared;
using Xunit;

namespace Clovent.MasterData.Infrastructure.Tests.Repositories;

public class CurrencyRepositoryTests : SqliteTestBase
{
    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var currency = Currency.Create(CurrencyCode.Create("USD"), "US Dollar", "$", 2);

        await using (var writeContext = CreateContext())
        {
            var repository = new CurrencyRepository(writeContext);
            await repository.AddAsync(currency);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new CurrencyRepository(readContext).GetByIdAsync(currency.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(currency.Code, reloaded!.Code);
        Assert.Equal(currency.Name, reloaded.Name);
        Assert.Equal(currency.Symbol, reloaded.Symbol);
        Assert.Equal(currency.DecimalPlaces, reloaded.DecimalPlaces);
        Assert.Equal(MasterDataStatus.Active, reloaded.Status);
    }

    [Fact]
    public async Task GetByCodeAsync_FindsMatch()
    {
        var currency = Currency.Create(CurrencyCode.Create("EUR"), "Euro", "€", 2);

        await using (var writeContext = CreateContext())
        {
            var repository = new CurrencyRepository(writeContext);
            await repository.AddAsync(currency);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new CurrencyRepository(readContext).GetByCodeAsync(CurrencyCode.Create("EUR"));

        Assert.NotNull(found);
        Assert.Equal(currency.Id, found!.Id);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEveryCurrency()
    {
        await using (var writeContext = CreateContext())
        {
            var repository = new CurrencyRepository(writeContext);
            await repository.AddAsync(Currency.Create(CurrencyCode.Create("USD"), "US Dollar", "$", 2));
            await repository.AddAsync(Currency.Create(CurrencyCode.Create("JPY"), "Japanese Yen", "¥", 0));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var all = await new CurrencyRepository(readContext).GetAllAsync();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new CurrencyRepository(context).GetByIdAsync(CurrencyId.New());

        Assert.Null(result);
    }
}
