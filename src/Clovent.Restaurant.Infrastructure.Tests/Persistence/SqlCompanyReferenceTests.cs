using Clovent.Restaurant.Customers;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Infrastructure.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Persistence;

public sealed partial class SqlCompanyOwnershipTests
{
    [Theory]
    [InlineData("foreign-order", 51009)]
    [InlineData("ambiguous-order", 51009)]
    [InlineData("foreign-shift", 51010)]
    [InlineData("foreign-allocation", 51011)]
    [InlineData("valid", 0)]
    public async Task MappingValidatesWholeReferenceGraph_WithoutChangingHistory(string scenario, int error)
    {
        var database = $"CBOS_Recovery_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<RestaurantDbContext>().UseSqlServer(SqlTestDatabaseGuard.ConnectionString(database)).Options;
        await using var db = new RestaurantDbContext(options);
        try
        {
            await db.Database.EnsureCreatedAsync();
            await SqlTestDatabaseGuard.CreatedAsync(db);
            var companyA = Guid.NewGuid(); var companyB = Guid.NewGuid();
            var branchA = Guid.NewGuid(); var branchB = Guid.NewGuid();
            var warehouseA = Guid.NewGuid(); var warehouseB = Guid.NewGuid();
            var a = Customer("MAP-A", null, 100m);
            var b = Customer("MAP-B", null, 200m);
            var order = Clovent.Restaurant.Orders.Order.Create(Clovent.Restaurant.Orders.OrderType.TakeAway,
                new Clovent.MasterData.Warehouses.WarehouseId(scenario is "foreign-order" or "ambiguous-order" or "foreign-allocation" ? warehouseB : warehouseA));
            order.SetCustomer(scenario == "foreign-allocation" ? b.Id : a.Id);
            db.AddRange(a, b, order);
            if (scenario == "ambiguous-order")
            {
                var second = Clovent.Restaurant.Orders.Order.Create(Clovent.Restaurant.Orders.OrderType.TakeAway,
                    new Clovent.MasterData.Warehouses.WarehouseId(warehouseA));
                second.SetCustomer(a.Id); db.Add(second);
            }
            var shift = Clovent.Restaurant.Shifts.Shift.Open(1,
                new Clovent.Identity.Branches.BranchId(scenario == "foreign-shift" ? branchB : branchA),
                new Clovent.MasterData.Warehouses.WarehouseId(warehouseA), Clovent.MasterData.Terminals.TerminalId.New(),
                Clovent.Identity.Users.UserId.New(), "Synthetic cashier", 0m);
            db.Add(shift);
            var entry = CustomerLedgerEntry.Create(a.Id, "MAP", "Immutable history", 0m, 10m, 90m, shift.Id);
            db.Add(entry);
            var audit = Clovent.Restaurant.ActivityLogs.ActivityLogEntry.Record("Synthetic", "Preserved audit", "Synthetic reviewer", "HARNESS");
            db.Add(audit);
            var allocation = CustomerPaymentAllocation.Create(a.Id, order.Id, 10m, entry.Id);
            db.Add(allocation);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            await db.Database.ExecuteSqlRawAsync("""
                DROP INDEX IX_Customers_CompanyId_Id ON Restaurant.Customers;
                DROP INDEX IX_CustomerLedgerEntries_CompanyId_CustomerId ON Restaurant.CustomerLedgerEntries;
                ALTER TABLE Restaurant.Customers DROP COLUMN CompanyId;
                ALTER TABLE Restaurant.CustomerLedgerEntries DROP COLUMN CompanyId;
                """);
            foreach (var command in db.GetService<IMigrationsSqlGenerator>().Generate(new MigrationProbe().Operations(), db.Model))
                await db.Database.ExecuteSqlRawAsync(command.CommandText);
            await db.Database.ExecuteSqlRawAsync("""
                CREATE SCHEMA [Identity];
                """);
            await db.Database.ExecuteSqlRawAsync("CREATE SCHEMA [MasterData];");
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE [Identity].Companies (Id uniqueidentifier PRIMARY KEY);
                CREATE TABLE [Identity].Branches (Id uniqueidentifier PRIMARY KEY, CompanyId uniqueidentifier NOT NULL);
                CREATE TABLE MasterData.Warehouses (Id uniqueidentifier PRIMARY KEY, BranchId uniqueidentifier NOT NULL);
                """);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT [Identity].Companies VALUES ({companyA}),({companyB});
                INSERT [Identity].Branches VALUES ({branchA},{companyA}),({branchB},{companyB});
                INSERT MasterData.Warehouses VALUES ({warehouseA},{branchA}),({warehouseB},{branchB});
                INSERT Restaurant.CustomerCompanyOwnershipMap VALUES
                    ({a.Id.Value},{companyA},{"Synthetic reviewer"},{DateTimeOffset.UtcNow},{"Approved A"}),
                    ({b.Id.Value},{companyB},{"Synthetic reviewer"},{DateTimeOffset.UtcNow},{"Approved B"});
                """);
            await db.Database.OpenConnectionAsync();
            await db.Database.ExecuteSqlInterpolatedAsync($"EXEC sys.sp_set_session_context @key=N'CBOS.Ownership.ExpectedDatabase', @value={database};");
            var script = await File.ReadAllTextAsync(Path.Combine(FindRepositoryRoot(), "docs", "audits", "2026-11-11", "company-ownership", "ApplyExplicitMapping.sql"));
            if (error != 0)
            {
                var failure = await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => db.Database.ExecuteSqlRawAsync(script));
                Assert.Equal(error, failure.Number);
                Assert.All(await db.Customers.ToListAsync(), c => Assert.Null(c.CompanyId));
            }
            else
            {
                await db.Database.ExecuteSqlRawAsync(script);
                Assert.Equal(companyA, (await db.Customers.SingleAsync(c => c.Id == a.Id)).CompanyId);
            }
            var preserved = await db.CustomerPaymentAllocations.SingleAsync();
            Assert.Equal(allocation.Id, preserved.Id);
            Assert.Equal(10m, preserved.Amount);
            Assert.Equal(entry.Id, preserved.CustomerLedgerEntryId);
            Assert.Equal(90m, (await db.CustomerLedgerEntries.SingleAsync()).RunningBalance);
            Assert.Equal(100m, (await db.Customers.SingleAsync(c => c.Id == a.Id)).OpeningBalance);
            var auditAfter = await db.ActivityLogEntries.SingleAsync();
            Assert.Equal(audit.Id, auditAfter.Id);
            Assert.Equal(audit.Details, auditAfter.Details);
            Assert.Equal(audit.OccurredAtUtc, auditAfter.OccurredAtUtc);
        }
        finally { await SqlTestDatabaseGuard.DeleteAndVerifyAsync(db); }
    }
}
