using Clovent.Identity.Infrastructure.Repositories;
using Clovent.Identity.Infrastructure.Tests.TestSupport;
using Clovent.Identity.Organizations;
using Clovent.Identity.Organizations.ValueObjects;
using Clovent.Identity.Shared.ValueObjects;
using Xunit;

namespace Clovent.Identity.Infrastructure.Tests.Repositories;

public class OrganizationRepositoryTests : SqliteTestBase
{
    private static Organization CreateOrganization(string name = "Acme Corp") =>
        Organization.Create(OrganizationName.Create(name), TaxId.Create("TAX-123"));

    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var organization = CreateOrganization();

        await using (var writeContext = CreateContext())
        {
            var repository = new OrganizationRepository(writeContext);
            await repository.AddAsync(organization);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new OrganizationRepository(readContext).GetByIdAsync(organization.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(organization.Name, reloaded!.Name);
        Assert.Equal(organization.TaxId, reloaded.TaxId);
        Assert.Equal(OrganizationStatus.Active, reloaded.Status);
        Assert.Equal(organization.CreatedAtUtc, reloaded.CreatedAtUtc);
    }

    [Fact]
    public async Task AddCompany_ThenReload_PersistsCompanyId()
    {
        var organization = CreateOrganization();
        var companyId = Companies.CompanyId.New();
        organization.AddCompany(companyId);

        await using (var writeContext = CreateContext())
        {
            var repository = new OrganizationRepository(writeContext);
            await repository.AddAsync(organization);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new OrganizationRepository(readContext).GetByIdAsync(organization.Id);

        Assert.Contains(companyId, reloaded!.CompanyIds);
    }

    [Fact]
    public async Task Deactivate_ThenReload_PersistsNewStatus()
    {
        var organization = CreateOrganization();

        await using (var writeContext = CreateContext())
        {
            var repository = new OrganizationRepository(writeContext);
            await repository.AddAsync(organization);
            await writeContext.SaveChangesAsync();
        }

        await using (var updateContext = CreateContext())
        {
            var repository = new OrganizationRepository(updateContext);
            var loaded = await repository.GetByIdAsync(organization.Id);
            loaded!.Deactivate();
            await updateContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new OrganizationRepository(readContext).GetByIdAsync(organization.Id);

        Assert.Equal(OrganizationStatus.Inactive, reloaded!.Status);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEveryOrganization()
    {
        await using (var writeContext = CreateContext())
        {
            var repository = new OrganizationRepository(writeContext);
            await repository.AddAsync(CreateOrganization("Org One"));
            await repository.AddAsync(CreateOrganization("Org Two"));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var all = await new OrganizationRepository(readContext).GetAllAsync();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new OrganizationRepository(context).GetByIdAsync(OrganizationId.New());

        Assert.Null(result);
    }
}
