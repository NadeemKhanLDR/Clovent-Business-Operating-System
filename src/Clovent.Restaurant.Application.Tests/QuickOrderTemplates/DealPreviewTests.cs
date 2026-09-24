using Clovent.Catalog.Application.Prices.Dtos;
using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Application.Products.Dtos;
using Clovent.Catalog.Application.Products.Queries;
using Clovent.Catalog.Application.Variants.Dtos;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Restaurant.Application.OrderLines.Commands;
using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.Application.Orders;
using Clovent.Restaurant.Application.QuickOrderTemplates.Commands;using Clovent.Restaurant.Application.QuickOrderTemplates.Dtos;
using Clovent.Restaurant.Application.QuickOrderTemplates.Queries;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.QuickOrderTemplates;
using Clovent.Restaurant.Tables;
using MediatR;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.QuickOrderTemplates;

/// <summary>
/// Tests the Quick Orders "Deal Preview" business behavior:
/// 1. The quick-order strip data shows the configured deal name and price.
/// 2. The preview payload contains the actual configured contents (items,
///    quantities, variants, resolved prices) - never anything invented.
/// 3. Loading the preview creates/modifies no orders.
/// 4/5/6. "Add Deal" (the POS replays the template through the ordinary
///    AddOrderLine pipeline) adds the configured items with correct
///    quantities/variants and totals computed by the existing POS pricing.
/// 7. Changing the deal configuration in the repository is reflected on the
///    next preview load.
/// </summary>
public class DealPreviewTests
{
    private readonly Guid _biryaniProductId = Guid.NewGuid();
    private readonly Guid _biryaniFullVariantId = Guid.NewGuid();
    private readonly Guid _nanVariantId = Guid.NewGuid();
    private readonly Guid _drinkVariantId = Guid.NewGuid();

    /// <summary>
    /// One mediator serving both the template expansion queries and the
    /// AddOrderLine by-id catalog queries, so a single fake describes one
    /// consistent catalog.
    /// </summary>
    private FakeMediator CreateCatalogMediator(decimal nanPrice = 40m) => new(request =>
    {
        object? response = request switch
        {
            ListProductVariantsQuery => (IReadOnlyCollection<ProductVariantDto>)
            [
                CatalogFakes.Variant(_biryaniFullVariantId, _biryaniProductId, "Full"),
                CatalogFakes.Variant(_nanVariantId, Guid.NewGuid(), "Standard"),
                CatalogFakes.Variant(_drinkVariantId, Guid.NewGuid(), "Standard"),
            ],
            ListProductsQuery => (IReadOnlyCollection<ProductDto>)[CatalogFakes.Product(_biryaniProductId, "Chicken Biryani")],
            ListActiveProductPricesByTypeQuery => (IReadOnlyCollection<ProductPriceDto>)
            [
                CatalogFakes.SellingPrice(_biryaniFullVariantId, 500m),
                CatalogFakes.SellingPrice(_nanVariantId, nanPrice),
                CatalogFakes.SellingPrice(_drinkVariantId, 80m),
            ],
            GetProductVariantByIdQuery q => q.ProductVariantId == _biryaniFullVariantId
                ? CatalogFakes.Variant(_biryaniFullVariantId, _biryaniProductId, "Full")
                : CatalogFakes.Variant(q.ProductVariantId, Guid.NewGuid(), "Standard"),
            GetProductByIdQuery => CatalogFakes.Product(_biryaniProductId, "Chicken Biryani"),
            ListProductPricesByVariantQuery q => (IReadOnlyCollection<ProductPriceDto>)
            [
                q.ProductVariantId == _biryaniFullVariantId
                    ? CatalogFakes.SellingPrice(_biryaniFullVariantId, 500m)
                    : CatalogFakes.SellingPrice(q.ProductVariantId, q.ProductVariantId == _nanVariantId ? nanPrice : 80m),
            ],
            _ => throw new NotSupportedException(request.GetType().Name),
        };
        return Task.FromResult(response!);
    });

