using Clovent.Identity;
using Clovent.Identity.Branches;
using Clovent.Identity.Branches.Events;
using Clovent.Identity.Branches.ValueObjects;
using Clovent.Identity.Companies;
using Clovent.Identity.Shared.ValueObjects;
using Xunit;

namespace Clovent.Identity.Tests.Branches;

public class BranchTests
{
    [Fact]
    public void Create_SetsCompanyId_AndRaisesBranchCreated()
    {
        var companyId = CompanyId.New();

        var branch = Branch.Create(companyId, BranchName.Create("Downtown"));

        Assert.Equal(companyId, branch.CompanyId);
        Assert.Equal("Downtown", branch.Name.Value);
        Assert.Equal(BranchStatus.Active, branch.Status);
        Assert.Null(branch.Address);
        Assert.IsType<BranchCreated>(Assert.Single(branch.DomainEvents));
    }

    [Fact]
    public void Create_WithAddress_SetsIt()
    {
        var address = Address.Create("1 Main St", "Springfield", "IL", "62704", "USA");

        var branch = Branch.Create(CompanyId.New(), BranchName.Create("Downtown"), address);

        Assert.Equal(address, branch.Address);
    }

    [Fact]
    public void SetAddress_RaisesBranchAddressChanged()
    {
        var branch = Branch.Create(CompanyId.New(), BranchName.Create("Downtown"));
        branch.ClearDomainEvents();
        var address = Address.Create("1 Main St", "Springfield", "IL", "62704", "USA");

        branch.SetAddress(address);

        Assert.Equal(address, branch.Address);
        Assert.IsType<BranchAddressChanged>(Assert.Single(branch.DomainEvents));
    }

    [Fact]
    public void SetAddress_Null_ClearsWithoutRaisingEvent()
    {
        var address = Address.Create("1 Main St", "Springfield", "IL", "62704", "USA");
        var branch = Branch.Create(CompanyId.New(), BranchName.Create("Downtown"), address);
        branch.ClearDomainEvents();

        branch.SetAddress(null);

        Assert.Null(branch.Address);
        Assert.Empty(branch.DomainEvents);
    }

    [Fact]
    public void Rename_DifferentName_RaisesBranchRenamed()
    {
        var branch = Branch.Create(CompanyId.New(), BranchName.Create("Downtown"));
        branch.ClearDomainEvents();

        branch.Rename(BranchName.Create("Uptown"));

        Assert.Equal("Uptown", branch.Name.Value);
        Assert.IsType<BranchRenamed>(Assert.Single(branch.DomainEvents));
    }

    [Fact]
    public void Deactivate_ThenActivate_RoundTrips()
    {
        var branch = Branch.Create(CompanyId.New(), BranchName.Create("Downtown"));
        branch.ClearDomainEvents();

        branch.Deactivate();
        Assert.Equal(BranchStatus.Inactive, branch.Status);
        Assert.IsType<BranchDeactivated>(Assert.Single(branch.DomainEvents));

        branch.Activate();
        Assert.Equal(BranchStatus.Active, branch.Status);
    }

    [Fact]
    public void Activate_AlreadyActive_Throws()
    {
        var branch = Branch.Create(CompanyId.New(), BranchName.Create("Downtown"));

        Assert.Throws<IdentityDomainException>(() => branch.Activate());
    }
}
