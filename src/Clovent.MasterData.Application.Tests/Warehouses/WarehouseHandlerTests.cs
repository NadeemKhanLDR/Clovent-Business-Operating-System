using Clovent.Identity.Branches;
using Clovent.MasterData.Application.Tests.TestSupport;
using Clovent.MasterData.Application.Warehouses.Commands;
using Clovent.MasterData.Application.Warehouses.Queries;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Warehouses;
using Clovent.MasterData.Warehouses.ValueObjects;
using Xunit;

namespace Clovent.MasterData.Application.Tests.Warehouses;

public class WarehouseHandlerTests
{
    [Fact]
    public async Task CreateWarehouseCommandHandler_ValidRequest_PersistsAndReturnsDto()
    {
        var repository = new FakeWarehouseRepository();
        var handler = new CreateWarehouseCommandHandler(repository);

        var dto = await handler.Handle(new CreateWarehouseCommand(BranchId.New().Value, "Main Warehouse", "WH-01"), CancellationToken.None);

        Assert.Equal("Main Warehouse", dto.Name);
        Assert.Equal("WH-01", dto.Code);
        Assert.NotNull(await repository.GetByIdAsync(new WarehouseId(dto.WarehouseId)));
    }

    [Fact]
    public async Task RenameWarehouseCommandHandler_UnknownWarehouse_Throws()
    {
        var handler = new RenameWarehouseCommandHandler(new FakeWarehouseRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RenameWarehouseCommand(Guid.NewGuid(), "New Name"), CancellationToken.None));
    }

    [Fact]
    public async Task ActivateAndDeactivateWarehouseCommandHandlers_RoundTrip()
    {
        var repository = new FakeWarehouseRepository();
        var warehouse = Warehouse.Create(BranchId.New(), WarehouseName.Create("Main Warehouse"), EntityCode.Create("WH-01"));
        warehouse.Deactivate();
        repository.Add(warehouse);

        var activated = await new ActivateWarehouseCommandHandler(repository)
            .Handle(new ActivateWarehouseCommand(warehouse.Id.Value), CancellationToken.None);
        Assert.Equal("Active", activated.Status);

        var deactivated = await new DeactivateWarehouseCommandHandler(repository)
            .Handle(new DeactivateWarehouseCommand(warehouse.Id.Value), CancellationToken.None);
        Assert.Equal("Inactive", deactivated.Status);
    }

    [Fact]
    public async Task GetWarehouseByIdQueryHandler_UnknownWarehouse_Throws()
    {
        var handler = new GetWarehouseByIdQueryHandler(new FakeWarehouseRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetWarehouseByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task ListWarehousesByBranchQueryHandler_FiltersToOwningBranch()
    {
        var repository = new FakeWarehouseRepository();
        var branchId = BranchId.New();
        repository.Add(Warehouse.Create(branchId, WarehouseName.Create("Warehouse A"), EntityCode.Create("WH-01")));
        repository.Add(Warehouse.Create(BranchId.New(), WarehouseName.Create("Warehouse B"), EntityCode.Create("WH-02")));
        var handler = new ListWarehousesByBranchQueryHandler(repository);

        var result = await handler.Handle(new ListWarehousesByBranchQuery(branchId.Value), CancellationToken.None);

        Assert.Single(result);
    }
}
