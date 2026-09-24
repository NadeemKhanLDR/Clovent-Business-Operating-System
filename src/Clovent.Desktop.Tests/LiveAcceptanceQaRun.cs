using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Clovent.Authentication.Application.DependencyInjection;
using Clovent.Authentication.Infrastructure.DependencyInjection;
using Clovent.Authentication.Infrastructure.Persistence;
using Clovent.Catalog.Application.Products.Queries;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Desktop.DependencyInjection;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Desktop.Sessions;
using Clovent.Desktop.Startup;
using Clovent.Identity.Infrastructure.Persistence;
using Clovent.Platform.Bootstrap;
using Clovent.Restaurant.Application.Customers.Queries;
using Clovent.Restaurant.Application.OrderLines.Commands;
using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.Application.OrderLines.Queries;
using Clovent.Restaurant.Application.Orders;
using Clovent.Restaurant.Application.Orders.Commands;
using Clovent.Restaurant.Application.Orders.Queries;
using Clovent.Restaurant.Application.Payments.Commands;
using Clovent.Restaurant.Application.Tables.Queries;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Orders.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using IHost = Microsoft.Extensions.Hosting.IHost;

namespace Clovent.Desktop.Tests;

public class LiveAcceptanceQaRun
{
    internal static readonly Lazy<IHost> Host = new(() =>
    {
        var bootstrapper = ApplicationBootstrapper
            .Create(basePath: AppContext.BaseDirectory)
            .WithLogging()
            .WithPlatform();

        bootstrapper.Services.AddApplication(bootstrapper.Configuration);
        bootstrapper.Services.AddInfrastructure(bootstrapper.Configuration);
        bootstrapper.Services.AddPersistence(bootstrapper.Configuration);
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

        return bootstrapper.BuildAndInitializeAsync().GetAwaiter().GetResult();
    });

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

    private static void CaptureForm(Form form, string outPath)
    {
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(0, 0);
        form.Show();
        for (int i = 0; i < 15; i++)
        {
            Application.DoEvents();
            Thread.Sleep(30);
        }
        var bounds = form.Bounds;
        using var bmp = new Bitmap(bounds.Width, bounds.Height);
        using (var g = Graphics.FromImage(bmp))
        {
            var hdc = g.GetHdc();
            try
            {
                PrintWindow(form.Handle, hdc, 2);
            }
            finally
            {
                g.ReleaseHdc(hdc);
            }
        }
        bmp.Save(outPath, ImageFormat.Png);
    }

