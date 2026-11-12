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

public sealed class SqlCompanyOwnershipTests
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
            foreach (var command in db.GetService<IMigrationsSqlGenerator>().Generate(new MigrationProbe().Operations(), db.Model))
                await db.Database.ExecuteSqlRawAsync(command.CommandText);
            var company = Guid.NewGuid();
            await db.Database.ExecuteSqlRawAsync("CREATE SCHEMA [Identity];");
            await db.Database.ExecuteSqlRawAsync("CREATE TABLE [Identity].[Companies] ([Id] uniqueidentifier NOT NULL PRIMARY KEY);");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [Identity].[Companies] VALUES ({company});");
            await db.Database.OpenConnectionAsync();
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"EXEC sys.sp_set_session_context @key=N'CBOS.Ownership.ExpectedDatabase', @value={database};");
            var root = FindRepositoryRoot();
            var script = await File.ReadAllTextAsync(Path.Combine(root, "docs", "audits", "2026-11-11", "company-ownership", "ApplyExplicitMapping.sql"));
            var missing = await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => db.Database.ExecuteSqlRawAsync(script));
            Assert.Equal(51003, missing.Number);
            Assert.Null((await db.Customers.SingleAsync()).CompanyId);
            var invalidCompany = Guid.NewGuid();
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
            await RunMappingLoader(root, database, legacy.Id.Value, company);
            await db.Database.ExecuteSqlRawAsync(script); // Idempotent, never reapplies financial effects.
            db.ChangeTracker.Clear();
            var mapped = await db.Customers.SingleAsync();
            var mappedEntry = await db.CustomerLedgerEntries.SingleAsync();
            Assert.Equal(company, mapped.CompanyId);
            Assert.Equal(company, mappedEntry.CompanyId);
            Assert.Equal(100m, mapped.OpeningBalance);
            Assert.Equal(100m, mapped.OutstandingBalance);
            Assert.Equal(10m, mappedEntry.Debit);
            Assert.Equal(110m, mappedEntry.RunningBalance);
            Assert.Equal(ledger.Date, mappedEntry.Date);
            var forged = CustomerLedgerEntry.Create(legacy.Id, "FORGED", "Wrong company", 1m, 0m, 111m);
            forged.AssignCompany(Guid.NewGuid());
            db.Add(forged);
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
        finally { await SqlTestDatabaseGuard.DeleteAndVerifyAsync(db); }
    }

    [Fact]
    public async Task ExplicitMapping_RejectsWrongTarget_MissingEvidence_Reassignment_AndConflictingLedger_WhileRevealingQuarantinedBalances()
    {
        var database = $"CBOS_Recovery_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<RestaurantDbContext>().UseSqlServer(SqlTestDatabaseGuard.ConnectionString(database)).Options;
        await using var db = new RestaurantDbContext(options);
        try
        {
            await db.Database.EnsureCreatedAsync();
            await SqlTestDatabaseGuard.CreatedAsync(db);
            var companyA = Guid.NewGuid();
            var companyB = Guid.NewGuid();
            var mappedA = Customer("MAP-A", companyA, 50m);
            var unmappedA = Customer("QUAR-A", null, 125m);
            var unmappedB = Customer("QUAR-B", null, 275m);
            var ledgerA = CustomerLedgerEntry.Create(unmappedA.Id, "Q-A", "Quarantined debit A", 25m, 0m, 150m);
            var ledgerB = CustomerLedgerEntry.Create(unmappedB.Id, "Q-B", "Quarantined advance B", 0m, 40m, 235m);
            db.AddRange(mappedA, unmappedA, unmappedB, ledgerA, ledgerB);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var accessor = new ExecutionContextAccessor();
            await using var scoped = new RestaurantDbContext(options, accessor);
            var customers = new CustomerRepository(scoped);
            var ledgerRepo = new CustomerLedgerEntryRepository(scoped);
            using (accessor.BeginScope(new PlatformExecutionContext { OrganizationId = Guid.NewGuid(), CompanyId = companyA }))
            {
                Assert.Equal(mappedA.Id, Assert.Single(await customers.GetAllAsync()).Id);
                Assert.Null(await customers.GetByIdAsync(unmappedA.Id));
                Assert.Empty(await ledgerRepo.GetByCustomerIdAsync(unmappedA.Id));
                await Assert.ThrowsAsync<ScopeMismatchException>(() =>
                    new GetCustomerLedgerQueryHandler(ledgerRepo, customers, accessor).Handle(new(unmappedA.Id.Value), default));
            }

            await db.Database.ExecuteSqlRawAsync("""
                DROP INDEX IX_Customers_CompanyId_Id ON Restaurant.Customers;
                DROP INDEX IX_CustomerLedgerEntries_CompanyId_CustomerId ON Restaurant.CustomerLedgerEntries;
                ALTER TABLE Restaurant.Customers DROP COLUMN CompanyId;
                ALTER TABLE Restaurant.CustomerLedgerEntries DROP COLUMN CompanyId;
                """);
            foreach (var command in db.GetService<IMigrationsSqlGenerator>().Generate(new MigrationProbe().Operations(), db.Model))
                await db.Database.ExecuteSqlRawAsync(command.CommandText);

            await db.Database.ExecuteSqlRawAsync("CREATE SCHEMA [Identity];");
            await db.Database.ExecuteSqlRawAsync("CREATE TABLE [Identity].[Companies] ([Id] uniqueidentifier NOT NULL PRIMARY KEY);");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [Identity].[Companies] VALUES ({companyA}), ({companyB});");
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE Restaurant.Customers SET CompanyId = {companyA} WHERE Id = {mappedA.Id.Value};");

            var root = FindRepositoryRoot();
            var script = await File.ReadAllTextAsync(Path.Combine(root, "docs", "audits", "2026-11-11", "company-ownership", "ApplyExplicitMapping.sql"));

            await db.Database.OpenConnectionAsync();
            // Wrong/missing ExpectedDatabase session context must fail closed (51000).
            var wrongTarget = await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => db.Database.ExecuteSqlRawAsync(script));
            Assert.Equal(51000, wrongTarget.Number);

            await db.Database.ExecuteSqlInterpolatedAsync(
                $"EXEC sys.sp_set_session_context @key=N'CBOS.Ownership.ExpectedDatabase', @value={database};");

            // Missing EvidenceReference must fail closed (51002).
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO Restaurant.CustomerCompanyOwnershipMap
                (CustomerId, CompanyId, ApprovedBy, ApprovedAtUtc, EvidenceReference)
                VALUES ({unmappedA.Id.Value}, {companyA}, {"Reviewer"}, {DateTimeOffset.UtcNow}, {"   "});
                """);
            var missingEvidence = await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => db.Database.ExecuteSqlRawAsync(script));
            Assert.Equal(51002, missingEvidence.Number);

            // Reassigning an already-mapped customer to a different company must fail closed (51002).
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Restaurant.CustomerCompanyOwnershipMap;");
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO Restaurant.CustomerCompanyOwnershipMap
                (CustomerId, CompanyId, ApprovedBy, ApprovedAtUtc, EvidenceReference)
                VALUES ({mappedA.Id.Value}, {companyB}, {"Reviewer"}, {DateTimeOffset.UtcNow}, {"Attempted reassignment"});
                """);
            var reassign = await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => db.Database.ExecuteSqlRawAsync(script));
            Assert.Equal(51002, reassign.Number);

            // Conflicting ledger entry CompanyId vs mapped customer must fail closed (51004).
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Restaurant.CustomerCompanyOwnershipMap;");
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO Restaurant.CustomerCompanyOwnershipMap
                (CustomerId, CompanyId, ApprovedBy, ApprovedAtUtc, EvidenceReference)
                VALUES
                ({unmappedA.Id.Value}, {companyA}, {"Reviewer"}, {DateTimeOffset.UtcNow}, {"Ticket A"}),
                ({unmappedB.Id.Value}, {companyB}, {"Reviewer"}, {DateTimeOffset.UtcNow}, {"Ticket B"});
                """);
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE Restaurant.CustomerLedgerEntries NOCHECK CONSTRAINT FK_CustomerLedger_CompanyCustomer;");
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE Restaurant.CustomerLedgerEntries SET CompanyId = {companyB} WHERE Id = {ledgerA.Id.Value};");
            var conflict = await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => db.Database.ExecuteSqlRawAsync(script));
            Assert.Equal(51004, conflict.Number);

            // Restore null ledger owner, re-enable FK, apply valid multi-company explicit mapping, and verify unquarantined visibility + preserved balances.
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE Restaurant.CustomerLedgerEntries SET CompanyId = NULL WHERE Id = {ledgerA.Id.Value};");
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE Restaurant.CustomerLedgerEntries WITH CHECK CHECK CONSTRAINT FK_CustomerLedger_CompanyCustomer;");
            await db.Database.ExecuteSqlRawAsync(script);

            scoped.ChangeTracker.Clear();
            using (accessor.BeginScope(new PlatformExecutionContext { OrganizationId = Guid.NewGuid(), CompanyId = companyA }))
            {
                var companyACustomers = await customers.GetAllAsync();
                Assert.Equal(2, companyACustomers.Count);
                var revealedA = Assert.Single(companyACustomers, c => c.Id == unmappedA.Id);
                Assert.Equal(125m, revealedA.OpeningBalance);
                Assert.Equal(125m, revealedA.OutstandingBalance);
                var revealedLedgerA = Assert.Single(await ledgerRepo.GetByCustomerIdAsync(unmappedA.Id));
                Assert.Equal(25m, revealedLedgerA.Debit);
                Assert.Equal(150m, revealedLedgerA.RunningBalance);
                Assert.Null(await customers.GetByIdAsync(unmappedB.Id));
            }
            using (accessor.BeginScope(new PlatformExecutionContext { OrganizationId = Guid.NewGuid(), CompanyId = companyB }))
            {
                var revealedB = Assert.Single(await customers.GetAllAsync());
                Assert.Equal(unmappedB.Id, revealedB.Id);
                Assert.Equal(275m, revealedB.OpeningBalance);
                Assert.Equal(275m, revealedB.OutstandingBalance);
                var revealedLedgerB = Assert.Single(await ledgerRepo.GetByCustomerIdAsync(unmappedB.Id));
                Assert.Equal(40m, revealedLedgerB.Credit);
                Assert.Equal(235m, revealedLedgerB.RunningBalance);
            }
        }
        finally { await SqlTestDatabaseGuard.DeleteAndVerifyAsync(db); }
    }

    private static async Task RunMappingLoader(string root, string database, Guid customer, Guid company)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"cbos-company-map-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var csv = Path.Combine(directory, "synthetic.csv");
        try
        {
            await File.WriteAllTextAsync(csv, "CustomerId,CompanyId,ApprovedBy,ApprovedAtUtc,EvidenceReference\n" +
                $"{customer},{company},Synthetic reviewer,{DateTimeOffset.UtcNow:O},Synthetic explicit mapping\n");
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
