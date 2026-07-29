using Clovent.Authentication.Application;
using Clovent.Desktop.Catalog.Barcodes;
using Clovent.Desktop.Catalog.Brands;
using Clovent.Desktop.Catalog.Categories;
using Clovent.Desktop.Catalog.Prices;
using Clovent.Desktop.Catalog.Products;
using Clovent.Desktop.Catalog.UnitsOfMeasure;
using Clovent.Desktop.Catalog.Variants;
using Clovent.Desktop.Composition;
using Clovent.Desktop.Dashboard;
using Clovent.Desktop.Inventory.Adjustments;
using Clovent.Desktop.Inventory.Transactions;
using Clovent.Desktop.Inventory.Transfers;
using Clovent.Desktop.Inventory.WarehouseStocks;
using Clovent.Desktop.Login;
using Clovent.Desktop.MasterData.Branches;
using Clovent.Desktop.MasterData.Companies;
using Clovent.Desktop.MasterData.Currencies;
using Clovent.Desktop.MasterData.Departments;
using Clovent.Desktop.MasterData.FiscalYears;
using Clovent.Desktop.MasterData.Organizations;
using Clovent.Desktop.MasterData.Settings;
using Clovent.Desktop.MasterData.Terminals;
using Clovent.Desktop.MasterData.Warehouses;
using Clovent.Desktop.Navigation;
using Clovent.Desktop.Notifications;
using Clovent.Desktop.Restaurant.DiningAreas;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Desktop.Restaurant.Tables;
using Clovent.Desktop.Seed;
using Clovent.Desktop.Sessions;
using Clovent.Desktop.Shell;
using Clovent.Desktop.Startup;
using Clovent.Desktop.Theming;
using Clovent.Platform.Bootstrap;
using Clovent.Platform.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Clovent.Desktop.DependencyInjection;

/// <summary>
/// Registers everything the desktop host itself needs (as opposed to any
/// business module) - theming, splash screen, global error dialog, the
/// Shell window, the navigation framework, the Login form, and the
/// Dashboard view. Follows the same
/// AddApplication()/AddInfrastructure()/AddPersistence() naming convention
/// every other project in the solution uses, split the same way: options
/// binding and the navigation/theme *contracts* are "Application"-shaped,
/// the DevExpress-backed implementations are "Infrastructure"-shaped. Both
/// are registered together here since, unlike a business module, the
/// desktop host has exactly one implementation of each and no second host
/// ever swaps them out.
/// </summary>
public static class DesktopServiceCollectionExtensions
{
    /// <summary>Registers every desktop-host-owned service described above.</summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">Configuration <see cref="DesktopOptions"/> is bound from.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddDesktopHost(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatedOptions<DesktopOptions>(configuration, DesktopOptions.SectionName);

        services.TryAddSingleton<IThemeService, ThemeService>();
        services.TryAddSingleton<ISplashScreenService, SplashScreenService>();
        services.TryAddSingleton<IErrorDialogService, ErrorDialogService>();

        services.TryAddSingleton<ShellForm>();
        services.TryAddSingleton<IWorkspaceHost>(sp => sp.GetRequiredService<ShellForm>());
        services.TryAddSingleton<INavigationService, NavigationService>();
        services.TryAddScoped<NavigationMenuBuilder>();

        services.TryAddSingleton<INotificationService, NotificationService>();
        services.TryAddSingleton<IRecentItemsService, RecentItemsService>();

        services.AddSingleton<IStartupTask, ThemeInitializationStartupTask>();
        services.AddScoped<IStartupTask, DevelopmentUserSeedStartupTask>();
        services.AddScoped<IStartupTask, DevelopmentAuthorizationSeedStartupTask>();
        services.AddScoped<IStartupTask, DevelopmentMasterDataSeedStartupTask>();
        services.AddScoped<IStartupTask, DevelopmentCatalogSeedStartupTask>();
        services.AddScoped<IStartupTask, DevelopmentRestaurantSeedStartupTask>();

        services.TryAddSingleton<ICurrentSession, CurrentSession>();
        services.TryAddScoped<IIdentityUserService, IdentityUserServiceAdapter>();

        // Milestone 9 ("Authentication Integration") replaces the
        // Milestone 8 placeholder with the real implementation.
        // LoginForm itself did not change.
        services.TryAddSingleton<ILoginService, LoginService>();
        services.TryAddTransient<LoginForm>();

        services.TryAddTransient<DashboardView>();

        // Milestone 13 ("Organization & Master Data Foundation") management
        // screens - Transient, matching DashboardView's convention: each
        // navigation creates a fresh instance, which starts its own DI scope
        // for the Scoped services it needs (IMediator, IFeatureAuthorizationPolicy).
        services.TryAddTransient<OrganizationManagementView>();
        services.TryAddTransient<CompanyManagementView>();
        services.TryAddTransient<BranchManagementView>();
        services.TryAddTransient<DepartmentManagementView>();
        services.TryAddTransient<WarehouseManagementView>();
        services.TryAddTransient<TerminalManagementView>();
        services.TryAddTransient<FiscalYearManagementView>();
        services.TryAddTransient<CurrencyManagementView>();
        services.TryAddTransient<BusinessSettingsManagementView>();

        // Milestone 14 ("Product Catalog & Inventory Foundation") management
        // screens - same Transient-per-navigation convention as Milestone 13's.
        services.TryAddTransient<ProductCategoryManagementView>();
        services.TryAddTransient<BrandManagementView>();
        services.TryAddTransient<UnitOfMeasureManagementView>();
        services.TryAddTransient<ProductManagementView>();
        services.TryAddTransient<ProductVariantManagementView>();
        services.TryAddTransient<BarcodeManagementView>();
        services.TryAddTransient<ProductPriceManagementView>();
        services.TryAddTransient<WarehouseStockManagementView>();
        services.TryAddTransient<StockAdjustmentManagementView>();
        services.TryAddTransient<StockTransferManagementView>();
        services.TryAddTransient<InventoryTransactionsView>();

        // Milestone 15 ("Restaurant POS Core") management screens - same
        // Transient-per-navigation convention as Milestones 13/14's.
        services.TryAddTransient<DiningAreaManagementView>();
        services.TryAddTransient<TableManagementView>();
        services.TryAddTransient<RestaurantPosView>();
        services.TryAddTransient<RunningOrdersView>();
        services.TryAddTransient<HoldOrdersView>();
        services.TryAddTransient<KitchenTicketViewerView>();

        return services;
    }
}