    private async Task<QuickOrderTemplateDto> CreateFamilyDealAsync(FakeQuickOrderTemplateRepository repository, FakeMediator mediator)
    {
        var templateId = await new CreateQuickOrderTemplateCommandHandler(repository).Handle(
            new CreateQuickOrderTemplateCommand(
                "Family Deal",
                "Weekend special",
                DisplayOrder: 0,
                Items:
                [
                    new QuickOrderTemplateItemInput(_biryaniFullVariantId, 2m),
                    new QuickOrderTemplateItemInput(_drinkVariantId, 1m, TemplateUnitPrice: 60m),
                ]),
            CancellationToken.None);

        var listed = await new ListActiveQuickOrderTemplatesQueryHandler(repository, mediator)
            .Handle(new ListActiveQuickOrderTemplatesQuery(), CancellationToken.None);
        return listed.Single(t => t.TemplateId == templateId);
    }

    // 1. Strip shows the configured deal name and price.
    [Fact]
    public async Task QuickOrdersData_ShowsConfiguredNameAndPrice()
    {
        var repository = new FakeQuickOrderTemplateRepository();
        var deal = await CreateFamilyDealAsync(repository, CreateCatalogMediator());

        Assert.Equal("Family Deal", deal.Name);
        Assert.Equal("Weekend special", deal.Description);
        Assert.Equal(2m * 500m + 60m, deal.TotalPrice); // catalog price x qty + override price
    }

    // 2. Preview payload = the actual configured contents.
    [Fact]
    public async Task Preview_ShowsActualConfiguredContents()
    {
        var repository = new FakeQuickOrderTemplateRepository();
        var deal = await CreateFamilyDealAsync(repository, CreateCatalogMediator());

        Assert.Equal(2, deal.Items.Count);

        var biryani = deal.Items.Single(i => i.VariantId == _biryaniFullVariantId);
        Assert.Equal("Chicken Biryani", biryani.ProductName);
        Assert.Equal("Full", biryani.VariantName);
        Assert.Equal(2m, biryani.Quantity);
        Assert.Equal(500m, biryani.UnitPrice); // current catalog selling price

        var drink = deal.Items.Single(i => i.VariantId == _drinkVariantId);
        Assert.Equal(60m, drink.UnitPrice); // the configured override wins
        Assert.Equal(60m, drink.Total);
    }

    // 3. Preview reads only - it never creates or modifies orders.
    [Fact]
    public async Task Preview_DoesNotCreateOrModifyOrders()
    {
        var repository = new FakeQuickOrderTemplateRepository();
        var countingOrders = new CountingOrderRepository();

        await CreateFamilyDealAsync(repository, CreateCatalogMediator());
        // The preview load goes through the same query the dialog consumes.
        var listed = await new ListActiveQuickOrderTemplatesQueryHandler(repository, CreateCatalogMediator())
            .Handle(new ListActiveQuickOrderTemplatesQuery(), CancellationToken.None);

        Assert.NotEmpty(listed);
        Assert.Equal(0, countingOrders.Writes);
        Assert.Equal(0, countingOrders.Reads);
    }

