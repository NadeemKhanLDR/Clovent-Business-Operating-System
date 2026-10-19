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
using Clovent.Desktop.Restaurant.Reports;
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
            Console.WriteLine("Usage: AcceptanceViewRunner <viewName> <outputPath> [mode: normal|maximized|small]");
            Console.WriteLine("Views: sales-summary, order-history, customers, receivables, stock, movements, db-settings, licensing, tax-summary, refund-audit, tax-summary-populated, refund-audit-populated");
            return 1;
        }

        string viewName = args[0].ToLowerInvariant();
        string outPath = args[1];
        string mode = args.Length > 2 ? args[2].ToLowerInvariant() : "normal";

        // Bootstrap using base path of published client
        string basePath = @"D:\Clovent Business Operating System\artifacts\release\Clovent.BusinessOperatingSystem-1.0.7-win-x64";
        if (!Directory.Exists(basePath))
        {
            basePath = @"C:\CloventClient106";
        }
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

        Action? postPopulate = null;

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
                targetForm = WrapInForm(view, "Sales Summary - Acceptance Test", mode);
                break;
            }

            case "order-history":
            {
                var view = host.Services.GetRequiredService<OrderHistoryView>();
                targetForm = WrapInForm(view, "Order History & Bills - Acceptance Test", mode);
                break;
            }

            case "customers":
            {
                var view = host.Services.GetRequiredService<CustomersView>();
                targetForm = WrapInForm(view, "Customers Directory - Acceptance Test", mode);
                break;
            }

            case "receivables":
            {
                var view = host.Services.GetRequiredService<CustomerReceivablesReportView>();
                targetForm = WrapInForm(view, "Customer Receivables & Balances - Acceptance Test", mode);
                break;
            }

            case "stock":
            {
                var view = host.Services.GetRequiredService<WarehouseStockManagementView>();
                targetForm = WrapInForm(view, "Stock On Hand - Acceptance Test", mode);
                break;
            }

            case "movements":
            {
                var view = host.Services.GetRequiredService<InventoryTransactionsView>();
                targetForm = WrapInForm(view, "Inventory Movements & Transactions - Acceptance Test", mode);
                break;
            }

            case "pos":
            {
                var session = host.Services.GetRequiredService<Clovent.Desktop.Sessions.ICurrentSession>();
                session.SignIn(Guid.Parse("11111111-1111-1111-1111-111111111111"), Guid.NewGuid(), "admin_acceptance");
                targetForm = host.Services.GetRequiredService<RestaurantPosForm>();
                break;
            }

            case "tax-summary":
            {
                var session = host.Services.GetRequiredService<Clovent.Desktop.Sessions.ICurrentSession>();
                session.SignIn(Guid.Parse("11111111-1111-1111-1111-111111111111"), Guid.NewGuid(), "admin_acceptance");
                var view = host.Services.GetRequiredService<TaxSummaryReportView>();
                targetForm = WrapInForm(view, "Tax Summary - Acceptance Test", mode);
                break;
            }

            case "refund-audit":
            {
                var session = host.Services.GetRequiredService<Clovent.Desktop.Sessions.ICurrentSession>();
                session.SignIn(Guid.Parse("11111111-1111-1111-1111-111111111111"), Guid.NewGuid(), "admin_acceptance");
                var view = host.Services.GetRequiredService<RefundAuditReportView>();
                targetForm = WrapInForm(view, "Refund Audit - Acceptance Test", mode);
                break;
            }

            case "tax-summary-populated":
            {
                var session = host.Services.GetRequiredService<Clovent.Desktop.Sessions.ICurrentSession>();
                session.SignIn(Guid.Parse("11111111-1111-1111-1111-111111111111"), Guid.NewGuid(), "admin_acceptance");
                var view = host.Services.GetRequiredService<TaxSummaryReportView>();
                targetForm = WrapInForm(view, "Tax Summary (Populated & Long Names) - Acceptance Test", mode);
                postPopulate = () =>
                {
                    try
                    {
                        var combo = GetPrivateField<DevExpress.XtraEditors.ComboBoxEdit>(view, "_warehouseCombo");
                        combo.Properties.Items.Add("WH-01 - Rawalpindi Central Warehouse");
                        combo.Properties.Items.Add("WH-02 - Islamabad Blue Area Distribution Center");
                        combo.SelectedIndex = combo.Properties.Items.Count - 1;

                        GetPrivateField<DevExpress.XtraEditors.LabelControl>(view, "_kpiGrossSales").Text = "Gross Sales: 12,345,678.00";
                        GetPrivateField<DevExpress.XtraEditors.LabelControl>(view, "_kpiTaxableBase").Text = "Taxable Base: 10,850,500.00";
                        GetPrivateField<DevExpress.XtraEditors.LabelControl>(view, "_kpiTaxCharged").Text = "Tax Charged: 1,844,585.00";
                        GetPrivateField<DevExpress.XtraEditors.LabelControl>(view, "_kpiTaxRefunded").Text = "Tax Refunded: 124,500.00";
                        GetPrivateField<DevExpress.XtraEditors.LabelControl>(view, "_kpiNetTax").Text = "Net Tax Payable: 1,720,085.00";
                        GetPrivateField<DevExpress.XtraEditors.LabelControl>(view, "_kpiNetSales").Text = "Net Sales: 12,221,178.00";

                        var grid = GetPrivateField<DevExpress.XtraGrid.GridControl>(view, "_gridControl");
                        grid.DataSource = new[]
                        {
                            new { Authority = "PRA", TaxCode = "PRA-16", RatePercentage = 16.00m, TaxClassification = "Services", TaxableBase = 8450000.00m, TaxCharged = 1352000.00m, TaxRefunded = 64000.00m, NetTax = 1288000.00m },
                            new { Authority = "FBR", TaxCode = "GST-18", RatePercentage = 18.00m, TaxClassification = "Goods", TaxableBase = 2400500.00m, TaxCharged = 432090.00m, TaxRefunded = 60500.00m, NetTax = 371590.00m },
                            new { Authority = "SRB", TaxCode = "SRB-13", RatePercentage = 13.00m, TaxClassification = "Services", TaxableBase = 460000.00m, TaxCharged = 59800.00m, TaxRefunded = 0.00m, NetTax = 59800.00m }
                        };
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error populating mock: {ex.Message}");
                    }
                };
                break;
            }

            case "refund-audit-populated":
            {
                var session = host.Services.GetRequiredService<Clovent.Desktop.Sessions.ICurrentSession>();
                session.SignIn(Guid.Parse("11111111-1111-1111-1111-111111111111"), Guid.NewGuid(), "admin_acceptance");
                var view = host.Services.GetRequiredService<RefundAuditReportView>();
                targetForm = WrapInForm(view, "Refund Audit (Populated & Long Names) - Acceptance Test", mode);
                postPopulate = () =>
                {
                    try
                    {
                        var combo = GetPrivateField<DevExpress.XtraEditors.ComboBoxEdit>(view, "_warehouseCombo");
                        combo.Properties.Items.Add("WH-01 - Rawalpindi Central Warehouse");
                        combo.Properties.Items.Add("WH-02 - Islamabad Blue Area Distribution Center");
                        combo.SelectedIndex = combo.Properties.Items.Count - 1;

                        GetPrivateField<DevExpress.XtraEditors.LabelControl>(view, "_kpiCount").Text = "Refunds: 42";
                        GetPrivateField<DevExpress.XtraEditors.LabelControl>(view, "_kpiGrossRefunded").Text = "Gross: 845,950.00";
                        GetPrivateField<DevExpress.XtraEditors.LabelControl>(view, "_kpiTaxReversed").Text = "Tax Reversed: 135,352.00";
                        GetPrivateField<DevExpress.XtraEditors.LabelControl>(view, "_kpiNetRefunded").Text = "Total Refunded: 981,302.00";
                        GetPrivateField<DevExpress.XtraEditors.LabelControl>(view, "_kpiCashPayouts").Text = "Cash Payouts: 450,200.00";
                        GetPrivateField<DevExpress.XtraEditors.LabelControl>(view, "_kpiCardRefunds").Text = "Acct Credits: 281,102.00 | Card: 250,000.00";

                        var grid = GetPrivateField<DevExpress.XtraGrid.GridControl>(view, "_gridControl");
                        grid.DataSource = new[]
                        {
                            new { CreditNoteNumber = "CN-2026-00041", CreatedAt = DateTime.Now.AddHours(-2), CashierName = "Muhammad Rizwan", ApprovedByName = "Asad Khan (Manager)", Reason = "Order Cancelled - Kitchen Delay", SettlementMethod = "Cash", Reference = "ORD-88231", Subtotal = 12500.00m, TaxReversed = 2000.00m, GrandTotal = 14500.00m },
                            new { CreditNoteNumber = "CN-2026-00042", CreatedAt = DateTime.Now.AddHours(-1), CashierName = "Fatima Noor", ApprovedByName = "Asad Khan (Manager)", Reason = "Customer Dissatisfaction - Cold Food", SettlementMethod = "Credit Card", Reference = "ORD-88239", Subtotal = 8500.00m, TaxReversed = 1360.00m, GrandTotal = 9860.00m }
                        };
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error populating mock: {ex.Message}");
                    }
                };
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
        postPopulate?.Invoke();
        Application.DoEvents();
        Thread.Sleep(500);

        // Capture
        CaptureForm(targetForm, outPath);
        targetForm.Close();
        targetForm.Dispose();
        Console.WriteLine($"SUCCESS: Captured {viewName} -> {outPath}");
        return 0;
    }

    private static T GetPrivateField<T>(object target, string name) where T : class
    {
        var field = target.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return (T)field!.GetValue(target)!;
    }

    private static Form WrapInForm(UserControl control, string title, string mode = "normal")
    {
        var f = new Form
        {
            Text = title,
            StartPosition = FormStartPosition.CenterScreen,
            ShowInTaskbar = true
        };
        if (mode == "maximized")
        {
            f.WindowState = FormWindowState.Maximized;
        }
        else if (mode == "compact")
        {
            f.Size = new Size(1900, 1200);
        }
        else if (mode == "small")
        {
            f.Size = new Size(1100, 750);
        }
        else
        {
            f.Size = new Size(2500, 1450);
        }
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
        if (w <= 0 || h <= 0) return;
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
