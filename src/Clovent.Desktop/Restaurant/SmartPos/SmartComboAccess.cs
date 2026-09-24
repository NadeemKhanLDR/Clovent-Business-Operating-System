using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Identity.Users;
using Clovent.Identity.Branches;
using Clovent.Identity.Companies;
using Clovent.MasterData.Warehouses;
using Clovent.MasterData.Settings;
using Clovent.MasterData.Currencies;
using Clovent.Restaurant.Application.SmartCombos;
namespace Clovent.Desktop.Restaurant.SmartPos;

/// <summary>Fail-closed company/branch scope derived from the authenticated user, never from UI claims.</summary>
public sealed class SmartComboAccess(ICurrentSession session, IUserRepository users, IBranchRepository branches,
    ICompanyRepository companies, IWarehouseRepository warehouses, IBusinessSettingsRepository settings,
    ICurrencyRepository currencies, IFeatureAuthorizationPolicy features, IMenuAuthorizationPolicy menus) : ISmartComboAccess
{
    private async Task<User> UserAsync(CancellationToken ct)
    {
        if (session.UserId is not { } id || !await menus.CanViewMenuItemAsync(id, "smartcombos", ct)) throw new UnauthorizedAccessException("Smart Combo Builder access is required.");
        var user = await users.GetByIdAsync(new UserId(id), ct);
        if (user == null || user.Status != UserStatus.Active || user.CompanyId == null)
            throw new UnauthorizedAccessException("An active user with an assigned company is required. Assign company/branch in User Administration.");
        return user;
    }
    public async Task<IReadOnlyList<ComboLocation>> LocationsAsync(CancellationToken ct)
    {
        var user = await UserAsync(ct);
        var allowed = (await branches.GetByCompanyIdAsync(user.CompanyId!.Value, ct))
            .Where(b => b.Status == BranchStatus.Active && (user.BranchId == null || b.Id == user.BranchId)).Select(b => b.Id).ToHashSet();
        return (await warehouses.GetAllAsync(ct)).Where(w => w.Status.ToString() == "Active" && allowed.Contains(w.BranchId))
            .OrderBy(w => w.Name.Value).Select(w => new ComboLocation(w.Id.Value, w.Name.Value)).ToList();
    }
    public async Task<Guid> RequireAsync(string operation, Guid warehouseId, CancellationToken ct)
    {
        var user = await UserAsync(ct);
        if (!await features.CanUseFeatureAsync(user.Id.Value, "smartcombos." + operation, ct) ||
            !(await LocationsAsync(ct)).Any(x => x.Id == warehouseId) ||
            operation == "create" && !await features.CanUseFeatureAsync(user.Id.Value, "quickordertemplates.create", ct))
            throw new UnauthorizedAccessException("This action or location is not permitted.");
        return user.Id.Value;
    }
    public async Task<ComboCurrency> CurrencyAsync(Guid warehouseId, CancellationToken ct)
    {
        if (!(await LocationsAsync(ct)).Any(x => x.Id == warehouseId)) throw new UnauthorizedAccessException("Location is not permitted.");
        var user = await UserAsync(ct);
        var company = await companies.GetByIdAsync(user.CompanyId!.Value, ct) ?? throw new InvalidOperationException("Company not found.");
        var configured = await settings.GetByOrganizationIdAsync(company.OrganizationId, ct);
        var currency = configured != null ? await currencies.GetByIdAsync(configured.DefaultCurrencyId, ct) : (await currencies.GetAllAsync(ct)).SingleOrDefault();
        if (currency == null || currency.Status.ToString() != "Active") throw new InvalidOperationException("Configure the organization's active default currency first.");
        return new(currency.Id.Value, currency.DecimalPlaces, currency.Symbol);
    }
}

