using Clovent.Catalog.Application.Barcodes.Commands;
using Clovent.Catalog.Application.Barcodes.Queries;
using Clovent.Catalog.Application.Tests.TestSupport;
using Clovent.Catalog.Barcodes;
using Clovent.Catalog.Barcodes.ValueObjects;
using Clovent.Catalog.Variants;
using Xunit;

namespace Clovent.Catalog.Application.Tests.Barcodes;

public class BarcodeHandlerTests
{
    [Fact]
    public async Task CreateBarcodeCommandHandler_ValidRequest_PersistsAndReturnsDto()
    {
        var repository = new FakeBarcodeRepository();
        var variantId = ProductVariantId.New();
        var handler = new CreateBarcodeCommandHandler(repository);

        var dto = await handler.Handle(new CreateBarcodeCommand(variantId.Value, "012345678905"), CancellationToken.None);

        Assert.Equal("012345678905", dto.Value);
        Assert.NotNull(await repository.GetByIdAsync(new BarcodeId(dto.BarcodeId)));
    }

    [Fact]
    public async Task CreateBarcodeCommandHandler_AsPrimary_UnmarksExistingPrimary()
    {
        var repository = new FakeBarcodeRepository();
        var variantId = ProductVariantId.New();
        var existingPrimary = Barcode.Create(variantId, BarcodeValue.Create("012345678905"), isPrimary: true);
        repository.Add(existingPrimary);
        var handler = new CreateBarcodeCommandHandler(repository);

        var dto = await handler.Handle(new CreateBarcodeCommand(variantId.Value, "111111111111", IsPrimary: true), CancellationToken.None);

        Assert.True(dto.IsPrimary);
        Assert.False(existingPrimary.IsPrimary);
    }

    [Fact]
    public async Task MarkBarcodeAsPrimaryCommandHandler_UnmarksOtherPrimaryForSameVariant()
    {
        var repository = new FakeBarcodeRepository();
        var variantId = ProductVariantId.New();
        var existingPrimary = Barcode.Create(variantId, BarcodeValue.Create("012345678905"), isPrimary: true);
        var newPrimary = Barcode.Create(variantId, BarcodeValue.Create("111111111111"));
        repository.Add(existingPrimary);
        repository.Add(newPrimary);
        var handler = new MarkBarcodeAsPrimaryCommandHandler(repository);

        var dto = await handler.Handle(new MarkBarcodeAsPrimaryCommand(newPrimary.Id.Value), CancellationToken.None);

        Assert.True(dto.IsPrimary);
        Assert.False(existingPrimary.IsPrimary);
    }

    [Fact]
    public async Task GetBarcodeByValueQueryHandler_UnknownValue_Throws()
    {
        var handler = new GetBarcodeByValueQueryHandler(new FakeBarcodeRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetBarcodeByValueQuery("999999999999"), CancellationToken.None));
    }

    [Fact]
    public async Task ListBarcodesByVariantQueryHandler_FiltersToOwningVariant()
    {
        var repository = new FakeBarcodeRepository();
        var variantId = ProductVariantId.New();
        repository.Add(Barcode.Create(variantId, BarcodeValue.Create("012345678905")));
        repository.Add(Barcode.Create(ProductVariantId.New(), BarcodeValue.Create("111111111111")));
        var handler = new ListBarcodesByVariantQueryHandler(repository);

        var result = await handler.Handle(new ListBarcodesByVariantQuery(variantId.Value), CancellationToken.None);

        Assert.Single(result);
    }
}
