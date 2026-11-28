using System;
using System.Collections.Generic;
using System.Linq;
using Clovent.Domain;

namespace Clovent.Inventory.Recipes;

/// <summary>
/// A restaurant recipe / bill of materials (BOM) linking a prepared sellable menu item
/// (<see cref="Catalog.Variants.ProductVariant"/>) to its constituent raw ingredients,
/// portion sizes, trim/cooking loss factors, modifiers, and version history.
/// </summary>
public sealed class Recipe : AggregateRoot<RecipeId>
{
    private readonly List<RecipeIngredient> _ingredients = [];
    private readonly List<RecipeModifier> _modifiers = [];
    private readonly List<RecipeVersion> _versions = [];

    /// <summary>The finished sellable product variant prepared by this recipe.</summary>
    public Guid ProductVariantId { get; }

    /// <summary>The name of the recipe (e.g. "Signature Beef Burger").</summary>
    public string RecipeName { get; private set; }

    /// <summary>Quantity of portions/units yielded per preparation run.</summary>
    public decimal YieldQuantity { get; private set; }

    /// <summary>Unit of measure for the yield (e.g. "Portion", "Item", "Serving", "Batch").</summary>
    public string YieldUnit { get; private set; }

    /// <summary>Sequential version number incremented upon ingredient composition changes.</summary>
    public int Version { get; private set; }

    /// <summary>Alias for Version.</summary>
    public int VersionNumber => Version;

    /// <summary>Whether this recipe is currently active for food costing and stock deduction.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Preparation, chef assembly, or kitchen execution notes.</summary>
    public string? PreparationNotes { get; private set; }

    /// <summary>The constituent raw ingredients required for this recipe.</summary>
    public IReadOnlyList<RecipeIngredient> Ingredients => _ingredients.AsReadOnly();

    /// <summary>Optional modifier ingredient consumption rules (e.g. Extra Cheese, Extra Sauce).</summary>
    public IReadOnlyList<RecipeModifier> Modifiers => _modifiers.AsReadOnly();

    /// <summary>Historical version snapshots of this recipe for auditability and retrospective costing.</summary>
    public IReadOnlyList<RecipeVersion> Versions => _versions.AsReadOnly();

    /// <summary>Alias for Versions.</summary>
    public IReadOnlyList<RecipeVersion> VersionHistory => Versions;

    /// <summary>UTC instant this recipe was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>UTC instant this recipe was last modified.</summary>
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>Constructor for persistence restoration.</summary>
    public Recipe(
        RecipeId id,
        Guid productVariantId,
        string recipeName,
        decimal yieldQuantity,
        string yieldUnit,
        int version,
        bool isActive,
        string? preparationNotes,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        IEnumerable<RecipeIngredient>? ingredients = null,
        IEnumerable<RecipeModifier>? modifiers = null,
        IEnumerable<RecipeVersion>? versions = null)
    {
        if (yieldQuantity <= 0m)
            throw new ArgumentOutOfRangeException(nameof(yieldQuantity), yieldQuantity, "Recipe yield quantity must be strictly positive.");

        Id = id;
        ProductVariantId = productVariantId;
        RecipeName = recipeName ?? throw new ArgumentNullException(nameof(recipeName));
        YieldQuantity = yieldQuantity;
        YieldUnit = string.IsNullOrWhiteSpace(yieldUnit) ? "Portion" : yieldUnit.Trim();
        Version = version > 0 ? version : 1;
        IsActive = isActive;
        PreparationNotes = preparationNotes;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;

        if (ingredients != null) _ingredients.AddRange(ingredients);
        if (modifiers != null) _modifiers.AddRange(modifiers);
        if (versions != null) _versions.AddRange(versions);
    }

    /// <summary>Creates a new active recipe for a product variant with an explicit ID.</summary>
    public static Recipe Create(
        RecipeId id,
        Guid productVariantId,
        string recipeName,
        decimal yieldQuantity = 1.0m,
        string yieldUnit = "Portion",
        string? preparationNotes = null)
    {
        if (productVariantId == Guid.Empty)
            throw new ArgumentException("ProductVariantId cannot be empty.", nameof(productVariantId));
        if (string.IsNullOrWhiteSpace(recipeName))
            throw new ArgumentException("Recipe name is required.", nameof(recipeName));
        if (yieldQuantity <= 0m)
            throw new ArgumentOutOfRangeException(nameof(yieldQuantity), yieldQuantity, "Yield quantity must be positive.");

        var now = DateTimeOffset.UtcNow;
        return new Recipe(
            id,
            productVariantId,
            recipeName.Trim(),
            yieldQuantity,
            yieldUnit,
            version: 1,
            isActive: true,
            preparationNotes: preparationNotes,
            createdAtUtc: now,
            updatedAtUtc: now);
    }

