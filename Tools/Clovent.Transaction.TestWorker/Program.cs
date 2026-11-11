using System.Text.Json;
using System.Text.RegularExpressions;
using Clovent.Restaurant.Application;
using Clovent.Restaurant.Application.Concurrency;
using Clovent.Restaurant.Application.Payments.Commands;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.Discounts;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Infrastructure.Repositories;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.ServiceCharges;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// Test-only worker: accepts a GUID-scoped database name, never a connection string.
if (args.Length != 7 || !Regex.IsMatch(args[0], "^CBOS_CrossProcess_[a-f0-9]{32}$"))
    throw new InvalidOperationException("Only a GUID-isolated LocalDB test target is accepted.");
var options = new DbContextOptionsBuilder<RestaurantDbContext>()
    .UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database={args[0]};Integrated Security=true;TrustServerCertificate=true").Options;
var services = new ServiceCollection();
services.AddSingleton(options);
services.AddScoped<RestaurantDbContext>();
services.AddScoped<ITransactionLockProvider, SqlTransactionLockProvider>();
services.AddScoped<IOrderRepository, OrderRepository>();
services.AddScoped<IPaymentRepository, PaymentRepository>();
services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
services.AddScoped<IOrderLineRepository, OrderLineRepository>();
services.AddScoped<IDiscountRepository, DiscountRepository>();
services.AddScoped<IServiceChargeRepository, ServiceChargeRepository>();
services.AddScoped<ICustomerRepository, CustomerRepository>();
services.AddScoped<ICustomerLedgerEntryRepository, CustomerLedgerEntryRepository>();
services.AddScoped<ICustomerPaymentAllocationRepository, CustomerPaymentAllocationRepository>();
services.AddScoped<Clovent.Restaurant.Shifts.IShiftRepository, ShiftRepository>();
services.AddScoped<Clovent.Restaurant.ActivityLogs.IActivityLogEntryRepository, ActivityLogEntryRepository>();
services.AddScoped(sp => new UnitOfWork(sp.GetRequiredService<RestaurantDbContext>()));
services.AddScoped<IUnitOfWork>(sp => new BarrierUnitOfWork(sp.GetRequiredService<UnitOfWork>(), args[4], args[5].Contains("pause")));
services.AddMediatR(c => c.RegisterServicesFromAssemblyContaining<RecordPaymentCommand>());
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionSerializationBehavior<,>));
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));
using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();
await scope.ServiceProvider.GetRequiredService<RestaurantDbContext>().Orders.AsNoTracking().FirstAsync();
if (!args[5].Contains("pause")) await File.WriteAllTextAsync(Path.Combine(args[4], "second-ready"), "ready");
try
{
    if (args[5] == "close-pause")
    {
        await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new Clovent.Restaurant.Application.Shifts.Commands.CloseShiftCommand(Guid.Parse(args[1]), 0m));
        Console.WriteLine(JsonSerializer.Serialize(new { outcome = "committed", processId = Environment.ProcessId }));
        return;
    }
    var result = await scope.ServiceProvider.GetRequiredService<IMediator>().Send(
        new RecordPaymentCommand(Guid.Parse(args[1]), Guid.Parse(args[2]), 500m,
            ShiftId: string.IsNullOrEmpty(args[6]) ? null : Guid.Parse(args[6]), IdempotencyKey: args[3]));
    Console.WriteLine(JsonSerializer.Serialize(new { outcome = "committed", result.PaymentId, processId = Environment.ProcessId }));
}
catch (Exception ex)
{
    Console.WriteLine(JsonSerializer.Serialize(new { outcome = "rejected", error = ex.GetType().Name, message = ex.Message, processId = Environment.ProcessId }));
}

sealed class BarrierUnitOfWork(UnitOfWork inner, string directory, bool pause) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (!pause) await File.WriteAllTextAsync(Path.Combine(directory, "second-staged"), "staged", cancellationToken);
        if (pause)
        {
            await File.WriteAllTextAsync(Path.Combine(directory, "ready"), "staged", cancellationToken);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            while (!File.Exists(Path.Combine(directory, "release"))) await Task.Delay(20, timeout.Token);
        }
        await inner.SaveChangesAsync(cancellationToken);
    }
}
