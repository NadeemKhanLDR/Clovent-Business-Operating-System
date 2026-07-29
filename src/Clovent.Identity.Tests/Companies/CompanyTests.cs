using Clovent.Identity;
using Clovent.Identity.Branches;
using Clovent.Identity.Companies;
using Clovent.Identity.Companies.Events;
using Clovent.Identity.Companies.ValueObjects;
using Clovent.Identity.Organizations;
using Xunit;

namespace Clovent.Identity.Tests.Companies;

public class CompanyTests
{
    [Fact]
    public void Create_SetsOrganizationId_AndRaisesCompanyCreated()
    {
        var organizationId = OrganizationId.New();

        var company = Company.Create(organizationId, CompanyName.Create("Acme Retail"));

        Assert.Equal(organizationId, company.OrganizationId);
        Assert.Empty(company.BranchIds);
        Assert.Equal(CompanyStatus.Active, company.Status);
        Assert.IsType<CompanyCreated>(Assert.Single(company.DomainEvents));
    }

    [Fact]
    public void Rename_DifferentName_RaisesCompanyRenamed()
    {
        var company = Company.Create(OrganizationId.New(), CompanyName.Create("Acme Retail"));
        company.ClearDomainEvents();

        company.Rename(CompanyName.Create("Acme Retail Inc"));

        Assert.Equal("Acme Retail Inc", company.Name.Value);
        Assert.IsType<CompanyRenamed>(Assert.Single(company.DomainEvents));
    }

    [Fact]
    public void Deactivate_ThenActivate_RoundTrips()
    {
        var company = Company.Create(OrganizationId.New(), CompanyName.Create("Acme Retail"));
        company.ClearDomainEvents();

        company.Deactivate();
        Assert.Equal(CompanyStatus.Inactive, company.Status);
        Assert.IsType<CompanyDeactivated>(Assert.Single(company.DomainEvents));

        company.Activate();
        Assert.Equal(CompanyStatus.Active, company.Status);
    }

    [Fact]
    public void Deactivate_AlreadyInactive_Throws()
    {
        var company = Company.Create(OrganizationId.New(), CompanyName.Create("Acme Retail"));
        company.Deactivate();

        Assert.Throws<IdentityDomainException>(() => company.Deactivate());
    }

    [Fact]
    public void AddBranch_New_Succeeds()
    {
        var company = Company.Create(OrganizationId.New(), CompanyName.Create("Acme Retail"));
        var branchId = BranchId.New();

        company.AddBranch(branchId);

        Assert.Contains(branchId, company.BranchIds);
        Assert.IsType<BranchAddedToCompany>(company.DomainEvents.Last());
    }

    [Fact]
    public void AddBranch_AlreadyBelongs_Throws()
    {
        var company = Company.Create(OrganizationId.New(), CompanyName.Create("Acme Retail"));
        var branchId = BranchId.New();
        company.AddBranch(branchId);

        Assert.Throws<IdentityDomainException>(() => company.AddBranch(branchId));
    }

    [Fact]
    public void RemoveBranch_Belongs_Succeeds()
    {
        var company = Company.Create(OrganizationId.New(), CompanyName.Create("Acme Retail"));
        var branchId = BranchId.New();
        company.AddBranch(branchId);

        company.RemoveBranch(branchId);

        Assert.DoesNotContain(branchId, company.BranchIds);
        Assert.IsType<BranchRemovedFromCompany>(company.DomainEvents.Last());
    }

    [Fact]
    public void RemoveBranch_DoesNotBelong_Throws()
    {
        var company = Company.Create(OrganizationId.New(), CompanyName.Create("Acme Retail"));

        Assert.Throws<IdentityDomainException>(() => company.RemoveBranch(BranchId.New()));
    }
}
