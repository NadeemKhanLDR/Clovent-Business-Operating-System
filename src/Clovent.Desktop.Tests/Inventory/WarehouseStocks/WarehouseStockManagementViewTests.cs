using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Catalog.Application.Variants.Dtos;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Inventory.WarehouseStocks;
using Clovent.Desktop.MasterData;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Inventory.Application.WarehouseStocks.Dtos;
using Clovent.Inventory.Application.WarehouseStocks.Queries;
using Clovent.MasterData.Application.Warehouses.Dtos;
using Clovent.MasterData.Application.Warehouses.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Desktop.Tests.Inventory.WarehouseStocks;

public class WarehouseStockManagementViewTests
{
    private sealed class FakeCurrentSession : ICurrentSession
    {
        public Guid? UserId { get; set; } = Guid.NewGuid();
        public Guid? SessionId { get; set; } = Guid.NewGuid();
        public string? DisplayName { get; set; } = "Inventory Manager";
        public bool IsAuthenticated => UserId.HasValue;
        public void SignIn(Guid userId, Guid sessionId, string displayName) { }
        public void SignOut() { }
#pragma warning disable CS0067
        public event EventHandler? Changed;
#pragma warning restore CS0067
    }

    private sealed class StockTestMediator : IMediator
    {
        public List<WarehouseDto> Warehouses { get; set; } = [];
        public List<ProductVariantDto> Variants { get; set; } = [];
        public List<WarehouseStockDto> Stocks { get; set; } = [];

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is ListAllWarehousesQuery)
            {
                return Task.FromResult((TResponse)(object)Warehouses.AsReadOnly());
            }

            if (request is ListProductVariantsQuery)
            {
                return Task.FromResult((TResponse)(object)Variants.AsReadOnly());
            }

            if (request is ListWarehouseStocksByWarehouseQuery wq)
            {
                var matching = Stocks.Where(s => s.WarehouseId == wq.WarehouseId).ToList();
                return Task.FromResult((TResponse)(object)matching.AsReadOnly());
            }

