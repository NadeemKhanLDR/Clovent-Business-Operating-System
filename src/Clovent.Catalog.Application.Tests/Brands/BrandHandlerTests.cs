using Clovent.Catalog.Application.Brands.Commands;
using Clovent.Catalog.Application.Brands.Queries;
using Clovent.Catalog.Application.Tests.TestSupport;
using Clovent.Catalog.Brands;
using Clovent.Catalog.Brands.ValueObjects;
using Xunit;

namespace Clovent.Catalog.Application.Tests.Brands;

public class BrandHandlerTests
{
    [Fact]
    public async Task CreateBrandCommandHandler_ValidRequest_PersistsAndReturnsDto()
    {
        var repository = new FakeBrandRepository();
        var handler = new CreateBrandCommandHandler(repository);

        var dto = await handler.Handle(new CreateBrandCommand("Acme"), CancellationToken.None);

        Assert.Equal("Acme", dto.Name);
        Assert.NotNull(await repository.GetByIdAsync(new BrandId(dto.BrandId)));
    }

    [Fact]
    public async Task RenameBrandCommandHandler_UnknownBrand_Throws()
    {
        var handler = new RenameBrandCommandHandler(new FakeBrandRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RenameBrandCommand(Guid.NewGuid(), "New Name"), CancellationToken.None));
    }

    [Fact]
    public async Task ActivateAndDeactivateBrandCommandHandlers_RoundTrip()
    {
        var repository = new FakeBrandRepository();
        var brand = Brand.Create(BrandName.Create("Acme"));
        brand.Deactivate();
        repository.Add(brand);

        var activated = await new ActivateBrandCommandHandler(repository)
            .Handle(new ActivateBrandCommand(brand.Id.Value), CancellationToken.None);
        Assert.Equal("Active", activated.Status);

        var deactivated = await new DeactivateBrandCommandHandler(repository)
            .Handle(new DeactivateBrandCommand(brand.Id.Value), CancellationToken.None);
        Assert.Equal("Inactive", deactivated.Status);
    }

    [Fact]
    public async Task ListBrandsQueryHandler_ReturnsEveryBrand()
    {
        var repository = new FakeBrandRepository();
        repository.Add(Brand.Create(BrandName.Create("Brand A")));
        repository.Add(Brand.Create(BrandName.Create("Brand B")));
        var handler = new ListBrandsQueryHandler(repository);

        var result = await handler.Handle(new ListBrandsQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }
}
