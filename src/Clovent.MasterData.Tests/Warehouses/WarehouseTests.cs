using Clovent.Identity.Branches;
using Clovent.MasterData;
using Clovent.MasterData.Shared;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Warehouses;
using Clovent.MasterData.Warehouses.Events;
using Clovent.MasterData.Warehouses.ValueObjects;
using Xunit;

namespace Clovent.MasterData.Tests.Warehouses;

public class WarehouseTests
{
    [Fact]
    public void Create_SetsFields_ActiveByDefault_RaisesWarehouseCreated()
    {
        var branchId = BranchId.New();

        var warehouse = Warehouse.Create(branchId, WarehouseName.Create("Main Warehouse"), EntityCode.Create("wh-01"));

        Assert.Equal(branchId, warehouse.BranchId);
        Assert.Equal("WH-01", warehouse.Code.Value);
        Assert.Equal(MasterDataStatus.Active, warehouse.Status);
        Assert.IsType<WarehouseCreated>(Assert.Single(warehouse.DomainEvents));
    }

    [Fact]
    public void Rename_DifferentName_RaisesWarehouseRenamed()
    {
        var warehouse = Warehouse.Create(BranchId.New(), WarehouseName.Create("Main"), EntityCode.Create("WH-01"));
        warehouse.ClearDomainEvents();

        warehouse.Rename(WarehouseName.Create("Central"));

        Assert.Equal("Central", warehouse.Name.Value);
        Assert.IsType<WarehouseRenamed>(Assert.Single(warehouse.DomainEvents));
    }

    [Fact]
    public void Deactivate_AlreadyInactive_Throws()
    {
        var warehouse = Warehouse.Create(BranchId.New(), WarehouseName.Create("Main"), EntityCode.Create("WH-01"));
        warehouse.Deactivate();

        Assert.Throws<MasterDataDomainException>(() => warehouse.Deactivate());
    }
}
