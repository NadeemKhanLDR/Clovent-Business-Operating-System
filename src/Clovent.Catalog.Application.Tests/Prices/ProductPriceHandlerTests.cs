using Clovent.Catalog.Application.Prices.Commands;
using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Application.Tests.TestSupport;
using Clovent.Catalog.Prices;
using Clovent.Catalog.Variants;
using Clovent.MasterData.Currencies;
using Xunit;

namespace Clovent.Catalog.Application.Tests.Prices;

public class ProductPriceHandlerTests
{
    [Fact]
    public async Task CreateProductPriceCommandHandler_ValidRequest_PersistsAndReturnsDto()
    {
        var repository = new FakeProductPriceRepository();
        var variantId = ProductVariantId.New();
        var handler = new CreateProductPriceCommandHandler(repository);

        var dto = await handler.Handle(new CreateProductPriceCommand(variantId.Value, PriceType.Selling, 9.99m, Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(9.99m, dto.Amount);
        Assert.NotNull(await repository.GetByIdAsync(new ProductPriceId(dto.ProductPriceId)));
    }

    [Fact]
    public async Task UpdateProductPriceAmountCommandHandler_UpdatesAmount()
    {
        var repository = new FakeProductPriceRepository();
        var price = ProductPrice.Create(ProductVariantId.New(), PriceType.Cost, 5m, CurrencyId.New());
        repository.Add(price);
        var handler = new UpdateProductPriceAmountCommandHandler(repository);

        var dto = await handler.Handle(new UpdateProductPriceAmountCommand(price.Id.Value, 6m), CancellationToken.None);

        Assert.Equal(6m, dto.Amount);
    }

    [Fact]
    public async Task ActivateAndDeactivateProductPriceCommandHandlers_RoundTrip()
    {
        var repository = new FakeProductPriceRepository();
        var price = ProductPrice.Create(ProductVariantId.New(), PriceType.Selling, 9.99m, CurrencyId.New());
        price.Deactivate();
        repository.Add(price);

        var activated = await new ActivateProductPriceCommandHandler(repository)
            .Handle(new ActivateProductPriceCommand(price.Id.Value), CancellationToken.None);
        Assert.Equal("Active", activated.Status);

        var deactivated = await new DeactivateProductPriceCommandHandler(repository)
            .Handle(new DeactivateProductPriceCommand(price.Id.Value), CancellationToken.None);
        Assert.Equal("Inactive", deactivated.Status);
    }

    [Fact]
    public async Task ListProductPricesByVariantQueryHandler_FiltersToOwningVariant()
    {
        var repository = new FakeProductPriceRepository();
        var variantId = ProductVariantId.New();
        repository.Add(ProductPrice.Create(variantId, PriceType.Cost, 5m, CurrencyId.New()));
        repository.Add(ProductPrice.Create(variantId, PriceType.Selling, 9.99m, CurrencyId.New()));
        repository.Add(ProductPrice.Create(ProductVariantId.New(), PriceType.Selling, 1m, CurrencyId.New()));
        var handler = new ListProductPricesByVariantQueryHandler(repository);

        var result = await handler.Handle(new ListProductPricesByVariantQuery(variantId.Value), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }
}
