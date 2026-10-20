using Clovent.Identity.Branches;
using Clovent.Identity.Branches.ValueObjects;
using Clovent.Identity.Companies;
using Clovent.MasterData.Application.Tests.TestSupport;
using Clovent.MasterData.Application.Warehouses.Commands;
using Clovent.MasterData.Application.Warehouses.Queries;
using Clovent.MasterData.Application.Warehouses.Security;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Warehouses;
using Clovent.MasterData.Warehouses.ValueObjects;
using Clovent.Platform.Execution;
using Xunit;

namespace Clovent.MasterData.Application.Tests.Warehouses;

public class WarehouseHandlerTests
{
    private readonly CompanyId _authorizedCompanyId = CompanyId.New();
    private readonly Branch _authorizedBranch;
    private readonly FakeBranchRepository _branchRepository;
    private readonly FakeWarehouseRepository _warehouseRepository;
    private readonly ExecutionContextAccessor _contextAccessor;
    private readonly WarehouseScopeValidator _scopeValidator;

    public WarehouseHandlerTests()
    {
        _branchRepository = new FakeBranchRepository();
        _warehouseRepository = new FakeWarehouseRepository();
        _contextAccessor = new ExecutionContextAccessor();

        _authorizedBranch = Branch.Create(_authorizedCompanyId, BranchName.Create("Main Branch"));
        _branchRepository.Add(_authorizedBranch);

        var context = new PlatformExecutionContext
        {
            UserId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            CompanyId = _authorizedCompanyId.Value,
            BranchId = _authorizedBranch.Id.Value
        };
        _contextAccessor.BeginScope(context);

        _scopeValidator = new WarehouseScopeValidator(_contextAccessor, _warehouseRepository, _branchRepository);
    }

    [Fact]
    public async Task CreateWarehouseCommandHandler_ValidRequest_PersistsAndReturnsDto()
    {
        var handler = new CreateWarehouseCommandHandler(_warehouseRepository, _scopeValidator);

        var dto = await handler.Handle(new CreateWarehouseCommand(_authorizedBranch.Id.Value, "Main Warehouse", "WH-01"), CancellationToken.None);

        Assert.Equal("Main Warehouse", dto.Name);
        Assert.Equal("WH-01", dto.Code);
        Assert.NotNull(await _warehouseRepository.GetByIdAsync(new WarehouseId(dto.WarehouseId)));
    }

    [Fact]
    public async Task RenameWarehouseCommandHandler_UnknownWarehouse_Throws()
    {
        var handler = new RenameWarehouseCommandHandler(_scopeValidator);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RenameWarehouseCommand(Guid.NewGuid(), "New Name"), CancellationToken.None));
    }

    [Fact]
    public async Task ActivateAndDeactivateWarehouseCommandHandlers_RoundTrip()
    {
        var warehouse = Warehouse.Create(_authorizedBranch.Id, WarehouseName.Create("Main Warehouse"), EntityCode.Create("WH-01"));
        warehouse.Deactivate();
        _warehouseRepository.Add(warehouse);

        var activated = await new ActivateWarehouseCommandHandler(_scopeValidator)
            .Handle(new ActivateWarehouseCommand(warehouse.Id.Value), CancellationToken.None);
        Assert.Equal("Active", activated.Status);

        var deactivated = await new DeactivateWarehouseCommandHandler(_scopeValidator)
            .Handle(new DeactivateWarehouseCommand(warehouse.Id.Value), CancellationToken.None);
        Assert.Equal("Inactive", deactivated.Status);
    }

    [Fact]
    public async Task GetWarehouseByIdQueryHandler_UnknownWarehouse_Throws()
    {
        var handler = new GetWarehouseByIdQueryHandler(_scopeValidator);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetWarehouseByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task ListWarehousesByBranchQueryHandler_FiltersToOwningBranch()
    {
        var otherBranch = Branch.Create(_authorizedCompanyId, BranchName.Create("Secondary Branch"));
        _branchRepository.Add(otherBranch);

        _warehouseRepository.Add(Warehouse.Create(_authorizedBranch.Id, WarehouseName.Create("Warehouse A"), EntityCode.Create("WH-01")));
        _warehouseRepository.Add(Warehouse.Create(otherBranch.Id, WarehouseName.Create("Warehouse B"), EntityCode.Create("WH-02")));
        var handler = new ListWarehousesByBranchQueryHandler(_warehouseRepository, _scopeValidator);

        var result = await handler.Handle(new ListWarehousesByBranchQuery(_authorizedBranch.Id.Value), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Warehouse A", result.First().Name);
    }
}
