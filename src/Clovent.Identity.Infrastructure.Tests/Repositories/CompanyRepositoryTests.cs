using Clovent.Identity.Companies;
using Clovent.Identity.Companies.ValueObjects;
using Clovent.Identity.Infrastructure.Repositories;
using Clovent.Identity.Infrastructure.Tests.TestSupport;
using Clovent.Identity.Organizations;
using Xunit;

namespace Clovent.Identity.Infrastructure.Tests.Repositories;

public class CompanyRepositoryTests : SqliteTestBase
{
    private static Company CreateCompany(OrganizationId organizationId, string name = "Acme Retail") =>
        Company.Create(organizationId, CompanyName.Create(name));

    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var organizationId = OrganizationId.New();
        var company = CreateCompany(organizationId);

        await using (var writeContext = CreateContext())
        {
            var repository = new CompanyRepository(writeContext);
            await repository.AddAsync(company);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new CompanyRepository(readContext).GetByIdAsync(company.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(organizationId, reloaded!.OrganizationId);
        Assert.Equal(company.Name, reloaded.Name);
        Assert.Equal(CompanyStatus.Active, reloaded.Status);
    }

    [Fact]
    public async Task GetByOrganizationIdAsync_FiltersToOwningOrganization()
    {
        var organizationId = OrganizationId.New();
        var otherOrganizationId = OrganizationId.New();

        await using (var writeContext = CreateContext())
        {
            var repository = new CompanyRepository(writeContext);
            await repository.AddAsync(CreateCompany(organizationId, "Company A"));
            await repository.AddAsync(CreateCompany(organizationId, "Company B"));
            await repository.AddAsync(CreateCompany(otherOrganizationId, "Company C"));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new CompanyRepository(readContext).GetByOrganizationIdAsync(organizationId);

        Assert.Equal(2, found.Count);
        Assert.All(found, c => Assert.Equal(organizationId, c.OrganizationId));
    }

    [Fact]
    public async Task AddBranch_ThenReload_PersistsBranchId()
    {
        var company = CreateCompany(OrganizationId.New());
        var branchId = Branches.BranchId.New();
        company.AddBranch(branchId);

        await using (var writeContext = CreateContext())
        {
            var repository = new CompanyRepository(writeContext);
            await repository.AddAsync(company);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new CompanyRepository(readContext).GetByIdAsync(company.Id);

        Assert.Contains(branchId, reloaded!.BranchIds);
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new CompanyRepository(context).GetByIdAsync(CompanyId.New());

        Assert.Null(result);
    }
}
