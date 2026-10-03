using System.Text.Json;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Licensing;
using Clovent.Identity.Branches;
using Clovent.Identity.Branches.ValueObjects;
using Clovent.Identity.Companies;
using Clovent.Identity.Companies.ValueObjects;
using Clovent.Identity.Infrastructure.Persistence;
using Clovent.Identity.Organizations;
using Clovent.Identity.Organizations.ValueObjects;
using Clovent.Identity.Shared.ValueObjects;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Commissioning.Services;

/// <summary>
/// Parameters for provisioning the foundational business hierarchy, master data, and regional settings.
/// </summary>
public sealed class InitialMasterDataProvisioningRequest
{
    /// <summary>Organization legal name (e.g. "Acme Hospitality Group").</summary>
    public string OrganizationName { get; set; } = "Clovent Business Solutions";

    /// <summary>Optional organization tax identifier or corporate code.</summary>
    public string? OrganizationCode { get; set; } = "ORG-001";

    /// <summary>Operating company legal name (e.g. "Acme Dining LLC").</summary>
    public string CompanyName { get; set; } = "Headquarters Company";

    /// <summary>Optional company registration identifier or code.</summary>
    public string? CompanyCode { get; set; } = "COMP-001";

    /// <summary>Store or branch location display name (e.g. "Downtown Flagship").</summary>
    public string BranchName { get; set; } = "Main Branch";

    /// <summary>Optional branch code or store number.</summary>
    public string? BranchCode { get; set; } = "BR-001";

    /// <summary>Default stock-holding warehouse name (e.g. "Main Warehouse").</summary>
    public string WarehouseName { get; set; } = "Main Warehouse";

    /// <summary>Default warehouse short code (e.g. "WH-01").</summary>
    public string WarehouseCode { get; set; } = "WH-01";

    /// <summary>Workstation POS terminal name (e.g. "Front Counter").</summary>
    public string TerminalName { get; set; } = "Front Counter";

    /// <summary>Workstation POS terminal short code (e.g. "T-001").</summary>
    public string TerminalCode { get; set; } = "T-001";

    /// <summary>Hardware identifier of the machine hosting this terminal (defaults to current hardware fingerprint).</summary>
    public string? HardwareId { get; set; }

    /// <summary>ISO 4217 currency code (e.g. "USD", "EUR", "PKR").</summary>
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>Currency display name (e.g. "US Dollar").</summary>
    public string CurrencyName { get; set; } = "US Dollar";

    /// <summary>Currency symbol (e.g. "$", "€", "Rs.").</summary>
    public string CurrencySymbol { get; set; } = "$";

    /// <summary>Currency decimal precision (0 to 4).</summary>
    public int CurrencyPrecision { get; set; } = 2;

    /// <summary>Language ISO code (e.g. "en", "ur", "es").</summary>
    public string LanguageCode { get; set; } = "en";

    /// <summary>Language display name (e.g. "English").</summary>
    public string LanguageName { get; set; } = "English";

    /// <summary>Timezone identifier (e.g. "UTC", "America/New_York", "Asia/Karachi").</summary>
    public string TimeZoneIanaId { get; set; } = "UTC";

    /// <summary>Timezone display description.</summary>
    public string? TimeZoneDisplayName { get; set; } = "Coordinated Universal Time (UTC+00:00)";

    /// <summary>Timezone standard offset in minutes from UTC.</summary>
    public int TimeZoneOffsetMinutes { get; set; } = 0;

    /// <summary>Date display format pattern (e.g. "dd-MMM-yyyy" or "MM/dd/yyyy").</summary>
    public string DateFormat { get; set; } = "dd-MMM-yyyy";

    /// <summary>Time format mode: "12 Hour" or "24 Hour".</summary>
    public string TimeFormat { get; set; } = "12 Hour";

    /// <summary>Quantity decimal display precision (0 to 4).</summary>
    public int QuantityPrecision { get; set; } = 2;
}

/// <summary>
/// Result of the foundational master data provisioning operation.
/// </summary>
public sealed class InitialMasterDataProvisioningResult
{
    /// <summary>Indicates whether provisioning succeeded.</summary>
    public bool Success { get; set; }

