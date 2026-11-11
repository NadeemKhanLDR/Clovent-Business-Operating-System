using System.Text.Json;
using Clovent.Catalog.Variants;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.Reports;
using Clovent.Restaurant.Application.EndOfDay.Queries;
using Clovent.Restaurant.Application.Shifts.Services;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Infrastructure.Repositories;
using Clovent.Restaurant.Infrastructure.Tests.TestSupport;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using Clovent.Restaurant.Payments;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Persistence;

// Synthetic report/persistence qualification only; not a command rollback test.
public sealed class SqlReportingAuditTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReloadedTaxFacts_ReconcileMixedDiscountedAndInclusiveSales(bool inclusive)
    {
        var database = $"CBOS_Recovery_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<RestaurantDbContext>()
            .UseSqlServer(SqlTestDatabaseGuard.ConnectionString(database)).Options;
        await using var db = new RestaurantDbContext(options);
        try
        {
            await db.Database.EnsureCreatedAsync();
            await SqlTestDatabaseGuard.CreatedAsync(db);
            var warehouse = WarehouseId.New();
            var order = Order.Create(OrderType.TakeAway, warehouse);
            var line = OrderLine.Create(order.Id, ProductVariantId.New(), inclusive ? 1m : 2m,
                inclusive ? 10.05m : 100m, 10m, inclusive);
            line.SetTaxSnapshot("Taxable", "PRA", "AUDIT", inclusive ? 9.14m : 180m,
                inclusive ? 0m : 20m, inclusive ? .91m : 18m, "AUDIT");
            order.AddOrderLine(line.Id);
            db.AddRange(order, line);
            if (!inclusive)
            {
                var exempt = OrderLine.Create(order.Id, ProductVariantId.New(), 1m, 50m, 0m, false);
                exempt.SetTaxSnapshot("Exempt", "PRA", "EXEMPT", 45m, 5m, 0m, "AUDIT");
                order.AddOrderLine(exempt.Id);
                db.Add(exempt);
            }
            var at = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
            var method = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
            var payment = Payment.Create(order.Id, method.Id, inclusive ? 10.05m : 243m);
            order.RecordPayment(payment.Id);
            db.AddRange(method, payment);
            order.SetReceiptSnapshot(JsonSerializer.Serialize(new ReceiptSnapshot(order.Id.Value, order.OrderNumber.Value,
                null, "TakeAway", at, [], inclusive ? 10.05m : 250m, inclusive ? .91m : 18m,
                inclusive ? 0m : 25m, 0m, inclusive ? 10.05m : 243m, 0m,
                [new(inclusive ? 10.05m : 243m, "Cash")], "Synthetic", "AUDIT", null,
                TotalExclusiveTax: inclusive ? 0m : 18m, TotalInclusiveTax: inclusive ? .91m : 0m)));
            order.Complete();
            typeof(Order).GetProperty("UpdatedAtUtc")!.SetValue(order, at);
            await db.SaveChangesAsync();
            // SaveChanges stamps UpdatedAt; set the persisted test timestamp explicitly.
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE [Restaurant].[Orders] SET [UpdatedAtUtc] = {at} WHERE [Id] = {order.Id.Value}");
            db.ChangeTracker.Clear();
            var report = await new GetTaxSummaryReportQueryHandler(
                new OrderRepository(db), new OrderLineRepository(db), new RefundRepository(db))
                .Handle(new(warehouse.Value, new(2026, 1, 10), new(2026, 1, 10)), default);
            Assert.Equal(inclusive ? 10.05m : 250m, report.GrossSalesBeforeDiscounts);
            Assert.Equal(inclusive ? 0m : 25m, report.TotalDiscounts);
            Assert.Equal(inclusive ? 9.14m : 225m, report.SalesExcludingTax);
            Assert.Equal(inclusive ? .91m : 18m, report.NetTaxPayable);
            Assert.Equal(inclusive ? 0m : 45m, report.ExemptSales);
            var expanded = await new GetExpandedSalesSummaryQueryHandler(new OrderRepository(db), new OrderLineRepository(db),
                new PaymentRepository(db), new PaymentMethodRepository(db), new CustomerRepository(db),
                new CustomerLedgerEntryRepository(db), new DiscountRepository(db), new ServiceChargeRepository(db),
                new ShiftRepository(db), new TableRepository(db), new NoCatalog(), new BusinessDateProvider(TimeZoneInfo.Utc),
                new RefundRepository(db)).Handle(new(warehouse.Value, new(2026, 1, 10), new(2026, 1, 10)), default);
            Assert.Equal(inclusive ? 10.05m : 243m, expanded.Kpis.TotalBillSales);
            Assert.Equal(expanded.Kpis.TotalBillSales, expanded.Orders.Sum(o => o.Total));
            Assert.Equal(expanded.Kpis.TotalBillSales, expanded.OrderTypes.Sum(o => o.TotalSales));
            Assert.Equal(inclusive ? .91m : 18m, expanded.Orders.SelectMany(o => o.Lines!).Sum(l => l.Tax));
            Assert.Equal(0m, expanded.Orders.SelectMany(o => o.Lines!).Sum(l => l.ServiceCharge));
            var foreign = await new GetTaxSummaryReportQueryHandler(
                new OrderRepository(db), new OrderLineRepository(db), new RefundRepository(db))
                .Handle(new(WarehouseId.New().Value, new(2026, 1, 10), new(2026, 1, 10)), default);
            Assert.Equal(0m, foreign.GrossSalesBeforeDiscounts);
        }
        finally { await SqlTestDatabaseGuard.DeleteAndVerifyAsync(db); }
    }

    // Only optional catalogue labels are absent. All financial repositories are SQL-backed.
    private sealed class NoCatalog : IMediator
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
            => Task.FromResult(default(TResponse)!);
        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => Task.FromResult<object?>(null);
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest => throw new NotSupportedException();
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task Publish(object notification, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => throw new NotSupportedException();
    }
}
