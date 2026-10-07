using Clovent.Authentication.Application.DependencyInjection;
using Clovent.Authentication.Infrastructure.DependencyInjection;
using Clovent.Desktop.Commissioning.Database;
using Clovent.Desktop.Commissioning.Security;
using Clovent.Desktop.Commissioning.Services;
using Clovent.Desktop.Commissioning.UI;
using Clovent.Desktop.Login;
using Clovent.Desktop.Catalog.Barcodes;
using Clovent.Desktop.Catalog.Brands;
using Clovent.Desktop.Catalog.Categories;
using Clovent.Desktop.Catalog.Prices;
using Clovent.Desktop.Catalog.UnitsOfMeasure;
using Clovent.Desktop.Catalog.Variants;
using Clovent.Desktop.DependencyInjection;
using Clovent.Desktop.Forms.Catalog.Products;
using Clovent.Desktop.Forms.Dashboard;
using Clovent.Desktop.Forms.Identity;
using Clovent.Desktop.Forms.Identity.Roles;
using Clovent.Desktop.Forms.Identity.Users;
using Clovent.Desktop.Forms.Restaurant.ActivityLog;
using Clovent.Desktop.Forms.Restaurant.Appearance;
using Clovent.Desktop.Forms.Restaurant.MenuItems;
using Clovent.Desktop.Forms.Restaurant.Setup;
using Clovent.Desktop.Forms.Shell;
using Clovent.Desktop.Inventory.Adjustments;
using Clovent.Desktop.Inventory.Transactions;
using Clovent.Desktop.Inventory.Transfers;
using Clovent.Desktop.Inventory.WarehouseStocks;
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
using Clovent.Desktop.Restaurant.Customers;
using Clovent.Desktop.Restaurant.DiningAreas;
using Clovent.Desktop.Restaurant.EndOfDay;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Desktop.Restaurant.SmartPos;
using Clovent.Desktop.Restaurant.Tables;
using Clovent.Desktop.Startup;
using Clovent.Platform;
using Clovent.Platform.Bootstrap;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop;

internal static class Program
{
    public static IServiceProvider? Services { get; internal set; }

    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Clovent.Desktop.Forms.Base.DesktopStyle.ApplyGlobalTypography();

