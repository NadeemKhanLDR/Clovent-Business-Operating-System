using System.Reflection;
using Clovent.Authentication.Application.DependencyInjection;
using Clovent.Authentication.Infrastructure.DependencyInjection;
using Clovent.Authentication.Infrastructure.Persistence;
using Clovent.Desktop.DependencyInjection;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Infrastructure.Persistence;
using Clovent.Restaurant.Application.KitchenTickets.Queries;
using Clovent.Restaurant.Application.Orders.Queries;
using Clovent.Restaurant.KitchenTickets;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Clovent.Platform.Bootstrap;
using IHost = Microsoft.Extensions.Hosting.IHost;

namespace Clovent.Desktop.Tests;

/// <summary>
/// TEMPORARY runtime-verification harness (deleted after the verification
/// run): composes the exact production DI graph against the developer
/// machine's real databases and drives the screens' real load paths,
/// including the Kitchen Tickets orphaned-order scenario.
/// </summary>
public sealed class RuntimeVerificationTests
{
    private static readonly Lazy<IHost> Host = new(() =>
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
        global::Clovent.Desktop.Modules.DesktopModuleLoader.LoadModules(bootstrapper.Services, bootstrapper.Configuration, global::Clovent.Desktop.Modules.DesktopModuleCatalog.ModuleTypes);

        return bootstrapper.BuildAndInitializeAsync().GetAwaiter().GetResult();
    });

    private static async Task<(IMediator Mediator, ICurrentSession Session)> ConnectAsAdminAsync()
    {
        var host = Host.Value;
        var session = host.Services.GetRequiredService<ICurrentSession>();
        using (var scope = host.Services.CreateScope())
        {
            var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var admin = identityDb.Users.Local.Count != 0
                ? identityDb.Users.Local.Single()
                : identityDb.Users.Single(u => u.Id == new Clovent.Identity.Users.UserId(Guid.Parse("BFE39055-F8A8-491A-90F8-676C667BE2EF")));
            session.SignIn(admin.Id.Value, Guid.NewGuid(), admin.UserName.Value);
        }

        var mediator = host.Services.GetRequiredService<IMediator>();
        return (mediator, session);
    }

    [Fact]
    public async Task KitchenTickets_RealDatabase_OrphanedTicketShowsUnavailableNotCrash()
    {
        var (mediator, _) = await ConnectAsAdminAsync();

        var tickets = await mediator.Send(new ListActiveKitchenTicketsQuery());
        Assert.NotEmpty(tickets);

        // The exact row that crashed the screen before the fix.
        var orphan = tickets.Single(t => t.OrderId == Guid.Parse("AE963CD6-9DEC-491B-979D-A58499B10D14"));
        Assert.Equal("New", orphan.Status);

        var rows = await KitchenTicketViewerView.BuildRowsAsync(mediator, tickets, CancellationToken.None);
        var orphanRow = rows.Single(r => r.KitchenTicketId == orphan.KitchenTicketId);
        Assert.Equal("(order unavailable)", orphanRow.OrderNumber);

        // Every other ticket still resolves its real order number.
        foreach (var row in rows.Where(r => r.KitchenTicketId != orphan.KitchenTicketId))
        {
            Assert.NotEqual("(order unavailable)", row.OrderNumber);
        }

        // Refresh path re-runs the same load without throwing.
        var again = await mediator.Send(new ListActiveKitchenTicketsQuery());
        Assert.Equal(tickets.Count, again.Count);
    }

    [Fact]
    public async Task MenuItems_RealDatabase_LoadsRowsAndPrices()
    {
        var (mediator, _) = await ConnectAsAdminAsync();
        var variants = await mediator.Send(new Clovent.Catalog.Application.Variants.Queries.ListProductVariantsQuery());
        Assert.NotEmpty(variants);
        var prices = await mediator.Send(new Clovent.Catalog.Application.Prices.Queries.ListActiveProductPricesByTypeQuery(Clovent.Catalog.Prices.PriceType.Selling));
        Assert.NotEmpty(prices);
        var categories = await mediator.Send(new Clovent.Catalog.Application.Categories.Queries.ListProductCategoriesQuery());
        Assert.NotEmpty(categories);
    }

    [Fact]
    public void DevExpressIcons_EveryUriUsed_ResolvesToAnActualImage()
    {
        var uris = new[]
        {
            Clovent.Desktop.Forms.Base.DesktopIcons.Add,
            Clovent.Desktop.Forms.Base.DesktopIcons.Edit,
            Clovent.Desktop.Forms.Base.DesktopIcons.ActivateIcon,
            Clovent.Desktop.Forms.Base.DesktopIcons.CancelIcon,
            Clovent.Desktop.Forms.Base.DesktopIcons.Refresh,
            Clovent.Desktop.Forms.Base.DesktopIcons.Save,
            Clovent.Desktop.Forms.Base.DesktopIcons.Search,
            Clovent.Desktop.Forms.Base.DesktopIcons.Up,
            Clovent.Desktop.Forms.Base.DesktopIcons.Down,
        };

        var cache = DevExpress.Images.ImageResourceCache.Default;
        foreach (var uri in uris)
        {
            var svg = cache.GetSvgImage(uri);
            Assert.True(svg is not null, $"Icon URI '{uri}' did not resolve to an SVG image.");
        }
    }

    [Fact]
    public async Task RestaurantPos_LoadQueries_SucceedsWithoutException()
    {
        var (mediator, session) = await ConnectAsAdminAsync();
        
        await Clovent.Desktop.Forms.Base.CurrencyDisplayLoader.ConfigureAsync(mediator);
        var warehouses = await mediator.Send(new Clovent.MasterData.Application.Warehouses.Queries.ListAllWarehousesQuery());
        Assert.NotNull(warehouses);

        var variants = await mediator.Send(new Clovent.Catalog.Application.Variants.Queries.ListProductVariantsQuery());
        Assert.NotNull(variants);

        var products = await mediator.Send(new Clovent.Catalog.Application.Products.Queries.ListProductsQuery());
        Assert.NotNull(products);

        var prices = await mediator.Send(new Clovent.Catalog.Application.Prices.Queries.ListActiveProductPricesByTypeQuery(Clovent.Catalog.Prices.PriceType.Selling));
        Assert.NotNull(prices);

        var categories = await mediator.Send(new Clovent.Catalog.Application.Categories.Queries.ListProductCategoriesQuery());
        Assert.NotNull(categories);

        var tables = await mediator.Send(new Clovent.Restaurant.Application.Tables.Queries.ListAllTablesQuery());
        Assert.NotNull(tables);

        var customers = await mediator.Send(new Clovent.Restaurant.Application.Customers.Queries.ListCustomersQuery());
        Assert.NotNull(customers);

        var openOrders = await mediator.Send(new Clovent.Restaurant.Application.Orders.Queries.ListOpenOrdersQuery());
        Assert.NotNull(openOrders);

        var heldOrders = await mediator.Send(new Clovent.Restaurant.Application.Orders.Queries.ListHeldOrdersQuery());
        Assert.NotNull(heldOrders);
    }

    [Fact]
    public async Task RestaurantPos_DineIn_RecordPayment_ExactCash_RealDatabase_CompletesSuccessfully()
    {
        var (mediator, session) = await ConnectAsAdminAsync();

        // 1. Get warehouse
        var warehouses = await mediator.Send(new Clovent.MasterData.Application.Warehouses.Queries.ListAllWarehousesQuery());
        var warehouseId = warehouses.First().WarehouseId;

        // 2. Get Table T-01
        var tables = await mediator.Send(new Clovent.Restaurant.Application.Tables.Queries.ListAllTablesQuery());
        var tableT01 = tables.FirstOrDefault(t => t.Name == "T-01") ?? tables.First();

        // 3. Ensure Table T-01 is available or reset if needed
        var existingOrders = await mediator.Send(new Clovent.Restaurant.Application.Orders.Queries.ListOpenOrdersQuery());
        foreach (var order in existingOrders.Where(o => o.TableId == tableT01.TableId))
        {
            await mediator.Send(new Clovent.Restaurant.Application.Orders.Commands.CancelOrderCommand(order.OrderId, "Reset for QA"));
        }

        // 4. Create Dine-In order for Table T-01
        var createdOrder = await mediator.Send(new Clovent.Restaurant.Application.Orders.Commands.CreateOrderCommand(
            Clovent.Restaurant.Orders.OrderType.DineIn, warehouseId, tableT01.TableId));
        Assert.NotNull(createdOrder);
        Assert.Equal("DineIn", createdOrder.OrderType);
        Assert.Equal(tableT01.TableId, createdOrder.TableId);

        // 5. Find Aloo Gobi - Full (380.00) and Half (250.00)
        var variants = await mediator.Send(new Clovent.Catalog.Application.Variants.Queries.ListProductVariantsQuery());
        var fullVariant = variants.FirstOrDefault(v => v.Name.Contains("Aloo Gobi") && v.Name.Contains("Full")) ?? variants.ElementAt(0);
        var halfVariant = variants.FirstOrDefault(v => v.Name.Contains("Aloo Gobi") && v.Name.Contains("Half")) ?? variants.ElementAt(1);

        await mediator.Send(new Clovent.Restaurant.Application.OrderLines.Commands.AddOrderLineCommand(createdOrder.OrderId, fullVariant.ProductVariantId, 1m));
        await mediator.Send(new Clovent.Restaurant.Application.OrderLines.Commands.AddOrderLineCommand(createdOrder.OrderId, halfVariant.ProductVariantId, 1m));

        var summary = await mediator.Send(new Clovent.Restaurant.Application.Orders.Queries.GetOrderSummaryQuery(createdOrder.OrderId));
        Assert.True(summary.GrandTotal > 0);

        // 6. Ensure active shift for the cashier (same query EnsureShiftActiveOrPromptAsync uses)
        var activeShift = await mediator.Send(new Clovent.Restaurant.Application.Shifts.Queries.GetActiveShiftQuery(CashierId: session.UserId!.Value));
        Guid shiftId;
        if (activeShift == null)
        {
            var openShiftResult = await mediator.Send(new Clovent.Restaurant.Application.Shifts.Commands.OpenShiftCommand(
                BranchId: Guid.Parse("00000000-0000-0000-0000-000000000001"),
                WarehouseId: warehouseId,
                TerminalId: Guid.Parse("00000000-0000-0000-0000-000000000001"),
                CashierId: session.UserId.Value,
                CashierName: session.DisplayName ?? "Administrator",
                StartingCash: 500.00m,
                Notes: "QA Shift"));
            shiftId = openShiftResult.ShiftId;
        }
        else
        {
            shiftId = activeShift.ShiftId;
        }

        // 7. Get Cash payment method
        var paymentMethods = await mediator.Send(new Clovent.Restaurant.Application.PaymentMethods.Queries.ListPaymentMethodsQuery());
        var cashMethod = paymentMethods.First(m => m.Name.Equals("Cash", StringComparison.OrdinalIgnoreCase));

        // 8. Record Payment (Exact amount with ShiftId)
        var paymentResult = await mediator.Send(new Clovent.Restaurant.Application.Payments.Commands.RecordPaymentCommand(
            createdOrder.OrderId, cashMethod.PaymentMethodId, summary.GrandTotal, false, shiftId));
        Assert.NotNull(paymentResult);
        Assert.Equal(summary.GrandTotal, paymentResult.Amount);
        Assert.Equal(shiftId, paymentResult.ShiftId);

        // 9. Verify order balance is 0 and complete order
        var postPaySummary = await mediator.Send(new Clovent.Restaurant.Application.Orders.Queries.GetOrderSummaryQuery(createdOrder.OrderId));
        Assert.Equal(0m, postPaySummary.Balance);

        var completedOrder = await mediator.Send(new Clovent.Restaurant.Application.Orders.Commands.CompleteOrderCommand(createdOrder.OrderId));
        Assert.Equal("Completed", completedOrder.Status);

        // 10. Verify DB persistence directly in Clovent_Restaurant
        var host = Host.Value;
        using var scope = host.Services.CreateScope();
        var restDb = scope.ServiceProvider.GetRequiredService<Clovent.Restaurant.Infrastructure.Persistence.RestaurantDbContext>();
        var dbOrder = restDb.Orders.FirstOrDefault(o => o.Id == new Clovent.Restaurant.Orders.OrderId(createdOrder.OrderId));
        Assert.NotNull(dbOrder);
        Assert.Equal(Clovent.Restaurant.Orders.OrderStatus.Completed, dbOrder.Status);

        var dbPayment = restDb.Payments.FirstOrDefault(p => p.Id == new Clovent.Restaurant.Payments.PaymentId(paymentResult.PaymentId));
        Assert.NotNull(dbPayment);
        Assert.Equal(shiftId, dbPayment.ShiftId?.Value);
    }
}