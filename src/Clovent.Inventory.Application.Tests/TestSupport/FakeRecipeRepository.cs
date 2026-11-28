using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Inventory.Recipes;

namespace Clovent.Inventory.Application.Tests.TestSupport;

internal sealed class FakeRecipeRepository : IRecipeRepository
{
    private readonly ConcurrentDictionary<Guid, Recipe> _recipes = new();

    public Task<Recipe?> GetByIdAsync(RecipeId id, CancellationToken cancellationToken = default)
    {
        _recipes.TryGetValue(id.Value, out var recipe);
        return Task.FromResult(recipe);
    }

    public Task<Recipe?> GetByProductVariantIdAsync(Guid productVariantId, CancellationToken cancellationToken = default)
    {
        var recipe = _recipes.Values.FirstOrDefault(r => r.ProductVariantId == productVariantId && r.IsActive);
        return Task.FromResult(recipe);
    }

    public Task<IReadOnlyList<Recipe>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Recipe> list = _recipes.Values.ToList().AsReadOnly();
        return Task.FromResult(list);
    }

    public Task AddAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        _recipes[recipe.Id.Value] = recipe;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        _recipes[recipe.Id.Value] = recipe;
        return Task.CompletedTask;
    }

    public Task SaveAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        _recipes[recipe.Id.Value] = recipe;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(RecipeId id, CancellationToken cancellationToken = default)
    {
        _recipes.TryRemove(id.Value, out _);
        return Task.CompletedTask;
    }
}