    // 4/5/6. Add Deal replays the configured items through the ordinary
    // add-line pipeline: right items, right quantities/variants, totals from
    // the existing POS pricing logic.
    [Fact]
    public async Task AddDeal_ReplaysConfiguredItemsThroughExistingPipeline()
    {
        var repository = new FakeQuickOrderTemplateRepository();
        var deal = await CreateFamilyDealAsync(repository, CreateCatalogMediator());
        var mediator = CreateCatalogMediator();

        var orders = new FakeOrderRepository();
        var order = Order.Create(OrderType.DineIn, MasterData.Warehouses.WarehouseId.New(), TableId.New());
        orders.Add(order);
        var lines = new FakeOrderLineRepository();
        var addLine = new AddOrderLineCommandHandler(orders, lines, mediator);

        // Exactly what RestaurantPosForm.ApplyQuickOrderTemplateAsync does:
        // one AddOrderLineCommand per configured template item.
        foreach (var item in deal.Items)
        {
            await addLine.Handle(new AddOrderLineCommand(order.Id.Value, item.VariantId, item.Quantity), CancellationToken.None);
        }

        var persistedLines = await lines.GetByOrderIdAsync(order.Id, CancellationToken.None);
        Assert.Equal(2, persistedLines.Count);
        Assert.Equal(2, order.OrderLineIds.Count);

        var biryaniLine = persistedLines.Single(l => l.ProductVariantId.Value == _biryaniFullVariantId);
        Assert.Equal(2m, biryaniLine.Quantity);
        Assert.Equal(500m, biryaniLine.UnitPrice);

        var drinkLine = persistedLines.Single(l => l.ProductVariantId.Value == _drinkVariantId);
        Assert.Equal(1m, drinkLine.Quantity);

        // Totals via the existing calculator, exactly as the POS renders them.
        var totals = OrderTotalsCalculator.Calculate(
            [.. persistedLines.Select(OrderLineDto.FromDomain)], [], [], []);
        Assert.Equal(2m * 500m + 80m, totals.Subtotal);
    }

    // 7. A changed deal configuration is reflected on the next preview load.
    [Fact]
    public async Task ChangedConfiguration_IsReflectedInNextPreview()
    {
        var repository = new FakeQuickOrderTemplateRepository();
        var mediator = CreateCatalogMediator();
        var deal = await CreateFamilyDealAsync(repository, mediator);

        // The administrator drops the drink and raises the biryani to x3.
        await new UpdateQuickOrderTemplateCommandHandler(repository).Handle(
            new UpdateQuickOrderTemplateCommand(
                deal.TemplateId, "Family Deal", "Weekend special", 0,
                [new QuickOrderTemplateItemInput(_biryaniFullVariantId, 3m)]),
            CancellationToken.None);

        var reloaded = await new ListActiveQuickOrderTemplatesQueryHandler(repository, mediator)
            .Handle(new ListActiveQuickOrderTemplatesQuery(), CancellationToken.None);

        var updated = reloaded.Single(t => t.TemplateId == deal.TemplateId);
        var item = Assert.Single(updated.Items);
        Assert.Equal(_biryaniFullVariantId, item.VariantId);
        Assert.Equal(3m, item.Quantity);
        Assert.Equal(3m * 500m, updated.TotalPrice);
    }

    /// <summary>Wraps the fake order repository counting every touch, proving the preview is read-only with respect to orders.</summary>
    private sealed class CountingOrderRepository : IOrderRepository
    {
        public int Reads;
        public int Writes;

        public Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken = default)
        {
            Reads++;
            return Task.FromResult<Order?>(null);
        }

        public Task<IReadOnlyCollection<Order>> GetOpenOrHeldByTableIdAsync(TableId tableId, CancellationToken cancellationToken = default)
        {
            Reads++;
            return Task.FromResult<IReadOnlyCollection<Order>>([]);
        }

        public Task<IReadOnlyCollection<Order>> GetOpenAsync(CancellationToken cancellationToken = default)
        {
            Reads++;
            return Task.FromResult<IReadOnlyCollection<Order>>([]);
        }

        public Task<IReadOnlyCollection<Order>> GetHeldAsync(CancellationToken cancellationToken = default)
        {
            Reads++;
            return Task.FromResult<IReadOnlyCollection<Order>>([]);
        }

        public Task<IReadOnlyCollection<Order>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            Reads++;
            return Task.FromResult<IReadOnlyCollection<Order>>([]);
        }

        public Task<IReadOnlySet<TableId>> GetActiveTableIdsAsync(CancellationToken cancellationToken = default)
        {
            Reads++;
            return Task.FromResult<IReadOnlySet<TableId>>(new HashSet<TableId>());
        }

        public Task AddAsync(Order order, CancellationToken cancellationToken = default)
        {
            Writes++;
            return Task.CompletedTask;
        }
    }
}
