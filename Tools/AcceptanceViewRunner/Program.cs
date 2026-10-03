using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Platform;
using Clovent.Platform.Bootstrap;
using Clovent.Desktop.Commissioning.Security;
using Clovent.Desktop.DependencyInjection;
using Clovent.Desktop.Restaurant.EndOfDay;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Desktop.Restaurant.Customers;
using Clovent.Desktop.Inventory.WarehouseStocks;
using Clovent.Desktop.Inventory.Transactions;
using Clovent.Desktop.Configuration;
using Clovent.Desktop.Licensing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AcceptanceViewRunner;

internal static class Program
{
    [DllImport("user32.dll")] private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiFlag);
    [DllImport("user32.dll")] private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    [STAThread]
    private static int Main(string[] args)
    {
        try { SetProcessDpiAwarenessContext((IntPtr)(-4)); } catch {}
        ApplicationConfiguration.Initialize();

        if (args.Length < 2)
        {
            Console.WriteLine("Usage: AcceptanceViewRunner <viewName> <outputPath>");
            Console.WriteLine("Views: sales-summary, order-history, customers, receivables, stock, movements, db-settings, licensing");
            return 1;
        }

        string viewName = args[0].ToLowerInvariant();
        string outPath = args[1];

        // Bootstrap using base path of published client
        string basePath = @"C:\CloventClient105";
        ProgramDataAclManager.ConfigureDirectorySecurity();

        var bootstrapper = ApplicationBootstrapper.Create(basePath: basePath).WithLogging().WithPlatform();
        var effectiveConnectionString = DatabaseSecretStore.ResolveConnectionString(bootstrapper.Configuration);
        bootstrapper.Configuration["ConnectionStrings:Default"] = effectiveConnectionString;

        // Register layers
        Clovent.Authentication.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(bootstrapper.Services, bootstrapper.Configuration);
        Clovent.Authentication.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure(bootstrapper.Services, bootstrapper.Configuration);
        Clovent.Authentication.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(bootstrapper.Services, bootstrapper.Configuration);

        Clovent.Identity.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(bootstrapper.Services, bootstrapper.Configuration);
        Clovent.Identity.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure(bootstrapper.Services, bootstrapper.Configuration);
        Clovent.Identity.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(bootstrapper.Services, bootstrapper.Configuration);

        Clovent.MasterData.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(bootstrapper.Services, bootstrapper.Configuration);
        Clovent.MasterData.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure(bootstrapper.Services, bootstrapper.Configuration);
        Clovent.MasterData.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(bootstrapper.Services, bootstrapper.Configuration);

        Clovent.Catalog.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(bootstrapper.Services, bootstrapper.Configuration);
        Clovent.Catalog.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure(bootstrapper.Services, bootstrapper.Configuration);
        Clovent.Catalog.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(bootstrapper.Services, bootstrapper.Configuration);

        Clovent.Inventory.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(bootstrapper.Services, bootstrapper.Configuration);
        Clovent.Inventory.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure(bootstrapper.Services, bootstrapper.Configuration);
        Clovent.Inventory.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(bootstrapper.Services, bootstrapper.Configuration);

        Clovent.Restaurant.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(bootstrapper.Services, bootstrapper.Configuration);
        Clovent.Restaurant.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure(bootstrapper.Services, bootstrapper.Configuration);
        Clovent.Restaurant.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(bootstrapper.Services, bootstrapper.Configuration);

        bootstrapper.Services.AddDesktopHost(bootstrapper.Configuration);
        Clovent.Desktop.Modules.DesktopModuleLoader.LoadModules(bootstrapper.Services, bootstrapper.Configuration, Clovent.Desktop.Modules.DesktopModuleCatalog.ModuleTypes);

        var host = Task.Run(async () =>
        {
            var h = await bootstrapper.BuildAndInitializeAsync().ConfigureAwait(false);
            using var initScope = h.Services.CreateScope();
            var initMediator = initScope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
            await Clovent.Desktop.Forms.Base.DateTimeDisplayLoader.ConfigureAsync(initMediator, h.Services).ConfigureAwait(false);
            await Clovent.Desktop.Forms.Base.CurrencyDisplayLoader.ConfigureAsync(initMediator).ConfigureAwait(false);
            return h;
        }).GetAwaiter().GetResult();

        // Instantiate view or dialog
        Form targetForm;
        switch (viewName)
        {
            case "db-settings":
                targetForm = new DatabaseConnectionDialog();
                break;

            case "licensing":
                targetForm = new SoftwareRegistrationForm();
                break;

            case "sales-summary":
            {
                var view = host.Services.GetRequiredService<EndOfDayReportView>();
                targetForm = WrapInForm(view, "Sales Summary - Acceptance Test");
                break;
            }

            case "order-history":
            {
                var view = host.Services.GetRequiredService<OrderHistoryView>();
                targetForm = WrapInForm(view, "Order History & Bills - Acceptance Test");
                break;
            }

            case "customers":
            {
                var view = host.Services.GetRequiredService<CustomersView>();
                targetForm = WrapInForm(view, "Customers Directory - Acceptance Test");
                break;
            }

            case "receivables":
            {
                var view = host.Services.GetRequiredService<CustomerReceivablesReportView>();
                targetForm = WrapInForm(view, "Customer Receivables & Balances - Acceptance Test");
                break;
            }

            case "stock":
            {
                var view = host.Services.GetRequiredService<WarehouseStockManagementView>();
                targetForm = WrapInForm(view, "Stock On Hand - Acceptance Test");
                break;
            }

            case "movements":
            {
                var view = host.Services.GetRequiredService<InventoryTransactionsView>();
                targetForm = WrapInForm(view, "Inventory Movements & Transactions - Acceptance Test");
                break;
            }

            default:
                Console.Error.WriteLine($"Unknown view '{viewName}'");
                return 2;
        }

        targetForm.StartPosition = FormStartPosition.CenterScreen;
        targetForm.Show();
        Application.DoEvents();
        Thread.Sleep(3000); // Allow data binding and formatting to complete

        // Capture
        CaptureForm(targetForm, outPath);
        targetForm.Close();
        targetForm.Dispose();
        Console.WriteLine($"SUCCESS: Captured {viewName} -> {outPath}");
        return 0;
    }

    private static Form WrapInForm(UserControl control, string title)
    {
        var f = new Form
        {
            Text = title,
            Size = new Size(1600, 1000),
            StartPosition = FormStartPosition.CenterScreen,
            ShowInTaskbar = true
        };
        control.Dock = DockStyle.Fill;
        f.Controls.Add(control);
        return f;
    }

    private static void CaptureForm(Form form, string outPath)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(outPath));
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

        GetWindowRect(form.Handle, out RECT r);
        int w = r.Right - r.Left;
        int h = r.Bottom - r.Top;
        using var bmp = new Bitmap(w, h);
        using (var g = Graphics.FromImage(bmp))
        {
            IntPtr hdc = g.GetHdc();
            bool ok = PrintWindow(form.Handle, hdc, 2);
            if (!ok) PrintWindow(form.Handle, hdc, 0);
            g.ReleaseHdc(hdc);
        }
        bmp.Save(outPath, ImageFormat.Png);
    }
}