            return Task.FromResult(default(TResponse)!);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest => Task.CompletedTask;
        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => Task.FromResult<object?>(null);
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class FakeServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = [];
        public void Register<T>(T service) where T : class => _services[typeof(T)] = service;
        public object? GetService(Type serviceType) => _services.TryGetValue(serviceType, out var service) ? service : null;
    }

    private sealed class FakeServiceScope(IServiceProvider serviceProvider) : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = serviceProvider;
        public void Dispose() { }
    }

    private sealed class FakeServiceScopeFactory(IServiceProvider serviceProvider) : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new FakeServiceScope(serviceProvider);
    }

    private sealed class AllowAllFeaturePolicy : IFeatureAuthorizationPolicy
    {
        public Task<bool> CanUseFeatureAsync(Guid userId, string featureCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private static (WarehouseStockManagementView View, StockTestMediator Mediator) CreateView()
    {
        var session = new FakeCurrentSession();
        var mediator = new StockTestMediator();
        var provider = new FakeServiceProvider();
        provider.Register<IMediator>(mediator);
        provider.Register<IFeatureAuthorizationPolicy>(new AllowAllFeaturePolicy());

        var view = new WarehouseStockManagementView(new FakeServiceScopeFactory(provider), session);
        return (view, mediator);
    }

    [Fact]
    public void WarehouseStockManagementView_DesignerSafety_InstantiatesWithoutExceptions()
    {
        // I. Designer safety: InitializeComponent does not call database, MediatR, or service resolution at design time
        var (view, _) = CreateView();
        using (view)
        {
            Assert.NotNull(view);
            Assert.NotNull(view.Controls);
            Assert.True(view.Controls.Count > 0);
        }
    }

    [Fact]
    public void WarehouseStockManagementView_ReceiveInventoryControl_IsNotClipped_AndHasProperSizingAndPadding()
    {
        // H. Receive Inventory control is not clipped: caption is fully visible, AutoSize is true, MinimumSize provides adequate headroom
        var (view, _) = CreateView();
        using (view)
        {
            var btnField = typeof(WarehouseStockManagementView).GetField("_receiveInventoryButton", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(btnField);
            var btn = Assert.IsType<SimpleButton>(btnField.GetValue(view));

            Assert.Equal("Receive Inventory", btn.Text);
            Assert.True(btn.AutoSize);
            Assert.True(btn.MinimumSize.Width >= 130, $"Expected minimum width >= 130, but was {btn.MinimumSize.Width}");
            Assert.True(btn.MinimumSize.Height >= 28, $"Expected minimum height >= 28, but was {btn.MinimumSize.Height}");
            Assert.Equal(AnchorStyles.Left, btn.Anchor);
            Assert.Equal("Segoe UI", btn.Appearance.Font.FontFamily.Name);
            Assert.True(btn.Appearance.Font.Bold);

            // Verify it is placed alongside WarehousePicker in a unified headerPanel
            var parentPanel = Assert.IsType<TableLayoutPanel>(btn.Parent);
            Assert.Equal(1, parentPanel.RowCount);
            Assert.Equal(2, parentPanel.ColumnCount);
        }
    }

    [Fact]
    public async Task WarehouseStockManagementView_Projection_ContainsSkuAndProduct_ForValidInventoryRecords()
    {
        // F & G. Stock On Hand projection contains SKU and Product for valid inventory records,
        // and a stock row with quantity cannot silently display blank Product identity.
        var (view, mediator) = CreateView();
        using (view)
        {
            var warehouseId = Guid.NewGuid();
            var variantId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            mediator.Warehouses.Add(new WarehouseDto(warehouseId, Guid.NewGuid(), "Kitchen Backup Warehouse", "KBW", "Active", DateTimeOffset.UtcNow));
            mediator.Variants.Add(new ProductVariantDto(
                variantId,
                productId,
                "Standard",
                "NAAN-STD",
                Guid.NewGuid(),
                "Active",
                1,
                DateTimeOffset.UtcNow,
                null,
                "Active",
                true,
                "Prepared",
                "Naan"));

            mediator.Stocks.Add(new WarehouseStockDto(
                Guid.NewGuid(),
                warehouseId,
                variantId,
                145m,
                0m,
                145m,
                10m,
                200m,
                false,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow));

            // Invoke load lookups
            var loadMethod = typeof(WarehouseStockManagementView).GetMethod("LoadLookupsAsync", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(loadMethod);
            await (Task)loadMethod.Invoke(view, null)!;

            // Load items
            var loadItemsMethod = typeof(WarehouseStockManagementView).GetMethod("LoadItemsAsync", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(loadItemsMethod);
            var resultTask = (Task)loadItemsMethod.Invoke(view, [CancellationToken.None])!;
            await resultTask;

            var rowsProperty = resultTask.GetType().GetProperty("Result");
            Assert.NotNull(rowsProperty);
            var rows = ((IEnumerable<object>)rowsProperty.GetValue(resultTask)!).ToList();

            Assert.Single(rows);
            var row = rows[0];

            var skuProp = row.GetType().GetProperty("Sku");
            var nameProp = row.GetType().GetProperty("Name");
            var onHandProp = row.GetType().GetProperty("QuantityOnHand");
            var reservedProp = row.GetType().GetProperty("QuantityReserved");
            var availableProp = row.GetType().GetProperty("QuantityAvailable");

            Assert.NotNull(skuProp);
            Assert.NotNull(nameProp);

            var sku = (string)skuProp.GetValue(row)!;
            var name = (string)nameProp.GetValue(row)!;
            var onHand = (decimal)onHandProp!.GetValue(row)!;
            var reserved = (decimal)reservedProp!.GetValue(row)!;
            var available = (decimal)availableProp!.GetValue(row)!;

            Assert.Equal("NAAN-STD", sku);
            Assert.Equal("Naan - Standard", name);
            Assert.Equal(145m, onHand);
            Assert.Equal(0m, reserved);
            Assert.Equal(145m, available);

            // G. Stock row cannot silently display blank Product identity
            Assert.False(string.IsNullOrWhiteSpace(sku), "SKU must not be blank for a valid stock record");
            Assert.False(string.IsNullOrWhiteSpace(name), "Product Name must not be blank for a valid stock record");
        }
    }

    [Fact]
    public async Task WarehouseStockManagementView_LoadItemsAsync_PreloadsVariants_IfLookupsNotYetPopulated()
    {
        // G. Guard test: Even if LoadItemsAsync is invoked before LoadLookupsAsync finishes,
        // it automatically queries and populates variant lookups so SKU and Product are never blank.
        var (view, mediator) = CreateView();
        using (view)
        {
            var warehouseId = Guid.NewGuid();
            var variantId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            mediator.Warehouses.Add(new WarehouseDto(warehouseId, Guid.NewGuid(), "Kitchen Backup Warehouse", "KBW", "Active", DateTimeOffset.UtcNow));
            mediator.Variants.Add(new ProductVariantDto(
                variantId,
                productId,
                "Standard",
                "NAAN-STD",
                Guid.NewGuid(),
                "Active",
                1,
                DateTimeOffset.UtcNow,
                null,
                "Active",
                true,
                "Prepared",
                "Naan"));

            mediator.Stocks.Add(new WarehouseStockDto(
                Guid.NewGuid(),
                warehouseId,
                variantId,
                145m,
                0m,
                145m,
                10m,
                200m,
                false,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow));

            // Select warehouse directly on picker without running LoadLookupsAsync
            var picker = (EntityPicker)typeof(WarehouseStockManagementView).GetField("_warehousePicker", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            picker.LoadItems([(warehouseId, "Kitchen Backup Warehouse")]);

            // Call LoadItemsAsync directly
            var loadItemsMethod = typeof(WarehouseStockManagementView).GetMethod("LoadItemsAsync", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(loadItemsMethod);
            var resultTask = (Task)loadItemsMethod.Invoke(view, [CancellationToken.None])!;
            await resultTask;

            var rowsProperty = resultTask.GetType().GetProperty("Result");
            Assert.NotNull(rowsProperty);
            var rows = ((IEnumerable<object>)rowsProperty.GetValue(resultTask)!).ToList();

            Assert.Single(rows);
            var row = rows[0];

            var sku = (string)row.GetType().GetProperty("Sku")!.GetValue(row)!;
            var name = (string)row.GetType().GetProperty("Name")!.GetValue(row)!;

            Assert.Equal("NAAN-STD", sku);
            Assert.Equal("Naan - Standard", name);
        }
    }
}