        try
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        }
        catch (InvalidOperationException)
        {
        }

        // Handle CLI diagnostics request:
        if (args.Any(a => string.Equals(a, "--diagnostics", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "-diagnostics", StringComparison.OrdinalIgnoreCase)))
        {
            Application.Run(new SupportDiagnosticsForm());
            return;
        }

        // Ensure ProgramData directories and ACLs are established
        ProgramDataAclManager.ConfigureDirectorySecurity();

        // First-Run Commissioning Check (Requirements 1 & 2):
        // If not yet commissioned on this machine, launch First-Run Wizard
        if (!CommissioningStateService.MarkerExists())
        {
            using var wizard = new FirstRunWizardForm();
            var wizardResult = wizard.ShowDialog();
            if (wizardResult != DialogResult.OK)
            {
                return; // User cancelled or exited onboarding
            }
        }

        var splash = new SplashScreenService();
        splash.Show("Clovent Business Operating System", "Starting...");

        try
        {
            var bootstrapper = ApplicationBootstrapper
                .Create(basePath: AppContext.BaseDirectory)
                .WithLogging()
                .WithPlatform();

            var effectiveConnectionString = Clovent.Desktop.Configuration.DatabaseSecretStore.ResolveConnectionString(bootstrapper.Configuration);
            bootstrapper.Configuration["ConnectionStrings:Default"] = effectiveConnectionString;

            // Validate schema compatibility safely without destructive auto-migrations (Requirement 8)
            var schemaValidator = new DatabaseSchemaCompatibilityValidator();
            var schemaResult = Task.Run(() => schemaValidator.ValidateCompatibilityAsync(effectiveConnectionString)).GetAwaiter().GetResult();
            if (schemaResult.Status != SchemaCompatibilityStatus.Compatible)
            {
                splash.Close();
                if (schemaResult.Status == SchemaCompatibilityStatus.DatabaseTooOld)
                {
                    MessageBox.Show(
                        $"Database Schema Update Required:\n\n{schemaResult.Message}\n\nPlease run the database upgrade utility or contact your system administrator.",
                        "Database Update Required",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
                else if (schemaResult.Status == SchemaCompatibilityStatus.DatabaseNewer)
                {
                    MessageBox.Show(
                        $"Database Newer Than Application:\n\n{schemaResult.Message}\n\nPlease update your Clovent Business Operating System installation.",
                        "Software Update Required",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
                else if (schemaResult.Status == SchemaCompatibilityStatus.ConnectionFailed)
                {
                    var choice = MessageBox.Show(
                        $"Clovent Business Operating System was unable to connect to the database:\n\n{schemaResult.Message}\n\nWould you like to open Database Connection Settings to configure your server or credentials?",
                        "Database Connection Required",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Error);

                    if (choice == DialogResult.Yes)
                    {
                        using var dbDialog = new Clovent.Desktop.Configuration.DatabaseConnectionDialog();
                        if (dbDialog.ShowDialog() == DialogResult.OK)
                        {
                            MessageBox.Show(
                                "Database settings saved. The application will now restart with the updated configuration.",
                                "Configuration Saved",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                            Application.Restart();
                        }
                    }
                    return;
                }
            }

            // Persistent file logging: WithLogging() above applies the
            // standard Logging:LogLevel configuration filters and the console
            // provider; this adds a per-user rolling file provider on top
            // (see FileLoggerProvider) so unhandled errors and startup
            // failures leave a diagnostic record that survives restarts -
            // without it, a crash shows a dialog and vanishes.
            bootstrapper.WithLogging(logging => logging.AddFileLogger(bootstrapper.Configuration));

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
            Microsoft.Extensions.Hosting.IHost host;
            try
            {
                host = Task.Run(async () =>
                {
                    var h = await bootstrapper.BuildAndInitializeAsync().ConfigureAwait(false);
                    using var initScope = h.Services.CreateScope();
                    var initMediator = initScope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
                    await Clovent.Desktop.Forms.Base.DateTimeDisplayLoader.ConfigureAsync(initMediator, h.Services).ConfigureAwait(false);
                    await Clovent.Desktop.Forms.Base.CurrencyDisplayLoader.ConfigureAsync(initMediator).ConfigureAwait(false);
                    return h;
                }).GetAwaiter().GetResult();
            }
            catch (Exception initEx)
            {
                splash.Close();
                var msg = initEx.InnerException?.Message ?? initEx.Message;
                var choice = MessageBox.Show(
                    $"Clovent Business Operating System was unable to connect to the database or complete initialization:\n\n{msg}\n\nWould you like to open Database Connection Settings to configure your server or credentials?",
                    "Database Connection Required",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Error);

                if (choice == DialogResult.Yes)
                {
                    using var dbDialog = new Clovent.Desktop.Configuration.DatabaseConnectionDialog();
                    if (dbDialog.ShowDialog() == DialogResult.OK)
                    {
                        MessageBox.Show(
                            "Database settings saved. The application will now restart with the updated configuration.",
                            "Configuration Saved",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                        Application.Restart();
                    }
                }
                return;
            }

            Services = host.Services;

            // Software Licensing and Registration Check (Requirement 2)
            var licenseResult = Clovent.Desktop.Licensing.LicenseService.ValidateCurrentLicense();
            if (!licenseResult.IsAuthorized)
            {
                splash.Close();
                var licChoice = MessageBox.Show(
                    $"Software Registration / Licensing Notice:\n\n{licenseResult.Message}\n\nWould you like to open the Registration window to import a valid license file?",
                    "License Notice",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (licChoice == DialogResult.Yes)
                {
                    using var regForm = new Clovent.Desktop.Licensing.SoftwareRegistrationForm();
                    regForm.ShowDialog();
                    licenseResult = Clovent.Desktop.Licensing.LicenseService.Refresh();
                }

                if (!licenseResult.IsAuthorized)
                {
                    if (licenseResult.Status == Clovent.Desktop.Licensing.LicenseStatus.Expired)
                    {
                        // Non-destructive licensing policy: expired licenses allow read-only data access
                        MessageBox.Show(
                            $"The software evaluation/license has expired ({licenseResult.Message}).\n\nOperating in Read-Only Mode. You can view historical reports and back-office data, but creating new operational transactions is disabled until a valid license is activated.",
                            "Read-Only Mode Active",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show(
                            $"A valid software license was not provided ({licenseResult.Message}). The application cannot run and will now terminate.",
                            "License Required",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }
                }
            }

            var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(Program));
            var errorDialogService = host.Services.GetRequiredService<IErrorDialogService>();
            GlobalExceptionHandler.Initialize(logger, errorDialogService);
            logger.LogInformation("STARTUP: Persistence and display loaders configured successfully.");

            // Register all desktop view factories centrally through NavigationRegistry
            var navigationService = host.Services.GetRequiredService<INavigationService>();
            NavigationRegistry.RegisterAllViews(navigationService, host.Services);

            string? selectedModule = null;
#if DEBUG
            if (args.Any(a => string.Equals(a, "--pos", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "-pos", StringComparison.OrdinalIgnoreCase)))
            {
                var loginService = host.Services.GetRequiredService<ILoginService>();
                var result = loginService.LoginAsync(new LoginRequest("Admin", "Admin123!", null, false)).GetAwaiter().GetResult();
                if (result.Succeeded)
                {
                    selectedModule = "pos";
                }
            }
            else if (args.Any(a => string.Equals(a, "--backoffice", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "-backoffice", StringComparison.OrdinalIgnoreCase)))
            {
                var loginService = host.Services.GetRequiredService<ILoginService>();
                var result = loginService.LoginAsync(new LoginRequest("Admin", "Admin123!", null, false)).GetAwaiter().GetResult();
                if (result.Succeeded)
                {
                    selectedModule = "backoffice";
                }
            }
#endif

            if (selectedModule == null)
            {
                logger.LogInformation("STARTUP: Opening sign-in form...");
                splash.SetDescription("Loading sign-in...");
                var loginForm = host.Services.GetRequiredService<LoginForm>();
                splash.Close();
                var dialogResult = loginForm.ShowDialog();
                logger.LogInformation("STARTUP: LoginForm closed with {Result}, SelectedModule={Module}", dialogResult, loginForm.SelectedModuleKey);
                if (dialogResult != DialogResult.OK || string.IsNullOrWhiteSpace(loginForm.SelectedModuleKey))
                {
                    return;
                }
                selectedModule = loginForm.SelectedModuleKey;
            }
            else
            {
                splash.Close();
            }

            if (selectedModule != null)
            {
                var code = Clovent.Desktop.Forms.Base.Localization.LanguagePreferenceStore.Load();
                var culture = System.Globalization.CultureInfo.GetCultureInfo(code);
                System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;
            }

            if (selectedModule is not null)
            {
                if (SynchronizationContext.Current is not WindowsFormsSynchronizationContext)
                {
                    SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
                }

                var navigator = host.Services.GetRequiredService<IApplicationModeNavigator>();
                if (SynchronizationContext.Current is not null)
                {
                    navigator.SetUiSynchronizationContext(SynchronizationContext.Current);
                }
                var targetModule = selectedModule;

                SynchronizationContext.Current?.Post(async _ =>
                {
                    try
                    {
                        if (string.Equals(targetModule, "pos", StringComparison.OrdinalIgnoreCase))
                        {
                            using var scope = host.Services.CreateScope();
                            var gate = scope.ServiceProvider.GetService<Clovent.Desktop.Restaurant.Services.IPosEntryGateCoordinator>();
                            var opened = gate != null && await gate.EnsureShiftAndOpenPosAsync().ConfigureAwait(false);
                            if (!opened)
                            {
                                await navigator.OpenBackOfficeAsync().ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            await navigator.OpenBackOfficeAsync().ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        var logger = host.Services.GetService<ILoggerFactory>()?.CreateLogger("Program");
                        logger?.LogError(ex, "Initial startup navigation failed.");
                        MessageBox.Show(
                            $"Clovent Business Operating System encountered an error during navigation:\n\n{ex.Message}",
                            "Navigation Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }, null);

                Application.Run(navigator.ApplicationContext);
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

