using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Catalog.Application.Variants.Dtos;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.MasterData;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Inventory.Application.Recipes.Commands;
using Clovent.Inventory.Application.Recipes.Dtos;
using Clovent.Inventory.Application.Recipes.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Inventory.Recipes;

/// <summary>
/// Recipe &amp; Food Costing back-office screen: view, search, create, edit, activate/deactivate,
/// and review theoretical costing for restaurant recipes and bills of materials.
/// </summary>
[DesignerCategory("Code")]
public sealed partial class RecipeManagementView : XtraUserControl
{
    private const string FeatureCode = "recipes";

    private readonly IServiceScope _scope;
    private readonly ScreenOperationGate _gate = new();
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;

    private Dictionary<Guid, ProductVariantDto> _variantsById = [];

    /// <summary>Design-time constructor.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public RecipeManagementView()
    {
        _scope = null!;
        _mediator = null!;
        _featurePolicy = null!;
        _currentSession = null!;
        InitializeComponent();
    }

    /// <summary>Initializes a new instance of <see cref="RecipeManagementView"/>.</summary>
    public RecipeManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        InitializeComponent();

        if (DesignModeHelper.IsInDesignMode)
        {
            _scope = null!;
            _mediator = null!;
            _featurePolicy = null!;
            _currentSession = null!;
            return;
        }

        _scope = scopeFactory.CreateScope();
        _mediator = new SerializedMediator(_scope.ServiceProvider.GetRequiredService<IMediator>(), _gate);
        _featurePolicy = new SerializedFeatureAuthorizationPolicy(_scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>(), _gate);
        _currentSession = currentSession;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _scope?.Dispose();
            _gate?.Dispose();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private async Task<IReadOnlyList<RecipeRow>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        await LoadVariantsAsync(cancellationToken);

        var recipes = await _mediator.Send(new ListRecipesQuery(), cancellationToken);

        var rows = new List<RecipeRow>();
        foreach (var r in recipes)
        {
            string variantName = _variantsById.TryGetValue(r.ProductVariantId, out var v)
                ? (!string.IsNullOrWhiteSpace(v.ProductName) ? $"{v.ProductName} - {v.Name}" : v.Name)
                : $"Variant {r.ProductVariantId.ToString()[..8]}";

            decimal costPerPortion = 0m;
            decimal sellingPrice = 0m;
            decimal foodCostPct = 0m;
            decimal grossMarginPct = 0m;
            bool isAlert = false;

            try
            {
                var cost = await _mediator.Send(new CalculateRecipeCostQuery(r.Id), cancellationToken);
                if (cost != null)
                {
                    costPerPortion = cost.CostPerPortion;
                    sellingPrice = cost.SellingPrice;
                    foodCostPct = cost.FoodCostPercentage;
                    grossMarginPct = cost.GrossMarginPercentage;
                    isAlert = cost.FoodCostPercentage > 35m;
                }
            }
            catch
            {
                // Fallback to zeros if catalog prices not reachable
            }

            rows.Add(new RecipeRow(
                r.Id,
                r.RecipeName,
                r.ProductVariantId,
                variantName,
                r.YieldQuantity,
                r.YieldUnit,
                r.Version,
                r.IsActive,
                r.Ingredients.Count,
                r.Modifiers.Count,
                costPerPortion,
                sellingPrice,
                foodCostPct,
                grossMarginPct,
                isAlert));
        }

