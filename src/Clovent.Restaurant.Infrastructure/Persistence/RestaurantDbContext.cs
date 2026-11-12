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
using Clovent.Restaurant.Outbox;
using Clovent.Restaurant.Tables;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Restaurant.Infrastructure.Persistence;

/// <summary>
/// EF Core persistence context for the Restaurant POS bounded context
/// (Milestone 15, "Restaurant POS Core"). Tables live under the
/// <c>Restaurant</c> schema, mirroring how <c>CatalogDbContext</c> uses the
/// <c>Catalog</c> schema.
/// </summary>
public sealed class RestaurantDbContext(DbContextOptions<RestaurantDbContext> options,
    Clovent.Platform.Execution.IExecutionContextAccessor? companyScope = null) : DbContext(options)
{
    // The application DI supplies the accessor. Explicit contexts without it are reserved
    // for migration tooling and disposable test harnesses, not interactive data access.
    private bool ScopeEnforced => companyScope is not null;
    private Guid? ScopedCompanyId => companyScope?.Current?.CompanyId;

    private void EnforceCompanyWrites()
    {
        if (!ScopeEnforced) return;
        var customerChanges = ChangeTracker.Entries<Customer>().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray();
        var ledgerChanges = ChangeTracker.Entries<CustomerLedgerEntry>().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray();
        var orderChanges = ChangeTracker.Entries<Order>().Where(e => e.State is EntityState.Added or EntityState.Modified)
            .Where(e => e.Entity.CustomerId.HasValue).ToArray();
        var allocationChanges = ChangeTracker.Entries<CustomerPaymentAllocation>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray();
        if (customerChanges.Length + ledgerChanges.Length + orderChanges.Length + allocationChanges.Length == 0) return;
        var company = ScopedCompanyId;
        if (!company.HasValue || company.Value == Guid.Empty)
            throw new Clovent.Platform.Execution.ScopeUnresolvedException("Company scope is required for Restaurant persistence.");
        foreach (var entry in customerChanges)
        {
            if (entry.State == EntityState.Deleted) throw new InvalidOperationException("Customer history cannot be physically deleted.");
            if (entry.State == EntityState.Added && entry.Entity.CompanyId is null) entry.Entity.AssignCompany(company.Value);
            if (entry.Entity.CompanyId != company || (entry.State == EntityState.Modified && entry.Property(c => c.CompanyId).IsModified))
                throw new Clovent.Platform.Execution.ScopeMismatchException("Customer ownership does not match the active company.");
        }
        foreach (var entry in ledgerChanges)
        {
            if (entry.State == EntityState.Deleted) throw new InvalidOperationException("Ledger history cannot be physically deleted.");
            var customer = Customers.Local.FirstOrDefault(c => c.Id == entry.Entity.CustomerId)
                ?? Customers.SingleOrDefault(c => c.Id == entry.Entity.CustomerId);
            if (customer?.CompanyId != company)
                throw new Clovent.Platform.Execution.ScopeMismatchException("Ledger customer is not owned by the active company.");
            if (entry.State == EntityState.Added && entry.Entity.CompanyId is null) entry.Entity.AssignCompany(company.Value);
            if (entry.Entity.CompanyId != company || (entry.State == EntityState.Modified && entry.Property(e => e.CompanyId).IsModified))
                throw new Clovent.Platform.Execution.ScopeMismatchException("Ledger ownership does not match the active company.");
        }
        foreach (var order in orderChanges)
        {
            if (order.Entity.CustomerId is not { } customerId) continue;
            var customer = Customers.Local.FirstOrDefault(c => c.Id == customerId)
                ?? Customers.SingleOrDefault(c => c.Id == customerId);
            if (customer?.CompanyId != company)
                throw new Clovent.Platform.Execution.ScopeMismatchException("Order customer is not owned by the active company.");
        }
        foreach (var allocation in allocationChanges)
        {
            var customer = Customers.Local.FirstOrDefault(c => c.Id == allocation.Entity.CustomerId)
                ?? Customers.SingleOrDefault(c => c.Id == allocation.Entity.CustomerId);
            if (customer?.CompanyId != company)
                throw new Clovent.Platform.Execution.ScopeMismatchException("Allocation customer is not owned by the active company.");
            var order = Orders.Local.FirstOrDefault(o => o.Id == allocation.Entity.OrderId)
                ?? Orders.SingleOrDefault(o => o.Id == allocation.Entity.OrderId);
            if (order?.CustomerId != allocation.Entity.CustomerId)
                throw new Clovent.Platform.Execution.ScopeMismatchException("Allocation order does not belong to its customer.");
            if (allocation.Entity.CustomerLedgerEntryId is { } ledgerId)
            {
                var ledger = CustomerLedgerEntries.Local.FirstOrDefault(e => e.Id == ledgerId)
                    ?? CustomerLedgerEntries.SingleOrDefault(e => e.Id == ledgerId);
                if (ledger?.CustomerId != allocation.Entity.CustomerId || ledger.CompanyId != company)
                    throw new Clovent.Platform.Execution.ScopeMismatchException("Allocation ledger does not belong to its customer and company.");
            }
        }
    }

    /// <inheritdoc/>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnforceCompanyWrites();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc/>
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        EnforceCompanyWrites();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
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

    /// <summary>CustomerPaymentAllocation aggregates.</summary>
    public DbSet<CustomerPaymentAllocation> CustomerPaymentAllocations => Set<CustomerPaymentAllocation>();

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

    /// <summary>Transactional OutboxMessage entities.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <summary>Compensating Refund aggregates.</summary>
    public DbSet<Clovent.Restaurant.Refunds.Refund> Refunds => Set<Clovent.Restaurant.Refunds.Refund>();

    /// <summary>Compensating RefundLine entities.</summary>
    public DbSet<Clovent.Restaurant.Refunds.RefundLine> RefundLines => Set<Clovent.Restaurant.Refunds.RefundLine>();

    /// <summary>Durable multi-terminal delta synchronization inbox records.</summary>
    public DbSet<Clovent.Platform.Sync.SyncInboxRecord> SyncInboxRecords => Set<Clovent.Platform.Sync.SyncInboxRecord>();

    /// <summary>Durable multi-terminal synchronization conflict records staged for manager review.</summary>
    public DbSet<Clovent.Platform.Sync.SyncConflictRecord> SyncConflicts => Set<Clovent.Platform.Sync.SyncConflictRecord>();

    /// <summary>Durable replicated terminal shift summaries across the branch.</summary>
    public DbSet<Clovent.Restaurant.Sync.TerminalShiftSyncSummary> TerminalShiftSyncSummaries => Set<Clovent.Restaurant.Sync.TerminalShiftSyncSummary>();

    /// <summary>Durable bidirectional QuickBooks transaction mapping ledger.</summary>
    public DbSet<Clovent.Restaurant.QuickBooks.QuickBooksSyncMap> QuickBooksSyncMaps => Set<Clovent.Restaurant.QuickBooks.QuickBooksSyncMap>();

    /// <summary>Durable cashier behavioral audit anomaly alerts and shrinkage flags.</summary>
    public DbSet<Clovent.Restaurant.AuditAlerts.CashierAuditAlert> CashierAuditAlerts => Set<Clovent.Restaurant.AuditAlerts.CashierAuditAlert>();


    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("Restaurant");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RestaurantDbContext).Assembly);
        modelBuilder.Entity<Customer>().HasQueryFilter(c =>
            !ScopeEnforced || (ScopedCompanyId != null && ScopedCompanyId != Guid.Empty && c.CompanyId == ScopedCompanyId));
        modelBuilder.Entity<CustomerLedgerEntry>().HasQueryFilter(e =>
            !ScopeEnforced || (ScopedCompanyId != null && ScopedCompanyId != Guid.Empty &&
                e.CompanyId == ScopedCompanyId && Customers.Any(c => c.Id == e.CustomerId)));
        modelBuilder.Entity<CustomerPaymentAllocation>().HasQueryFilter(e =>
            !ScopeEnforced || Customers.Any(c => c.Id == e.CustomerId));
    }
}
