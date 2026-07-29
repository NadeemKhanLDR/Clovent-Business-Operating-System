using Clovent.Identity.Branches;
using Clovent.Restaurant;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.DiningAreas.Events;
using Clovent.Restaurant.DiningAreas.ValueObjects;
using Clovent.Restaurant.Shared;
using Xunit;

namespace Clovent.Restaurant.Tests.DiningAreas;

public class DiningAreaTests
{
    [Fact]
    public void Create_Valid_ActiveByDefault_RaisesDiningAreaCreated()
    {
        var area = DiningArea.Create(BranchId.New(), DiningAreaName.Create("Patio"));

        Assert.Equal("Patio", area.Name.Value);
        Assert.Equal(RestaurantStatus.Active, area.Status);
        Assert.IsType<DiningAreaCreated>(Assert.Single(area.DomainEvents));
    }

    [Fact]
    public void Rename_DifferentName_RaisesDiningAreaRenamed()
    {
        var area = DiningArea.Create(BranchId.New(), DiningAreaName.Create("Patio"));
        area.ClearDomainEvents();

        area.Rename(DiningAreaName.Create("Main Hall"));

        Assert.IsType<DiningAreaRenamed>(Assert.Single(area.DomainEvents));
    }

    [Fact]
    public void Rename_SameName_NoEventRaised()
    {
        var area = DiningArea.Create(BranchId.New(), DiningAreaName.Create("Patio"));
        area.ClearDomainEvents();

        area.Rename(DiningAreaName.Create("Patio"));

        Assert.Empty(area.DomainEvents);
    }

    [Fact]
    public void Deactivate_ThenActivate_RaisesExpectedEvents()
    {
        var area = DiningArea.Create(BranchId.New(), DiningAreaName.Create("Patio"));
        area.ClearDomainEvents();

        area.Deactivate();
        Assert.Equal(RestaurantStatus.Inactive, area.Status);
        Assert.IsType<DiningAreaDeactivated>(Assert.Single(area.DomainEvents));

        area.ClearDomainEvents();
        area.Activate();
        Assert.Equal(RestaurantStatus.Active, area.Status);
        Assert.IsType<DiningAreaActivated>(Assert.Single(area.DomainEvents));
    }

    [Fact]
    public void Activate_AlreadyActive_Throws()
    {
        var area = DiningArea.Create(BranchId.New(), DiningAreaName.Create("Patio"));

        Assert.Throws<RestaurantDomainException>(() => area.Activate());
    }

    [Fact]
    public void Deactivate_AlreadyInactive_Throws()
    {
        var area = DiningArea.Create(BranchId.New(), DiningAreaName.Create("Patio"));
        area.Deactivate();

        Assert.Throws<RestaurantDomainException>(() => area.Deactivate());
    }
}
