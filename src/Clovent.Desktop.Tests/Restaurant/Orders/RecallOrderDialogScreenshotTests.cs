using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using Clovent.Catalog.Application.Products.Dtos;
using Clovent.Catalog.Application.Products.Queries;
using Clovent.Catalog.Application.Variants.Dtos;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Restaurant.Application.ActivityLogs.Dtos;
using Clovent.Restaurant.Application.ActivityLogs.Queries;
using Clovent.Restaurant.Application.Customers.Dtos;
using Clovent.Restaurant.Application.Customers.Queries;
using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.Application.OrderLines.Queries;
using Clovent.Restaurant.Application.Orders;
using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Application.Orders.Queries;
using Clovent.Restaurant.Application.Tables.Dtos;
using Clovent.Restaurant.Application.Tables.Queries;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Orders;

public sealed class RecallOrderDialogScreenshotTests
{
    private sealed class MockMediator : IMediator
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest => Task.CompletedTask;
        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public readonly List<OrderDto> HeldOrders = [];
        public readonly List<OrderDto> OpenOrders = [];
        public readonly List<OrderDto> AllOrders = [];
        public readonly List<TableDto> Tables = [];
        public readonly List<CustomerDto> Customers = [];
        public readonly List<ActivityLogEntryDto> Activities = [];
        public readonly Dictionary<Guid, OrderTotals> Summaries = [];
        public readonly Dictionary<Guid, List<OrderLineDto>> OrderLines = [];
        public readonly Dictionary<Guid, ProductVariantDto> Variants = [];

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            object? result = request switch
            {
                ListHeldOrdersQuery => HeldOrders,
                ListOpenOrdersQuery => OpenOrders,
                ListAllOrdersQuery => AllOrders,
                ListAllTablesQuery => Tables,
                ListCustomersQuery => Customers,
                ListRecentActivityQuery => Activities,
                GetOrderSummaryQuery q => Summaries.TryGetValue(q.OrderId, out var s) ? s : new OrderTotals(100m, 0m, 0m, 0m, 100m, 0m, 100m),
                ListOrderLinesByOrderQuery q => OrderLines.TryGetValue(q.OrderId, out var lines) ? lines : new List<OrderLineDto>(),
                GetProductVariantByIdQuery q => Variants.TryGetValue(q.ProductVariantId, out var v) ? v : new ProductVariantDto(q.ProductVariantId, Guid.NewGuid(), "Item", "SKU", Guid.NewGuid(), "Active", 0, DateTimeOffset.UtcNow),
                GetProductByIdQuery q => new ProductDto(q.ProductId, "Item", "SKU", null, null, null, Guid.NewGuid(), 0m, false, "Active", DateTimeOffset.UtcNow),
                GetOrderByIdQuery q => AllOrders.FirstOrDefault(o => o.OrderId == q.OrderId),
                _ => null,
            };

            return Task.FromResult((TResponse)result!);
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

    private static void CaptureDialog(RecallOrderDialog dialog, string outPath)
    {
        dialog.StartPosition = FormStartPosition.Manual;
        dialog.Location = new Point(10, 10);
        dialog.Show();
        for (int i = 0; i < 20; i++)
        {
            Application.DoEvents();
            Thread.Sleep(30);
        }
        var bounds = dialog.Bounds;
        using var bmp = new Bitmap(bounds.Width, bounds.Height);
        using (var g = Graphics.FromImage(bmp))
        {
            var hdc = g.GetHdc();
            try
            {
                PrintWindow(dialog.Handle, hdc, 2); // 2 = PW_RENDERFULLCONTENT
            }
            finally
            {
                g.ReleaseHdc(hdc);
            }
        }
        Console.WriteLine($"[CaptureDialog] Path={Path.GetFileName(outPath)}, Size={dialog.Size.Width}x{dialog.Size.Height}, ClientSize={dialog.ClientSize.Width}x{dialog.ClientSize.Height}, DPI={dialog.DeviceDpi}");
        bmp.Save(outPath, ImageFormat.Png);
        dialog.Close();
        Application.DoEvents();
    }

