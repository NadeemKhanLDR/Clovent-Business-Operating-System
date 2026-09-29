using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Authentication.Application.DependencyInjection;
using Clovent.Authentication.Infrastructure.DependencyInjection;
using Clovent.Desktop.DependencyInjection;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Seed;
using Clovent.Desktop.Theming;
using Clovent.MasterData.Warehouses;
using Clovent.Platform.Bootstrap;
using Clovent.Restaurant.Application.EndOfDay.Queries;
using Clovent.Restaurant.Orders;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.EndOfDay;

public sealed class DevelopmentRestaurantReportingSeedLiveSqlServerExecutionTests
{
    [Fact]
    public async Task LiveSqlServer_ExecuteReportingSeed_PopulatesRealDatabaseAndReconcilesSalesSummary()
    {
        var desktopPath = @"D:\Clovent Business Operating System\src\Clovent.Desktop";

        var bootstrapper = ApplicationBootstrapper
            .Create(basePath: desktopPath)
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

        using var host = await bootstrapper.BuildAndInitializeAsync();
        using var scope = host.Services.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Configure DateTimeDisplay from business settings
        await DateTimeDisplayLoader.ConfigureAsync(mediator, host.Services);

        // 1. Assert database persistence directly via order repository
        var orderRepo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var allOrders = await orderRepo.GetAllAsync();
        var rptOrders = allOrders.Where(o => o.OrderNumber.Value.StartsWith("ORD-RPT-")).ToList();

        Assert.True(rptOrders.Count >= 12, $"Expected at least 12 ORD-RPT orders, but found {rptOrders.Count}");

        // 2. Query Sales Summary covering seeded orders
        var warehouses = await scope.ServiceProvider.GetRequiredService<IWarehouseRepository>().GetAllAsync();
        var warehouse = warehouses.First();
        var today = BusinessDateTimeService.Instance.Today;
        var fromDate = DateOnly.FromDateTime(rptOrders.Min(o => o.CreatedAtUtc).LocalDateTime);

        var summaryToday = await mediator.Send(new GetExpandedSalesSummaryQuery(warehouse.Id.Value, fromDate, today));
        Assert.NotNull(summaryToday);
        Assert.True(summaryToday.Kpis.TotalOrders > 0);

        // 3. Verify Item Classifications
        var prepClass = summaryToday.ItemTypes.FirstOrDefault(c => c.ItemType == "Prepared");
        var resaleClass = summaryToday.ItemTypes.FirstOrDefault(c => c.ItemType == "PurchasedResale");
        var serviceClass = summaryToday.ItemTypes.FirstOrDefault(c => c.ItemType == "Service");

        Assert.NotNull(prepClass);
        Assert.True(prepClass.QuantitySold > 0, "Prepared quantity must be > 0");
        Assert.True(prepClass.TotalSales > 0, "Prepared sales must be > 0");
        Assert.Equal("N/A", prepClass.CostDisplay);

        Assert.NotNull(resaleClass);
        Assert.True(resaleClass.QuantitySold > 0, "PurchasedResale quantity must be > 0");
        Assert.True(resaleClass.TotalSales > 0, "PurchasedResale sales must be > 0");
        Assert.NotNull(resaleClass.TotalCost);
        Assert.True(resaleClass.TotalCost > 0);
        Assert.NotNull(resaleClass.GrossProfit);
        Assert.True(resaleClass.GrossProfit > 0);
        Assert.Equal(20m, resaleClass.MarginPercent);

        Assert.NotNull(serviceClass);
        Assert.True(serviceClass.QuantitySold > 0, "Service quantity must be > 0");
        Assert.True(serviceClass.TotalSales > 0, "Service sales must be > 0");
        Assert.Equal(0m, serviceClass.TotalCost);
        Assert.Equal(serviceClass.TotalSales, serviceClass.GrossProfit);
        Assert.Equal(100m, serviceClass.MarginPercent);

        // 4. Verify Items Tab rows
        var items = summaryToday.Items;
        var naanItem = items.FirstOrDefault(i => i.ItemName.Contains("Naan"));
        var heatingItem = items.FirstOrDefault(i => i.ItemName.Contains("Heating"));
        var biryaniItem = items.FirstOrDefault(i => i.ItemName.Contains("Biryani"));

        Assert.NotNull(naanItem);
        Assert.Equal(20m, naanItem.MarginPercent);

        Assert.NotNull(heatingItem);
        Assert.Equal(100m, heatingItem.MarginPercent);

        Assert.NotNull(biryaniItem);
    }
}
