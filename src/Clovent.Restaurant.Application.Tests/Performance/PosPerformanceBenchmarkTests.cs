using System.Diagnostics;
using Clovent.MasterData.Warehouses;
using Clovent.Platform.CircuitBreakers;
using Clovent.Restaurant.Application.Orders.Commands;
using Clovent.Restaurant.Application.Outbox.Handlers;
using Clovent.Restaurant.Application.Printing;
using Clovent.Restaurant.Application.QuickBooks;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.Continuity;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Outbox;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.Payments;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace Clovent.Restaurant.Application.Tests.Performance;

public sealed class PosPerformanceBenchmarkTests(ITestOutputHelper output)
{
    private static (double Avg, double P50, double P95, double P99, double Max, double Total) CalculatePercentiles(List<double> latenciesMs)
    {
        latenciesMs.Sort();
        var count = latenciesMs.Count;
        var avg = latenciesMs.Average();
        var p50 = latenciesMs[(int)(count * 0.50)];
        var p95 = latenciesMs[Math.Min((int)(count * 0.95), count - 1)];
        var p99 = latenciesMs[Math.Min((int)(count * 0.99), count - 1)];
        var max = latenciesMs[^1];
        var total = latenciesMs.Sum();
        return (avg, p50, p95, p99, max, total);
    }

    private sealed class BenchmarkFixture
    {
        public FakeOrderRepository Orders { get; } = new();
        public FakeOrderLineRepository OrderLines { get; } = new();
        public FakeDiscountRepository Discounts { get; } = new();
        public FakeServiceChargeRepository ServiceCharges { get; } = new();
        public FakePaymentRepository Payments { get; } = new();
        public FakeTableRepository Tables { get; } = new();
        public FakeDailySalesSequenceRepository DailySalesSequences { get; } = new();
        public FakeOutboxRepository Outbox { get; } = new();

        public CompleteOrderCommandHandler CreateHandler() => new(
            Orders,
            OrderLines,
            Discounts,
            ServiceCharges,
            Payments,
            Tables,
            DailySalesSequences,
            new FakeMediator(_ => Task.FromResult<object?>(null)),
            Outbox);

        public (Order Order, decimal Amount) CreateSampleOrder(WarehouseId warehouseId, decimal lineAmount)
        {
            var order = Order.Create(OrderType.TakeAway, warehouseId);
            Orders.Add(order);

            var variantId = Clovent.Catalog.Variants.ProductVariantId.New();
            var line = OrderLine.Create(order.Id, variantId, 1, lineAmount, 0, false);
            order.AddOrderLine(line.Id);
            OrderLines.Add(line);

            var payment = Payment.Create(order.Id, PaymentMethodId.New(), lineAmount);
            order.RecordPayment(payment.Id);
            Payments.Add(payment);

            return (order, lineAmount);
        }
    }

    [Fact]
    public async Task Benchmark_100_Standard_POS_Sales()
    {
        var fixture = new BenchmarkFixture();
        var handler = fixture.CreateHandler();
        var warehouseId = WarehouseId.New();
        var latencies = new List<double>(100);

        var sw = new Stopwatch();

        for (int i = 0; i < 100; i++)
        {
            var (order, _) = fixture.CreateSampleOrder(warehouseId, 150m);

            sw.Restart();
            var result = await handler.Handle(new CompleteOrderCommand(order.Id.Value), CancellationToken.None);
            sw.Stop();

            latencies.Add(sw.Elapsed.TotalMilliseconds);
            Assert.Equal("Completed", result.Status);
        }

        var (avg, p50, p95, p99, max, total) = CalculatePercentiles(latencies);
        output.WriteLine($"BENCHMARK [100 Standard Sales]: Total={total:F2}ms, Avg={avg:F3}ms, P50={p50:F3}ms, P95={p95:F3}ms, P99={p99:F3}ms, Max={max:F3}ms");
        Assert.True(p95 < 20.0, $"Expected P95 < 20ms, got {p95}ms");
    }

    [Fact]
    public async Task Benchmark_500_Standard_POS_Sales()
    {
        var fixture = new BenchmarkFixture();
        var handler = fixture.CreateHandler();
        var warehouseId = WarehouseId.New();
        var latencies = new List<double>(500);

        var sw = new Stopwatch();

        for (int i = 0; i < 500; i++)
        {
            var (order, _) = fixture.CreateSampleOrder(warehouseId, 250m);

            sw.Restart();
            var result = await handler.Handle(new CompleteOrderCommand(order.Id.Value), CancellationToken.None);
            sw.Stop();

            latencies.Add(sw.Elapsed.TotalMilliseconds);
            Assert.Equal("Completed", result.Status);
        }

        var (avg, p50, p95, p99, max, total) = CalculatePercentiles(latencies);
        output.WriteLine($"BENCHMARK [500 Standard Sales]: Total={total:F2}ms, Avg={avg:F3}ms, P50={p50:F3}ms, P95={p95:F3}ms, P99={p99:F3}ms, Max={max:F3}ms");
        Assert.True(p95 < 20.0, $"Expected P95 < 20ms, got {p95}ms");
    }

