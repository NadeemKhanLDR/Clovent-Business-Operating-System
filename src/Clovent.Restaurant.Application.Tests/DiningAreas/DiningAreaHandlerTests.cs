using Clovent.Identity.Branches;
using Clovent.Restaurant.Application.DiningAreas.Commands;
using Clovent.Restaurant.Application.DiningAreas.Queries;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.DiningAreas.ValueObjects;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.DiningAreas;

public class DiningAreaHandlerTests
{
    [Fact]
    public async Task CreateDiningAreaCommandHandler_Valid_CreatesAndReturnsDto()
    {
        var repository = new FakeDiningAreaRepository();
        var handler = new CreateDiningAreaCommandHandler(repository);
        var branchId = BranchId.New();

        var result = await handler.Handle(new CreateDiningAreaCommand(branchId.Value, "Patio"), CancellationToken.None);

        Assert.Equal("Patio", result.Name);
        Assert.NotNull(await repository.GetByIdAsync(new DiningAreaId(result.DiningAreaId)));
    }

    [Fact]
    public async Task RenameDiningAreaCommandHandler_Existing_Renames()
    {
        var repository = new FakeDiningAreaRepository();
        var area = DiningArea.Create(BranchId.New(), DiningAreaName.Create("Patio"));
        repository.Add(area);
        var handler = new RenameDiningAreaCommandHandler(repository);

        var result = await handler.Handle(new RenameDiningAreaCommand(area.Id.Value, "Main Hall"), CancellationToken.None);

        Assert.Equal("Main Hall", result.Name);
    }

    [Fact]
    public async Task RenameDiningAreaCommandHandler_NotFound_Throws()
    {
        var handler = new RenameDiningAreaCommandHandler(new FakeDiningAreaRepository());

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new RenameDiningAreaCommand(Guid.NewGuid(), "X"), CancellationToken.None));
    }

    [Fact]
    public async Task ActivateThenDeactivate_RoundTrips()
    {
        var repository = new FakeDiningAreaRepository();
        var area = DiningArea.Create(BranchId.New(), DiningAreaName.Create("Patio"));
        area.Deactivate();
        repository.Add(area);

        var activated = await new ActivateDiningAreaCommandHandler(repository).Handle(new ActivateDiningAreaCommand(area.Id.Value), CancellationToken.None);
        Assert.Equal("Active", activated.Status);

        var deactivated = await new DeactivateDiningAreaCommandHandler(repository).Handle(new DeactivateDiningAreaCommand(area.Id.Value), CancellationToken.None);
        Assert.Equal("Inactive", deactivated.Status);
    }

    [Fact]
    public async Task GetDiningAreaByIdQueryHandler_Existing_ReturnsDto()
    {
        var repository = new FakeDiningAreaRepository();
        var area = DiningArea.Create(BranchId.New(), DiningAreaName.Create("Patio"));
        repository.Add(area);

        var result = await new GetDiningAreaByIdQueryHandler(repository).Handle(new GetDiningAreaByIdQuery(area.Id.Value), CancellationToken.None);

        Assert.Equal(area.Id.Value, result.DiningAreaId);
    }

    [Fact]
    public async Task ListDiningAreasByBranchQueryHandler_FiltersToBranch()
    {
        var repository = new FakeDiningAreaRepository();
        var branchId = BranchId.New();
        repository.Add(DiningArea.Create(branchId, DiningAreaName.Create("Patio")));
        repository.Add(DiningArea.Create(branchId, DiningAreaName.Create("Main Hall")));
        repository.Add(DiningArea.Create(BranchId.New(), DiningAreaName.Create("Other Branch Area")));

        var result = await new ListDiningAreasByBranchQueryHandler(repository).Handle(new ListDiningAreasByBranchQuery(branchId.Value), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }
}
