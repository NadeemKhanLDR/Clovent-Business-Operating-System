using Clovent.Identity;
using Clovent.Identity.Companies;
using Clovent.Identity.Organizations;
using Clovent.Identity.Organizations.Events;
using Clovent.Identity.Organizations.ValueObjects;
using Clovent.Identity.Shared.ValueObjects;
using Xunit;

namespace Clovent.Identity.Tests.Organizations;

public class OrganizationTests
{
    [Fact]
    public void Create_RaisesOrganizationCreated()
    {
        var organization = Organization.Create(OrganizationName.Create("Acme Corp"));

        Assert.Empty(organization.CompanyIds);
        Assert.Equal(OrganizationStatus.Active, organization.Status);
        Assert.Null(organization.TaxId);
        Assert.IsType<OrganizationCreated>(Assert.Single(organization.DomainEvents));
    }

    [Fact]
    public void Create_WithTaxId_SetsIt()
    {
        var organization = Organization.Create(OrganizationName.Create("Acme Corp"), TaxId.Create("12-3456789"));

        Assert.Equal("12-3456789", organization.TaxId!.Value);
    }

    [Fact]
    public void Rename_DifferentName_RaisesOrganizationRenamed()
    {
        var organization = Organization.Create(OrganizationName.Create("Acme Corp"));
        organization.ClearDomainEvents();

        organization.Rename(OrganizationName.Create("Acme Corporation"));

        Assert.Equal("Acme Corporation", organization.Name.Value);
        Assert.IsType<OrganizationRenamed>(Assert.Single(organization.DomainEvents));
    }

    [Fact]
    public void Rename_SameName_IsNoOp()
    {
        var organization = Organization.Create(OrganizationName.Create("Acme Corp"));
        organization.ClearDomainEvents();

        organization.Rename(OrganizationName.Create("Acme Corp"));

        Assert.Empty(organization.DomainEvents);
    }

    [Fact]
    public void Deactivate_ThenActivate_RoundTrips()
    {
        var organization = Organization.Create(OrganizationName.Create("Acme Corp"));
        organization.ClearDomainEvents();

        organization.Deactivate();
        Assert.Equal(OrganizationStatus.Inactive, organization.Status);
        Assert.IsType<OrganizationDeactivated>(Assert.Single(organization.DomainEvents));

        organization.Activate();
        Assert.Equal(OrganizationStatus.Active, organization.Status);
    }

    [Fact]
    public void Deactivate_AlreadyInactive_Throws()
    {
        var organization = Organization.Create(OrganizationName.Create("Acme Corp"));
        organization.Deactivate();

        Assert.Throws<IdentityDomainException>(() => organization.Deactivate());
    }

    [Fact]
    public void Activate_AlreadyActive_Throws()
    {
        var organization = Organization.Create(OrganizationName.Create("Acme Corp"));

        Assert.Throws<IdentityDomainException>(() => organization.Activate());
    }

    [Fact]
    public void AddCompany_New_Succeeds()
    {
        var organization = Organization.Create(OrganizationName.Create("Acme Corp"));
        var companyId = CompanyId.New();

        organization.AddCompany(companyId);

        Assert.Contains(companyId, organization.CompanyIds);
        Assert.IsType<CompanyAddedToOrganization>(organization.DomainEvents.Last());
    }

    [Fact]
    public void AddCompany_AlreadyBelongs_Throws()
    {
        var organization = Organization.Create(OrganizationName.Create("Acme Corp"));
        var companyId = CompanyId.New();
        organization.AddCompany(companyId);

        Assert.Throws<IdentityDomainException>(() => organization.AddCompany(companyId));
    }

    [Fact]
    public void RemoveCompany_Belongs_Succeeds()
    {
        var organization = Organization.Create(OrganizationName.Create("Acme Corp"));
        var companyId = CompanyId.New();
        organization.AddCompany(companyId);

        organization.RemoveCompany(companyId);

        Assert.DoesNotContain(companyId, organization.CompanyIds);
        Assert.IsType<CompanyRemovedFromOrganization>(organization.DomainEvents.Last());
    }

    [Fact]
    public void RemoveCompany_DoesNotBelong_Throws()
    {
        var organization = Organization.Create(OrganizationName.Create("Acme Corp"));

        Assert.Throws<IdentityDomainException>(() => organization.RemoveCompany(CompanyId.New()));
    }
}
