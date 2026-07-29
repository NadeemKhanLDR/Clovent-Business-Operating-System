using Clovent.Catalog.Application.Tests.TestSupport;
using Clovent.Catalog.Application.UnitsOfMeasure.Commands;
using Clovent.Catalog.Application.UnitsOfMeasure.Queries;
using Clovent.Catalog.UnitsOfMeasure;
using Clovent.Catalog.UnitsOfMeasure.ValueObjects;
using Xunit;

namespace Clovent.Catalog.Application.Tests.UnitsOfMeasure;

public class UnitOfMeasureHandlerTests
{
    [Fact]
    public async Task CreateUnitOfMeasureCommandHandler_ValidRequest_PersistsAndReturnsDto()
    {
        var repository = new FakeUnitOfMeasureRepository();
        var handler = new CreateUnitOfMeasureCommandHandler(repository);

        var dto = await handler.Handle(new CreateUnitOfMeasureCommand("kg", "Kilogram"), CancellationToken.None);

        Assert.Equal("KG", dto.Code);
        Assert.NotNull(await repository.GetByIdAsync(new UnitOfMeasureId(dto.UnitOfMeasureId)));
    }

    [Fact]
    public async Task RenameUnitOfMeasureCommandHandler_UnknownUnit_Throws()
    {
        var handler = new RenameUnitOfMeasureCommandHandler(new FakeUnitOfMeasureRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RenameUnitOfMeasureCommand(Guid.NewGuid(), "New Name"), CancellationToken.None));
    }

    [Fact]
    public async Task ActivateAndDeactivateUnitOfMeasureCommandHandlers_RoundTrip()
    {
        var repository = new FakeUnitOfMeasureRepository();
        var unit = UnitOfMeasure.Create(UnitOfMeasureCode.Create("KG"), "Kilogram");
        unit.Deactivate();
        repository.Add(unit);

        var activated = await new ActivateUnitOfMeasureCommandHandler(repository)
            .Handle(new ActivateUnitOfMeasureCommand(unit.Id.Value), CancellationToken.None);
        Assert.Equal("Active", activated.Status);

        var deactivated = await new DeactivateUnitOfMeasureCommandHandler(repository)
            .Handle(new DeactivateUnitOfMeasureCommand(unit.Id.Value), CancellationToken.None);
        Assert.Equal("Inactive", deactivated.Status);
    }

    [Fact]
    public async Task ListUnitsOfMeasureQueryHandler_ReturnsEveryUnit()
    {
        var repository = new FakeUnitOfMeasureRepository();
        repository.Add(UnitOfMeasure.Create(UnitOfMeasureCode.Create("KG"), "Kilogram"));
        repository.Add(UnitOfMeasure.Create(UnitOfMeasureCode.Create("PCS"), "Piece"));
        var handler = new ListUnitsOfMeasureQueryHandler(repository);

        var result = await handler.Handle(new ListUnitsOfMeasureQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }
}