    /// <summary>Creates a new active recipe for a product variant.</summary>
    public static Recipe Create(
        Guid productVariantId,
        string recipeName,
        decimal yieldQuantity = 1.0m,
        string yieldUnit = "Portion",
        string? preparationNotes = null) =>
        Create(RecipeId.New(), productVariantId, recipeName, yieldQuantity, yieldUnit, preparationNotes);

    /// <summary>Convenience method to add or update an ingredient.</summary>
    public void AddOrUpdateIngredient(
        Guid ingredientVariantId,
        string ingredientName,
        decimal quantity,
        string unitOfMeasureCode,
        decimal wastagePercentage = 0m,
        decimal? costPerUnitOverride = null)
    {
        SetIngredient(new RecipeIngredient(
            ingredientVariantId,
            ingredientName,
            quantity,
            unitOfMeasureCode,
            wastagePercentage,
            costPerUnitOverride));
    }

    /// <summary>Convenience method to add or update a modifier ingredient rule.</summary>
    public void AddModifier(
        string modifierName,
        Guid ingredientVariantId,
        string ingredientName,
        decimal quantity,
        string unitOfMeasureCode,
        decimal wastagePercentage = 0m,
        decimal? costPerUnitOverride = null)
    {
        SetModifier(new RecipeModifier(
            modifierName,
            ingredientVariantId,
            ingredientName,
            quantity,
            unitOfMeasureCode,
            wastagePercentage,
            costPerUnitOverride));
    }

    /// <summary>Convenience method to snapshot the current version and optionally update yield.</summary>
    public void CreateNewVersion(decimal? yieldQuantity = null, string? yieldUnit = null)
    {
        CreateNewVersionSnapshot();
        if (yieldQuantity.HasValue || !string.IsNullOrWhiteSpace(yieldUnit))
        {
            UpdateDetails(RecipeName, yieldQuantity ?? YieldQuantity, yieldUnit ?? YieldUnit, PreparationNotes);
        }
    }

    /// <summary>Adds or replaces an ingredient in the recipe.</summary>
    public void SetIngredient(RecipeIngredient ingredient)
    {
        ArgumentNullException.ThrowIfNull(ingredient);

        if (ingredient.Quantity <= 0m)
            throw new ArgumentOutOfRangeException(nameof(ingredient.Quantity), ingredient.Quantity, "Ingredient quantity must be positive.");

        _ingredients.RemoveAll(i => i.IngredientVariantId == ingredient.IngredientVariantId);
        _ingredients.Add(ingredient);
        Touch();
    }

    /// <summary>Removes an ingredient by variant ID.</summary>
    public bool RemoveIngredient(Guid ingredientVariantId)
    {
        var removed = _ingredients.RemoveAll(i => i.IngredientVariantId == ingredientVariantId) > 0;
        if (removed) Touch();
        return removed;
    }

    /// <summary>Adds or replaces a modifier ingredient rule.</summary>
    public void SetModifier(RecipeModifier modifier)
    {
        ArgumentNullException.ThrowIfNull(modifier);

        if (modifier.Quantity <= 0m)
            throw new ArgumentOutOfRangeException(nameof(modifier.Quantity), modifier.Quantity, "Modifier quantity must be positive.");

        _modifiers.RemoveAll(m => string.Equals(m.ModifierName, modifier.ModifierName, StringComparison.OrdinalIgnoreCase));
        _modifiers.Add(modifier);
        Touch();
    }

    /// <summary>Removes a modifier by name.</summary>
    public bool RemoveModifier(string modifierName)
    {
        var removed = _modifiers.RemoveAll(m => string.Equals(m.ModifierName, modifierName, StringComparison.OrdinalIgnoreCase)) > 0;
        if (removed) Touch();
        return removed;
    }

    /// <summary>Updates recipe name, yield, and kitchen preparation notes.</summary>
    public void UpdateDetails(string recipeName, decimal yieldQuantity, string yieldUnit, string? notes)
    {
        if (string.IsNullOrWhiteSpace(recipeName))
            throw new ArgumentException("Recipe name is required.", nameof(recipeName));
        if (yieldQuantity <= 0m)
            throw new ArgumentOutOfRangeException(nameof(yieldQuantity), yieldQuantity, "Yield quantity must be positive.");

        RecipeName = recipeName.Trim();
        YieldQuantity = yieldQuantity;
        YieldUnit = string.IsNullOrWhiteSpace(yieldUnit) ? "Portion" : yieldUnit.Trim();
        PreparationNotes = notes;
        Touch();
    }

    /// <summary>Snapshots the current recipe version and increments the version counter.</summary>
    public void CreateNewVersionSnapshot()
    {
        var snapshot = new RecipeVersion(
            Version,
            DateTimeOffset.UtcNow,
            YieldQuantity,
            YieldUnit,
            _ingredients.ToList(),
            _modifiers.ToList());

        _versions.Add(snapshot);
        Version++;
        Touch();
    }

    /// <summary>Activates the recipe for sales deduction.</summary>
    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    /// <summary>Deactivates the recipe, suppressing ingredient deduction.</summary>
    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