    /// <summary>Descriptive outcome message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Created or existing organization identifier.</summary>
    public Guid OrganizationId { get; set; }

    /// <summary>Created or existing company identifier.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Created or existing branch identifier.</summary>
    public Guid BranchId { get; set; }

    /// <summary>Created or existing warehouse identifier.</summary>
    public Guid WarehouseId { get; set; }

    /// <summary>Created or existing terminal identifier.</summary>
    public Guid TerminalId { get; set; }

    /// <summary>Created or updated business settings identifier.</summary>
    public Guid BusinessSettingsId { get; set; }

    /// <summary>Hardware identifier associated with this workstation terminal.</summary>
    public string HardwareId { get; set; } = string.Empty;
}

/// <summary>
/// Workstation terminal hardware mapping record stored locally.
/// </summary>
public sealed class TerminalHardwareMapping
{
    public Guid TerminalId { get; set; }
    public string TerminalName { get; set; } = string.Empty;
    public string TerminalCode { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
    public string HardwareId { get; set; } = string.Empty;
    public DateTimeOffset AssociatedAtUtc { get; set; }
}

/// <summary>
/// Contract for provisioning the foundational business hierarchy and master data.
/// </summary>
public interface IInitialMasterDataProvisioningService
{
    /// <summary>
    /// Idempotently provisions the business hierarchy, default warehouse, terminal, currency, and business settings.
    /// </summary>
    Task<InitialMasterDataProvisioningResult> ProvisionInitialMasterDataAsync(
        IServiceProvider services,
        InitialMasterDataProvisioningRequest request,
        CancellationToken ct = default);
}

/// <summary>
/// Implements Requirements 10, 13, 14, and 15:
/// Foundational business hierarchy provisioning (Organization -> Company -> Branch -> Warehouse -> Terminal)
/// and business settings configuration. Fully idempotent to ensure robust recovery from partial setup interruptions.
/// </summary>
public sealed class InitialMasterDataProvisioningService(ILogger<InitialMasterDataProvisioningService>? logger = null)
    : IInitialMasterDataProvisioningService
{
    /// <inheritdoc/>
    public async Task<InitialMasterDataProvisioningResult> ProvisionInitialMasterDataAsync(
        IServiceProvider services,
        InitialMasterDataProvisioningRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(request);

        using var scope = services.CreateScope();
        var identityDbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var masterDataDbContext = scope.ServiceProvider.GetRequiredService<MasterDataDbContext>();

        try
        {
            // 1. Organization (Idempotent: find existing or create new)
            var organization = await identityDbContext.Organizations.FirstOrDefaultAsync(ct).ConfigureAwait(false);
            if (organization == null)
            {
                TaxId? taxId = !string.IsNullOrWhiteSpace(request.OrganizationCode)
                    ? TaxId.Create(request.OrganizationCode.Trim())
                    : null;

                var orgName = OrganizationName.Create(
                    string.IsNullOrWhiteSpace(request.OrganizationName) ? "Clovent Business Solutions" : request.OrganizationName.Trim());

                organization = Organization.Create(orgName, taxId);
                await identityDbContext.Organizations.AddAsync(organization, ct).ConfigureAwait(false);
                await identityDbContext.SaveChangesAsync(ct).ConfigureAwait(false);
                logger?.LogInformation("Created foundational Organization: {OrganizationName} ({OrganizationId})", organization.Name.Value, organization.Id.Value);
            }
            else
            {
                logger?.LogInformation("Reusing existing Organization: {OrganizationName} ({OrganizationId})", organization.Name.Value, organization.Id.Value);
            }

            // 2. Company (Idempotent: find existing under org or create new)
            var company = await identityDbContext.Companies
                .FirstOrDefaultAsync(c => c.OrganizationId == organization.Id, ct)
                .ConfigureAwait(false);

            if (company == null)
            {
                TaxId? compTaxId = !string.IsNullOrWhiteSpace(request.CompanyCode)
                    ? TaxId.Create(request.CompanyCode.Trim())
                    : null;

                var compName = CompanyName.Create(
                    string.IsNullOrWhiteSpace(request.CompanyName) ? "Headquarters Company" : request.CompanyName.Trim());

                company = Company.Create(organization.Id, compName, compTaxId);
                organization.AddCompany(company.Id);
                await identityDbContext.Companies.AddAsync(company, ct).ConfigureAwait(false);
                await identityDbContext.SaveChangesAsync(ct).ConfigureAwait(false);
                logger?.LogInformation("Created foundational Company: {CompanyName} ({CompanyId})", company.Name.Value, company.Id.Value);
            }
            else
            {
                logger?.LogInformation("Reusing existing Company: {CompanyName} ({CompanyId})", company.Name.Value, company.Id.Value);
            }

            // 3. Branch (Idempotent: find existing under company or create new)
            var branch = await identityDbContext.Branches
                .FirstOrDefaultAsync(b => b.CompanyId == company.Id, ct)
                .ConfigureAwait(false);

            if (branch == null)
            {
                var brName = BranchName.Create(
                    string.IsNullOrWhiteSpace(request.BranchName) ? "Main Branch" : request.BranchName.Trim());

                branch = Branch.Create(company.Id, brName);
                company.AddBranch(branch.Id);
                await identityDbContext.Branches.AddAsync(branch, ct).ConfigureAwait(false);
                await identityDbContext.SaveChangesAsync(ct).ConfigureAwait(false);
                logger?.LogInformation("Created foundational Branch: {BranchName} ({BranchId})", branch.Name.Value, branch.Id.Value);
            }
            else
            {
                logger?.LogInformation("Reusing existing Branch: {BranchName} ({BranchId})", branch.Name.Value, branch.Id.Value);
            }

            // 4. Department (Idempotent: administration department under branch)
            var department = await masterDataDbContext.Departments
                .FirstOrDefaultAsync(d => d.BranchId == branch.Id, ct)
                .ConfigureAwait(false);

            if (department == null)
            {
                department = Department.Create(branch.Id, DepartmentName.Create("Administration"));
                await masterDataDbContext.Departments.AddAsync(department, ct).ConfigureAwait(false);
                await masterDataDbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            }

            // 5. Warehouse (Idempotent: default warehouse under branch)
            var warehouse = await masterDataDbContext.Warehouses
                .FirstOrDefaultAsync(w => w.BranchId == branch.Id, ct)
                .ConfigureAwait(false);

            if (warehouse == null)
            {
                var whName = WarehouseName.Create(
                    string.IsNullOrWhiteSpace(request.WarehouseName) ? "Main Warehouse" : request.WarehouseName.Trim());

                var whCode = EntityCode.Create(
                    string.IsNullOrWhiteSpace(request.WarehouseCode) ? "WH-01" : request.WarehouseCode.Trim());

                warehouse = Warehouse.Create(branch.Id, whName, whCode);
                await masterDataDbContext.Warehouses.AddAsync(warehouse, ct).ConfigureAwait(false);
                await masterDataDbContext.SaveChangesAsync(ct).ConfigureAwait(false);
                logger?.LogInformation("Created foundational Warehouse: {WarehouseName} ({WarehouseId})", warehouse.Name.Value, warehouse.Id.Value);
            }

            // 6. Terminal (Idempotent: default POS terminal under branch)
            var terminal = await masterDataDbContext.Terminals
                .FirstOrDefaultAsync(t => t.BranchId == branch.Id, ct)
                .ConfigureAwait(false);

            if (terminal == null)
            {
                var termName = TerminalName.Create(
                    string.IsNullOrWhiteSpace(request.TerminalName) ? "Front Counter" : request.TerminalName.Trim());

                var termCode = EntityCode.Create(
                    string.IsNullOrWhiteSpace(request.TerminalCode) ? "T-001" : request.TerminalCode.Trim());

                terminal = Terminal.Create(branch.Id, termName, termCode);
                await masterDataDbContext.Terminals.AddAsync(terminal, ct).ConfigureAwait(false);
                await masterDataDbContext.SaveChangesAsync(ct).ConfigureAwait(false);
                logger?.LogInformation("Created foundational Terminal: {TerminalName} ({TerminalId})", terminal.Name.Value, terminal.Id.Value);
            }

            // 7. Currency Reference Data
            var curCode = CurrencyCode.Create(string.IsNullOrWhiteSpace(request.CurrencyCode) ? "USD" : request.CurrencyCode.Trim());
            var currency = await masterDataDbContext.Currencies
                .FirstOrDefaultAsync(c => c.Code == curCode, ct)
                .ConfigureAwait(false);

            if (currency == null)
            {
                currency = Currency.Create(
                    curCode,
                    string.IsNullOrWhiteSpace(request.CurrencyName) ? "US Dollar" : request.CurrencyName.Trim(),
                    string.IsNullOrWhiteSpace(request.CurrencySymbol) ? "$" : request.CurrencySymbol.Trim(),
                    Math.Clamp(request.CurrencyPrecision, 0, 4));

                await masterDataDbContext.Currencies.AddAsync(currency, ct).ConfigureAwait(false);
                await masterDataDbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            }

            // 8. Language Reference Data
            var langCode = LanguageCode.Create(string.IsNullOrWhiteSpace(request.LanguageCode) ? "en" : request.LanguageCode.Trim());
            var language = await masterDataDbContext.Languages
                .FirstOrDefaultAsync(l => l.Code == langCode, ct)
                .ConfigureAwait(false);

            if (language == null)
            {
                language = Language.Create(
                    langCode,
                    string.IsNullOrWhiteSpace(request.LanguageName) ? "English" : request.LanguageName.Trim(),
                    string.IsNullOrWhiteSpace(request.LanguageName) ? "English" : request.LanguageName.Trim());

                await masterDataDbContext.Languages.AddAsync(language, ct).ConfigureAwait(false);
                await masterDataDbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            }

            // 9. TimeZone Reference Data
            var tzIana = IanaId.Create(string.IsNullOrWhiteSpace(request.TimeZoneIanaId) ? "UTC" : request.TimeZoneIanaId.Trim());
            var timeZone = await masterDataDbContext.TimeZoneEntries
                .FirstOrDefaultAsync(tz => tz.IanaId == tzIana, ct)
                .ConfigureAwait(false);

            if (timeZone == null)
            {
                var tzDisplay = !string.IsNullOrWhiteSpace(request.TimeZoneDisplayName)
                    ? request.TimeZoneDisplayName.Trim()
                    : $"{tzIana.Value}";

                timeZone = TimeZoneEntry.Create(tzIana, tzDisplay, request.TimeZoneOffsetMinutes);
                await masterDataDbContext.TimeZoneEntries.AddAsync(timeZone, ct).ConfigureAwait(false);
                await masterDataDbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            }

            // 10. Fiscal Year Reference Data
            var fiscalYear = await masterDataDbContext.FiscalYears
                .FirstOrDefaultAsync(fy => fy.OrganizationId == organization.Id, ct)
                .ConfigureAwait(false);

            if (fiscalYear == null)
            {
                var today = DateTimeOffset.UtcNow;
                var yearStart = new DateOnly(today.Year, 1, 1);
                var yearEnd = new DateOnly(today.Year, 12, 31);
                fiscalYear = FiscalYear.Create(
                    organization.Id,
                    FiscalYearName.Create($"FY{today.Year}"),
                    yearStart,
                    yearEnd);

                await masterDataDbContext.FiscalYears.AddAsync(fiscalYear, ct).ConfigureAwait(false);
                await masterDataDbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            }

            // 11. Business Settings (Idempotent: update defaults if already exists)
            var dateFormat = string.IsNullOrWhiteSpace(request.DateFormat) ? "dd-MMM-yyyy" : request.DateFormat.Trim();
            var businessSettings = await masterDataDbContext.BusinessSettings
                .FirstOrDefaultAsync(bs => bs.OrganizationId == organization.Id, ct)
                .ConfigureAwait(false);

            if (businessSettings == null)
            {
                businessSettings = BusinessSettings.Create(
                    organization.Id,
                    currency.Id,
                    language.Id,
                    timeZone.Id,
                    dateFormat);

                businessSettings.UpdateDefaults(
                    currency.Id,
                    language.Id,
                    timeZone.Id,
                    fiscalYear.Id,
                    dateFormat);

                await masterDataDbContext.BusinessSettings.AddAsync(businessSettings, ct).ConfigureAwait(false);
            }
            else
            {
                businessSettings.UpdateDefaults(
                    currency.Id,
                    language.Id,
                    timeZone.Id,
                    fiscalYear.Id,
                    dateFormat);
            }
            await masterDataDbContext.SaveChangesAsync(ct).ConfigureAwait(false);

            // 12. Regional Display Formatting & Workstation POS Settings
            var timeFormat = string.Equals(request.TimeFormat, "24 Hour", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(request.TimeFormat, "24", StringComparison.OrdinalIgnoreCase)
                ? "24 Hour"
                : "12 Hour";

            int qtyPrecision = Math.Clamp(request.QuantityPrecision, 0, 4);

            CompanyDisplaySettingsStore.Save(new CompanyDisplaySettings
            {
                DateFormat = dateFormat,
                TimeFormat = timeFormat,
                QuantityPrecision = qtyPrecision
            });

            QuantityDisplay.Configure(qtyPrecision);
            CurrencyDisplay.Configure(currency.Code.Value, currency.Symbol, currency.DecimalPlaces);

            // Set POS workstation default branch and terminal
            PosSettingsStore.SaveBranchId(branch.Id.Value);
            PosSettingsStore.SaveTerminalId(terminal.Id.Value);

            // 13. Terminal Hardware ID Association
            var effectiveHardwareId = !string.IsNullOrWhiteSpace(request.HardwareId)
                ? request.HardwareId.Trim()
                : MachineFingerprint.GetCurrentMachineId();

            await SaveTerminalHardwareAssociationAsync(
                terminal.Id.Value,
                terminal.Name.Value,
                terminal.Code.Value,
                branch.Id.Value,
                effectiveHardwareId,
                ct).ConfigureAwait(false);

            return new InitialMasterDataProvisioningResult
            {
                Success = true,
                Message = "Foundational business hierarchy and settings successfully provisioned.",
                OrganizationId = organization.Id.Value,
                CompanyId = company.Id.Value,
                BranchId = branch.Id.Value,
                WarehouseId = warehouse.Id.Value,
                TerminalId = terminal.Id.Value,
                BusinessSettingsId = businessSettings.Id.Value,
                HardwareId = effectiveHardwareId
            };
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to provision foundational master data.");
            return new InitialMasterDataProvisioningResult
            {
                Success = false,
                Message = $"Failed to provision foundational master data: {ex.Message}"
            };
        }
    }

    private static async Task SaveTerminalHardwareAssociationAsync(
        Guid terminalId,
        string terminalName,
        string terminalCode,
        Guid branchId,
        string hardwareId,
        CancellationToken ct)
    {
        var mapping = new TerminalHardwareMapping
        {
            TerminalId = terminalId,
            TerminalName = terminalName,
            TerminalCode = terminalCode,
            BranchId = branchId,
            HardwareId = hardwareId,
            AssociatedAtUtc = DateTimeOffset.UtcNow
        };

        var json = JsonSerializer.Serialize(mapping, new JsonSerializerOptions { WriteIndented = true });

        // 1. Try writing to machine config
        try
        {
            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var machinePath = Path.Combine(programData, "Clovent", "BusinessOperatingSystem", "Config", "terminal.json");
            var dir = Path.GetDirectoryName(machinePath)!;
            Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(machinePath, json, ct).ConfigureAwait(false);
        }
        catch
        {
            // Fall through to user-level store
        }

        // 2. Also write to user config for local access
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var userPath = Path.Combine(localAppData, "Clovent", "BusinessOperatingSystem", "Config", "terminal.json");
            var userDir = Path.GetDirectoryName(userPath)!;
            Directory.CreateDirectory(userDir);
            await File.WriteAllTextAsync(userPath, json, ct).ConfigureAwait(false);
        }
        catch
        {
            // Best effort write
        }
    }
}
