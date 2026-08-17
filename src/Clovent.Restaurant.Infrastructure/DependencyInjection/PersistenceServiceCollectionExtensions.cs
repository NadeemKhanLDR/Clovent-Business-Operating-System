using Clovent.Platform.Bootstrap;
using Clovent.Restaurant.ActivityLogs;
using Clovent.Restaurant.Application;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.Discounts;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Infrastructure.Repositories;
using Clovent.Restaurant.KitchenTickets;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Sales;
using Clovent.Restaurant.ServiceCharges;
using Clovent.Restaurant.Tables;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Clovent.Restaurant.Infrastructure.DependencyInjection;

/// <summary>Restaurant's own Persistence-layer registration, following the same AddApplication()/AddInfrastructure()/AddPersistence() convention as every other module.</summary>
public static class PersistenceServiceCollectionExtensions
{
    /// <summary>The <c>ConnectionStrings</c> configuration key this module reads its SQL Server connection string from.</summary>
    public const string ConnectionStringName = "Restaurant";

    /// <summary>
    /// Registers <see cref="RestaurantDbContext"/>, every repository
    /// implementation (DiningArea/Table/Order/OrderLine/KitchenTicket/Payment/PaymentMethod/Discount/ServiceCharge/DailySalesSequence),
    /// the <see cref="IUnitOfWork"/> seam, and an <see cref="IPersistenceInitializer"/>
    /// that applies migrations.
    /// </summary>
    /// <exception cref="InvalidOperationException">No <c>ConnectionStrings:Restaurant</c> value is configured.</exception>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Missing required connection string 'ConnectionStrings:{ConnectionStringName}'.");

        services.AddDbContext<RestaurantDbContext>(options => options.UseSqlServer(connectionString));

        services.TryAddScoped<IDiningAreaRepository, DiningAreaRepository>();
        services.TryAddScoped<ITableRepository, TableRepository>();
        services.TryAddScoped<IOrderRepository, OrderRepository>();
        services.TryAddScoped<IOrderLineRepository, OrderLineRepository>();
        services.TryAddScoped<IKitchenTicketRepository, KitchenTicketRepository>();
        services.TryAddScoped<IPaymentRepository, PaymentRepository>();
        services.TryAddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
        services.TryAddScoped<IDiscountRepository, DiscountRepository>();
        services.TryAddScoped<IServiceChargeRepository, ServiceChargeRepository>();
        services.TryAddScoped<IDailySalesSequenceRepository, DailySalesSequenceRepository>();
        services.TryAddScoped<IOrderNumberSequenceRepository, OrderNumberSequenceRepository>();
        services.TryAddScoped<IActivityLogEntryRepository, ActivityLogEntryRepository>();
        services.TryAddScoped<ICustomerRepository, CustomerRepository>();
        services.TryAddScoped<ICustomerLedgerEntryRepository, CustomerLedgerEntryRepository>();

        services.TryAddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IPersistenceInitializer, RestaurantPersistenceInitializer>();

        return services;
    }
}