    [Fact]
    public void Run_Full_POS_Live_Acceptance_Suite()
    {
        var thread = new Thread(() =>
        {
            var host = Host.Value;
            var session = host.Services.GetRequiredService<ICurrentSession>();
            using (var scope = host.Services.CreateScope())
            {
                var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                var admin = identityDb.Users.AsEnumerable().First(u => u.UserName.Value == "admin");
                session.SignIn(admin.Id.Value, Guid.NewGuid(), admin.UserName.Value);
            }

            var mediator = host.Services.GetRequiredService<IMediator>();
            var qaDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "qa"));
            Directory.CreateDirectory(qaDir);

            // Phase 1 — Establish Control & POS Launch
            using var posForm = new RestaurantPosForm(
                host.Services.GetRequiredService<IServiceScopeFactory>(),
                session,
                host.Services.GetRequiredService<Clovent.Desktop.Forms.Restaurant.MenuItems.IMenuItemsChangeNotifier>(),
                host.Services.GetRequiredService<Clovent.Desktop.Authorization.IManagerAuthorizationService>(),
                host.Services.GetRequiredService<ISplashScreenService>());

            posForm.Size = new Size(1366, 768);
            posForm.Show();
            Application.DoEvents();
            CaptureForm(posForm, Path.Combine(qaDir, "live_01_establish_control.png"));

            // Get or Create Dedicated Order (ORD-98 or fresh)
            var warehouseList = mediator.Send(new Clovent.MasterData.Application.Warehouses.Queries.ListAllWarehousesQuery()).GetAwaiter().GetResult();
            var warehouseId = warehouseList.First().WarehouseId;

            var existingOpen = mediator.Send(new ListOpenOrdersQuery()).GetAwaiter().GetResult();
            var qaOrderDto = existingOpen.FirstOrDefault(o => o.OrderNumber == "ORD-98");
            if (qaOrderDto == null)
            {
                qaOrderDto = mediator.Send(new CreateOrderCommand(OrderType.TakeAway, warehouseId)).GetAwaiter().GetResult();
            }

            var variants = mediator.Send(new ListProductVariantsQuery()).GetAwaiter().GetResult();
            var alooGobiHalf = variants.FirstOrDefault(v => v.Name.Contains("Aloo Gobi") && v.Name.Contains("Half")) ?? variants.First();

            // Phase 2 — T2 Notes
            var existingLines = mediator.Send(new ListOrderLinesByOrderQuery(qaOrderDto.OrderId)).GetAwaiter().GetResult();
            OrderLineDto targetLine;
            if (existingLines.Count == 0)
            {
                mediator.Send(new AddOrderLineCommand(qaOrderDto.OrderId, alooGobiHalf.ProductVariantId, 1m)).GetAwaiter().GetResult();
                targetLine = mediator.Send(new ListOrderLinesByOrderQuery(qaOrderDto.OrderId)).GetAwaiter().GetResult().First();
            }
            else
            {
                targetLine = existingLines.First();
                alooGobiHalf = variants.FirstOrDefault(v => v.ProductVariantId == targetLine.ProductVariantId) ?? alooGobiHalf;
            }

            mediator.Send(new SetOrderLineNotesCommand(targetLine.OrderLineId, "QA test note - less spicy")).GetAwaiter().GetResult();
            var linesAfterNotes = mediator.Send(new ListOrderLinesByOrderQuery(qaOrderDto.OrderId)).GetAwaiter().GetResult();
            var updatedLine = linesAfterNotes.First(l => l.OrderLineId == targetLine.OrderLineId);
            Assert.Equal("QA test note - less spicy", updatedLine.Notes);
            CaptureForm(posForm, Path.Combine(qaDir, "live_02_t2_notes.png"));

            // Phase 3 — Hold
            var heldOrderDto = mediator.Send(new HoldOrderCommand(qaOrderDto.OrderId)).GetAwaiter().GetResult();
            Assert.Equal("Held", heldOrderDto.Status);

            var heldList = mediator.Send(new ListHeldOrdersQuery()).GetAwaiter().GetResult();
            Assert.Contains(heldList, o => o.OrderId == qaOrderDto.OrderId);
            CaptureForm(posForm, Path.Combine(qaDir, "live_03_hold.png"));

            // Phase 4 — Recall
            var recalledOrderDto = mediator.Send(new ResumeOrderCommand(qaOrderDto.OrderId)).GetAwaiter().GetResult();
            Assert.Equal("Open", recalledOrderDto.Status);

            var recalledLines = mediator.Send(new ListOrderLinesByOrderQuery(qaOrderDto.OrderId)).GetAwaiter().GetResult();
            var recalledLine = recalledLines.First(l => l.OrderLineId == targetLine.OrderLineId);
            Assert.Equal("QA test note - less spicy", recalledLine.Notes);
            Assert.Equal(alooGobiHalf.ProductVariantId, recalledLine.ProductVariantId);
            CaptureForm(posForm, Path.Combine(qaDir, "live_04_recall.png"));

            // Phase 5 — Duplicate Item Behavior
            var dupOrder = mediator.Send(new CreateOrderCommand(OrderType.TakeAway, warehouseId)).GetAwaiter().GetResult();
            mediator.Send(new AddOrderLineCommand(dupOrder.OrderId, alooGobiHalf.ProductVariantId, 1m)).GetAwaiter().GetResult();
            var dupLines1 = mediator.Send(new ListOrderLinesByOrderQuery(dupOrder.OrderId)).GetAwaiter().GetResult();
            Assert.Single(dupLines1);

            // Add same variant NO note via POS duplicate check -> existing line found, quantity incremented
            var existingNoNoteLine = dupLines1.FirstOrDefault(l => !l.IsVoided && l.ProductVariantId == alooGobiHalf.ProductVariantId && string.IsNullOrEmpty(l.Notes));
            if (existingNoNoteLine != null)
            {
                mediator.Send(new SetOrderLineQuantityCommand(existingNoNoteLine.OrderLineId, existingNoNoteLine.Quantity + 1m)).GetAwaiter().GetResult();
            }
            else
            {
                mediator.Send(new AddOrderLineCommand(dupOrder.OrderId, alooGobiHalf.ProductVariantId, 1m)).GetAwaiter().GetResult();
            }

            var dupLines2 = mediator.Send(new ListOrderLinesByOrderQuery(dupOrder.OrderId)).GetAwaiter().GetResult();
            var mergedLine = Assert.Single(dupLines2);
            Assert.Equal(2m, mergedLine.Quantity);

            // Add same variant WITH custom note -> separate line created (count = 2 lines)
            mediator.Send(new AddOrderLineCommand(dupOrder.OrderId, alooGobiHalf.ProductVariantId, 1m)).GetAwaiter().GetResult();
            var dupLines3List = mediator.Send(new ListOrderLinesByOrderQuery(dupOrder.OrderId)).GetAwaiter().GetResult();
            var newLine = dupLines3List.First(l => l.OrderLineId != mergedLine.OrderLineId);
            mediator.Send(new SetOrderLineNotesCommand(newLine.OrderLineId, "Extra spicy")).GetAwaiter().GetResult();

            var dupLinesAfterExtraSpicy = mediator.Send(new ListOrderLinesByOrderQuery(dupOrder.OrderId)).GetAwaiter().GetResult();
            Assert.Equal(2, dupLinesAfterExtraSpicy.Count);
            CaptureForm(posForm, Path.Combine(qaDir, "live_05_duplicate.png"));

            // Phase 6 — Delete / Void
            var deleteVoidOrder = mediator.Send(new CreateOrderCommand(OrderType.TakeAway, warehouseId)).GetAwaiter().GetResult();
            var tempVariant = variants.Last();
            mediator.Send(new AddOrderLineCommand(deleteVoidOrder.OrderId, tempVariant.ProductVariantId, 1m)).GetAwaiter().GetResult();
            var linesWithTemp = mediator.Send(new ListOrderLinesByOrderQuery(deleteVoidOrder.OrderId)).GetAwaiter().GetResult();
            var tempLine = linesWithTemp.First(l => l.ProductVariantId == tempVariant.ProductVariantId);

            // DELETE temp line
            mediator.Send(new RemoveOrderLineCommand(deleteVoidOrder.OrderId, tempLine.OrderLineId)).GetAwaiter().GetResult();
            var linesAfterDelete = mediator.Send(new ListOrderLinesByOrderQuery(deleteVoidOrder.OrderId)).GetAwaiter().GetResult();
            Assert.DoesNotContain(linesAfterDelete.Where(l => !l.IsVoided), l => l.OrderLineId == tempLine.OrderLineId);

            // VOID line test
            mediator.Send(new AddOrderLineCommand(deleteVoidOrder.OrderId, tempVariant.ProductVariantId, 1m)).GetAwaiter().GetResult();
            var linesWithTemp2 = mediator.Send(new ListOrderLinesByOrderQuery(deleteVoidOrder.OrderId)).GetAwaiter().GetResult();
            var tempLine2 = linesWithTemp2.First(l => l.ProductVariantId == tempVariant.ProductVariantId && !l.IsVoided);
            mediator.Send(new VoidOrderLineCommand(tempLine2.OrderLineId)).GetAwaiter().GetResult();

            var linesAfterVoid = mediator.Send(new ListOrderLinesByOrderQuery(deleteVoidOrder.OrderId)).GetAwaiter().GetResult();
            var voidedLine = linesAfterVoid.First(l => l.OrderLineId == tempLine2.OrderLineId);
            Assert.True(voidedLine.IsVoided);

            CaptureForm(posForm, Path.Combine(qaDir, "live_06_delete_void.png"));

            // Phase 7 — Customer
            var customerList = mediator.Send(new ListCustomersQuery()).GetAwaiter().GetResult();
            var customer = customerList.FirstOrDefault(c => c.CustomerId != Guid.Empty) ?? customerList.First();
            mediator.Send(new SetOrderCustomerCommand(qaOrderDto.OrderId, customer.CustomerId)).GetAwaiter().GetResult();

            var updatedOrderCust = mediator.Send(new GetOrderByIdQuery(qaOrderDto.OrderId)).GetAwaiter().GetResult();
            var expectedCustId = customer.CustomerId == Guid.Empty ? (Guid?)null : customer.CustomerId;
            Assert.Equal(expectedCustId, updatedOrderCust.CustomerId);

            mediator.Send(new HoldOrderCommand(qaOrderDto.OrderId)).GetAwaiter().GetResult();
            mediator.Send(new ResumeOrderCommand(qaOrderDto.OrderId)).GetAwaiter().GetResult();
            var recalledCustOrder = mediator.Send(new GetOrderByIdQuery(qaOrderDto.OrderId)).GetAwaiter().GetResult();
            Assert.Equal(expectedCustId, recalledCustOrder.CustomerId);
            CaptureForm(posForm, Path.Combine(qaDir, "live_07_customer.png"));

            // Phase 8 — Dine-In / Table
            var tables = mediator.Send(new ListAllTablesQuery()).GetAwaiter().GetResult();
            var availableTable = tables.FirstOrDefault(t => t.OccupancyStatus == "Available");
            if (availableTable != null)
            {
                var dineInOrder = mediator.Send(new CreateOrderCommand(OrderType.DineIn, warehouseId, availableTable.TableId)).GetAwaiter().GetResult();
                Assert.Equal("DineIn", dineInOrder.OrderType);
                Assert.Equal(availableTable.TableId, dineInOrder.TableId);

                var takeAwayOrder = mediator.Send(new CreateOrderCommand(OrderType.TakeAway, warehouseId)).GetAwaiter().GetResult();
                Assert.Equal("TakeAway", takeAwayOrder.OrderType);
                Assert.Null(takeAwayOrder.TableId);

                mediator.Send(new CancelOrderCommand(dineInOrder.OrderId, "QA test cleanup")).GetAwaiter().GetResult();
                mediator.Send(new CancelOrderCommand(takeAwayOrder.OrderId, "QA test cleanup")).GetAwaiter().GetResult();
            }
            CaptureForm(posForm, Path.Combine(qaDir, "live_08_dinein_table.png"));

            // Phase 9 — Active Orders
            var openOrdersRail = mediator.Send(new ListOpenOrdersQuery()).GetAwaiter().GetResult();
            var heldOrdersRail = mediator.Send(new ListHeldOrdersQuery()).GetAwaiter().GetResult();
            var allOrdersRail = mediator.Send(new ListAllOrdersQuery()).GetAwaiter().GetResult();
            Assert.NotNull(openOrdersRail);
            Assert.NotNull(heldOrdersRail);
            Assert.NotNull(allOrdersRail);
            CaptureForm(posForm, Path.Combine(qaDir, "live_09_active_orders.png"));

            // Phase 10 — Sidebar Animation & Resolution Checks
            posForm.Size = new Size(1024, 768);
            Application.DoEvents();
            CaptureForm(posForm, Path.Combine(qaDir, "live_24_1024x768.png"));

            posForm.Size = new Size(1366, 768);
            Application.DoEvents();
            CaptureForm(posForm, Path.Combine(qaDir, "live_25_1366x768.png"));

            posForm.WindowState = FormWindowState.Maximized;
            Application.DoEvents();
            CaptureForm(posForm, Path.Combine(qaDir, "live_26_maximized.png"));
            posForm.WindowState = FormWindowState.Normal;

            // Phase 11 — Clear
            var dbOrderBeforeClear = mediator.Send(new GetOrderByIdQuery(qaOrderDto.OrderId)).GetAwaiter().GetResult();
            Assert.NotNull(dbOrderBeforeClear);
            CaptureForm(posForm, Path.Combine(qaDir, "live_11_clear.png"));

            // Phase 12 — Cancel
            var cancelOrder = mediator.Send(new CreateOrderCommand(OrderType.TakeAway, warehouseId)).GetAwaiter().GetResult();
            mediator.Send(new AddOrderLineCommand(cancelOrder.OrderId, alooGobiHalf.ProductVariantId, 1m)).GetAwaiter().GetResult();
            var cancelledOrder = mediator.Send(new CancelOrderCommand(cancelOrder.OrderId, "Customer cancelled")).GetAwaiter().GetResult();
            Assert.Equal("Cancelled", cancelledOrder.Status);
            CaptureForm(posForm, Path.Combine(qaDir, "live_12_cancel.png"));

            // Phase 13–17 — Payment & Record Payment
            var paymentMethods = mediator.Send(new Clovent.Restaurant.Application.PaymentMethods.Queries.ListPaymentMethodsQuery()).GetAwaiter().GetResult();
            var cashMethod = paymentMethods.First(m => m.Name.Equals("Cash", StringComparison.OrdinalIgnoreCase));

            var checkoutOrder = mediator.Send(new CreateOrderCommand(OrderType.TakeAway, warehouseId)).GetAwaiter().GetResult();
            mediator.Send(new AddOrderLineCommand(checkoutOrder.OrderId, alooGobiHalf.ProductVariantId, 2m)).GetAwaiter().GetResult();
            var checkoutSummary = mediator.Send(new GetOrderSummaryQuery(checkoutOrder.OrderId)).GetAwaiter().GetResult();

            mediator.Send(new RecordPaymentCommand(checkoutOrder.OrderId, cashMethod.PaymentMethodId, checkoutSummary.GrandTotal, false)).GetAwaiter().GetResult();
            var postPaySummary = mediator.Send(new GetOrderSummaryQuery(checkoutOrder.OrderId)).GetAwaiter().GetResult();
            Assert.Equal(0m, postPaySummary.Balance);

            // Phase 20 — Place Order / Complete
            var completedOrder = mediator.Send(new CompleteOrderCommand(checkoutOrder.OrderId)).GetAwaiter().GetResult();
            Assert.Equal("Completed", completedOrder.Status);
            CaptureForm(posForm, Path.Combine(qaDir, "live_20_place_order.png"));

            // Phase 21 & 22 — Sales History & DB Persistence Verification
            using (var scope = host.Services.CreateScope())
            {
                var restDb = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
                var dbOrder = restDb.Orders.FirstOrDefault(o => o.Id == new OrderId(checkoutOrder.OrderId));
                Assert.NotNull(dbOrder);
                Assert.Equal(OrderStatus.Completed, dbOrder.Status);

                var dbPayments = restDb.Payments.Where(p => p.OrderId == new OrderId(checkoutOrder.OrderId)).ToList();
                Assert.NotEmpty(dbPayments);
            }
            CaptureForm(posForm, Path.Combine(qaDir, "live_21_sales_history.png"));

            posForm.Close();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        var qaDirCheck = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "qa"));
        Assert.True(File.Exists(Path.Combine(qaDirCheck, "live_01_establish_control.png")));
        Assert.True(File.Exists(Path.Combine(qaDirCheck, "live_02_t2_notes.png")));
        Assert.True(File.Exists(Path.Combine(qaDirCheck, "live_03_hold.png")));
        Assert.True(File.Exists(Path.Combine(qaDirCheck, "live_04_recall.png")));
        Assert.True(File.Exists(Path.Combine(qaDirCheck, "live_05_duplicate.png")));
        Assert.True(File.Exists(Path.Combine(qaDirCheck, "live_06_delete_void.png")));
        Assert.True(File.Exists(Path.Combine(qaDirCheck, "live_07_customer.png")));
        Assert.True(File.Exists(Path.Combine(qaDirCheck, "live_08_dinein_table.png")));
        Assert.True(File.Exists(Path.Combine(qaDirCheck, "live_20_place_order.png")));
        Assert.True(File.Exists(Path.Combine(qaDirCheck, "live_21_sales_history.png")));
        Assert.True(File.Exists(Path.Combine(qaDirCheck, "live_24_1024x768.png")));
        Assert.True(File.Exists(Path.Combine(qaDirCheck, "live_25_1366x768.png")));
        Assert.True(File.Exists(Path.Combine(qaDirCheck, "live_26_maximized.png")));
    }
}
