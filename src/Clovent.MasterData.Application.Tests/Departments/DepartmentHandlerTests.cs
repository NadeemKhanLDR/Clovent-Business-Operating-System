using Clovent.Identity.Branches;
using Clovent.MasterData.Application.Departments.Commands;
using Clovent.MasterData.Application.Departments.Queries;
using Clovent.MasterData.Application.Tests.TestSupport;
using Clovent.MasterData.Departments;
using Clovent.MasterData.Departments.ValueObjects;
using Xunit;

namespace Clovent.MasterData.Application.Tests.Departments;

public class DepartmentHandlerTests
{
    [Fact]
    public async Task CreateDepartmentCommandHandler_ValidRequest_PersistsAndReturnsDto()
    {
        var repository = new FakeDepartmentRepository();
        var branchId = BranchId.New();
        var handler = new CreateDepartmentCommandHandler(repository);

        var dto = await handler.Handle(new CreateDepartmentCommand(branchId.Value, "Kitchen"), CancellationToken.None);

        Assert.Equal("Kitchen", dto.Name);
        Assert.Equal("Active", dto.Status);
        Assert.NotNull(await repository.GetByIdAsync(new DepartmentId(dto.DepartmentId)));
    }

    [Fact]
    public async Task RenameDepartmentCommandHandler_UnknownDepartment_Throws()
    {
        var handler = new RenameDepartmentCommandHandler(new FakeDepartmentRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RenameDepartmentCommand(Guid.NewGuid(), "New Name"), CancellationToken.None));
    }

    [Fact]
    public async Task ActivateAndDeactivateDepartmentCommandHandlers_RoundTrip()
    {
        var repository = new FakeDepartmentRepository();
        var department = Department.Create(BranchId.New(), DepartmentName.Create("Kitchen"));
        department.Deactivate();
        repository.Add(department);

        var activated = await new ActivateDepartmentCommandHandler(repository)
            .Handle(new ActivateDepartmentCommand(department.Id.Value), CancellationToken.None);
        Assert.Equal("Active", activated.Status);

        var deactivated = await new DeactivateDepartmentCommandHandler(repository)
            .Handle(new DeactivateDepartmentCommand(department.Id.Value), CancellationToken.None);
        Assert.Equal("Inactive", deactivated.Status);
    }

    [Fact]
    public async Task GetDepartmentByIdQueryHandler_UnknownDepartment_Throws()
    {
        var handler = new GetDepartmentByIdQueryHandler(new FakeDepartmentRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetDepartmentByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task ListDepartmentsByBranchQueryHandler_FiltersToOwningBranch()
    {
        var repository = new FakeDepartmentRepository();
        var branchId = BranchId.New();
        repository.Add(Department.Create(branchId, DepartmentName.Create("Kitchen")));
        repository.Add(Department.Create(BranchId.New(), DepartmentName.Create("Front Desk")));
        var handler = new ListDepartmentsByBranchQueryHandler(repository);

        var result = await handler.Handle(new ListDepartmentsByBranchQuery(branchId.Value), CancellationToken.None);

        Assert.Single(result);
    }
}
