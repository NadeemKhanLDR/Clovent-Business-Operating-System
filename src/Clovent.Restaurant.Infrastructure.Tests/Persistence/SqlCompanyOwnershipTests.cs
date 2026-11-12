using Clovent.MasterData.Shared.ValueObjects;
using Clovent.Platform.Execution;
using Clovent.Restaurant.Application.Customers.Queries;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.Infrastructure.Migrations;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Infrastructure.Repositories;
using Clovent.Restaurant.Infrastructure.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Clovent.Restaurant.Infrastructure.DependencyInjection;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Persistence;

public sealed partial class SqlCompanyOwnershipTests
{
    private static Customer Customer(string code, Guid? company, decimal opening = 0m)
    {
        var customer = Clovent.Restaurant.Customers.Customer.Create(EntityCode.Create(code),
            "Synthetic " + code, "0000000000", "Synthetic address", null, opening, 1000m, null);
        if (company.HasValue) customer.AssignCompany(company.Value);
        return customer;
    }

    [Fact]
    public async Task CompanyReadsAndWrites_FailClosedAcrossScopeReuseAndUnmappedHistory()
    {
        var database = $"CBOS_Recovery_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<RestaurantDbContext>().UseSqlServer(SqlTestDatabaseGuard.ConnectionString(database)).Options;
        await using var seed = new RestaurantDbContext(options); // Explicit unscoped synthetic/migration harness.
        try
        {
            await seed.Database.EnsureCreatedAsync();
            await SqlTestDatabaseGuard.CreatedAsync(seed);
            var companyA = Guid.NewGuid();
            var companyB = Guid.NewGuid();
            var a = Customer("OWN-A", companyA);
            var b = Customer("OWN-B", companyB);
            var unmapped = Customer("UNMAPPED", null, 300m);
            var entryA = CustomerLedgerEntry.Create(a.Id, "A", "Synthetic debit", 100m, 0m, 100m);
            entryA.AssignCompany(companyA);
            var entryB = CustomerLedgerEntry.Create(b.Id, "B", "Synthetic debit", 200m, 0m, 200m);
            entryB.AssignCompany(companyB);
            seed.AddRange(a, b, unmapped, entryA, entryB);
            await seed.SaveChangesAsync();
            var accessor = new ExecutionContextAccessor();
            await using var scoped = new RestaurantDbContext(options, accessor);
            var customers = new CustomerRepository(scoped);
            var ledger = new CustomerLedgerEntryRepository(scoped);
            using (accessor.BeginScope(new PlatformExecutionContext { OrganizationId = Guid.NewGuid(), CompanyId = companyA }))
            {
                var services = new ServiceCollection();
                services.AddSingleton<IExecutionContextAccessor>(accessor);
                var configuration = new ConfigurationBuilder().AddInMemoryCollection(
                    new Dictionary<string, string?> { ["ConnectionStrings:Restaurant"] = SqlTestDatabaseGuard.ConnectionString(database) }).Build();
                services.AddPersistence(configuration);
                await using var provider = services.BuildServiceProvider();
                await using var serviceScope = provider.CreateAsyncScope();
                var injected = serviceScope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
                Assert.Equal(a.Id, Assert.Single(await new CustomerRepository(injected).GetAllAsync()).Id);
                var noScopeServices = new ServiceCollection();
                noScopeServices.AddPersistence(configuration);
                await using var noScopeProvider = noScopeServices.BuildServiceProvider();
                await using var noScopeLease = noScopeProvider.CreateAsyncScope();
                var noScopeContext = noScopeLease.ServiceProvider.GetRequiredService<RestaurantDbContext>();
                using (accessor.BeginScope(new PlatformExecutionContext()))
                    Assert.Empty(await new CustomerRepository(noScopeContext).GetAllAsync());
                Assert.Equal(a.Id, Assert.Single(await customers.GetAllAsync()).Id);
                Assert.Null(await customers.GetByIdAsync(b.Id));
                Assert.Empty(await ledger.GetByCustomerIdAsync(b.Id));
                Assert.Equal(0, await scoped.Customers.Where(c => c.Id == b.Id)
                    .ExecuteUpdateAsync(set => set.SetProperty(c => c.IsActive, false)));
                Assert.Equal(0, await scoped.CustomerLedgerEntries.Where(e => e.CustomerId == b.Id)
                    .ExecuteUpdateAsync(set => set.SetProperty(e => e.RunningBalance, 999m)));
                var statement = await new GetCustomerReceivablesReportQueryHandler(customers, ledger,
                    new OrderRepository(scoped), new PaymentRepository(scoped), new PaymentMethodRepository(scoped),
                    new CustomerPaymentAllocationRepository(scoped), accessor).Handle(new(), default);
                Assert.Equal(100m, statement.TotalReceivables);
                await Assert.ThrowsAsync<ScopeMismatchException>(() =>
                    new GetCustomerLedgerQueryHandler(ledger, customers, accessor).Handle(new(b.Id.Value), default));

                var created = Customer("NEW-A", null);
                var addedEntry = CustomerLedgerEntry.Create(created.Id, "NEW", "Synthetic advance", 0m, 10m, -10m);
                scoped.AddRange(created, addedEntry);
                await scoped.SaveChangesAsync();
                Assert.Equal(companyA, created.CompanyId);
                Assert.Equal(companyA, addedEntry.CompanyId);
                scoped.ChangeTracker.Clear();
                scoped.Attach(b);
                b.SetStatus(false);
                await Assert.ThrowsAsync<ScopeMismatchException>(() => scoped.SaveChangesAsync());
                scoped.ChangeTracker.Clear();
            }
            using (accessor.BeginScope(new PlatformExecutionContext { OrganizationId = Guid.NewGuid(), CompanyId = companyB }))
            {
                Assert.Equal(b.Id, Assert.Single(await customers.GetAllAsync()).Id);
                Assert.Null(await customers.GetByIdAsync(a.Id));
                Assert.Equal(200m, Assert.Single(await ledger.GetByCustomerIdAsync(b.Id)).RunningBalance);
            }
            Assert.Empty(await customers.GetAllAsync());
            Assert.Empty(await ledger.GetByCustomerIdAsync(a.Id));
            scoped.Add(Customer("NO-SCOPE", null));
            await Assert.ThrowsAsync<ScopeUnresolvedException>(() => scoped.SaveChangesAsync());
        }
        finally { await SqlTestDatabaseGuard.DeleteAndVerifyAsync(seed); }
    }

