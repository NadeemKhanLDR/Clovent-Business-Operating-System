using Clovent.Platform.Bootstrap;
using Clovent.Desktop.Sessions;
using Clovent.Desktop.Restaurant.SmartPos;
using Clovent.Identity.Users;
using Clovent.Identity.Users.ValueObjects;
using Clovent.Restaurant.Application.SmartCombos;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

var b = ApplicationBootstrapper.Create(basePath: Path.GetFullPath("src/Clovent.Desktop")).WithLogging().WithPlatform();
Clovent.Identity.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(b.Services, b.Configuration);
Clovent.Identity.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(b.Services, b.Configuration);
Clovent.MasterData.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(b.Services, b.Configuration);
Clovent.MasterData.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(b.Services, b.Configuration);
Clovent.Catalog.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(b.Services, b.Configuration);
Clovent.Catalog.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(b.Services, b.Configuration);
Clovent.Restaurant.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(b.Services, b.Configuration);
Clovent.Restaurant.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(b.Services, b.Configuration);
Clovent.Identity.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure(b.Services, b.Configuration);
Clovent.Inventory.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(b.Services, b.Configuration);
Clovent.Inventory.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(b.Services, b.Configuration);
Clovent.Inventory.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure(b.Services, b.Configuration);
Clovent.Restaurant.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure(b.Services, b.Configuration);
b.Services.AddSingleton<ICurrentSession, CurrentSession>();
b.Services.AddScoped<ISmartComboAccess, SmartComboAccess>();
b.Services.AddSingleton(new SmartComboOptions { IncludeDevelopmentSamples = false }); // Test WITHOUT development samples!

using var host = b.Build();
using var scope = host.Services.CreateScope();
var sp = scope.ServiceProvider;
var admin = await sp.GetRequiredService<IUserRepository>().GetByUserNameAsync(UserName.Create("admin")) ?? throw new Exception("Admin not found.");
sp.GetRequiredService<ICurrentSession>().SignIn(admin.Id.Value, Guid.NewGuid(), "admin");

var access = sp.GetRequiredService<ISmartComboAccess>();
var locations = await access.LocationsAsync(default);
var warehouse = locations.First().Id;
var now = DateTimeOffset.UtcNow;
var from = now.AddDays(-30);

var db = sp.GetRequiredService<RestaurantDbContext>();
var totalInPeriod = await db.Orders.CountAsync(o => o.WarehouseId == new Clovent.MasterData.Warehouses.WarehouseId(warehouse) && o.UpdatedAtUtc >= from && o.UpdatedAtUtc < now);
var completedInPeriod = await db.Orders.CountAsync(o => o.WarehouseId == new Clovent.MasterData.Warehouses.WarehouseId(warehouse) && o.Status == OrderStatus.Completed && o.UpdatedAtUtc >= from && o.UpdatedAtUtc < now);
var devOrdersCount = await db.Orders.CountAsync(o => o.WarehouseId == new Clovent.MasterData.Warehouses.WarehouseId(warehouse) && o.Status == OrderStatus.Completed && o.UpdatedAtUtc >= from && o.UpdatedAtUtc < now && o.Notes != null && o.Notes.StartsWith("DEV-SMARTCOMBO:"));
var qaNotesCount = await db.Orders.CountAsync(o => o.WarehouseId == new Clovent.MasterData.Warehouses.WarehouseId(warehouse) && o.Status == OrderStatus.Completed && o.UpdatedAtUtc >= from && o.UpdatedAtUtc < now && o.Notes != null && (o.Notes.StartsWith("QA") || o.Notes.StartsWith("TEST") || o.Notes.StartsWith("[QA]")));

var store = sp.GetRequiredService<ISmartComboStore>();
var basketsNoDev = await store.ReadBasketsAsync(warehouse, from, now, 50000, default, includeDevelopmentSamples: false);
var basketsWithDev = await store.ReadBasketsAsync(warehouse, from, now, 50000, default, includeDevelopmentSamples: true);

