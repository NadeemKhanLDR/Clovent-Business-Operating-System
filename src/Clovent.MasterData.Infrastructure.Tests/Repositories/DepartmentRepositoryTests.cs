using Clovent.Identity.Branches;
using Clovent.MasterData.Departments;
using Clovent.MasterData.Departments.ValueObjects;
using Clovent.MasterData.Infrastructure.Repositories;
using Clovent.MasterData.Infrastructure.Tests.TestSupport;
using Clovent.MasterData.Shared;
using Xunit;

namespace Clovent.MasterData.Infrastructure.Tests.Repositories;

public class DepartmentRepositoryTests : SqliteTestBase
{
    private static Department CreateDepartment(BranchId branchId, string name = "Administration") =>
        Department.Create(branchId, DepartmentName.Create(name));

    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var branchId = BranchId.New();
        var department = CreateDepartment(branchId);

        await using (var writeContext = CreateContext())
        {
            var repository = new DepartmentRepository(writeContext);
            await repository.AddAsync(department);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new DepartmentRepository(readContext).GetByIdAsync(department.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(branchId, reloaded!.BranchId);
        Assert.Equal(department.Name, reloaded.Name);
        Assert.Equal(MasterDataStatus.Active, reloaded.Status);
        Assert.Equal(department.CreatedAtUtc, reloaded.CreatedAtUtc);
    }

    [Fact]
    public async Task GetByBranchIdAsync_FiltersToOwningBranch()
    {
        var branchId = BranchId.New();
        var otherBranchId = BranchId.New();

        await using (var writeContext = CreateContext())
        {
            var repository = new DepartmentRepository(writeContext);
            await repository.AddAsync(CreateDepartment(branchId, "Kitchen"));
            await repository.AddAsync(CreateDepartment(branchId, "Accounting"));
            await repository.AddAsync(CreateDepartment(otherBranchId, "Front Desk"));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new DepartmentRepository(readContext).GetByBranchIdAsync(branchId);

        Assert.Equal(2, found.Count);
        Assert.All(found, d => Assert.Equal(branchId, d.BranchId));
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new DepartmentRepository(context).GetByIdAsync(DepartmentId.New());

        Assert.Null(result);
    }
}