    [Fact]
    public async Task Benchmark_100_Sales_QuickBooks_Outage()
    {
        var fixture = new BenchmarkFixture();
        var handler = fixture.CreateHandler();
        var warehouseId = WarehouseId.New();
        var latencies = new List<double>(100);

        var qbGateway = new DefaultQuickBooksGateway();
        qbGateway.SetSimulatedOutage(true);
        var cbRegistry = new CircuitBreakerRegistry();
        var qbHandler = new QuickBooksSyncOutboxHandler(qbGateway, cbRegistry, NullLogger<QuickBooksSyncOutboxHandler>.Instance);

        var sw = new Stopwatch();

        for (int i = 0; i < 100; i++)
        {
            var (order, _) = fixture.CreateSampleOrder(warehouseId, 1200m);

            sw.Restart();
            var result = await handler.Handle(new CompleteOrderCommand(order.Id.Value), CancellationToken.None);
            sw.Stop();

            latencies.Add(sw.Elapsed.TotalMilliseconds);
            Assert.Equal("Completed", result.Status);

            var qbMsg = fixture.Outbox.AllMessages
                .FirstOrDefault(m => m.MessageType == OutboxMessageType.QuickBooksSync && m.AggregateId == order.Id.Value.ToString());
            Assert.NotNull(qbMsg);

            try
            {
                await qbHandler.HandleAsync(qbMsg, CancellationToken.None);
            }
            catch
            {
                qbMsg.ScheduleRetry("QB offline", maxAttempts: 5);
            }
        }

        var (avg, p50, p95, p99, max, total) = CalculatePercentiles(latencies);
        output.WriteLine($"BENCHMARK [100 Sales with QuickBooks Outage]: Total={total:F2}ms, Avg={avg:F3}ms, P50={p50:F3}ms, P95={p95:F3}ms, P99={p99:F3}ms, Max={max:F3}ms");
        Assert.True(p95 < 20.0, $"Expected cashier P95 < 20ms during QB outage, got {p95}ms");
    }

    [Fact]
    public async Task Benchmark_100_Sales_Printer_Offline()
    {
        var fixture = new BenchmarkFixture();
        var handler = fixture.CreateHandler();
        var warehouseId = WarehouseId.New();
        var latencies = new List<double>(100);

        var printService = new DefaultReceiptPrintService();
        printService.SetSimulatedOutage(true);
        var cbRegistry = new CircuitBreakerRegistry();
        var printHandler = new ReceiptPrintOutboxHandler(printService, cbRegistry, NullLogger<ReceiptPrintOutboxHandler>.Instance);

        var sw = new Stopwatch();

        for (int i = 0; i < 100; i++)
        {
            var (order, _) = fixture.CreateSampleOrder(warehouseId, 450m);

            sw.Restart();
            var result = await handler.Handle(new CompleteOrderCommand(order.Id.Value), CancellationToken.None);
            sw.Stop();

            latencies.Add(sw.Elapsed.TotalMilliseconds);
            Assert.Equal("Completed", result.Status);

            var printMsg = fixture.Outbox.AllMessages
                .FirstOrDefault(m => m.MessageType == OutboxMessageType.ReceiptPrint && m.AggregateId == order.Id.Value.ToString());
            Assert.NotNull(printMsg);

            try
            {
                await printHandler.HandleAsync(printMsg, CancellationToken.None);
            }
            catch
            {
                printMsg.ScheduleRetry("Printer offline", maxAttempts: 5);
            }
        }

        var (avg, p50, p95, p99, max, total) = CalculatePercentiles(latencies);
        output.WriteLine($"BENCHMARK [100 Sales with Printer Offline]: Total={total:F2}ms, Avg={avg:F3}ms, P50={p50:F3}ms, P95={p95:F3}ms, P99={p99:F3}ms, Max={max:F3}ms");
        Assert.True(p95 < 20.0, $"Expected cashier P95 < 20ms during printer outage, got {p95}ms");
    }

    [Fact]
    public async Task Benchmark_100_Emergency_Sales_Continuity_Mode()
    {
        var journalStore = new FakeContinuityJournalStore();
        var branchId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var latencies = new List<double>(100);

        var sw = new Stopwatch();

        for (int i = 0; i < 100; i++)
        {
            sw.Restart();
            var txId = Guid.NewGuid();
            var seq = await journalStore.GetNextSequenceNumberAsync();
            var prevHash = await journalStore.GetLastTransactionHashAsync();
            var now = DateTimeOffset.UtcNow;

            var checksum = EmergencyTransaction.ComputeChecksum(
                txId, seq, now, "POS-01", branchId, warehouseId, "Cashier", 100m, "Cash");
            var hmac = EmergencyTransaction.ComputeHmacSignature(
                txId, seq, prevHash, now, "POS-01", branchId, warehouseId, "Cashier", 100m, "Cash");

            var snapshot = new EmergencyOrderSnapshot(
                "TakeAway", null, null,
                [new EmergencyTransactionLine(Guid.NewGuid(), "SKU-BREAD", "Naan", 2, 50m, 100m, null)],
                100m, 0, 0, 0, 100m, null, null);

            var tx = new EmergencyTransaction
            {
                TransactionId = txId,
                SequenceNumber = seq,
                PreviousTransactionHash = prevHash,
                TimestampUtc = now,
                TerminalId = "POS-01",
                BranchId = branchId,
                WarehouseId = warehouseId,
                CashierName = "Cashier",
                OrderSnapshot = snapshot,
                PaymentType = "Cash",
                AmountTendered = 100m,
                ChangeGiven = 0,
                Checksum = checksum,
                HmacSignature = hmac
            };

            await journalStore.AppendAsync(tx);
            sw.Stop();

            latencies.Add(sw.Elapsed.TotalMilliseconds);
        }

        var (avg, p50, p95, p99, max, total) = CalculatePercentiles(latencies);
        output.WriteLine($"BENCHMARK [100 Continuity Mode Emergency Sales]: Total={total:F2}ms, Avg={avg:F3}ms, P50={p50:F3}ms, P95={p95:F3}ms, P99={p99:F3}ms, Max={max:F3}ms");
        Assert.True(p95 < 15.0, $"Expected Continuity P95 < 15ms, got {p95}ms");
    }
}
