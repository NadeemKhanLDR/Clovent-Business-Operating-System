using Clovent.Identity.Branches;
using Clovent.Identity.Branches.ValueObjects;
using Clovent.Identity.Companies;
using Clovent.Identity.Infrastructure.Repositories;
using Clovent.Identity.Infrastructure.Tests.TestSupport;
using Clovent.Identity.Shared.ValueObjects;
using Xunit;

namespace Clovent.Identity.Infrastructure.Tests.Repositories;

public class BranchRepositoryTests : SqliteTestBase
{
    private static Branch CreateBranch(CompanyId companyId, string name = "Downtown", Address? address = null) =>
        Branch.Create(companyId, BranchName.Create(name), address);

    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields_NoAddress()
    {
        var companyId = CompanyId.New();
        var branch = CreateBranch(companyId);

        await using (var writeContext = CreateContext())
        {
            var repository = new BranchRepository(writeContext);
            await repository.AddAsync(branch);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new BranchRepository(readContext).GetByIdAsync(branch.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(companyId, reloaded!.CompanyId);
        Assert.Equal(branch.Name, reloaded.Name);
        Assert.Null(reloaded.Address);
        Assert.Equal(BranchStatus.Active, reloaded.Status);
    }

    [Fact]
    public async Task AddAsync_WithAddress_RoundTripsEveryField()
    {
        var address = Address.Create("123 Main St", "Springfield", "IL", "62704", "USA");
        var branch = CreateBranch(CompanyId.New(), address: address);

        await using (var writeContext = CreateContext())
        {
            var repository = new BranchRepository(writeContext);
            await repository.AddAsync(branch);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new BranchRepository(readContext).GetByIdAsync(branch.Id);

        Assert.Equal(address, reloaded!.Address);
    }

    [Fact]
    public async Task GetByCompanyIdAsync_FiltersToOwningCompany()
    {
        var companyId = CompanyId.New();
        var otherCompanyId = CompanyId.New();

        await using (var writeContext = CreateContext())
        {
            var repository = new BranchRepository(writeContext);
            await repository.AddAsync(CreateBranch(companyId, "Branch A"));
            await repository.AddAsync(CreateBranch(companyId, "Branch B"));
            await repository.AddAsync(CreateBranch(otherCompanyId, "Branch C"));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new BranchRepository(readContext).GetByCompanyIdAsync(companyId);

        Assert.Equal(2, found.Count);
        Assert.All(found, b => Assert.Equal(companyId, b.CompanyId));
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new BranchRepository(context).GetByIdAsync(BranchId.New());

        Assert.Null(result);
    }
}
