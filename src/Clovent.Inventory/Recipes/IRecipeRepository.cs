using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Clovent.Inventory.Recipes;

/// <summary>Repository for persisting and querying <see cref="Recipe"/> aggregates.</summary>
public interface IRecipeRepository
{
    /// <summary>Retrieves a recipe by its strongly-typed identity.</summary>
    Task<Recipe?> GetByIdAsync(RecipeId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves an active recipe for a product variant.</summary>
    Task<Recipe?> GetByProductVariantIdAsync(Guid productVariantId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves all recipes in the system.</summary>
    Task<IReadOnlyList<Recipe>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a newly created recipe aggregate.</summary>
    Task AddAsync(Recipe recipe, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing recipe aggregate.</summary>
    Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default);

    /// <summary>Deletes a recipe by its identifier.</summary>
    Task DeleteAsync(RecipeId id, CancellationToken cancellationToken = default);
}
