using Clovent.Restaurant.ActivityLogs;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.Discounts;
using Clovent.Restaurant.KitchenTickets;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.QuickOrderTemplates;
using Clovent.Restaurant.SmartRecommendations;
using Clovent.Restaurant.Sales;
using Clovent.Restaurant.ServiceCharges;
using Clovent.Restaurant.Shifts;
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

    /// <summary>The single OrderNumberSequence row.</summary>
    public DbSet<OrderNumberSequence> OrderNumberSequences => Set<OrderNumberSequence>();

    /// <summary>ActivityLogEntry aggregates.</summary>
    public DbSet<ActivityLogEntry> ActivityLogEntries => Set<ActivityLogEntry>();

    /// <summary>Customer aggregates.</summary>
    public DbSet<Customer> Customers => Set<Customer>();

    /// <summary>CustomerLedgerEntry aggregates.</summary>
    public DbSet<CustomerLedgerEntry> CustomerLedgerEntries => Set<CustomerLedgerEntry>();

    /// <summary>Shift aggregates.</summary>
    public DbSet<Shift> Shifts => Set<Shift>();

    /// <summary>CashMovement entities.</summary>
    public DbSet<CashMovement> CashMovements => Set<CashMovement>();

    /// <summary>RecommendationRule aggregates.</summary>
    public DbSet<RecommendationRule> RecommendationRules => Set<RecommendationRule>();

    /// <summary>SuggestionEvent analytics facts.</summary>
    public DbSet<SuggestionEvent> SuggestionEvents => Set<SuggestionEvent>();

    /// <summary>QuickOrderTemplate aggregates.</summary>
    public DbSet<QuickOrderTemplate> QuickOrderTemplates => Set<QuickOrderTemplate>();

    /// <summary>BusinessDayClose aggregates.</summary>
    public DbSet<Clovent.Restaurant.DayClose.BusinessDayClose> BusinessDayCloses => Set<Clovent.Restaurant.DayClose.BusinessDayClose>();

    /// <summary>EmployeeAttendanceSession aggregates.</summary>
    public DbSet<Clovent.Restaurant.Attendance.EmployeeAttendanceSession> AttendanceSessions => Set<Clovent.Restaurant.Attendance.EmployeeAttendanceSession>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RestaurantDbContext).Assembly);
    }
}
