using Clovent.Desktop.Theming;
using Clovent.Identity.Branches;
using Clovent.Identity.Branches.ValueObjects;
using Clovent.Identity.Companies;
using Clovent.Identity.Companies.ValueObjects;
using Clovent.Identity.Infrastructure.Persistence;
using Clovent.Identity.Organizations;
using Clovent.Identity.Organizations.ValueObjects;
using Clovent.MasterData.Currencies;
using Clovent.MasterData.Departments;
using Clovent.MasterData.Departments.ValueObjects;
using Clovent.MasterData.FiscalYears;
using Clovent.MasterData.FiscalYears.ValueObjects;
using Clovent.MasterData.Infrastructure.Persistence;
using Clovent.MasterData.Languages;
using Clovent.MasterData.Settings;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Terminals;
using Clovent.MasterData.Terminals.ValueObjects;
using Clovent.MasterData.TimeZones;
using Clovent.MasterData.Warehouses;
using Clovent.MasterData.Warehouses.ValueObjects;
using Clovent.Platform.Bootstrap;
using Microsoft.Extensions.Options;

namespace Clovent.Desktop.Seed;

/// <summary>
/// Development-only convenience: creates one demo Organization -&gt; Company
/// -&gt; Branch hierarchy (with a Department, Warehouse, Terminal, and Fiscal
/// Year underneath) plus baseline reference data (currencies, languages,
/// time zones) and a Business Settings record, if no organization exists
/// yet - so the Dashboard's Current Organization/Company/Branch/Fiscal Year
/// tiles (Milestone 13) are demonstrable end-to-end without a separate
/// provisioning flow. Gated by <see cref="DesktopOptions.SeedDevelopmentMasterData"/>,
/// mirroring <see cref="DevelopmentUserSeedStartupTask"/>'s identical
/// reasoning. Not a substitute for a real organization-provisioning feature.
/// </summary>
public sealed class DevelopmentMasterDataSeedStartupTask(
    IOrganizationRepository organizationRepository,
    ICompanyRepository companyRepository,
    IBranchRepository branchRepository,
    IdentityDbContext identityDbContext,
    IDepartmentRepository departmentRepository,
    IWarehouseRepository warehouseRepository,
    ITerminalRepository terminalRepository,
    IFiscalYearRepository fiscalYearRepository,
    ICurrencyRepository currencyRepository,
    ILanguageRepository languageRepository,
    ITimeZoneRepository timeZoneRepository,
    IBusinessSettingsRepository businessSettingsRepository,
    MasterDataDbContext masterDataDbContext,
    TimeProvider timeProvider,
    IOptions<DesktopOptions> options) : IStartupTask
{
    /// <inheritdoc/>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!options.Value.SeedDevelopmentMasterData)
        {
            return;
        }

        var existingOrganizations = await organizationRepository.GetAllAsync(cancellationToken);
        if (existingOrganizations.Count > 0)
        {
            return;
        }

        var organization = Organization.Create(OrganizationName.Create("Clovent Demo Organization"));
        var company = Company.Create(organization.Id, CompanyName.Create("Clovent Demo Company"));
        organization.AddCompany(company.Id);
        var branch = Branch.Create(company.Id, BranchName.Create("Main Branch"));
        company.AddBranch(branch.Id);

        await organizationRepository.AddAsync(organization, cancellationToken);
        await companyRepository.AddAsync(company, cancellationToken);
        await branchRepository.AddAsync(branch, cancellationToken);
        await identityDbContext.SaveChangesAsync(cancellationToken);

        var department = Department.Create(branch.Id, DepartmentName.Create("Administration"));
        var warehouse = Warehouse.Create(branch.Id, WarehouseName.Create("Main Warehouse"), EntityCode.Create("WH-01"));
        var terminal = Terminal.Create(branch.Id, TerminalName.Create("Front Counter"), EntityCode.Create("T-001"));

        var usd = Currency.Create(CurrencyCode.Create("USD"), "US Dollar", "$", 2);
        var eur = Currency.Create(CurrencyCode.Create("EUR"), "Euro", "€", 2);

        var english = Language.Create(LanguageCode.Create("en"), "English", "English");
        var spanish = Language.Create(LanguageCode.Create("es"), "Spanish", "Español");

        var utc = TimeZoneEntry.Create(IanaId.Create("UTC"), "(UTC) Coordinated Universal Time", 0);
        var eastern = TimeZoneEntry.Create(IanaId.Create("America/New_York"), "(UTC-05:00) Eastern Time", -300);

        var today = timeProvider.GetUtcNow();
        var yearStart = new DateOnly(today.Year, 1, 1);
        var yearEnd = new DateOnly(today.Year, 12, 31);
        var fiscalYear = FiscalYear.Create(organization.Id, FiscalYearName.Create($"FY{today.Year}"), yearStart, yearEnd);

        var businessSettings = BusinessSettings.Create(organization.Id, usd.Id, english.Id, utc.Id, "MM/dd/yyyy");
        businessSettings.UpdateDefaults(usd.Id, english.Id, utc.Id, fiscalYear.Id, "MM/dd/yyyy");

        await departmentRepository.AddAsync(department, cancellationToken);
        await warehouseRepository.AddAsync(warehouse, cancellationToken);
        await terminalRepository.AddAsync(terminal, cancellationToken);
        await currencyRepository.AddAsync(usd, cancellationToken);
        await currencyRepository.AddAsync(eur, cancellationToken);
        await languageRepository.AddAsync(english, cancellationToken);
        await languageRepository.AddAsync(spanish, cancellationToken);
        await timeZoneRepository.AddAsync(utc, cancellationToken);
        await timeZoneRepository.AddAsync(eastern, cancellationToken);
        await fiscalYearRepository.AddAsync(fiscalYear, cancellationToken);
        await businessSettingsRepository.AddAsync(businessSettings, cancellationToken);
        await masterDataDbContext.SaveChangesAsync(cancellationToken);
    }
}
