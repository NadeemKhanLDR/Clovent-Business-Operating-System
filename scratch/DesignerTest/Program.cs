using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Authentication.Application.DependencyInjection;
using Clovent.Authentication.Infrastructure.DependencyInjection;
using Clovent.Desktop.DependencyInjection;
using Clovent.Desktop.Forms.Dashboard;
using Clovent.Desktop.Forms.Shell;
using Clovent.Desktop.Modules;
using Clovent.Desktop.Navigation;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Desktop.Restaurant.Services;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Users;
using Clovent.Identity.Users.ValueObjects;
using Clovent.Platform.Bootstrap;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DesignerTest;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var basePath = @"D:\Clovent Business Operating System\src\Clovent.Desktop\bin\Debug\net10.0-windows";
        Console.WriteLine($"Starting diagnostic with basePath: {basePath}");

        var bootstrapper = ApplicationBootstrapper
            .Create(basePath: basePath)
            .WithLogging()
            .WithPlatform();

        bootstrapper.Services.AddApplication(bootstrapper.Configuration);
        bootstrapper.Services.AddInfrastructure(bootstrapper.Configuration);
        bootstrapper.Services.AddPersistence(bootstrapper.Configuration);
        Clovent.Identity.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(
            bootstrapper.Services, bootstrapper.Configuration);
        Clovent.Identity.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure(
            bootstrapper.Services, bootstrapper.Configuration);
        Clovent.Identity.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(
            bootstrapper.Services, bootstrapper.Configuration);

        Clovent.MasterData.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(
            bootstrapper.Services, bootstrapper.Configuration);
        Clovent.MasterData.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure(
            bootstrapper.Services, bootstrapper.Configuration);
        Clovent.MasterData.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(
            bootstrapper.Services, bootstrapper.Configuration);

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

        Clovent.Restaurant.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(
            bootstrapper.Services, bootstrapper.Configuration);
        Clovent.Restaurant.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure(
            bootstrapper.Services, bootstrapper.Configuration);
        Clovent.Restaurant.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(
            bootstrapper.Services, bootstrapper.Configuration);

        bootstrapper.Services.AddDesktopHost(bootstrapper.Configuration);
        bootstrapper.Services.LoadModules(bootstrapper.Configuration, DesktopModuleCatalog.ModuleTypes);

        Console.WriteLine("Building host...");
        var host = bootstrapper.BuildAndInitializeAsync().GetAwaiter().GetResult();
        Console.WriteLine("Host built successfully.");

        // Sign in as admin
        using (var loginScope = host.Services.CreateScope())
        {
            var userRepo = loginScope.ServiceProvider.GetRequiredService<IUserRepository>();
            var adminUser = userRepo.GetByUserNameAsync(UserName.Create("admin")).GetAwaiter().GetResult();
            var session = host.Services.GetRequiredService<ICurrentSession>();
            session.SignIn(adminUser!.Id.Value, Guid.NewGuid(), "Administrator");
            Console.WriteLine($"Signed in as {session.DisplayName} ({session.UserId})");
        }

        var navigationService = host.Services.GetRequiredService<INavigationService>();
        navigationService.Register("dashboard", () => host.Services.GetRequiredService<DashboardView>());
        navigationService.Register("pos", () => host.Services.GetRequiredService<RestaurantPosForm>());

        var navigator = host.Services.GetRequiredService<IApplicationModeNavigator>();
        Console.WriteLine("Got navigator.");

        // Open Back Office first
        navigator.OpenBackOfficeAsync().GetAwaiter().GetResult();
        Console.WriteLine($"Back Office opened. CurrentForm={navigator.CurrentForm?.GetType().Name}");

        var secondTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        int seconds = 0;
        secondTimer.Tick += (s, e) =>
        {
            seconds++;
            Console.WriteLine($"[{seconds}s] CurrentForm={navigator.CurrentForm?.Name}, IsDisposed={navigator.CurrentForm?.IsDisposed}");
            if (seconds >= 20)
            {
                Console.WriteLine("20s reached, stopping.");
                secondTimer.Stop();
                Application.ExitThread();
            }
        };
        secondTimer.Start();

        var triggerTimer = new System.Windows.Forms.Timer { Interval = 300 };
        triggerTimer.Tick += async (s, e) =>
        {
            triggerTimer.Stop();
            Console.WriteLine("Simulating Click on Restaurant POS with Gate on UI Thread...");
            try
            {
                using var gateScope = host.Services.CreateScope();
                var gate = gateScope.ServiceProvider.GetRequiredService<IPosEntryGateCoordinator>();
                var sw = Stopwatch.StartNew();
                var opened = await gate.EnsureShiftAndOpenPosAsync(navigator.CurrentForm);
                Console.WriteLine($"EnsureShiftAndOpenPosAsync returned {opened} in {sw.ElapsedMilliseconds} ms. CurrentForm={navigator.CurrentForm?.GetType().Name}");

                if (navigator.CurrentForm is RestaurantPosForm posForm)
                {
                    Console.WriteLine($"Observing RestaurantPosForm (HandleCreated={posForm.IsHandleCreated}, Visible={posForm.Visible}, IsDisposed={posForm.IsDisposed})");
                    posForm.FormClosing += (s2, e2) => Console.WriteLine($"[EVENT] RestaurantPosForm FormClosing! CloseReason={e2.CloseReason}, Cancel={e2.Cancel}");
                    posForm.FormClosed += (s2, e2) => Console.WriteLine($"[EVENT] RestaurantPosForm FormClosed! CloseReason={e2.CloseReason}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EX] Gate threw: {ex}");
            }
        };
        triggerTimer.Start();

        Console.WriteLine("Running Application.Run(navigator.ApplicationContext)...");
        Application.Run(navigator.ApplicationContext);
        Console.WriteLine($"Application loop ended after {seconds} seconds. Exiting diagnostic.");
    }
}