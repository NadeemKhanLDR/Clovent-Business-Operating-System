using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Catalog.Application.Variants.Dtos;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Desktop.Inventory.Recipes;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Inventory.Application.Recipes.Dtos;
using Clovent.Inventory.Application.Recipes.Queries;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Desktop.Tests.Inventory.Recipes;

public class RecipeManagementViewTests
{
    private sealed class FakeSession : ICurrentSession
    {
        public Guid? UserId { get; set; } = Guid.NewGuid();
        public Guid? SessionId { get; set; } = Guid.NewGuid();
        public string? DisplayName { get; set; } = "Chef / Recipe Manager";
        public bool IsAuthenticated => UserId.HasValue;
        public void SignIn(Guid userId, Guid sessionId, string displayName) { }
        public void SignOut() { }
#pragma warning disable CS0067
        public event EventHandler? Changed;
#pragma warning restore CS0067
    }

    private sealed class RecipeTestMediator : IMediator
    {
        public List<RecipeDto> Recipes { get; set; } = [];
        public List<ProductVariantDto> Variants { get; set; } = [];
        public Dictionary<Guid, RecipeCostDto> CostsByRecipe { get; set; } = [];

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is ListRecipesQuery)
            {
                return Task.FromResult((TResponse)(object)Recipes.AsReadOnly());
            }

            if (request is ListProductVariantsQuery)
            {
                return Task.FromResult((TResponse)(object)Variants.AsReadOnly());
            }

            if (request is CalculateRecipeCostQuery cq && CostsByRecipe.TryGetValue(cq.RecipeId, out var cost))
            {
                return Task.FromResult((TResponse)(object)cost);
            }

            return Task.FromResult(default(TResponse)!);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest => Task.CompletedTask;
        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => Task.FromResult<object?>(null);
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    [Fact]
    public void RecipeRow_CostCalculations_And_AlertBadge_CorrectlyReflected()
    {
        // Scenario 1: Normal food cost (30% <= 35%)
        var healthyRow = new RecipeRow(
            RecipeId: Guid.NewGuid(),
            RecipeName: "Gourmet Burger",
            ProductVariantId: Guid.NewGuid(),
            ProductVariantName: "Classic Beef",
            YieldQuantity: 1m,
            YieldUnit: "Portion",
            Version: 1,
            IsActive: true,
            IngredientCount: 5,
            ModifierCount: 2,
            CostPerPortion: 4.50m,
            SellingPrice: 15.00m,
            FoodCostPercentage: 30.0m,
            GrossMarginPercentage: 70.0m,
            IsAlert: false);

        Assert.False(healthyRow.IsAlert);
        Assert.Equal("1 Portion", healthyRow.YieldDisplay);
        Assert.Equal("Active", healthyRow.Status);
        Assert.Equal(30.0m, healthyRow.FoodCostPercentage);
        Assert.Equal(70.0m, healthyRow.GrossMarginPercentage);

        // Scenario 2: High food cost (42% > 35%)
        var warningRow = new RecipeRow(
            RecipeId: Guid.NewGuid(),
            RecipeName: "Ribeye Steak 300g",
            ProductVariantId: Guid.NewGuid(),
            ProductVariantName: "Prime Cut",
            YieldQuantity: 1m,
            YieldUnit: "Portion",
            Version: 2,
            IsActive: true,
            IngredientCount: 4,
            ModifierCount: 1,
            CostPerPortion: 16.80m,
            SellingPrice: 40.00m,
            FoodCostPercentage: 42.0m,
            GrossMarginPercentage: 58.0m,
            IsAlert: true);

        Assert.True(warningRow.IsAlert);
        Assert.Equal(42.0m, warningRow.FoodCostPercentage);
    }

    private sealed class FakeFeaturePolicy : IFeatureAuthorizationPolicy
    {
        public Task<bool> CanUseFeatureAsync(Guid userId, string featureKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class FakeServiceScope(IServiceProvider serviceProvider) : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = serviceProvider;
        public void Dispose() { }
    }

    private sealed class FakeServiceScopeFactory(IServiceProvider serviceProvider) : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new FakeServiceScope(serviceProvider);
    }

    [Fact]
    public void RecipeManagementView_CanInstantiate_WithInjectedServices()
    {
        var services = new ServiceCollection();
        var mediator = new RecipeTestMediator();
        services.AddSingleton<IMediator>(mediator);
        services.AddSingleton<IFeatureAuthorizationPolicy>(new FakeFeaturePolicy());

        var provider = services.BuildServiceProvider();
        var scopeFactory = new FakeServiceScopeFactory(provider);
        var session = new FakeSession();

        using var view = new RecipeManagementView(scopeFactory, session);
        Assert.NotNull(view);
        Assert.NotNull(view.Controls);
    }
}