        return rows;
    }

    private async Task LoadVariantsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var variants = await _mediator.Send(new ListProductVariantsQuery(), cancellationToken);
            _variantsById = variants.ToDictionary(v => v.ProductVariantId);
        }
        catch
        {
            _variantsById = [];
        }
    }

    private async Task CreateRecipeAsync()
    {
        if (!await CanUseFeatureAsync("create"))
        {
            XtraMessageBox.Show(this, "You do not have permission to create recipes.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        await LoadVariantsAsync();

        var sellableOptions = _variantsById.Values
            .Select(v => (v.ProductVariantId, !string.IsNullOrWhiteSpace(v.ProductName) ? $"{v.ProductName} - {v.Name}" : v.Name))
            .ToList();

        var ingredientOptions = _variantsById.Values
            .Select(v => (v.ProductVariantId, !string.IsNullOrWhiteSpace(v.ProductName) ? $"{v.ProductName} - {v.Name}" : v.Name, v.Sku))
            .ToList();

        using var form = new RecipeEditForm(sellableOptions, ingredientOptions);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            try
            {
                var command = new CreateRecipeCommand(
                    form.ProductVariantId!.Value,
                    form.RecipeName,
                    form.YieldQuantity,
                    form.YieldUnit,
                    form.PreparationNotes,
                    form.IngredientInputs,
                    form.ModifierInputs);

                await _mediator.Send(command);
                await _listView.RefreshAsync();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(this, $"Failed to create recipe: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private async Task EditRecipeAsync(RecipeRow row)
    {
        if (!await CanUseFeatureAsync("edit"))
        {
            XtraMessageBox.Show(this, "You do not have permission to edit recipes.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var recipeDto = await _mediator.Send(new GetRecipeByIdQuery(row.RecipeId));
        if (recipeDto is null)
        {
            XtraMessageBox.Show(this, "Recipe no longer exists.", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        await LoadVariantsAsync();

        var sellableOptions = _variantsById.Values
            .Select(v => (v.ProductVariantId, !string.IsNullOrWhiteSpace(v.ProductName) ? $"{v.ProductName} - {v.Name}" : v.Name))
            .ToList();

        var ingredientOptions = _variantsById.Values
            .Select(v => (v.ProductVariantId, !string.IsNullOrWhiteSpace(v.ProductName) ? $"{v.ProductName} - {v.Name}" : v.Name, v.Sku))
            .ToList();

        using var form = new RecipeEditForm(
            sellableOptions,
            ingredientOptions,
            recipeDto,
            id => _mediator.Send(new CalculateRecipeCostQuery(id)));

        if (form.ShowDialog(this) == DialogResult.OK)
        {
            try
            {
                var command = new UpdateRecipeCommand(
                    row.RecipeId,
                    form.RecipeName,
                    form.YieldQuantity,
                    form.YieldUnit,
                    form.PreparationNotes,
                    form.IngredientInputs,
                    form.ModifierInputs,
                    form.CreateNewVersion);

                await _mediator.Send(command);

                if (recipeDto.IsActive != form.IsRecipeActive)
                {
                    await _mediator.Send(new SetRecipeStatusCommand(row.RecipeId, form.IsRecipeActive));
                }

                await _listView.RefreshAsync();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(this, $"Failed to update recipe: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private async Task ViewCostingAsync(RecipeRow row)
    {
        try
        {
            var cost = await _mediator.Send(new CalculateRecipeCostQuery(row.RecipeId));
            if (cost is null)
            {
                XtraMessageBox.Show(this, "Cost calculation unavailable for this recipe.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new RecipeCostDialog(cost, row.RecipeName, row.ProductVariantName);
            dialog.ShowDialog(this);
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show(this, $"Failed to calculate recipe cost: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ToggleActiveAsync(RecipeRow row)
    {
        string action = row.IsActive ? "deactivate" : "activate";
        if (!await CanUseFeatureAsync(action))
        {
            XtraMessageBox.Show(this, $"You do not have permission to {action} recipes.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var recipe = await _mediator.Send(new GetRecipeByIdQuery(row.RecipeId));
        if (recipe is null) return;

        await _mediator.Send(new SetRecipeStatusCommand(row.RecipeId, !row.IsActive));
        await _listView.RefreshAsync();
    }

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession?.UserId is { } userId && _featurePolicy != null
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}")
            : Task.FromResult(true);
}
