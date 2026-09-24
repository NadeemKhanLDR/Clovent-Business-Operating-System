using Clovent.Catalog.Application.Products.Queries;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Platform.Bootstrap;
using Clovent.Restaurant.Application.SmartRecommendations.Queries;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

// Read-only diagnostic: no startup seeds, order writes, rule writes or UI automation.
var bootstrapper = ApplicationBootstrapper.Create(basePath: AppContext.BaseDirectory).WithLogging().WithPlatform();
Clovent.MasterData.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(bootstrapper.Services, bootstrapper.Configuration);
Clovent.MasterData.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(bootstrapper.Services, bootstrapper.Configuration);
Clovent.Catalog.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(bootstrapper.Services, bootstrapper.Configuration);
Clovent.Catalog.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(bootstrapper.Services, bootstrapper.Configuration);
Clovent.Restaurant.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(bootstrapper.Services, bootstrapper.Configuration);
Clovent.Restaurant.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(bootstrapper.Services, bootstrapper.Configuration);
using var host = bootstrapper.Build();
using var scope = host.Services.CreateScope();
var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
var products = await mediator.Send(new ListProductsQuery());
var variants = await mediator.Send(new ListProductVariantsQuery());
var product = products.Single(p => p.Name == "Chicken Biryani");
var variant = variants.First(v => v.ProductId == product.ProductId && v.Status == "Active");
var recommendations = await mediator.Send(new GetBasketRecommendationsQuery(null,
    new[] { variant.ProductVariantId }, DateTime.Now, int.MaxValue));
var remaining = await mediator.Send(new GetBasketRecommendationsQuery(null,
    recommendations.Take(3).Select(r => r.VariantId).Append(variant.ProductVariantId).ToArray(), DateTime.Now, int.MaxValue));
Console.WriteLine(JsonSerializer.Serialize(new
{
    Trigger = product.Name, Portion = variant.Name,
    Recommendations = recommendations,
    BasketExclusionVerified = !remaining.Any(r => recommendations.Take(3).Any(a => a.VariantId == r.VariantId)),
    Remaining = remaining
}, new JsonSerializerOptions { WriteIndented = true }));
