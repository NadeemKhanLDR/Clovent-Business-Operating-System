using System.Diagnostics;
using System.Text.Json;
using Clovent.Catalog.Variants;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Infrastructure.Tests.TestSupport;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Concurrency;

public sealed class CrossProcessPaymentTests
{
    [Theory]
    [InlineData("different-keys")]
    [InlineData("same-key")]
    [InlineData("shared-customer")]
    [InlineData("closing-shift")]
    public async Task IndependentProcesses_RespectOrderCustomerAndShiftCommitBoundaries(string scenario)
    {
        var sameKey = scenario == "same-key";
        var database = $"CBOS_CrossProcess_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<RestaurantDbContext>().UseSqlServer(SqlTestDatabaseGuard.ConnectionString(database)).Options;
        var directory = Path.Combine(Path.GetTempPath(), $"cbos-process-barrier-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        await using var db = new RestaurantDbContext(options);
        Process? first = null, second = null;
        try
        {
            await db.Database.EnsureCreatedAsync();
            await SqlTestDatabaseGuard.CreatedAsync(db);
            var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
            var line = OrderLine.Create(order.Id, ProductVariantId.New(), 1m, 500m, 0m, false);
            order.AddOrderLine(line.Id);
            var method = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
            db.AddRange(order, line, method);
            var secondOrderId = order.Id.Value;
            Guid? shiftId = null;
            if (scenario == "shared-customer")
            {
                var customer = Clovent.Restaurant.Customers.Customer.Create(Clovent.MasterData.Shared.ValueObjects.EntityCode.Create("PROCESS-CUSTOMER"),
                    "Synthetic customer", "03000000000", "Synthetic address", null, 0m, 500m, null);
                order.SetCustomer(customer.Id);
                method.Rename(PaymentMethodName.Create("On Account"));
                var other = Order.Create(OrderType.TakeAway, order.WarehouseId);
                other.SetCustomer(customer.Id);
                var otherLine = OrderLine.Create(other.Id, ProductVariantId.New(), 1m, 500m, 0m, false);
                other.AddOrderLine(otherLine.Id);
                db.AddRange(customer, other, otherLine);
                secondOrderId = other.Id.Value;
            }
            if (scenario == "closing-shift")
            {
                var shift = Clovent.Restaurant.Shifts.Shift.Open(1, new(Guid.NewGuid()), order.WarehouseId, new(Guid.NewGuid()), new(Guid.NewGuid()), "Synthetic cashier", 0m);
                db.Shifts.Add(shift);
                shiftId = shift.Id.Value;
            }
            await db.SaveChangesAsync();
            Process Start(Guid targetId, string key, string mode)
            {
                var info = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
                foreach (var argument in new[] { Path.Combine(AppContext.BaseDirectory, "TransactionWorker", "Clovent.Transaction.TestWorker.dll"), database,
                    targetId.ToString(), method.Id.Value.ToString(), key, directory, mode, shiftId?.ToString() ?? "" }) info.ArgumentList.Add(argument);
                return Process.Start(info)!;
            }
            first = Start(shiftId ?? order.Id.Value, "first", scenario == "closing-shift" ? "close-pause" : "pause");
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                while (!File.Exists(Path.Combine(directory, "ready"))) await Task.Delay(20, timeout.Token);
            second = Start(secondOrderId, sameKey ? "first" : "second", "run");
            Assert.NotEqual(first.Id, second.Id);
            // First request is staged but not committed; another OS process
            // cannot share the in-process semaphores.
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                while (!File.Exists(Path.Combine(directory, "second-ready"))) await Task.Delay(20, timeout.Token);
            var deadline = Stopwatch.StartNew();
            while (!File.Exists(Path.Combine(directory, "second-staged")) && !second.HasExited && deadline.Elapsed < TimeSpan.FromSeconds(3))
                await Task.Delay(20);
            Assert.False(File.Exists(Path.Combine(directory, "second-staged")));
            Assert.False(second.HasExited);
            await File.WriteAllTextAsync(Path.Combine(directory, "release"), "commit");
            await Task.WhenAll(first.WaitForExitAsync(), second.WaitForExitAsync()).WaitAsync(TimeSpan.FromSeconds(25));
            var firstText = await first.StandardOutput.ReadToEndAsync();
            var secondText = await second.StandardOutput.ReadToEndAsync();
            Assert.Equal(0, first.ExitCode);
            Assert.Equal(0, second.ExitCode);
            using var firstResult = JsonDocument.Parse(firstText);
            using var secondResult = JsonDocument.Parse(secondText);
            Assert.Equal("committed", firstResult.RootElement.GetProperty("outcome").GetString());
            await using var verify = new RestaurantDbContext(options);
            if (scenario == "closing-shift")
            {
                Assert.Empty(await verify.Payments.ToListAsync());
                Assert.Equal(Clovent.Restaurant.Shifts.ShiftStatus.Closed, (await verify.Shifts.SingleAsync()).Status);
            }
            else Assert.Equal(500m, Assert.Single(await verify.Payments.ToListAsync()).Amount);
            if (scenario == "shared-customer")
            {
                Assert.Equal(500m, (await verify.Customers.SingleAsync()).OutstandingBalance);
                Assert.Single(await verify.CustomerLedgerEntries.ToListAsync());
            }
            Assert.Equal(sameKey ? "committed" : "rejected", secondResult.RootElement.GetProperty("outcome").GetString());
            if (sameKey) Assert.Equal(firstResult.RootElement.GetProperty("PaymentId").GetGuid(), secondResult.RootElement.GetProperty("PaymentId").GetGuid());
        }
        finally
        {
            foreach (var child in new[] { first, second })
            {
                if (child is null) continue;
                if (!child.HasExited) { child.Kill(entireProcessTree: true); await child.WaitForExitAsync(); }
                child.Dispose();
            }
            await SqlTestDatabaseGuard.DeleteAndVerifyAsync(db);
            if (!Path.GetFullPath(directory).StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Unsafe cleanup path");
            Directory.Delete(directory, recursive: true);
        }
    }
}