Console.WriteLine($"--- DIAGNOSTIC DATA ---");
Console.WriteLine($"Total orders in period: {totalInPeriod}");
Console.WriteLine($"Completed in period: {completedInPeriod}");
Console.WriteLine($"Orders with QA/TEST notes: {qaNotesCount}");
Console.WriteLine($"Orders with DEV-SMARTCOMBO: notes: {devOrdersCount}");
Console.WriteLine($"Eligible orders (IncludeDevelopmentSamples=false): {basketsNoDev.Count}");
Console.WriteLine($"Eligible orders (IncludeDevelopmentSamples=true): {basketsWithDev.Count}");

// Analyze basketsNoDev
var service = sp.GetRequiredService<SmartComboService>();
var resultNoDev = await service.AnalyzeAsync(warehouse, 30, default);
Console.WriteLine($"Analysis result WITHOUT dev samples: Opportunities = {resultNoDev.Opportunities.Count}, Eligible = {resultNoDev.EligibleOrders}");

// Basket variant statistics on basketsNoDev
var variantOccurrences = basketsNoDev.SelectMany(b => b.Variants).GroupBy(v => v).ToDictionary(g => g.Key, g => g.Count());
Console.WriteLine($"Unique variants in baskets (no dev): {variantOccurrences.Count}");
var topVariants = variantOccurrences.OrderByDescending(x => x.Value).Take(10);
foreach (var kvp in topVariants)
{
    Console.WriteLine($"  Variant {kvp.Key}: {kvp.Value} orders");
}

// Check pair frequencies
var pairCounts = new Dictionary<string, int>();
foreach (var bkt in basketsNoDev)
{
    var vars = bkt.Variants.Distinct().Order().ToArray();
    for (int i = 0; i < vars.Length; i++)
        for (int j = i + 1; j < vars.Length; j++)
        {
            var key = $"{vars[i]:N}|{vars[j]:N}";
            pairCounts[key] = pairCounts.TryGetValue(key, out var c) ? c + 1 : 1;
        }
}
Console.WriteLine($"Unique pairs generated (no dev): {pairCounts.Count}");
var pairsMeetingMinFreq = pairCounts.Where(p => p.Value >= 3).ToList();
Console.WriteLine($"Pairs with frequency >= 3 (no dev): {pairsMeetingMinFreq.Count}");
foreach (var p in pairsMeetingMinFreq)
{
    Console.WriteLine($"  Pair {p.Key}: {p.Value} times");
}

// Check triple frequencies
var tripleCounts = new Dictionary<string, int>();
foreach (var bkt in basketsNoDev)
{
    var vars = bkt.Variants.Distinct().Order().ToArray();
    for (int i = 0; i < vars.Length; i++)
        for (int j = i + 1; j < vars.Length; j++)
            for (int k = j + 1; k < vars.Length; k++)
            {
                var key = $"{vars[i]:N}|{vars[j]:N}|{vars[k]:N}";
                tripleCounts[key] = tripleCounts.TryGetValue(key, out var c) ? c + 1 : 1;
            }
}
Console.WriteLine($"Unique triples generated (no dev): {tripleCounts.Count}");
var triplesMeetingMinFreq = tripleCounts.Where(p => p.Value >= 3).ToList();
Console.WriteLine($"Triples with frequency >= 3 (no dev): {triplesMeetingMinFreq.Count}");

File.WriteAllText("scratch/diagnostic_summary.txt", 
$@"Total orders in period: {totalInPeriod}
Completed in period: {completedInPeriod}
Orders with QA/TEST notes: {qaNotesCount}
DEV-SMARTCOMBO orders: {devOrdersCount}
Eligible orders (IncludeDevelopmentSamples=false): {basketsNoDev.Count}
Eligible orders (IncludeDevelopmentSamples=true): {basketsWithDev.Count}
Unique pairs generated: {pairCounts.Count}
Pairs with freq >= 3: {pairsMeetingMinFreq.Count}
Unique triples generated: {tripleCounts.Count}
Triples with freq >= 3: {triplesMeetingMinFreq.Count}
");
