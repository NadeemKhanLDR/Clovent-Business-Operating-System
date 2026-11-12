using Clovent.Identity.Branches;
using Clovent.Identity.Branches.ValueObjects;
using Clovent.Identity.Companies;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Warehouses;
using Clovent.MasterData.Warehouses.ValueObjects;
using Clovent.Platform.Execution;
using Clovent.Restaurant.Application;
using Clovent.Restaurant.Application.Concurrency;
using Clovent.Restaurant.Application.Customers.Commands;
using Clovent.Restaurant.Application.Payments.Commands;
using Clovent.Restaurant.Application.Refunds.Commands;
using Clovent.Restaurant.Application.Security;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.Infrastructure.DependencyInjection;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Infrastructure.Repositories;
using Clovent.Restaurant.Infrastructure.Tests.TestSupport;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Refunds;
using Clovent.Restaurant.Continuity;
using Clovent.Restaurant.Application.Continuity;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Persistence;

public sealed partial class SqlCompanyOwnershipTests
{
    [Fact]
    public async Task RealFinancialPipeline_DeniesForeignCollectionPaymentAndRefund_ThenCollectsOwnedBalance()
    {
        var database = $"CBOS_Recovery_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<RestaurantDbContext>().UseSqlServer(SqlTestDatabaseGuard.ConnectionString(database)).Options;
        await using var seed = new RestaurantDbContext(options);
        try
        {
            await seed.Database.EnsureCreatedAsync();
            await SqlTestDatabaseGuard.CreatedAsync(seed);
            var ca = Guid.NewGuid(); var cb = Guid.NewGuid();
            var ba = Guid.NewGuid(); var bb = Guid.NewGuid();
            var wa = Guid.NewGuid(); var wb = Guid.NewGuid();
            await seed.Database.ExecuteSqlRawAsync("CREATE SCHEMA [Identity];");
            await seed.Database.ExecuteSqlRawAsync("CREATE SCHEMA [MasterData];");
            await seed.Database.ExecuteSqlRawAsync("""
                CREATE TABLE [Identity].Branches (Id uniqueidentifier PRIMARY KEY, CompanyId uniqueidentifier NOT NULL);
                CREATE TABLE MasterData.Warehouses (Id uniqueidentifier PRIMARY KEY, BranchId uniqueidentifier NOT NULL);
                """);
            await seed.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT [Identity].Branches VALUES ({ba},{ca}),({bb},{cb});
                INSERT MasterData.Warehouses VALUES ({wa},{ba}),({wb},{bb});
                """);
            var a = Customer("PIPE-A", ca, 100m);
            var b = Customer("PIPE-B", cb, 200m);
            var foreign = Order.Create(OrderType.TakeAway, new WarehouseId(wb)); foreign.SetCustomer(b.Id);
            seed.AddRange(a, b, foreign);
            await seed.SaveChangesAsync();
            var accessor = new ExecutionContextAccessor();
            using var lease = accessor.BeginScope(new PlatformExecutionContext {
                OrganizationId = Guid.NewGuid(), CompanyId = ca, BranchId = ba });
            var services = new ServiceCollection();
            services.AddSingleton<IExecutionContextAccessor>(accessor);
            services.AddLogging();
            services.AddPersistence(new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string,string?> { ["ConnectionStrings:Restaurant"] = SqlTestDatabaseGuard.ConnectionString(database) }).Build());
            services.AddScoped<SqlHierarchy>();
            services.AddScoped<IWarehouseRepository>(sp => sp.GetRequiredService<SqlHierarchy>());
            services.AddScoped<IBranchRepository>(sp => sp.GetRequiredService<SqlHierarchy>());
            services.AddScoped<IOrderScopeValidator, OrderScopeValidator>();
            services.AddScoped<IFinancialCompanyScope, FinancialCompanyScope>();
            services.AddMediatR(c => c.RegisterServicesFromAssemblyContaining<RecordCustomerPaymentCommand>());
            services.AddScopeEnforcement();
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionSerializationBehavior<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));
            await using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await Assert.ThrowsAsync<ScopeMismatchException>(() => mediator.Send(new RecordCustomerPaymentCommand(b.Id.Value, 10m)));
            await Assert.ThrowsAsync<ScopeMismatchException>(() => mediator.Send(new RecordPaymentCommand(foreign.Id.Value, Guid.NewGuid(), 10m)));
            await Assert.ThrowsAsync<ScopeMismatchException>(() => mediator.Send(new ProcessRefundCommand(foreign.Id.Value,
                Array.Empty<RefundItemRequest>(), "Synthetic forged target", RefundSettlementMethod.CustomerAccountCredit,
                "forged-refund", Guid.NewGuid(), "Synthetic", BranchId: ba)));
            await using (var verify = new RestaurantDbContext(options))
            {
                Assert.Equal(200m, (await verify.Customers.SingleAsync(c => c.Id == b.Id)).OutstandingBalance);
                Assert.Empty(await verify.Payments.ToListAsync());
                Assert.Empty(await verify.Refunds.ToListAsync());
                Assert.Empty(await verify.CustomerLedgerEntries.ToListAsync());
            }
            var result = await mediator.Send(new RecordCustomerPaymentCommand(a.Id.Value, 10m));
            Assert.Equal(90m, result.OutstandingAfter);
            // A signed journal and an already committed foreign idempotency key are not authority.
            var txId = Guid.NewGuid(); var when = DateTimeOffset.UtcNow;
            var cash = Clovent.Restaurant.PaymentMethods.PaymentMethod.Create(
                Clovent.Restaurant.PaymentMethods.ValueObjects.PaymentMethodName.Create("Cash"));
            seed.Add(cash);
            var foreignPayment = Clovent.Restaurant.Payments.Payment.Create(foreign.Id, cash.Id, 1m,
                idempotencyKey: $"emergency:{txId}");
            seed.Add(foreignPayment); await seed.SaveChangesAsync();
            var tx = new EmergencyTransaction {
                TransactionId=txId, SequenceNumber=1, TimestampUtc=when, TerminalId="SYNTHETIC",
                BranchId=bb, WarehouseId=wb, CashierName="Synthetic", PaymentType="Cash",
                OrderSnapshot=new EmergencyOrderSnapshot("TakeAway",null,null,[],1m,0m,0m,0m,1m,null,null),
                Checksum=EmergencyTransaction.ComputeChecksum(txId,1,when,"SYNTHETIC",bb,wb,"Synthetic",1m,"Cash"),
                HmacSignature=EmergencyTransaction.ComputeHmacSignature(txId,1,EmergencyTransaction.GenesisHash,
                    when,"SYNTHETIC",bb,wb,"Synthetic",1m,"Cash")
            };
            var journal = new SingleJournal(tx);
            var replayDb = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
            var replay = new EmergencyJournalReplayer(journal, new OrderRepository(replayDb), new OrderLineRepository(replayDb),
                new PaymentRepository(replayDb), new PaymentMethodRepository(replayDb), new UnitOfWork(replayDb),
                new OutboxRepository(replayDb), scope.ServiceProvider.GetRequiredService<ITransactionLockProvider>(),
                replayScope: new ContinuityReplayScopeValidator(scope.ServiceProvider.GetRequiredService<IOrderScopeValidator>(),
                    scope.ServiceProvider.GetRequiredService<IWarehouseRepository>()));
            var rejected = await replay.ReplayPendingAsync();
            Assert.Equal(1, rejected.FailedCount); Assert.Equal(0, rejected.DuplicateIgnoredCount);
            Assert.Equal(ReconciliationStatus.RequiresManagerReview, tx.ReconciliationStatus);
            var ownTargetSameForeignKey = new EmergencyTransaction {
                TransactionId=txId, SequenceNumber=1, TimestampUtc=when, TerminalId="SYNTHETIC",
                BranchId=ba, WarehouseId=wa, CashierName="Synthetic", PaymentType="Cash",
                OrderSnapshot=tx.OrderSnapshot,
                Checksum=EmergencyTransaction.ComputeChecksum(txId,1,when,"SYNTHETIC",ba,wa,"Synthetic",1m,"Cash"),
                HmacSignature=EmergencyTransaction.ComputeHmacSignature(txId,1,EmergencyTransaction.GenesisHash,
                    when,"SYNTHETIC",ba,wa,"Synthetic",1m,"Cash")
            };
            var collisionReplay = new EmergencyJournalReplayer(new SingleJournal(ownTargetSameForeignKey),
                new OrderRepository(replayDb), new OrderLineRepository(replayDb), new PaymentRepository(replayDb),
                new PaymentMethodRepository(replayDb), new UnitOfWork(replayDb), new OutboxRepository(replayDb),
                scope.ServiceProvider.GetRequiredService<ITransactionLockProvider>(),
                replayScope: new ContinuityReplayScopeValidator(scope.ServiceProvider.GetRequiredService<IOrderScopeValidator>(),
                    scope.ServiceProvider.GetRequiredService<IWarehouseRepository>()));
            var collision = await collisionReplay.ReplayPendingAsync();
            Assert.Equal(1, collision.FailedCount); Assert.Equal(0, collision.DuplicateIgnoredCount);
            Assert.Equal(ReconciliationStatus.RequiresManagerReview, ownTargetSameForeignKey.ReconciliationStatus);
            await using var final = new RestaurantDbContext(options);
            Assert.Equal(90m, (await final.Customers.SingleAsync(c => c.Id == a.Id)).OutstandingBalance);
            var posted = Assert.Single(await final.CustomerLedgerEntries.ToListAsync());
            Assert.Equal(ca, posted.CompanyId); Assert.Equal(10m, posted.Credit);
            Assert.Equal(foreignPayment.Id, Assert.Single(await final.Payments.ToListAsync()).Id);
            Assert.Equal(foreign.Id, Assert.Single(await final.Orders.ToListAsync()).Id);
        }
        finally { await SqlTestDatabaseGuard.DeleteAndVerifyAsync(seed); }
    }

    private sealed class SingleJournal(EmergencyTransaction tx) : IContinuityJournalStore
    {
        public Task AppendAsync(EmergencyTransaction value, CancellationToken ct=default) => throw new NotSupportedException();
        public Task<IReadOnlyList<EmergencyTransaction>> GetAllAsync(CancellationToken ct=default) => Task.FromResult<IReadOnlyList<EmergencyTransaction>>([tx]);
        public Task<IReadOnlyList<EmergencyTransaction>> GetPendingReplayAsync(CancellationToken ct=default) => GetAllAsync(ct);
        public Task UpdateAsync(EmergencyTransaction value, CancellationToken ct=default) => Task.CompletedTask;
        public Task<ContinuityJournalStatistics> GetStatisticsAsync(CancellationToken ct=default) => throw new NotSupportedException();
        public Task<long> GetNextSequenceNumberAsync(CancellationToken ct=default) => throw new NotSupportedException();
        public Task<string> GetLastTransactionHashAsync(CancellationToken ct=default) => Task.FromResult(EmergencyTransaction.GenesisHash);
    }

    // Fixture adapter reads actual SQL hierarchy rows; it grants nothing from caller-supplied IDs.
    private sealed class SqlHierarchy(RestaurantDbContext db) : IWarehouseRepository, IBranchRepository
    {
        private sealed class WarehouseRow { public Guid Id { get; set; } public Guid BranchId { get; set; } }
        private sealed class BranchRow { public Guid Id { get; set; } public Guid CompanyId { get; set; } }
        public async Task<Warehouse?> GetByIdAsync(WarehouseId id, CancellationToken ct = default)
        {
            var row = await db.Database.SqlQuery<WarehouseRow>($"SELECT Id,BranchId FROM MasterData.Warehouses WHERE Id={id.Value}").SingleOrDefaultAsync(ct);
            if (row is null) return null;
            var value = Warehouse.Create(new BranchId(row.BranchId), WarehouseName.Create("Synthetic warehouse"), EntityCode.Create("SQL"));
            typeof(Warehouse).GetProperty("Id")!.SetValue(value, new WarehouseId(row.Id)); return value;
        }
        public async Task<Branch?> GetByIdAsync(BranchId id, CancellationToken ct = default)
        {
            var row = await db.Database.SqlQuery<BranchRow>($"SELECT Id,CompanyId FROM [Identity].Branches WHERE Id={id.Value}").SingleOrDefaultAsync(ct);
            if (row is null) return null;
            var value = Branch.Create(new CompanyId(row.CompanyId), BranchName.Create("Synthetic branch"));
            typeof(Branch).GetProperty("Id")!.SetValue(value, new BranchId(row.Id)); return value;
        }
        public Task<IReadOnlyCollection<Warehouse>> GetByBranchIdAsync(BranchId id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Warehouse>> GetAllAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task AddAsync(Warehouse value, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Branch>> GetByCompanyIdAsync(CompanyId id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task AddAsync(Branch value, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
