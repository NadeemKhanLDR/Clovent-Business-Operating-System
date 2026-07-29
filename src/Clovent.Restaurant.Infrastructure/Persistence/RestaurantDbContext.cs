using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.Discounts;
using Clovent.Restaurant.KitchenTickets;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Sales;
using Clovent.Restaurant.ServiceCharges;
using Clovent.Restaurant.Tables;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Restaurant.Infrastructure.Persistence;

/// <summary>
/// EF Core persistence context for the Restaurant POS bounded context
/// (Milestone 15, "Restaurant POS Core"). Tables live under the
/// <c>Restaurant</c> schema, mirroring how <c>CatalogDbContext</c> uses the
/// <c>Catalog</c> schema.
/// </summary>
public sealed class RestaurantDbContext(DbContextOptions<RestaurantDbContext> options) : DbContext(options)
{
    /// <summary>DiningArea aggregates.</summary>
    public DbSet<DiningArea> DiningAreas => Set<DiningArea>();

    /// <summary>Table aggregates.</summary>
    public DbSet<Table> Tables => Set<Table>();

    /// <summary>Order aggregates.</summary>
    public DbSet<Order> Orders => Set<Order>();

    /// <summary>OrderLine aggregates.</summary>
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    /// <summary>KitchenTicket aggregates.</summary>
    public DbSet<KitchenTicket> KitchenTickets => Set<KitchenTicket>();

    /// <summary>Payment aggregates.</summary>
    public DbSet<Payment> Payments => Set<Payment>();

    /// <summary>PaymentMethod aggregates.</summary>
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();

    /// <summary>Discount aggregates.</summary>
    public DbSet<Discount> Discounts => Set<Discount>();

    /// <summary>ServiceCharge aggregates.</summary>
    public DbSet<ServiceCharge> ServiceCharges => Set<ServiceCharge>();

    /// <summary>DailySalesSequence aggregates.</summary>
    public DbSet<DailySalesSequence> DailySalesSequences => Set<DailySalesSequence>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RestaurantDbContext).Assembly);
    }
}
