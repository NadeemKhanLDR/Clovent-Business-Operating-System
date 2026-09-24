using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Restaurant.MenuItems;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Desktop.Sessions;
using Clovent.Desktop.Startup;
using Clovent.Identity.Infrastructure.Persistence;
using Clovent.MasterData.Application.Warehouses.Queries;
using Clovent.Restaurant.Application.Orders.Commands;
using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Application.Orders.Queries;
using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.Application.OrderLines.Queries;
using Clovent.Restaurant.Application.QuickOrderTemplates.Dtos;
using Clovent.Restaurant.Application.QuickOrderTemplates.Queries;
using Clovent.Restaurant.Application.Tables.Dtos;
using Clovent.Restaurant.Application.Tables.Queries;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Tables;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Orders;

public class QuickOrderDealLiveAcceptanceTests
{
    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint nFlags);

    private static void CaptureForm(Form form, string outPath, Action<string>? log = null)
    {
        log?.Invoke($"CaptureForm: start {form.GetType().Name} -> {Path.GetFileName(outPath)}");
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(0, 0);
        if (!form.Visible)
        {
            form.Show();
        }
        for (int i = 0; i < 15; i++)
        {
            Application.DoEvents();
            Thread.Sleep(30);
        }
        log?.Invoke("CaptureForm: pumping done, capturing bitmap");
        var bounds = form.Bounds;
        using var bmp = new Bitmap(bounds.Width, bounds.Height);
        using (var g = Graphics.FromImage(bmp))
        {
            var hdc = g.GetHdc();
            try
            {
                log?.Invoke("CaptureForm: calling PrintWindow");
                PrintWindow(form.Handle, hdc, 2);
                log?.Invoke("CaptureForm: PrintWindow completed");
            }
            finally
            {
                g.ReleaseHdc(hdc);
            }
        }
        log?.Invoke("CaptureForm: saving file");
        bmp.Save(outPath, ImageFormat.Png);
        log?.Invoke($"CaptureForm: file saved successfully -> {Path.GetFileName(outPath)}");
    }

    private static T RunSync<T>(Func<Task<T>> func) => Task.Run(func).GetAwaiter().GetResult();
    private static void RunSync(Func<Task> func) => Task.Run(func).GetAwaiter().GetResult();

    private static void PumpUntil(Task task, int maxWaitMs = 90000)
    {
        var start = Environment.TickCount;
        while (!task.IsCompleted)
        {
            Application.DoEvents();
            Thread.Sleep(15);
            if (Environment.TickCount - start > maxWaitMs)
            {
                throw new TimeoutException($"Task did not complete within {maxWaitMs}ms.");
            }
        }
        task.GetAwaiter().GetResult();
    }

    [Fact]
    public void Execute_Live_Quick_Order_Deal_Acceptance_Suite()
    {
        Exception? threadEx = null;
        var thread = new Thread(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
            try
            {
                var host = LiveAcceptanceQaRun.Host.Value;
                var session = host.Services.GetRequiredService<ICurrentSession>();
                using (var scope = host.Services.CreateScope())
                {
                    var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                    var admin = identityDb.Users.AsEnumerable().First(u => u.UserName.Value == "admin");
                    session.SignIn(admin.Id.Value, Guid.NewGuid(), admin.UserName.Value);
                }

                var mediator = host.Services.GetRequiredService<IMediator>();
                var qaDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "qa", "quick_orders_deals_live"));
                Directory.CreateDirectory(qaDir);
                var tracePath = Path.Combine(qaDir, "qa_trace.log");
                File.WriteAllText(tracePath, $"[{DateTime.Now:HH:mm:ss.fff}] Test suite started\r\n");
                void Log(string msg) => File.AppendAllText(tracePath, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\r\n");

                Log("Fetching warehouses, tables, deals...");
                // Fetch warehouses and active tables
                var warehouses = RunSync(() => mediator.Send(new ListAllWarehousesQuery()));
                Assert.NotEmpty(warehouses);
                var warehouseId = warehouses.First().WarehouseId;

                var tables = RunSync(() => mediator.Send(new ListAllTablesQuery()));
                Assert.NotEmpty(tables);

                var deals = RunSync(() => mediator.Send(new ListActiveQuickOrderTemplatesQuery()));
                Assert.NotEmpty(deals);
                var familyBiryaniDeal = deals.FirstOrDefault(d => d.Name.Contains("Biryani")) ?? deals.First();
                Log($"Using deal: {familyBiryaniDeal.Name}");

                // -------------------------------------------------------------
                // TEST 1: Deal Preview Dialog Visual Validation
                // -------------------------------------------------------------
                Log("Running Test 1: Deal Preview Dialog");
                using (var preview = new QuickOrderPreviewDialog(
                    familyBiryaniDeal,
                    null,
                    () => false, // no active order
                    () => mediator.Send(new ListAllTablesQuery())))
                {
                    CaptureForm(preview, Path.Combine(qaDir, "01_deal_preview_dialog.png"), Log);
                    preview.Close();
                    Log("Closed preview dialog");
                }

                // -------------------------------------------------------------
                // TEST 2: Start Order Choice Dialog Visual Validation
                // -------------------------------------------------------------
                Log("Running Test 2: Start Order Choice Dialog");
                using (var choiceDialog = new StartOrderChoiceDialog(null))
                {
                    CaptureForm(choiceDialog, Path.Combine(qaDir, "02_start_order_choice_dialog.png"), Log);
                    choiceDialog.Close();
                    Log("Closed choiceDialog");
                }

                // -------------------------------------------------------------
                // TEST 3: Select Table Dialog Visual Validation
                // -------------------------------------------------------------
                Log("Running Test 3: Select Table Dialog");
                using (var tableDialog = new SelectTableDialog(tables, null))
                {
                    CaptureForm(tableDialog, Path.Combine(qaDir, "03_select_table_dialog.png"), Log);
                    tableDialog.Close();
                    Log("Closed tableDialog");
                }

                Log("Instantiating RestaurantPosForm...");
                // Launch Real Restaurant POS Form
                using var posForm = new RestaurantPosForm(
                    host.Services.GetRequiredService<IServiceScopeFactory>(),
                    session,
                    host.Services.GetRequiredService<IMenuItemsChangeNotifier>(),
                    host.Services.GetRequiredService<Clovent.Desktop.Authorization.IManagerAuthorizationService>(),
                    host.Services.GetRequiredService<ISplashScreenService>());

                posForm.Size = new Size(1366, 768);
                Log("Calling posForm.Show()...");
                posForm.Show();

                var flags = BindingFlags.NonPublic | BindingFlags.Instance;

                Log("Waiting for posForm.IsInitialLoadComplete...");
                var waitStart = Environment.TickCount;
                while (!posForm.IsInitialLoadComplete && Environment.TickCount - waitStart < 20000)
                {
                    Application.DoEvents();
                    Thread.Sleep(30);
                }
                for (int i = 0; i < 10; i++) { Application.DoEvents(); Thread.Sleep(20); }
                Log($"posForm initial load complete: IsInitialLoadComplete={posForm.IsInitialLoadComplete}");

            // -------------------------------------------------------------
            // TEST 4: Case B - No Order -> Deal -> Take Away
            // -------------------------------------------------------------
            Log("Running Test 4: Case B Take Away");
            // Ensure no current order
            typeof(RestaurantPosForm).GetField("_currentOrder", flags)!.SetValue(posForm, null);
            typeof(RestaurantPosForm).GetField("_currentOrderLines", flags)!.SetValue(posForm, Array.Empty<OrderLineDto>());
            var warehousePickerField = typeof(RestaurantPosForm).GetField("_warehousePicker", flags);
            var picker = warehousePickerField?.GetValue(posForm) as Clovent.Desktop.MasterData.EntityPicker;
            picker?.SelectId(warehouseId);

            var handleDealMethod = typeof(RestaurantPosForm).GetMethod("HandleQuickOrderAddAsync", flags)!;

            Log("Invoking HandleQuickOrderAddAsync for Take Away");
            var taskTakeAway = (Task)handleDealMethod.Invoke(posForm, new object?[] { familyBiryaniDeal, StartOrderChoice.TakeAway, null })!;
            PumpUntil(taskTakeAway);
            Log("Take Away deal added successfully!");

            var currentOrder = (OrderDto?)typeof(RestaurantPosForm).GetField("_currentOrder", flags)!.GetValue(posForm);
            Assert.NotNull(currentOrder);
            Assert.Equal("TakeAway", currentOrder.OrderType);
            Assert.Null(currentOrder.TableId);

            var lines = RunSync(() => mediator.Send(new ListOrderLinesByOrderQuery(currentOrder.OrderId)));
            Assert.Equal(familyBiryaniDeal.Items.Count, lines.Count);

            var totalAmount = lines.Sum(l => l.Quantity * l.UnitPrice);
            Assert.Equal(familyBiryaniDeal.TotalPrice, totalAmount);
            CaptureForm(posForm, Path.Combine(qaDir, "04_case_b_takeaway_deal_added.png"), Log);

            // -------------------------------------------------------------
            // TEST 5: Hold and Recall of the Deal Order
            // -------------------------------------------------------------
            Log("Running Test 5: Hold and Recall");
            var holdMethod = typeof(RestaurantPosForm).GetMethod("HoldCurrentOrderAsync", flags);
            if (holdMethod != null)
            {
                var holdTask = (Task)holdMethod.Invoke(posForm, null)!;
                PumpUntil(holdTask);
            }
            else
            {
                RunSync(() => mediator.Send(new HoldOrderCommand(currentOrder.OrderId)));
                typeof(RestaurantPosForm).GetField("_currentOrder", flags)!.SetValue(posForm, null);
            }
            for (int i = 0; i < 10; i++) { Application.DoEvents(); Thread.Sleep(15); }
            CaptureForm(posForm, Path.Combine(qaDir, "05_deal_order_held.png"), Log);

            // Recall order
            Log("Recalling order...");
            var resumeTask = RunSync(() => mediator.Send(new ResumeOrderCommand(currentOrder.OrderId)));
            Assert.Equal("Open", resumeTask.Status);
            var setOrderMethod = typeof(RestaurantPosForm).GetMethod("LoadOrderByIdAsync", flags);
            if (setOrderMethod != null)
            {
                var lTask = (Task)setOrderMethod.Invoke(posForm, new object[] { currentOrder.OrderId })!;
                PumpUntil(lTask);
            }
            else
            {
                typeof(RestaurantPosForm).GetField("_currentOrder", flags)!.SetValue(posForm, resumeTask);
            }
            for (int i = 0; i < 10; i++) { Application.DoEvents(); Thread.Sleep(15); }
            CaptureForm(posForm, Path.Combine(qaDir, "06_deal_order_recalled.png"), Log);

            // -------------------------------------------------------------
            // TEST 6: Case A - Existing Order -> Deal Appended
            // -------------------------------------------------------------
            Log("Running Test 6: Case A - Existing Order -> Deal Appended");
            // With active order currently loaded, add another deal
            var secondDeal = deals.FirstOrDefault(d => d.TemplateId != familyBiryaniDeal.TemplateId) ?? familyBiryaniDeal;
            var initialLineCount = lines.Count;

            var taskExisting = (Task)handleDealMethod.Invoke(posForm, new object?[] { secondDeal, StartOrderChoice.Cancel, null })!;
            PumpUntil(taskExisting);

            var activeOrderAfter = (OrderDto?)typeof(RestaurantPosForm).GetField("_currentOrder", flags)!.GetValue(posForm);
            Assert.NotNull(activeOrderAfter);
            Assert.Equal(currentOrder.OrderId, activeOrderAfter.OrderId); // Same order!

            var updatedLines = RunSync(() => mediator.Send(new ListOrderLinesByOrderQuery(activeOrderAfter.OrderId)));
            Assert.True(updatedLines.Count > initialLineCount);
            CaptureForm(posForm, Path.Combine(qaDir, "07_case_a_existing_order_deal_appended.png"), Log);

            // Cancel the order cleanly to free up
            RunSync(() => mediator.Send(new CancelOrderCommand(activeOrderAfter.OrderId, "QA acceptance complete")));
            typeof(RestaurantPosForm).GetField("_currentOrder", flags)!.SetValue(posForm, null);
            typeof(RestaurantPosForm).GetField("_currentOrderLines", flags)!.SetValue(posForm, Array.Empty<OrderLineDto>());

            // -------------------------------------------------------------
            // TEST 7: Case B - No Order -> Deal -> Dine-In -> Available Table
            // -------------------------------------------------------------
            Log("Running Test 7: Case B - Dine-In");
            var refreshedTables = RunSync(() => mediator.Send(new ListAllTablesQuery()));
            var availableTable = refreshedTables.FirstOrDefault(t => string.Equals(t.OccupancyStatus, "Available", StringComparison.OrdinalIgnoreCase));
            if (availableTable == null && refreshedTables.Count > 0)
            {
                var candidate = refreshedTables.First();
                var openOrders = RunSync(() => mediator.Send(new ListOpenOrdersQuery()));
                foreach (var to in openOrders.Where(o => o.TableId == candidate.TableId))
                {
                    RunSync(() => mediator.Send(new CancelOrderCommand(to.OrderId, "Cleanup for test")));
                }
                var heldOrders = RunSync(() => mediator.Send(new ListHeldOrdersQuery()));
                foreach (var ho in heldOrders.Where(o => o.TableId == candidate.TableId))
                {
                    RunSync(() => mediator.Send(new CancelOrderCommand(ho.OrderId, "Cleanup for test")));
                }
                refreshedTables = RunSync(() => mediator.Send(new ListAllTablesQuery()));
                availableTable = refreshedTables.FirstOrDefault(t => t.TableId == candidate.TableId);
            }
            Assert.NotNull(availableTable);

            var taskDineIn = (Task)handleDealMethod.Invoke(posForm, new object?[] { familyBiryaniDeal, StartOrderChoice.DineIn, availableTable.TableId })!;
            PumpUntil(taskDineIn);

            var dineInOrder = (OrderDto?)typeof(RestaurantPosForm).GetField("_currentOrder", flags)!.GetValue(posForm);
            Assert.NotNull(dineInOrder);
            Assert.Equal("DineIn", dineInOrder.OrderType);
            Assert.Equal(availableTable.TableId, dineInOrder.TableId);

            var dineInLines = RunSync(() => mediator.Send(new ListOrderLinesByOrderQuery(dineInOrder.OrderId)));
            Assert.Equal(familyBiryaniDeal.Items.Count, dineInLines.Count);
            CaptureForm(posForm, Path.Combine(qaDir, "08_case_b_dinein_deal_added.png"), Log);

            // Clean up Dine-In order
            RunSync(() => mediator.Send(new CancelOrderCommand(dineInOrder.OrderId, "QA test cleanup")));
            typeof(RestaurantPosForm).GetField("_currentOrder", flags)!.SetValue(posForm, null);

            // -------------------------------------------------------------
            // TEST 8: Suggested Add-ons Regression Verification
            // -------------------------------------------------------------
            Log("Running Test 8: Suggested Add-ons");
            var iconControl = (PictureEdit?)typeof(RestaurantPosForm).GetField("_suggestionHeaderIcon", flags)!.GetValue(posForm);
            var headerLabel = (LabelControl?)typeof(RestaurantPosForm).GetField("_suggestionHeaderLabel", flags)!.GetValue(posForm);
            Assert.NotNull(iconControl);
            Assert.NotNull(headerLabel);
            Assert.NotNull(iconControl.SvgImage);

            // -------------------------------------------------------------
            // TEST 9: Multi-Resolution Responsiveness Tests
            // -------------------------------------------------------------
            Log("Running Test 9: Multi-Resolution 1024x768");
            posForm.WindowState = FormWindowState.Normal;
            posForm.Size = new Size(1024, 768);
            for (int i = 0; i < 10; i++) { Application.DoEvents(); Thread.Sleep(15); }
            CaptureForm(posForm, Path.Combine(qaDir, "09_res_1024x768.png"), Log);

            Log("Running Test 10: Multi-Resolution 1366x768");
            posForm.Size = new Size(1366, 768);
            for (int i = 0; i < 10; i++) { Application.DoEvents(); Thread.Sleep(15); }
            CaptureForm(posForm, Path.Combine(qaDir, "10_res_1366x768.png"), Log);

            Log("Running Test 11: Multi-Resolution Maximized");
            posForm.WindowState = FormWindowState.Maximized;
            for (int i = 0; i < 10; i++) { Application.DoEvents(); Thread.Sleep(15); }
            CaptureForm(posForm, Path.Combine(qaDir, "11_res_maximized.png"), Log);

            Log("Closing posForm...");
            posForm.Close();
            Log("Test suite completed successfully!");
            }
            catch (Exception ex)
            {
                threadEx = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadEx != null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(threadEx).Throw();
        }
    }
}
