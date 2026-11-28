using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Inventory.Recipes;

namespace Clovent.Inventory.Infrastructure.Recipes;

/// <summary>
/// Production-grade JSON file repository implementing <see cref="IRecipeRepository"/>.
/// Guarantees atomic writes (write-to-temp-then-replace), thread-safe in-memory caching,
/// zero database migrations, and safe durability across restarts.
/// </summary>
public sealed class JsonRecipeRepository : IRecipeRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _filePath;
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private readonly ConcurrentDictionary<Guid, Recipe> _cache = new();
    private bool _initialized;

    /// <summary>DTO representation of a recipe for clean JSON serialization.</summary>
    private sealed record RecipeRecord(
        Guid Id,
        Guid ProductVariantId,
        string RecipeName,
        decimal YieldQuantity,
        string YieldUnit,
        int Version,
        bool IsActive,
        string? PreparationNotes,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc,
        List<RecipeIngredient>? Ingredients,
        List<RecipeModifier>? Modifiers,
        List<RecipeVersion>? Versions);

    /// <summary>Initializes a new instance of <see cref="JsonRecipeRepository"/>.</summary>
    public JsonRecipeRepository(string? customFilePath = null)
    {
        if (!string.IsNullOrWhiteSpace(customFilePath))
        {
            _filePath = customFilePath;
        }
        else
        {
            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var baseDir = !string.IsNullOrWhiteSpace(programData)
                ? Path.Combine(programData, "Clovent", "Recipes")
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Recipes");

            _filePath = Path.Combine(baseDir, "recipes.json");
        }
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_initialized) return;

        await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized) return;

            if (File.Exists(_filePath))
            {
                var json = await File.ReadAllTextAsync(_filePath, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var records = JsonSerializer.Deserialize<List<RecipeRecord>>(json, JsonOptions);
                    if (records != null)
                    {
                        foreach (var r in records)
                        {
                            var recipe = new Recipe(
                                new RecipeId(r.Id),
                                r.ProductVariantId,
                                r.RecipeName,
                                r.YieldQuantity,
                                r.YieldUnit,
                                r.Version,
                                r.IsActive,
                                r.PreparationNotes,
                                r.CreatedAtUtc,
                                r.UpdatedAtUtc,
                                r.Ingredients,
                                r.Modifiers,
                                r.Versions);

                            _cache[r.Id] = recipe;
                        }
                    }
                }
            }

            _initialized = true;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task PersistToFileAsync(CancellationToken cancellationToken)
    {
        var records = _cache.Values.Select(r => new RecipeRecord(
            r.Id.Value,
            r.ProductVariantId,
            r.RecipeName,
            r.YieldQuantity,
            r.YieldUnit,
            r.Version,
            r.IsActive,
            r.PreparationNotes,
            r.CreatedAtUtc,
            r.UpdatedAtUtc,
            r.Ingredients.ToList(),
            r.Modifiers.ToList(),
            r.Versions.ToList())).ToList();

        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var tempPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";
        var json = JsonSerializer.Serialize(records, JsonOptions);

        await File.WriteAllTextAsync(tempPath, json, cancellationToken).ConfigureAwait(false);

        if (File.Exists(_filePath))
        {
            File.Replace(tempPath, _filePath, null);
        }
        else
        {
            File.Move(tempPath, _filePath);
        }
    }

    /// <inheritdoc/>
    public async Task<Recipe?> GetByIdAsync(RecipeId id, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        _cache.TryGetValue(id.Value, out var recipe);
        return recipe;
    }

    /// <inheritdoc/>
    public async Task<Recipe?> GetByProductVariantIdAsync(Guid productVariantId, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        return _cache.Values.FirstOrDefault(r => r.ProductVariantId == productVariantId && r.IsActive);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Recipe>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        return _cache.Values.OrderBy(r => r.RecipeName).ToList().AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task AddAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

        await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _cache[recipe.Id.Value] = recipe;
            await PersistToFileAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

        await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _cache[recipe.Id.Value] = recipe;
            await PersistToFileAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(RecipeId id, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

        await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cache.TryRemove(id.Value, out _))
            {
                await PersistToFileAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _fileLock.Release();
        }
    }
}
