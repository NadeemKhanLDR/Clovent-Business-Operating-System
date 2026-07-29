using Clovent.MasterData.Shared.ValueObjects;
using Clovent.Restaurant;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.Shared;
using Clovent.Restaurant.Tables;
using Clovent.Restaurant.Tables.Events;
using Xunit;

namespace Clovent.Restaurant.Tests.Tables;

public class TableTests
{
    private static Table CreateTable(int capacity = 4) =>
        Table.Create(DiningAreaId.New(), EntityCode.Create("T-01"), capacity);

    [Fact]
    public void Create_Valid_ActiveAndAvailableByDefault_RaisesTableCreated()
    {
        var table = CreateTable();

        Assert.Equal(RestaurantStatus.Active, table.Status);
        Assert.Equal(TableOccupancyStatus.Available, table.OccupancyStatus);
        Assert.IsType<TableCreated>(Assert.Single(table.DomainEvents));
    }

    [Fact]
    public void Create_NonPositiveCapacity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Table.Create(DiningAreaId.New(), EntityCode.Create("T-01"), 0));
    }

    [Fact]
    public void Occupy_FromAvailable_Succeeds()
    {
        var table = CreateTable();
        table.ClearDomainEvents();

        table.Occupy();

        Assert.Equal(TableOccupancyStatus.Occupied, table.OccupancyStatus);
        Assert.IsType<TableOccupancyChanged>(Assert.Single(table.DomainEvents));
    }

    [Fact]
    public void Occupy_FromReserved_Succeeds()
    {
        var table = CreateTable();
        table.Reserve();

        table.Occupy();

        Assert.Equal(TableOccupancyStatus.Occupied, table.OccupancyStatus);
    }

    [Fact]
    public void Occupy_AlreadyOccupied_Throws()
    {
        var table = CreateTable();
        table.Occupy();

        Assert.Throws<RestaurantDomainException>(() => table.Occupy());
    }

    [Fact]
    public void Occupy_OutOfService_Throws()
    {
        var table = CreateTable();
        table.SetOutOfService();

        Assert.Throws<RestaurantDomainException>(() => table.Occupy());
    }

    [Fact]
    public void Vacate_FromOccupied_ReturnsToAvailable()
    {
        var table = CreateTable();
        table.Occupy();

        table.Vacate();

        Assert.Equal(TableOccupancyStatus.Available, table.OccupancyStatus);
    }

    [Fact]
    public void Vacate_OutOfService_Throws()
    {
        var table = CreateTable();
        table.SetOutOfService();

        Assert.Throws<RestaurantDomainException>(() => table.Vacate());
    }

    [Fact]
    public void Reserve_FromAvailable_Succeeds()
    {
        var table = CreateTable();

        table.Reserve();

        Assert.Equal(TableOccupancyStatus.Reserved, table.OccupancyStatus);
    }

    [Fact]
    public void Reserve_WhileOccupied_Throws()
    {
        var table = CreateTable();
        table.Occupy();

        Assert.Throws<RestaurantDomainException>(() => table.Reserve());
    }

    [Fact]
    public void SetOutOfService_FromAvailable_Succeeds()
    {
        var table = CreateTable();

        table.SetOutOfService();

        Assert.Equal(TableOccupancyStatus.OutOfService, table.OccupancyStatus);
    }

    [Fact]
    public void SetOutOfService_WhileOccupied_Throws()
    {
        var table = CreateTable();
        table.Occupy();

        Assert.Throws<RestaurantDomainException>(() => table.SetOutOfService());
    }

    [Fact]
    public void ReturnToService_FromOutOfService_ReturnsToAvailable()
    {
        var table = CreateTable();
        table.SetOutOfService();

        table.ReturnToService();

        Assert.Equal(TableOccupancyStatus.Available, table.OccupancyStatus);
    }

    [Fact]
    public void ReturnToService_NotOutOfService_Throws()
    {
        var table = CreateTable();

        Assert.Throws<RestaurantDomainException>(() => table.ReturnToService());
    }

    [Fact]
    public void SetCapacity_Different_RaisesTableCapacityChanged()
    {
        var table = CreateTable();
        table.ClearDomainEvents();

        table.SetCapacity(6);

        Assert.Equal(6, table.Capacity);
        Assert.IsType<TableCapacityChanged>(Assert.Single(table.DomainEvents));
    }

    [Fact]
    public void SetCapacity_NonPositive_Throws()
    {
        var table = CreateTable();

        Assert.Throws<ArgumentOutOfRangeException>(() => table.SetCapacity(0));
    }

    [Fact]
    public void Deactivate_ThenActivate_RaisesExpectedEvents()
    {
        var table = CreateTable();
        table.ClearDomainEvents();

        table.Deactivate();
        Assert.Equal(RestaurantStatus.Inactive, table.Status);
        Assert.IsType<TableDeactivated>(Assert.Single(table.DomainEvents));

        table.ClearDomainEvents();
        table.Activate();
        Assert.Equal(RestaurantStatus.Active, table.Status);
        Assert.IsType<TableActivated>(Assert.Single(table.DomainEvents));
    }
}
