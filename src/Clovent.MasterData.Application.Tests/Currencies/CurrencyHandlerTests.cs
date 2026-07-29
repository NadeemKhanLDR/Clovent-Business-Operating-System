using Clovent.MasterData.Application.Currencies.Commands;
using Clovent.MasterData.Application.Currencies.Queries;
using Clovent.MasterData.Application.Tests.TestSupport;
using Clovent.MasterData.Currencies;
using Xunit;

namespace Clovent.MasterData.Application.Tests.Currencies;

public class CurrencyHandlerTests
{
    [Fact]
    public async Task CreateCurrencyCommandHandler_ValidRequest_PersistsAndReturnsDto()
    {
        var repository = new FakeCurrencyRepository();
        var handler = new CreateCurrencyCommandHandler(repository);

        var dto = await handler.Handle(new CreateCurrencyCommand("usd", "US Dollar", "$", 2), CancellationToken.None);

        Assert.Equal("USD", dto.Code);
        Assert.Equal("Active", dto.Status);
        Assert.NotNull(await repository.GetByIdAsync(new CurrencyId(dto.CurrencyId)));
    }

    [Fact]
    public async Task ActivateAndDeactivateCurrencyCommandHandlers_RoundTrip()
    {
        var repository = new FakeCurrencyRepository();
        var currency = Currency.Create(CurrencyCode.Create("USD"), "US Dollar", "$", 2);
        currency.Deactivate();
        repository.Add(currency);

        var activated = await new ActivateCurrencyCommandHandler(repository)
            .Handle(new ActivateCurrencyCommand(currency.Id.Value), CancellationToken.None);
        Assert.Equal("Active", activated.Status);

        var deactivated = await new DeactivateCurrencyCommandHandler(repository)
            .Handle(new DeactivateCurrencyCommand(currency.Id.Value), CancellationToken.None);
        Assert.Equal("Inactive", deactivated.Status);
    }

    [Fact]
    public async Task GetCurrencyByIdQueryHandler_UnknownCurrency_Throws()
    {
        var handler = new GetCurrencyByIdQueryHandler(new FakeCurrencyRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetCurrencyByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task ListCurrenciesQueryHandler_ReturnsEveryCurrency()
    {
        var repository = new FakeCurrencyRepository();
        repository.Add(Currency.Create(CurrencyCode.Create("USD"), "US Dollar", "$", 2));
        repository.Add(Currency.Create(CurrencyCode.Create("EUR"), "Euro", "€", 2));
        var handler = new ListCurrenciesQueryHandler(repository);

        var result = await handler.Handle(new ListCurrenciesQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }
}
