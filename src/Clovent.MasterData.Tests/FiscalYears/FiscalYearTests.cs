using Clovent.Identity.Organizations;
using Clovent.MasterData;
using Clovent.MasterData.FiscalYears;
using Clovent.MasterData.FiscalYears.Events;
using Clovent.MasterData.FiscalYears.ValueObjects;
using Xunit;

namespace Clovent.MasterData.Tests.FiscalYears;

public class FiscalYearTests
{
    private static readonly DateOnly StartDate = new(2026, 1, 1);
    private static readonly DateOnly EndDate = new(2026, 12, 31);

    [Fact]
    public void Create_Valid_IsOpenByDefault_RaisesFiscalYearCreated()
    {
        var organizationId = OrganizationId.New();

        var fiscalYear = FiscalYear.Create(organizationId, FiscalYearName.Create("FY2026"), StartDate, EndDate);

        Assert.Equal(organizationId, fiscalYear.OrganizationId);
        Assert.Equal(FiscalYearStatus.Open, fiscalYear.Status);
        Assert.IsType<FiscalYearCreated>(Assert.Single(fiscalYear.DomainEvents));
    }

    [Fact]
    public void Create_EndDateNotAfterStartDate_Throws()
    {
        Assert.Throws<MasterDataDomainException>(() =>
            FiscalYear.Create(OrganizationId.New(), FiscalYearName.Create("FY2026"), EndDate, StartDate));
    }

    [Fact]
    public void Create_EndDateEqualsStartDate_Throws()
    {
        Assert.Throws<MasterDataDomainException>(() =>
            FiscalYear.Create(OrganizationId.New(), FiscalYearName.Create("FY2026"), StartDate, StartDate));
    }

    [Fact]
    public void Close_ThenCloseAgain_Throws()
    {
        var fiscalYear = FiscalYear.Create(OrganizationId.New(), FiscalYearName.Create("FY2026"), StartDate, EndDate);
        fiscalYear.ClearDomainEvents();

        fiscalYear.Close();

        Assert.Equal(FiscalYearStatus.Closed, fiscalYear.Status);
        Assert.IsType<FiscalYearClosed>(Assert.Single(fiscalYear.DomainEvents));
        Assert.Throws<MasterDataDomainException>(() => fiscalYear.Close());
    }

    [Fact]
    public void Rename_DifferentName_RaisesFiscalYearRenamed()
    {
        var fiscalYear = FiscalYear.Create(OrganizationId.New(), FiscalYearName.Create("FY2026"), StartDate, EndDate);
        fiscalYear.ClearDomainEvents();

        fiscalYear.Rename(FiscalYearName.Create("2026 Fiscal Year"));

        Assert.Equal("2026 Fiscal Year", fiscalYear.Name.Value);
        Assert.IsType<FiscalYearRenamed>(Assert.Single(fiscalYear.DomainEvents));
    }
}
