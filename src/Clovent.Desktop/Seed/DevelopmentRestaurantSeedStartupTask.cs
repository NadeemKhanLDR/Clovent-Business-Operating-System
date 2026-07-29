using Clovent.Desktop.Theming;
using Clovent.Identity.Companies;
using Clovent.Identity.Organizations;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.Platform.Bootstrap;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.DiningAreas.ValueObjects;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using Clovent.Restaurant.Tables;
using Microsoft.Extensions.Options;

namespace Clovent.Desktop.Seed;

/// <summary>
/// Development-only convenience: creates one demo dining area with three
/// tables under the demo Branch (seeded by
/// <see cref="DevelopmentMasterDataSeedStartupTask"/>, which must run
/// first) plus two payment methods (Cash, Credit Card), if no dining area
/// exists yet - so the Milestone 15 ("Restaurant POS Core") screens are
/// demonstrable end-to-end without a separate provisioning flow. Gated by
/// <see cref="DesktopOptions.SeedDevelopmentRestaurantData"/>, mirroring
/// <see cref="DevelopmentCatalogSeedStartupTask"/>'s identical reasoning.
/// </summary>
public sealed class DevelopmentRestaurantSeedStartupTask(
    IOrganizationRepository organizationRepository,
    ICompanyRepository companyRepository,
    IDiningAreaRepository diningAreaRepository,
    ITableRepository tableRepository,
    IPaymentMethodRepository paymentMethodRepository,
    RestaurantDbContext restaurantDbContext,
    IOptions<DesktopOptions> options) : IStartupTask
{
    /// <inheritdoc/>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!options.Value.SeedDevelopmentRestaurantData)
        {
            return;
        }

        var branchId = await ResolveBranchIdOrDefaultAsync(cancellationToken);
        if (branchId is null)
        {
            return;
        }

        var existingAreas = await diningAreaRepository.GetByBranchIdAsync(branchId.Value, cancellationToken);
        if (existingAreas.Count > 0)
        {
            return;
        }

        var diningArea = DiningArea.Create(branchId.Value, DiningAreaName.Create("Main Hall"));
        var tableOne = Table.Create(diningArea.Id, EntityCode.Create("T-01"), 2);
        var tableTwo = Table.Create(diningArea.Id, EntityCode.Create("T-02"), 4);
        var tableThree = Table.Create(diningArea.Id, EntityCode.Create("T-03"), 6);

        var cash = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        var creditCard = PaymentMethod.Create(PaymentMethodName.Create("Credit Card"));

        await diningAreaRepository.AddAsync(diningArea, cancellationToken);
        await tableRepository.AddAsync(tableOne, cancellationToken);
        await tableRepository.AddAsync(tableTwo, cancellationToken);
        await tableRepository.AddAsync(tableThree, cancellationToken);
        await paymentMethodRepository.AddAsync(cash, cancellationToken);
        await paymentMethodRepository.AddAsync(creditCard, cancellationToken);
        await restaurantDbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Clovent.Identity.Branches.BranchId?> ResolveBranchIdOrDefaultAsync(CancellationToken cancellationToken)
    {
        var organizations = await organizationRepository.GetAllAsync(cancellationToken);
        if (organizations.Count == 0 || organizations.First().CompanyIds.Count == 0)
        {
            return null;
        }

        var company = await companyRepository.GetByIdAsync(organizations.First().CompanyIds.First(), cancellationToken);
        if (company is null || company.BranchIds.Count == 0)
        {
            return null;
        }

        return company.BranchIds.First();
    }
}
