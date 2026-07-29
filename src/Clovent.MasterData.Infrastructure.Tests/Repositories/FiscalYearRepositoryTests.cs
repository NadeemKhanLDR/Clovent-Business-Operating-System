using Clovent.Identity.Organizations;
using Clovent.MasterData.FiscalYears;
using Clovent.MasterData.FiscalYears.ValueObjects;
using Clovent.MasterData.Infrastructure.Repositories;
using Clovent.MasterData.Infrastructure.Tests.TestSupport;
using Xunit;

namespace Clovent.MasterData.Infrastructure.Tests.Repositories;

public class FiscalYearRepositoryTests : SqliteTestBase
{
    private static readonly DateOnly StartDate = new(2026, 1, 1);
    private static readonly DateOnly EndDate = new(2026, 12, 31);

    private static FiscalYear CreateFiscalYear(OrganizationId organizationId, string name = "FY2026") =>
        FiscalYear.Create(organizationId, FiscalYearName.Create(name), StartDate, EndDate);

    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var organizationId = OrganizationId.New();
        var fiscalYear = CreateFiscalYear(organizationId);

        await using (var writeContext = CreateContext())
        {
            var repository = new FiscalYearRepository(writeContext);
            await repository.AddAsync(fiscalYear);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new FiscalYearRepository(readContext).GetByIdAsync(fiscalYear.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(organizationId, reloaded!.OrganizationId);
        Assert.Equal(fiscalYear.Name, reloaded.Name);
        Assert.Equal(StartDate, reloaded.StartDate);
        Assert.Equal(EndDate, reloaded.EndDate);
        Assert.Equal(FiscalYearStatus.Open, reloaded.Status);
    }

    [Fact]
    public async Task Close_ThenReload_PersistsClosedStatus()
    {
        var fiscalYear = CreateFiscalYear(OrganizationId.New());

        await using (var writeContext = CreateContext())
        {
            var repository = new FiscalYearRepository(writeContext);
            await repository.AddAsync(fiscalYear);
            await writeContext.SaveChangesAsync();
        }

        await using (var updateContext = CreateContext())
        {
            var repository = new FiscalYearRepository(updateContext);
            var loaded = await repository.GetByIdAsync(fiscalYear.Id);
            loaded!.Close();
            await updateContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new FiscalYearRepository(readContext).GetByIdAsync(fiscalYear.Id);

        Assert.Equal(FiscalYearStatus.Closed, reloaded!.Status);
    }

    [Fact]
    public async Task GetByOrganizationIdAsync_FiltersToOwningOrganization()
    {
        var organizationId = OrganizationId.New();
        var otherOrganizationId = OrganizationId.New();

        await using (var writeContext = CreateContext())
        {
            var repository = new FiscalYearRepository(writeContext);
            await repository.AddAsync(CreateFiscalYear(organizationId, "FY A"));
            await repository.AddAsync(CreateFiscalYear(otherOrganizationId, "FY B"));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new FiscalYearRepository(readContext).GetByOrganizationIdAsync(organizationId);

        Assert.Single(found);
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new FiscalYearRepository(context).GetByIdAsync(FiscalYearId.New());

        Assert.Null(result);
    }
}
