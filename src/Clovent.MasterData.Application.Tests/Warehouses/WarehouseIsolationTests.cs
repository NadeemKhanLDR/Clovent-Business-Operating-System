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

public class WarehouseIsolationTests
{
    private readonly CompanyId _authorizedCompanyId = CompanyId.New();
    private readonly CompanyId _foreignCompanyId = CompanyId.New();
    private readonly Branch _authorizedBranch;
    private readonly Branch _foreignBranch;
    private readonly Warehouse _authorizedWarehouse;
    private readonly Warehouse _foreignWarehouse;
    private readonly FakeBranchRepository _branchRepository;
    private readonly FakeWarehouseRepository _warehouseRepository;
    private readonly ExecutionContextAccessor _contextAccessor;
    private readonly WarehouseScopeValidator _scopeValidator;

    public WarehouseIsolationTests()
    {
        _branchRepository = new FakeBranchRepository();
        _warehouseRepository = new FakeWarehouseRepository();
        _contextAccessor = new ExecutionContextAccessor();

        // Authorized company and branch setup
        _authorizedBranch = Branch.Create(_authorizedCompanyId, BranchName.Create("Main Branch"));
        _branchRepository.Add(_authorizedBranch);
        _authorizedWarehouse = Warehouse.Create(_authorizedBranch.Id, WarehouseName.Create("Authorized Warehouse"), EntityCode.Create("WH-AUTH"));
        _warehouseRepository.Add(_authorizedWarehouse);

        // Foreign tenant company and branch setup
        _foreignBranch = Branch.Create(_foreignCompanyId, BranchName.Create("Foreign Branch"));
        _branchRepository.Add(_foreignBranch);
        _foreignWarehouse = Warehouse.Create(_foreignBranch.Id, WarehouseName.Create("Foreign Tenant Warehouse"), EntityCode.Create("WH-EXT-CO"));
        _warehouseRepository.Add(_foreignWarehouse);

        // Establish authorized execution context
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
    public async Task ListAllWarehousesQuery_FiltersToAuthorizedCompanyWarehouses_ExcludesForeign()
    {
        var handler = new ListAllWarehousesQueryHandler(_warehouseRepository, _scopeValidator);

        var result = await handler.Handle(new ListAllWarehousesQuery(), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("WH-AUTH", result.First().Code);
        Assert.DoesNotContain(result, w => w.Code == "WH-EXT-CO");
    }

    [Fact]
    public async Task GetWarehouseByIdQuery_ForeignWarehouse_ThrowsScopeMismatchException()
    {
        var handler = new GetWarehouseByIdQueryHandler(_scopeValidator);

        var ex = await Assert.ThrowsAsync<ScopeMismatchException>(() =>
            handler.Handle(new GetWarehouseByIdQuery(_foreignWarehouse.Id.Value), CancellationToken.None));

        Assert.Contains("Access denied", ex.Message);
        Assert.Contains(_foreignWarehouse.Id.Value.ToString(), ex.Message);
    }

    [Fact]
    public async Task GetWarehouseByIdQuery_AuthorizedWarehouse_ReturnsWarehouseDto()
    {
        var handler = new GetWarehouseByIdQueryHandler(_scopeValidator);

        var result = await handler.Handle(new GetWarehouseByIdQuery(_authorizedWarehouse.Id.Value), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("WH-AUTH", result.Code);
    }

    [Fact]
    public async Task GetWarehouseByIdQuery_MissingScope_ThrowsScopeUnresolvedException()
    {
        var unscopedAccessor = new FakeExecutionContextAccessor(null);
        var unscopedValidator = new WarehouseScopeValidator(unscopedAccessor, _warehouseRepository, _branchRepository);
        var handler = new GetWarehouseByIdQueryHandler(unscopedValidator);

        var ex = await Assert.ThrowsAsync<ScopeUnresolvedException>(() =>
            handler.Handle(new GetWarehouseByIdQuery(_authorizedWarehouse.Id.Value), CancellationToken.None));

        Assert.Contains("Execution context is not established", ex.Message);
    }

    [Fact]
    public async Task ListWarehousesByBranchQuery_ForeignBranch_ThrowsScopeMismatchException()
    {
        var handler = new ListWarehousesByBranchQueryHandler(_warehouseRepository, _scopeValidator);

        var ex = await Assert.ThrowsAsync<ScopeMismatchException>(() =>
            handler.Handle(new ListWarehousesByBranchQuery(_foreignBranch.Id.Value), CancellationToken.None));

        Assert.Contains("Access denied", ex.Message);
    }

    [Fact]
    public async Task CreateWarehouseCommand_ForeignBranch_ThrowsScopeMismatchException()
    {
        var handler = new CreateWarehouseCommandHandler(_warehouseRepository, _scopeValidator);

        var ex = await Assert.ThrowsAsync<ScopeMismatchException>(() =>
            handler.Handle(new CreateWarehouseCommand(_foreignBranch.Id.Value, "Illegal Warehouse", "WH-BAD"), CancellationToken.None));

        Assert.Contains("Access denied", ex.Message);
    }

    [Fact]
    public async Task WarehouseScopeValidator_EmptyIdentifiers_FailsClosed()
    {
        await Assert.ThrowsAsync<ScopeUnresolvedException>(() =>
            _scopeValidator.ValidateAndGetWarehouseAsync(Guid.Empty));

        await Assert.ThrowsAsync<ScopeUnresolvedException>(() =>
            _scopeValidator.ValidateAndGetBranchAsync(Guid.Empty));
    }
}
