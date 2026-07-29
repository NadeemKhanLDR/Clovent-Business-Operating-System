using Clovent.Authentication.Application.DependencyInjection;
using Clovent.Authentication.Infrastructure.DependencyInjection;
using Clovent.Desktop.Catalog.Barcodes;
using Clovent.Desktop.Catalog.Brands;
using Clovent.Desktop.Catalog.Categories;
using Clovent.Desktop.Catalog.Prices;
using Clovent.Desktop.Catalog.Products;
using Clovent.Desktop.Catalog.UnitsOfMeasure;
using Clovent.Desktop.Catalog.Variants;
using Clovent.Desktop.Dashboard;
using Clovent.Desktop.DependencyInjection;
using Clovent.Desktop.Identity.Roles;
using Clovent.Desktop.Identity.Users;
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
using Clovent.Desktop.Modules;
using Clovent.Desktop.Navigation;
using Clovent.Desktop.Restaurant.DiningAreas;
using Clovent.Desktop.Restaurant.EndOfDay;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Desktop.Restaurant.Tables;
using Clovent.Desktop.Shell;
using Clovent.Desktop.Startup;
using Clovent.Platform.Bootstrap;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var splash = new SplashScreenService();
        splash.Show("Clovent Business Operating System", "Starting...");

        try
        {
            var bootstrapper = ApplicationBootstrapper
                .Create(basePath: AppContext.BaseDirectory)
                .WithLogging()
                .WithPlatform();

            // Authentication and Identity have no IModule implementation yet
            // (see DesktopModuleCatalog's doc comment), so their own
            // AddApplication()/AddInfrastructure()/AddPersistence() are
            // called directly here, the composition root, rather than via
            // WithModule<T>(). Every one of these three method names is
            // shared by both Authentication.* and Identity.* projects (the
            // documented convention - see AuthenticationInfrastructure.md) -
            // importing both namespaces' extension methods at once would
            // make an unqualified call ambiguous, so this project's `using`s
            // cover Authentication's only and every Identity.* call below is
            // fully qualified instead.
            bootstrapper.Services.AddApplication(bootstrapper.Configuration);
            bootstrapper.Services.AddInfrastructure(bootstrapper.Configuration);
            bootstrapper.Services.AddPersistence(bootstrapper.Configuration);
            Clovent.Identity.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(
                bootstrapper.Services, bootstrapper.Configuration);
            Clovent.Identity.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure(
                bootstrapper.Services, bootstrapper.Configuration);
            Clovent.Identity.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(
                bootstrapper.Services, bootstrapper.Configuration);

            // Milestone 13 ("Organization & Master Data Foundation") adds
            // Clovent.MasterData.* alongside Identity, sharing the same
            // AddApplication()/AddInfrastructure()/AddPersistence() naming
            // convention - fully-qualified for the same ambiguity reason as
            // the Identity calls above.
            Clovent.MasterData.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(
                bootstrapper.Services, bootstrapper.Configuration);
            Clovent.MasterData.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure(
                bootstrapper.Services, bootstrapper.Configuration);
            Clovent.MasterData.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(
                bootstrapper.Services, bootstrapper.Configuration);

            // Milestone 14 ("Product Catalog & Inventory Foundation") adds
            // Clovent.Catalog.* and Clovent.Inventory.* alongside
            // Identity/MasterData, sharing the same
            // AddApplication()/AddInfrastructure()/AddPersistence() naming
            // convention - fully-qualified for the same ambiguity reason as
            // the Identity calls above.
            Clovent.Catalog.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(
                bootstrapper.Services, bootstrapper.Configuration);
            Clovent.Catalog.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure(
                bootstrapper.Services, bootstrapper.Configuration);
            Clovent.Catalog.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(
                bootstrapper.Services, bootstrapper.Configuration);
            Clovent.Inventory.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(
                bootstrapper.Services, bootstrapper.Configuration);
            Clovent.Inventory.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure(
                bootstrapper.Services, bootstrapper.Configuration);
            Clovent.Inventory.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(
                bootstrapper.Services, bootstrapper.Configuration);

            // Milestone 15 ("Restaurant POS Core") adds Clovent.Restaurant.*
            // alongside Catalog/Inventory, sharing the same
            // AddApplication()/AddInfrastructure()/AddPersistence() naming
            // convention - fully-qualified for the same ambiguity reason as
            // the Identity calls above.
            Clovent.Restaurant.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(
                bootstrapper.Services, bootstrapper.Configuration);
            Clovent.Restaurant.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure(
                bootstrapper.Services, bootstrapper.Configuration);
            Clovent.Restaurant.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(
                bootstrapper.Services, bootstrapper.Configuration);

            bootstrapper.Services.AddDesktopHost(bootstrapper.Configuration);
            bootstrapper.Services.LoadModules(bootstrapper.Configuration, DesktopModuleCatalog.ModuleTypes);

            splash.SetDescription("Initializing persistence...");
            var host = bootstrapper.BuildAndInitializeAsync().GetAwaiter().GetResult();

            var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(Program));
            var errorDialogService = host.Services.GetRequiredService<IErrorDialogService>();
            GlobalExceptionHandler.Initialize(logger, errorDialogService);

            // The Dashboard is part of the Shell itself, not a business
            // module (none exists yet - see DesktopModuleCatalog's doc
            // comment), so it registers directly here rather than through
            // DesktopModuleLoader/WithModule<T>().
            var navigationService = host.Services.GetRequiredService<INavigationService>();
            navigationService.Register("dashboard", () => host.Services.GetRequiredService<DashboardView>());

            // User Administration gap-closing pass - registered the same way
            // as Dashboard, since Identity has no IModule implementation yet
            // (see DesktopModuleCatalog's doc comment).
            navigationService.Register("users", () => host.Services.GetRequiredService<UserListView>());
            navigationService.Register("roles", () => host.Services.GetRequiredService<RoleEditorView>());

            // Milestone 13 ("Organization & Master Data Foundation") management
            // screens - registered the same way as Dashboard, since no business
            // module infrastructure exists yet for Organization/MasterData
            // (see DesktopModuleCatalog's doc comment).
            navigationService.Register("organizations", () => host.Services.GetRequiredService<OrganizationManagementView>());
            navigationService.Register("companies", () => host.Services.GetRequiredService<CompanyManagementView>());
            navigationService.Register("branches", () => host.Services.GetRequiredService<BranchManagementView>());
            navigationService.Register("departments", () => host.Services.GetRequiredService<DepartmentManagementView>());
            navigationService.Register("warehouses", () => host.Services.GetRequiredService<WarehouseManagementView>());
            navigationService.Register("terminals", () => host.Services.GetRequiredService<TerminalManagementView>());
            navigationService.Register("fiscalyears", () => host.Services.GetRequiredService<FiscalYearManagementView>());
            navigationService.Register("currencies", () => host.Services.GetRequiredService<CurrencyManagementView>());
            navigationService.Register("businesssettings", () => host.Services.GetRequiredService<BusinessSettingsManagementView>());

            // Milestone 14 ("Product Catalog & Inventory Foundation") management
            // screens - registered the same way as Milestone 13's, since no
            // business module infrastructure exists yet for Catalog/Inventory
            // (see DesktopModuleCatalog's doc comment).
            navigationService.Register("categories", () => host.Services.GetRequiredService<ProductCategoryManagementView>());
            navigationService.Register("brands", () => host.Services.GetRequiredService<BrandManagementView>());
            navigationService.Register("units", () => host.Services.GetRequiredService<UnitOfMeasureManagementView>());
            navigationService.Register("products", () => host.Services.GetRequiredService<ProductManagementView>());
            navigationService.Register("variants", () => host.Services.GetRequiredService<ProductVariantManagementView>());
            navigationService.Register("barcodes", () => host.Services.GetRequiredService<BarcodeManagementView>());
            navigationService.Register("prices", () => host.Services.GetRequiredService<ProductPriceManagementView>());
            navigationService.Register("warehousestocks", () => host.Services.GetRequiredService<WarehouseStockManagementView>());
            navigationService.Register("stockadjustments", () => host.Services.GetRequiredService<StockAdjustmentManagementView>());
            navigationService.Register("stocktransfers", () => host.Services.GetRequiredService<StockTransferManagementView>());
            navigationService.Register("inventorytransactions", () => host.Services.GetRequiredService<InventoryTransactionsView>());

            // Milestone 15 ("Restaurant POS Core") management screens -
            // registered the same way as Milestones 13/14's.
            navigationService.Register("diningareas", () => host.Services.GetRequiredService<DiningAreaManagementView>());
            navigationService.Register("tables", () => host.Services.GetRequiredService<TableManagementView>());
            navigationService.Register("pos", () => host.Services.GetRequiredService<RestaurantPosView>());
            navigationService.Register("runningorders", () => host.Services.GetRequiredService<RunningOrdersView>());
            navigationService.Register("holdorders", () => host.Services.GetRequiredService<HoldOrdersView>());
            navigationService.Register("kitchentickets", () => host.Services.GetRequiredService<KitchenTicketViewerView>());

            // End-of-Day reporting gap-closing pass.
            navigationService.Register("endofday", () => host.Services.GetRequiredService<EndOfDayReportView>());

            splash.SetDescription("Loading sign-in...");
            var loginForm = host.Services.GetRequiredService<LoginForm>();

            var signedIn = false;
            loginForm.LoginSucceeded += (_, _) => signedIn = true;

            splash.Close();
            Application.Run(loginForm);

            if (signedIn)
            {
                var shell = host.Services.GetRequiredService<ShellForm>();
                navigationService.NavigateTo("dashboard");
                Application.Run(shell);
            }
        }
        catch (Exception ex)
        {
            splash.Close();
            MessageBox.Show(
                $"Clovent Business Operating System failed to start:\n\n{ex.Message}",
                "Startup Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
