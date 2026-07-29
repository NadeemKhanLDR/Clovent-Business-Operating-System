using Clovent.Identity.Branches;
using Clovent.MasterData;
using Clovent.MasterData.Departments;
using Clovent.MasterData.Departments.Events;
using Clovent.MasterData.Departments.ValueObjects;
using Clovent.MasterData.Shared;
using Xunit;

namespace Clovent.MasterData.Tests.Departments;

public class DepartmentTests
{
    [Fact]
    public void Create_SetsBranchId_ActiveByDefault_RaisesDepartmentCreated()
    {
        var branchId = BranchId.New();

        var department = Department.Create(branchId, DepartmentName.Create("Kitchen"));

        Assert.Equal(branchId, department.BranchId);
        Assert.Equal(MasterDataStatus.Active, department.Status);
        Assert.IsType<DepartmentCreated>(Assert.Single(department.DomainEvents));
    }

    [Fact]
    public void Rename_DifferentName_RaisesDepartmentRenamed()
    {
        var department = Department.Create(BranchId.New(), DepartmentName.Create("Kitchen"));
        department.ClearDomainEvents();

        department.Rename(DepartmentName.Create("Back of House"));

        Assert.Equal("Back of House", department.Name.Value);
        Assert.IsType<DepartmentRenamed>(Assert.Single(department.DomainEvents));
    }

    [Fact]
    public void Rename_SameName_IsNoOp()
    {
        var department = Department.Create(BranchId.New(), DepartmentName.Create("Kitchen"));
        department.ClearDomainEvents();

        department.Rename(DepartmentName.Create("Kitchen"));

        Assert.Empty(department.DomainEvents);
    }

    [Fact]
    public void Deactivate_ThenActivate_RoundTrips()
    {
        var department = Department.Create(BranchId.New(), DepartmentName.Create("Kitchen"));
        department.ClearDomainEvents();

        department.Deactivate();
        Assert.Equal(MasterDataStatus.Inactive, department.Status);
        Assert.IsType<DepartmentDeactivated>(Assert.Single(department.DomainEvents));

        department.Activate();
        Assert.Equal(MasterDataStatus.Active, department.Status);
    }

    [Fact]
    public void Deactivate_AlreadyInactive_Throws()
    {
        var department = Department.Create(BranchId.New(), DepartmentName.Create("Kitchen"));
        department.Deactivate();

        Assert.Throws<MasterDataDomainException>(() => department.Deactivate());
    }

    [Fact]
    public void Activate_AlreadyActive_Throws()
    {
        var department = Department.Create(BranchId.New(), DepartmentName.Create("Kitchen"));

        Assert.Throws<MasterDataDomainException>(() => department.Activate());
    }
}
