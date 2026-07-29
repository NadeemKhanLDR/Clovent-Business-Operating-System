using Clovent.Desktop.Shell;
using Xunit;

namespace Clovent.Desktop.Tests.Shell;

public class RecentItemsServiceTests
{
    [Fact]
    public void RecordCompanySelected_AddsToFront()
    {
        var service = new RecentItemsService();

        service.RecordCompanySelected("Acme Corp");
        service.RecordCompanySelected("Globex");

        Assert.Equal(["Globex", "Acme Corp"], service.RecentCompanies);
    }

    [Fact]
    public void RecordCompanySelected_ReselectingMovesToFrontWithoutDuplicating()
    {
        var service = new RecentItemsService();
        service.RecordCompanySelected("Acme Corp");
        service.RecordCompanySelected("Globex");

        service.RecordCompanySelected("Acme Corp");

        Assert.Equal(["Acme Corp", "Globex"], service.RecentCompanies);
    }

    [Fact]
    public void RecordCompanySelected_CapsAtFiveEntries()
    {
        var service = new RecentItemsService();

        for (var i = 1; i <= 6; i++)
        {
            service.RecordCompanySelected($"Company {i}");
        }

        Assert.Equal(5, service.RecentCompanies.Count);
        Assert.Equal("Company 6", service.RecentCompanies[0]);
        Assert.DoesNotContain("Company 1", service.RecentCompanies);
    }

    [Fact]
    public void RecordBranchSelected_IsIndependentOfCompanies()
    {
        var service = new RecentItemsService();

        service.RecordCompanySelected("Acme Corp");
        service.RecordBranchSelected("Downtown");

        Assert.Equal(["Acme Corp"], service.RecentCompanies);
        Assert.Equal(["Downtown"], service.RecentBranches);
    }
}