    [Fact]
    public void RenderRecallScreenshots_At1024_1366_1920()
    {
        var thread = new Thread(() =>
        {
            var mediator = new MockMediator();
            var logger = NullLogger<RecallOrderDialog>.Instance;

            // Setup tables
            var t1Id = Guid.NewGuid();
            var t2Id = Guid.NewGuid();
            var t3Id = Guid.NewGuid();
            mediator.Tables.Add(new TableDto(t1Id, Guid.NewGuid(), "T-01", "Table 1", 4, "Active", "Available", DateTimeOffset.UtcNow));
            mediator.Tables.Add(new TableDto(t2Id, Guid.NewGuid(), "T-02", "Table 2", 4, "Active", "Available", DateTimeOffset.UtcNow));
            mediator.Tables.Add(new TableDto(t3Id, Guid.NewGuid(), "T-03", "Table 3", 6, "Active", "Available", DateTimeOffset.UtcNow));

            // Setup customers
            var c1Id = Guid.NewGuid();
            var c2Id = Guid.NewGuid();
            mediator.Customers.Add(new CustomerDto(c1Id, "WALK", "Walk-in Customer", "-", "Counter", null, 0, 0, 0, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
            mediator.Customers.Add(new CustomerDto(c2Id, "CUST-1", "Ahmed Khan", "0300-1234567", "Gulberg, Lahore", null, 0, 0, 0, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

            // Setup variants
            var v1Id = Guid.NewGuid();
            var v2Id = Guid.NewGuid();
            var v3Id = Guid.NewGuid();
            mediator.Variants[v1Id] = new ProductVariantDto(v1Id, Guid.NewGuid(), "Chicken Biryani - Full", "CB-F", Guid.NewGuid(), "Active", 0, DateTimeOffset.UtcNow);
            mediator.Variants[v2Id] = new ProductVariantDto(v2Id, Guid.NewGuid(), "Aloo Gobi - Half", "AG-H", Guid.NewGuid(), "Active", 0, DateTimeOffset.UtcNow);
            mediator.Variants[v3Id] = new ProductVariantDto(v3Id, Guid.NewGuid(), "Garlic Nan", "GN-1", Guid.NewGuid(), "Active", 0, DateTimeOffset.UtcNow);

            // ORD-87 (Held, Dine In, T-03, Walk-in, Rs. 1,760.00)
            var o87Id = Guid.NewGuid();
            var l87_1 = new OrderLineDto(Guid.NewGuid(), o87Id, v1Id, 2m, 340m, 340m, false, null, null, null, 0m, true, null, false, 680m, DateTimeOffset.UtcNow.AddMinutes(-19));
            var l87_2 = new OrderLineDto(Guid.NewGuid(), o87Id, v2Id, 3m, 220m, 220m, false, null, null, null, 0m, true, null, false, 660m, DateTimeOffset.UtcNow.AddMinutes(-19));
            var l87_3 = new OrderLineDto(Guid.NewGuid(), o87Id, v3Id, 4m, 105m, 105m, false, null, null, null, 0m, true, null, false, 420m, DateTimeOffset.UtcNow.AddMinutes(-19));
            var dto87 = new OrderDto(o87Id, "ORD-87", 87, "DineIn", "Held", t3Id, Guid.NewGuid(), null, null, [l87_1.OrderLineId, l87_2.OrderLineId, l87_3.OrderLineId], [], [], [], DateTimeOffset.UtcNow.AddMinutes(-19), DateTimeOffset.UtcNow.AddMinutes(-19), c1Id);
            mediator.HeldOrders.Add(dto87);
            mediator.AllOrders.Add(dto87);
            mediator.OrderLines[o87Id] = [l87_1, l87_2, l87_3];
            mediator.Summaries[o87Id] = new OrderTotals(1760m, 0m, 0m, 0m, 1760m, 0m, 1760m);

            // ORD-89 (Held, Dine In, T-02, Walk-in, Rs. 30.00)
            var o89Id = Guid.NewGuid();
            var l89_1 = new OrderLineDto(Guid.NewGuid(), o89Id, v3Id, 1m, 30m, 30m, false, null, null, null, 0m, true, null, false, 30m, DateTimeOffset.UtcNow.AddMinutes(-19));
            var dto89 = new OrderDto(o89Id, "ORD-89", 89, "DineIn", "Held", t2Id, Guid.NewGuid(), null, null, [l89_1.OrderLineId], [], [], [], DateTimeOffset.UtcNow.AddMinutes(-19), DateTimeOffset.UtcNow.AddMinutes(-19), c1Id);
            mediator.HeldOrders.Add(dto89);
            mediator.AllOrders.Add(dto89);
            mediator.OrderLines[o89Id] = [l89_1];
            mediator.Summaries[o89Id] = new OrderTotals(30m, 0m, 0m, 0m, 30m, 0m, 30m);

            // ORD-91 (Held, Take Away, -, Ahmed Khan, Rs. 57.50)
            var o91Id = Guid.NewGuid();
            var l91_1 = new OrderLineDto(Guid.NewGuid(), o91Id, v3Id, 1m, 57.50m, 57.50m, false, null, null, null, 0m, true, null, false, 57.50m, DateTimeOffset.UtcNow.AddHours(-2));
            var dto91 = new OrderDto(o91Id, "ORD-91", 91, "TakeAway", "Held", null, Guid.NewGuid(), null, null, [l91_1.OrderLineId], [], [], [], DateTimeOffset.UtcNow.AddHours(-2), DateTimeOffset.UtcNow.AddHours(-2), c2Id);
            mediator.HeldOrders.Add(dto91);
            mediator.AllOrders.Add(dto91);
            mediator.OrderLines[o91Id] = [l91_1];
            mediator.Summaries[o91Id] = new OrderTotals(57.50m, 0m, 0m, 0m, 57.50m, 0m, 57.50m);

            // ORD-85 (Closed, Take Away, -, Walk-in, Rs. 1,200.00)
            var o85Id = Guid.NewGuid();
            var l85_1 = new OrderLineDto(Guid.NewGuid(), o85Id, v1Id, 4m, 300m, 300m, false, null, null, null, 0m, true, null, false, 1200m, DateTimeOffset.UtcNow.AddHours(-3));
            var dto85 = new OrderDto(o85Id, "ORD-85", 85, "TakeAway", "Completed", null, Guid.NewGuid(), null, null, [l85_1.OrderLineId], [], [], [Guid.NewGuid()], DateTimeOffset.UtcNow.AddHours(-3), DateTimeOffset.UtcNow.AddHours(-3), c1Id);
            mediator.AllOrders.Add(dto85);
            mediator.OrderLines[o85Id] = [l85_1];
            mediator.Summaries[o85Id] = new OrderTotals(1200m, 0m, 0m, 0m, 1200m, 1200m, 0m);

            // ORD-80 (Voided, Dine In, T-01, Walk-in, Rs. 500.00)
            var o80Id = Guid.NewGuid();
            var l80_1 = new OrderLineDto(Guid.NewGuid(), o80Id, v2Id, 2m, 250m, 250m, false, null, null, null, 0m, true, null, false, 500m, DateTimeOffset.UtcNow.AddHours(-4));
            var dto80 = new OrderDto(o80Id, "ORD-80", 80, "DineIn", "Voided", t1Id, Guid.NewGuid(), "Customer cancelled", null, [l80_1.OrderLineId], [], [], [], DateTimeOffset.UtcNow.AddHours(-4), DateTimeOffset.UtcNow.AddHours(-4), c1Id);
            mediator.AllOrders.Add(dto80);
            mediator.OrderLines[o80Id] = [l80_1];
            mediator.Summaries[o80Id] = new OrderTotals(500m, 0m, 0m, 0m, 500m, 0m, 500m);

            // Activity log
            mediator.Activities.Add(new ActivityLogEntryDto(Guid.NewGuid(), "Hold Order", "Held order ORD-87", "Cashier 1", "POS-01", DateTimeOffset.UtcNow.AddMinutes(-19)));
            mediator.Activities.Add(new ActivityLogEntryDto(Guid.NewGuid(), "Hold Order", "Held order ORD-89", "Cashier 1", "POS-01", DateTimeOffset.UtcNow.AddMinutes(-19)));
            mediator.Activities.Add(new ActivityLogEntryDto(Guid.NewGuid(), "Hold Order", "Held order ORD-91", "Cashier 2", "POS-02", DateTimeOffset.UtcNow.AddHours(-2)));
            mediator.Activities.Add(new ActivityLogEntryDto(Guid.NewGuid(), "Void Order", "Voided order ORD-80: Customer changed mind", "Manager", "POS-01", DateTimeOffset.UtcNow.AddHours(-4)));

            var qaDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "qa"));
            Directory.CreateDirectory(qaDir);

            var loadMethod = typeof(RecallOrderDialog).GetMethod("LoadAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var switchStatusMethod = typeof(RecallOrderDialog).GetMethod("SwitchStatus", BindingFlags.Instance | BindingFlags.NonPublic)!;

            // Resolution 1: 1024x768 (Operational bounds ~950x640)
            using (var dialog1024 = new RecallOrderDialog(mediator, logger))
            {
                dialog1024.ApplyOperationalSize(1024, 768);
                ((Task)loadMethod.Invoke(dialog1024, null)!).GetAwaiter().GetResult();
                CaptureDialog(dialog1024, Path.Combine(qaDir, "runtime_1024_held.png"));
            }

            // Resolution 2: 1366x768 (Operational bounds ~1140x680)
            using (var dialog1366 = new RecallOrderDialog(mediator, logger))
            {
                dialog1366.ApplyOperationalSize(1366, 768);
                ((Task)loadMethod.Invoke(dialog1366, null)!).GetAwaiter().GetResult();
                CaptureDialog(dialog1366, Path.Combine(qaDir, "runtime_1366_held.png"));
            }

            // Resolution 3: 1920x1080 (Operational bounds 1200x740)
            using (var dialog1920 = new RecallOrderDialog(mediator, logger))
            {
                dialog1920.ApplyOperationalSize(1920, 1080);
                ((Task)loadMethod.Invoke(dialog1920, null)!).GetAwaiter().GetResult();
                CaptureDialog(dialog1920, Path.Combine(qaDir, "runtime_1920_held.png"));
            }

            // Open status tab at 1200x740
            using (var dialogOpen = new RecallOrderDialog(mediator, logger))
            {
                dialogOpen.ApplyOperationalSize(1920, 1080);
                switchStatusMethod.Invoke(dialogOpen, [RecallStatusFilter.Open]);
                CaptureDialog(dialogOpen, Path.Combine(qaDir, "runtime_1920_open.png"));
            }

            // Closed status tab at 1200x740
            using (var dialogClosed = new RecallOrderDialog(mediator, logger))
            {
                dialogClosed.ApplyOperationalSize(1920, 1080);
                switchStatusMethod.Invoke(dialogClosed, [RecallStatusFilter.Closed]);
                CaptureDialog(dialogClosed, Path.Combine(qaDir, "runtime_1920_closed.png"));
            }

            // Voided status tab at 1200x740
            using (var dialogVoided = new RecallOrderDialog(mediator, logger))
            {
                dialogVoided.ApplyOperationalSize(1920, 1080);
                switchStatusMethod.Invoke(dialogVoided, [RecallStatusFilter.Voided]);
                CaptureDialog(dialogVoided, Path.Combine(qaDir, "runtime_1920_voided.png"));
            }

            // Search by customer name
            using (var dialogSearch = new RecallOrderDialog(mediator, logger))
            {
                dialogSearch.ApplyOperationalSize(1920, 1080);
                ((Task)loadMethod.Invoke(dialogSearch, null)!).GetAwaiter().GetResult();
                var searchEdit = (DevExpress.XtraEditors.TextEdit)typeof(RecallOrderDialog)
                    .GetField("_searchEdit", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(dialogSearch)!;
                searchEdit.Text = "Ahmed";
                CaptureDialog(dialogSearch, Path.Combine(qaDir, "runtime_search_customer.png"));
            }

            // Search empty state
            using (var dialogEmpty = new RecallOrderDialog(mediator, logger))
            {
                dialogEmpty.ApplyOperationalSize(1920, 1080);
                ((Task)loadMethod.Invoke(dialogEmpty, null)!).GetAwaiter().GetResult();
                var searchEdit = (DevExpress.XtraEditors.TextEdit)typeof(RecallOrderDialog)
                    .GetField("_searchEdit", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(dialogEmpty)!;
                searchEdit.Text = "NonExistentOrder999";
                CaptureDialog(dialogEmpty, Path.Combine(qaDir, "runtime_empty_state.png"));
            }

            // Render POS context composites showing dialog centered relative to POS at each resolution
            RenderPosComposite(Path.Combine(qaDir, "runtime_1024_held.png"), 1024, 768, Path.Combine(qaDir, "runtime_1024_pos_composite.png"));
            RenderPosComposite(Path.Combine(qaDir, "runtime_1366_held.png"), 1366, 768, Path.Combine(qaDir, "runtime_1366_pos_composite.png"));
            RenderPosComposite(Path.Combine(qaDir, "runtime_1920_held.png"), 1920, 1080, Path.Combine(qaDir, "runtime_1920_pos_composite.png"));

            // Overwrite legacy/previous QA filenames so all viewers see the updated UI
            File.Copy(Path.Combine(qaDir, "runtime_1024_pos_composite.png"), Path.Combine(qaDir, "final_1024_recall.png"), true);
            File.Copy(Path.Combine(qaDir, "runtime_1366_pos_composite.png"), Path.Combine(qaDir, "final_1366_recall.png"), true);
            File.Copy(Path.Combine(qaDir, "runtime_1920_pos_composite.png"), Path.Combine(qaDir, "final_1080_recall.png"), true);
            File.Copy(Path.Combine(qaDir, "runtime_1024_held.png"), Path.Combine(qaDir, "qa_02_recall_held.png"), true);
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        var qa = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "qa"));
        Assert.True(File.Exists(Path.Combine(qa, "runtime_1024_held.png")));
        Assert.True(File.Exists(Path.Combine(qa, "runtime_1366_held.png")));
        Assert.True(File.Exists(Path.Combine(qa, "runtime_1920_held.png")));
        Assert.True(File.Exists(Path.Combine(qa, "runtime_1920_open.png")));
        Assert.True(File.Exists(Path.Combine(qa, "runtime_1920_closed.png")));
        Assert.True(File.Exists(Path.Combine(qa, "runtime_1920_voided.png")));
        Assert.True(File.Exists(Path.Combine(qa, "runtime_search_customer.png")));
        Assert.True(File.Exists(Path.Combine(qa, "runtime_empty_state.png")));
        Assert.True(File.Exists(Path.Combine(qa, "runtime_1024_pos_composite.png")));
        Assert.True(File.Exists(Path.Combine(qa, "runtime_1366_pos_composite.png")));
        Assert.True(File.Exists(Path.Combine(qa, "runtime_1920_pos_composite.png")));
    }

    private static void RenderPosComposite(string dialogPngPath, int resW, int resH, string outPath)
    {
        if (!File.Exists(dialogPngPath)) return;
        using var dialogBmp = new Bitmap(dialogPngPath);
        using var posBmp = new Bitmap(resW, resH);
        using var g = Graphics.FromImage(posBmp);

        // Fill POS background
        g.Clear(Color.FromArgb(241, 245, 249));

        // Draw POS header bar
        using (var headerBrush = new SolidBrush(Color.White))
            g.FillRectangle(headerBrush, 0, 0, resW, 56);
        using (var brandBrush = new SolidBrush(Color.FromArgb(13, 148, 136)))
            g.DrawString("Clovent POS", new Font("Segoe UI", 13, FontStyle.Bold), brandBrush, 16, 14);

        // Draw Active orders rail
        using (var railBrush = new SolidBrush(Color.White))
            g.FillRectangle(railBrush, 0, 60, 240, resH - 60);
        using (var railText = new SolidBrush(Color.FromArgb(71, 85, 105)))
            g.DrawString("Active Orders", new Font("Segoe UI", 10, FontStyle.Bold), railText, 16, 76);

        // Draw menu grid placeholder
        using (var menuText = new SolidBrush(Color.FromArgb(100, 116, 139)))
            g.DrawString($"Foodies Menu ({resW}x{resH})", new Font("Segoe UI", 11, FontStyle.Bold), menuText, 260, 76);

        // Draw Dim backdrop over POS
        using (var dimBrush = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
            g.FillRectangle(dimBrush, 0, 0, resW, resH);

        // Center dialog
        int dX = (resW - dialogBmp.Width) / 2;
        int dY = Math.Max(20, (resH - 40 - dialogBmp.Height) / 2); // 40px taskbar
        g.DrawImage(dialogBmp, dX, dY);

        posBmp.Save(outPath, ImageFormat.Png);
    }

    [Fact]
    public void RenderDealPreviewScreenshots_At1024_1366_1920()
    {
        var thread = new Thread(() =>
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string? dir = baseDir;
            string? qaDir = null;
            while (!string.IsNullOrEmpty(dir))
            {
                var candidate = Path.Combine(dir, "qa");
                if (Directory.Exists(candidate))
                {
                    qaDir = candidate;
                    break;
                }
                dir = Path.GetDirectoryName(dir);
            }
            if (string.IsNullOrEmpty(qaDir))
            {
                qaDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "qa"));
                Directory.CreateDirectory(qaDir);
            }

            // Real deal template item data matching production quick orders
            var template = new Clovent.Restaurant.Application.QuickOrderTemplates.Dtos.QuickOrderTemplateDto(
                Guid.NewGuid(),
                "Chicken Karahi Feast",
                "Full Chicken Karahi with 4 Garlic Naan & 2 Cold Drinks",
                true,
                1,
                [
                    new Clovent.Restaurant.Application.QuickOrderTemplates.Dtos.QuickOrderTemplateItemDto(
                        Guid.NewGuid(), "Chicken Karahi", "Full", 1m, 1200.00m),
                    new Clovent.Restaurant.Application.QuickOrderTemplates.Dtos.QuickOrderTemplateItemDto(
                        Guid.NewGuid(), "Garlic Naan", "Fresh", 4m, 60.00m),
                    new Clovent.Restaurant.Application.QuickOrderTemplates.Dtos.QuickOrderTemplateItemDto(
                        Guid.NewGuid(), "Cold Drink", "500ml", 2m, 60.00m)
                ],
                1560.00m);

            // Resolution 1: 1024x768
            using (var dialog1024 = new QuickOrderPreviewDialog(template))
            {
                dialog1024.ApplyOperationalSize(1024, 768);
                CaptureGenericDialog(dialog1024, Path.Combine(qaDir, "runtime_1024_deal_preview.png"));
            }

            // Resolution 2: 1366x768
            using (var dialog1366 = new QuickOrderPreviewDialog(template))
            {
                dialog1366.ApplyOperationalSize(1366, 768);
                CaptureGenericDialog(dialog1366, Path.Combine(qaDir, "runtime_1366_deal_preview.png"));
            }

            // Resolution 3: 1920x1080
            using (var dialog1920 = new QuickOrderPreviewDialog(template))
            {
                dialog1920.ApplyOperationalSize(1920, 1080);
                CaptureGenericDialog(dialog1920, Path.Combine(qaDir, "runtime_1920_deal_preview.png"));
            }

            // Composite POS preview
            RenderPosComposite(Path.Combine(qaDir, "runtime_1920_deal_preview.png"), 1920, 1080, Path.Combine(qaDir, "runtime_1920_deal_preview_pos_composite.png"));
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        var qa = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "qa"));
        Assert.True(File.Exists(Path.Combine(qa, "runtime_1920_deal_preview.png")));
        Assert.True(File.Exists(Path.Combine(qa, "runtime_1920_deal_preview_pos_composite.png")));
    }

    private static void CaptureGenericDialog(Form dialog, string outPath)
    {
        dialog.StartPosition = FormStartPosition.Manual;
        dialog.Location = new Point(10, 10);
        dialog.Show();
        for (int i = 0; i < 20; i++)
        {
            Application.DoEvents();
            Thread.Sleep(30);
        }
        var bounds = dialog.Bounds;
        using var bmp = new Bitmap(bounds.Width, bounds.Height);
        using (var g = Graphics.FromImage(bmp))
        {
            var hdc = g.GetHdc();
            try
            {
                PrintWindow(dialog.Handle, hdc, 2);
            }
            finally
            {
                g.ReleaseHdc(hdc);
            }
        }
        bmp.Save(outPath, ImageFormat.Png);
        dialog.Close();
        Application.DoEvents();
    }
}