    private sealed class MigrationProbe : AddCustomerCompanyOwnership
    {
        internal IReadOnlyList<MigrationOperation> Operations()
        {
            var builder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");
            Up(builder);
            return builder.Operations;
        }
    }

    [Fact]
    public async Task ExpandAndExplicitMapping_RejectMissingMapping_PreserveFacts_EnforceLedgerOwner()
    {
        var database = $"CBOS_Recovery_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<RestaurantDbContext>().UseSqlServer(SqlTestDatabaseGuard.ConnectionString(database)).Options;
        await using var db = new RestaurantDbContext(options);
        try
        {
            Assert.False(db.Database.HasPendingModelChanges());
            await db.Database.EnsureCreatedAsync();
            await SqlTestDatabaseGuard.CreatedAsync(db);
            var legacy = Customer("LEGACY", null, 100m);
            var ledger = CustomerLedgerEntry.Create(legacy.Id, "LEGACY", "Preserved historical debit", 10m, 0m, 110m);
            db.AddRange(legacy, ledger);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            // Recreate the exact pre-expand shape in this disposable database.
            await db.Database.ExecuteSqlRawAsync("""
                DROP INDEX IX_Customers_CompanyId_Id ON Restaurant.Customers;
                DROP INDEX IX_CustomerLedgerEntries_CompanyId_CustomerId ON Restaurant.CustomerLedgerEntries;
                ALTER TABLE Restaurant.Customers DROP COLUMN CompanyId;
                ALTER TABLE Restaurant.CustomerLedgerEntries DROP COLUMN CompanyId;
                """);
            await db.Database.OpenConnectionAsync();
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"EXEC sys.sp_set_session_context @key=N'CBOS.Ownership.ExpectedDatabase', @value={database};");
            await db.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(Path.Combine(FindRepositoryRoot(),
                "docs", "audits", "2026-11-11", "company-ownership", "OwnershipBaselinePreflight.sql")));
            foreach (var command in db.GetService<IMigrationsSqlGenerator>().Generate(new MigrationProbe().Operations(), db.Model))
                await db.Database.ExecuteSqlRawAsync(command.CommandText);
            foreach (var command in db.GetService<IMigrationsSqlGenerator>().Generate(
                new PreventCompanyOwnershipReassignment().UpOperations, db.Model))
                await db.Database.ExecuteSqlRawAsync(command.CommandText);
            var company = Guid.NewGuid();
            await db.Database.ExecuteSqlRawAsync("CREATE SCHEMA [Identity];");
            await db.Database.ExecuteSqlRawAsync("CREATE TABLE [Identity].[Companies] ([Id] uniqueidentifier NOT NULL PRIMARY KEY);");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [Identity].[Companies] VALUES ({company});");
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"EXEC sys.sp_set_session_context @key=N'CBOS.Ownership.ExpectedDatabase', @value={database};");
            var root = FindRepositoryRoot();
            var script = await File.ReadAllTextAsync(Path.Combine(root, "docs", "audits", "2026-11-11", "company-ownership", "ApplyExplicitMapping.sql"));
            var missing = await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => db.Database.ExecuteSqlRawAsync(script));
            Assert.Equal(51003, missing.Number);
            Assert.Null((await db.Customers.SingleAsync()).CompanyId);
            var invalidCompany = Guid.NewGuid();
            var takeover = await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() =>
                db.Customers.Where(c => c.Id == legacy.Id).ExecuteUpdateAsync(set => set.SetProperty(c => c.CompanyId, company)));
            Assert.Equal(51015, takeover.Number);
            await db.Database.ExecuteSqlRawAsync("""
                CREATE USER OwnershipWriter WITHOUT LOGIN;
                GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::Restaurant TO OwnershipWriter;
                """);
            var metadataWrite = await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($"""
                    EXECUTE AS USER=N'OwnershipWriter';
                    BEGIN TRY
                        INSERT Restaurant.CustomerCompanyOwnershipMap VALUES
                            ({legacy.Id.Value},{company},{"Forged approval"},{DateTimeOffset.UtcNow},{"Not administrator approved"});
                        REVERT;
                    END TRY
                    BEGIN CATCH
                        REVERT;
                        THROW;
                    END CATCH;
                    """));
            Assert.Equal(229, metadataWrite.Number);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO Restaurant.CustomerCompanyOwnershipMap
                (CustomerId, CompanyId, ApprovedBy, ApprovedAtUtc, EvidenceReference)
                VALUES ({legacy.Id.Value}, {invalidCompany}, {"Synthetic reviewer"}, {DateTimeOffset.UtcNow}, {"Synthetic invalid company"});
                """);
            var invalid = await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => db.Database.ExecuteSqlRawAsync(script));
            Assert.Equal(51002, invalid.Number);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE Restaurant.CustomerCompanyOwnershipMap SET CompanyId={company}, ApprovedBy={""}
                WHERE CustomerId={legacy.Id.Value};
                """);
            var unapproved = await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => db.Database.ExecuteSqlRawAsync(script));
            Assert.Equal(51002, unapproved.Number);
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Restaurant.CustomerCompanyOwnershipMap WHERE CustomerId={legacy.Id.Value};");
            // Force failure after the customer assignment but before ledger assignment finishes.
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT Restaurant.CustomerCompanyOwnershipMap VALUES
                    ({legacy.Id.Value},{company},{"Synthetic reviewer"},{DateTimeOffset.UtcNow},{"Rollback probe"});
                """);
            await db.Database.ExecuteSqlRawAsync("""
                ALTER TABLE Restaurant.CustomerLedgerEntries ADD CONSTRAINT CK_TestOwnerFailure CHECK (CompanyId IS NULL);
                """);
            var interrupted = await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => db.Database.ExecuteSqlRawAsync(script));
            Assert.Equal(547, interrupted.Number);
            db.ChangeTracker.Clear();
            Assert.Null((await db.Customers.SingleAsync()).CompanyId);
            Assert.Null((await db.CustomerLedgerEntries.SingleAsync()).CompanyId);
            Assert.Equal(100m, (await db.Customers.SingleAsync()).OutstandingBalance);
            Assert.Equal(110m, (await db.CustomerLedgerEntries.SingleAsync()).RunningBalance);
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE Restaurant.CustomerLedgerEntries DROP CONSTRAINT CK_TestOwnerFailure;");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Restaurant.CustomerCompanyOwnershipMap WHERE CustomerId={legacy.Id.Value};");
            await RunMappingLoader(root, database, legacy.Id.Value, company, duplicate: true);
            Assert.Null((await db.Customers.AsNoTracking().SingleAsync()).CompanyId);
            await RunMappingLoader(root, database, legacy.Id.Value, company);
            await db.Database.ExecuteSqlRawAsync(script); // Idempotent, never reapplies financial effects.
            await db.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(
                Path.Combine(root, "docs", "audits", "2026-11-11", "company-ownership", "OwnershipPreflight.sql")));
            db.ChangeTracker.Clear();
            var mapped = await db.Customers.SingleAsync();
            var mappedEntry = await db.CustomerLedgerEntries.SingleAsync();
            Assert.Equal(company, mapped.CompanyId);
            Assert.Equal(company, mappedEntry.CompanyId);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                EXECUTE AS USER=N'OwnershipWriter';
                BEGIN TRY
                    UPDATE Restaurant.Customers SET OutstandingBalance=OutstandingBalance WHERE Id={legacy.Id.Value};
                    REVERT;
                END TRY
                BEGIN CATCH
                    REVERT;
                    THROW;
                END CATCH;
                """);
            Assert.Equal(100m, mapped.OpeningBalance);
            Assert.Equal(100m, mapped.OutstandingBalance);
            Assert.Equal(10m, mappedEntry.Debit);
            Assert.Equal(110m, mappedEntry.RunningBalance);
            Assert.Equal(ledger.Date, mappedEntry.Date);
            var emptyAccount = Customer("NO-LEDGER", company);
            db.Add(emptyAccount);
            await db.SaveChangesAsync();
            var reassignment = await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() =>
                db.Customers.Where(c => c.Id == emptyAccount.Id).ExecuteUpdateAsync(
                    set => set.SetProperty(c => c.CompanyId, Guid.NewGuid())));
            Assert.Equal(51012, reassignment.Number);
            var deletion = await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() =>
                db.CustomerLedgerEntries.Where(e => e.Id == mappedEntry.Id).ExecuteDeleteAsync());
            Assert.Equal(51013, deletion.Number);
            Assert.Equal(mappedEntry.Id, (await db.CustomerLedgerEntries.SingleAsync()).Id);
            var forged = CustomerLedgerEntry.Create(legacy.Id, "FORGED", "Wrong company", 1m, 0m, 111m);
            forged.AssignCompany(Guid.NewGuid());
            db.Add(forged);
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
        finally { await SqlTestDatabaseGuard.DeleteAndVerifyAsync(db); }
    }

    private static async Task RunMappingLoader(string root, string database, Guid customer, Guid company, bool duplicate = false)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"cbos-company-map-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var csv = Path.Combine(directory, "synthetic.csv");
        try
        {
            var row = $"{customer},{company},Synthetic reviewer,{DateTimeOffset.UtcNow:O},Synthetic explicit mapping\n";
            await File.WriteAllTextAsync(csv, "CustomerId,CompanyId,ApprovedBy,ApprovedAtUtc,EvidenceReference\n" +
                row + (duplicate ? row : ""));
            var start = new System.Diagnostics.ProcessStartInfo {
                FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe"),
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
            };
            foreach (var argument in new[] { "-NoProfile", "-File", Path.Combine(root, "docs", "audits", "2026-11-11", "company-ownership", "Invoke-ExplicitMapping.ps1"),
                "-MappingCsv", csv, "-ExpectedDatabase", database, "-ConnectionString", SqlTestDatabaseGuard.ConnectionString(database), "-Apply" })
                start.ArgumentList.Add(argument);
            using var process = System.Diagnostics.Process.Start(start)!;
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            try { await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(40)); }
            catch (TimeoutException) { process.Kill(entireProcessTree: true); throw; }
            if (duplicate)
            {
                Assert.NotEqual(0, process.ExitCode);
                Assert.Contains("duplicate", await stderr);
                return;
            }
            Assert.True(process.ExitCode == 0, $"Mapping loader exit {process.ExitCode}: {await stderr}\n{await stdout}");
            Assert.Contains("MAPPED", await stdout);
        }
        finally
        {
            if (File.Exists(csv)) File.Delete(csv);
            Directory.Delete(directory); // Only the known empty test-owned directory; no recursive deletion.
        }
    }

    private static string FindRepositoryRoot([System.Runtime.CompilerServices.CallerFilePath] string source = "")
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(source)!);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Clovent.BusinessOperatingSystem.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root unavailable.");
    }
}
