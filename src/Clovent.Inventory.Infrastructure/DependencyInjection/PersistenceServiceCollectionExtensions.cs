using Clovent.Inventory.Adjustments;
using Clovent.Inventory.Application;
using Clovent.Inventory.Infrastructure.Persistence;
using Clovent.Inventory.Infrastructure.Repositories;
using Clovent.Inventory.Transactions;
using Clovent.Inventory.Transfers;
using Clovent.Inventory.WarehouseStocks;
using Clovent.Platform.Bootstrap;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Clovent.Inventory.Infrastructure.DependencyInjection;

/// <summary>Inventory's own Persistence-layer registration, following the same AddApplication()/AddInfrastructure()/AddPersistence() convention as every other module.</summary>
public static class PersistenceServiceCollectionExtensions
{
    /// <summary>The <c>ConnectionStrings</c> configuration key this module reads its SQL Server connection string from.</summary>
    public const string ConnectionStringName = "Inventory";

    /// <summary>
    /// Registers <see cref="InventoryDbContext"/>, every repository
    /// implementation (WarehouseStock/InventoryTransaction/StockAdjustment/StockTransfer),
    /// the <see cref="IUnitOfWork"/> seam, and an <see cref="IPersistenceInitializer"/>
    /// that applies migrations.
    /// </summary>
    /// <exception cref="InvalidOperationException">No <c>ConnectionStrings:Inventory</c> value is configured.</exception>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Missing required connection string 'ConnectionStrings:{ConnectionStringName}'.");

        services.AddDbContext<InventoryDbContext>(options => options.UseSqlServer(connectionString));

        services.TryAddScoped<IWarehouseStockRepository, WarehouseStockRepository>();
        services.TryAddScoped<IInventoryTransactionRepository, InventoryTransactionRepository>();
        services.TryAddScoped<IStockAdjustmentRepository, StockAdjustmentRepository>();
        services.TryAddScoped<IStockTransferRepository, StockTransferRepository>();

        services.TryAddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IPersistenceInitializer, InventoryPersistenceInitializer>();

        return services;
    }
}
