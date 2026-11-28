using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.MasterData;
using Clovent.Inventory.Application.Recipes.Commands;
using Clovent.Inventory.Application.Recipes.Dtos;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Inventory.Recipes;

/// <summary>
/// Comprehensive back-office dialog for creating and editing recipes, ingredient compositions,
/// trim/prep wastage, modifiers, version bumping, and theoretical food costing.
/// </summary>
public sealed partial class RecipeEditForm : XtraForm
{
    private readonly Dictionary<string, Guid?> _variantsByDisplay;
    private readonly IReadOnlyList<(Guid Id, string Display, string Sku)> _ingredientOptions;
    private readonly BindingList<IngredientEditItem> _ingredients = [];
    private readonly BindingList<ModifierEditItem> _modifiers = [];
    private readonly RecipeDto? _existingRecipe;
    private readonly Func<Guid, Task<RecipeCostDto?>>? _costCalculator;

    /// <summary>Design-time constructor.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public RecipeEditForm()
    {
        _variantsByDisplay = null!;
        _ingredientOptions = [];
        InitializeComponent();
        Text = "Recipe Editor";
    }

    /// <summary>Initializes a new instance of <see cref="RecipeEditForm"/>.</summary>
    public RecipeEditForm(
        IReadOnlyList<(Guid Id, string Display)> sellableVariantOptions,
        IReadOnlyList<(Guid Id, string Display, string Sku)> rawIngredientOptions,
        RecipeDto? existingRecipe = null,
        Func<Guid, Task<RecipeCostDto?>>? costCalculator = null)
    {
        _existingRecipe = existingRecipe;
        _ingredientOptions = rawIngredientOptions;
        _costCalculator = costCalculator;

        InitializeComponent();
        Text = existingRecipe is null ? "New Recipe" : $"Edit Recipe — {existingRecipe.RecipeName}";

        if (DesignModeHelper.IsInDesignMode)
        {
            _variantsByDisplay = null!;
            return;
        }

        _variantsByDisplay = ComboBoxBinder.Bind(_variantCombo, sellableVariantOptions, includeEmpty: false);

        _ingredientsGrid.DataSource = _ingredients;
        _modifiersGrid.DataSource = _modifiers;

        PopulateUomOptions();

        if (existingRecipe != null)
        {
            _nameEdit.Text = existingRecipe.RecipeName;
            _yieldQtyEdit.Value = existingRecipe.YieldQuantity;
            _yieldUnitCombo.Text = existingRecipe.YieldUnit;
            _notesEdit.Text = existingRecipe.PreparationNotes ?? string.Empty;
            _isActiveCheck.Checked = existingRecipe.IsActive;
            ComboBoxBinder.SelectById(_variantCombo, _variantsByDisplay, existingRecipe.ProductVariantId);

            _versionLabel.Text = $"Current: Version {existingRecipe.Version}";
            _newVersionCheck.Visible = true;
            _newVersionCheck.Text = $"Save as New Version (Bump to Version {existingRecipe.Version + 1})";

            foreach (var ing in existingRecipe.Ingredients)
            {
                _ingredients.Add(new IngredientEditItem
                {
                    IngredientVariantId = ing.IngredientVariantId,
                    IngredientName = ing.IngredientName,
                    Quantity = ing.Quantity,
                    UnitOfMeasureCode = ing.UnitOfMeasureCode,
                    WastagePercentage = ing.WastagePercentage,
                    CostPerUnitOverride = ing.CostPerUnitOverride
                });
            }

            foreach (var mod in existingRecipe.Modifiers)
            {
                _modifiers.Add(new ModifierEditItem
                {
                    ModifierName = mod.ModifierName,
                    IngredientVariantId = mod.IngredientVariantId,
                    IngredientName = mod.IngredientName,
                    Quantity = mod.Quantity,
                    UnitOfMeasureCode = mod.UnitOfMeasureCode,
                    WastagePercentage = mod.WastagePercentage,
                    CostPerUnitOverride = mod.CostPerUnitOverride
                });
            }

            if (existingRecipe.Versions.Count > 0)
            {
                _versionsGrid.DataSource = existingRecipe.Versions.Select(v => new VersionHistoryRow(
                    v.VersionNumber,
                    v.EffectiveFromUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    $"{v.YieldQuantity:0.####} {v.YieldUnit}",
                    v.Ingredients.Count)).ToList();
            }
        }
        else
        {
            _yieldQtyEdit.Value = 1m;
            _yieldUnitCombo.Text = "Portion";
            _isActiveCheck.Checked = true;
            _versionLabel.Text = "Initial Version 1";
            _newVersionCheck.Visible = false;
        }

        WireEvents();
        UpdateTheoreticalCostDisplay();
    }

    private void PopulateUomOptions()
    {
        string[] uoms = ["G", "KG", "MG", "LB", "OZ", "ML", "L", "CL", "CUP", "TBSP", "TSP", "FL_OZ", "PCS", "EACH", "SLICE", "PORTION", "DOZEN"];
        foreach (var u in uoms)
        {
            _uomRepositoryItem.Items.Add(u);
            _modifierUomRepositoryItem.Items.Add(u);
        }
    }

    private void WireEvents()
    {
        _addIngredientBtn.Click += (s, e) => AddIngredient();
        _removeIngredientBtn.Click += (s, e) => RemoveSelectedIngredient();
        _addModifierBtn.Click += (s, e) => AddModifier();
        _removeModifierBtn.Click += (s, e) => RemoveSelectedModifier();

        _ingredients.ListChanged += (s, e) => UpdateTheoreticalCostDisplay();
        _yieldQtyEdit.ValueChanged += (s, e) => UpdateTheoreticalCostDisplay();
        _saveButton.Click += (s, e) => OnSaveClicked();
        _cancelButton.Click += (s, e) => DialogResult = DialogResult.Cancel;
    }

    private void AddIngredient()
    {
        if (_ingredientOptions.Count == 0)
        {
            XtraMessageBox.Show(this, "No raw ingredients found in catalog.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var first = _ingredientOptions[0];
        _ingredients.Add(new IngredientEditItem
        {
            IngredientVariantId = first.Id,
            IngredientName = first.Display,
            Quantity = 1.0000m,
            UnitOfMeasureCode = "KG",
            WastagePercentage = 0m
        });
    }

    private void RemoveSelectedIngredient()
    {
        if (_ingredientsGridView.FocusedRowHandle >= 0 && _ingredientsGridView.FocusedRowHandle < _ingredients.Count)
        {
            _ingredients.RemoveAt(_ingredientsGridView.FocusedRowHandle);
        }
    }

    private void AddModifier()
    {
        if (_ingredientOptions.Count == 0) return;
        var first = _ingredientOptions[0];
        _modifiers.Add(new ModifierEditItem
        {
            ModifierName = "Extra Item",
            IngredientVariantId = first.Id,
            IngredientName = first.Display,
            Quantity = 1.0000m,
            UnitOfMeasureCode = "PCS",
            WastagePercentage = 0m
        });
    }

    private void RemoveSelectedModifier()
    {
        if (_modifiersGridView.FocusedRowHandle >= 0 && _modifiersGridView.FocusedRowHandle < _modifiers.Count)
        {
            _modifiers.RemoveAt(_modifiersGridView.FocusedRowHandle);
        }
    }

    private async void UpdateTheoreticalCostDisplay()
    {
        if (_existingRecipe != null && _costCalculator != null)
        {
            try
            {
                var cost = await _costCalculator(_existingRecipe.Id);
                if (cost != null)
                {
                    _portionCostLabel.Text = cost.CostPerPortion.ToString("C2");
                    _sellingPriceLabel.Text = cost.SellingPrice.ToString("C2");
                    _foodCostPctLabel.Text = $"{cost.FoodCostPercentage:0.##}%";
                    _grossMarginPctLabel.Text = $"{cost.GrossMarginPercentage:0.##}%";

                    if (cost.FoodCostPercentage > 35m)
                    {
                        _costAlertBadge.Visible = true;
                        _foodCostPctLabel.ForeColor = Color.FromArgb(220, 38, 38);
                    }
                    else
                    {
                        _costAlertBadge.Visible = false;
                        _foodCostPctLabel.ForeColor = Color.FromArgb(22, 163, 74);
                    }
                    return;
                }
            }
            catch
            {
                // Fallback to calculation from ingredient overrides
            }
        }

        // Local calculation fallback
        decimal totalCost = 0m;
        foreach (var ing in _ingredients)
        {
            decimal gross = ing.GrossQuantityRequired;
            decimal unitCost = ing.CostPerUnitOverride ?? 0m;
            totalCost += gross * unitCost;
        }

        decimal yieldQty = _yieldQtyEdit.Value > 0 ? _yieldQtyEdit.Value : 1m;
        decimal portionCost = Math.Round(totalCost / yieldQty, 2, MidpointRounding.AwayFromZero);
        _portionCostLabel.Text = portionCost.ToString("C2");
        _foodCostPctLabel.Text = "-";
        _grossMarginPctLabel.Text = "-";
        _costAlertBadge.Visible = false;
    }

    private void OnSaveClicked()
    {
        if (!ValidateInputs(out var error))
        {
            XtraMessageBox.Show(this, error, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DialogResult = DialogResult.OK;
    }

    private bool ValidateInputs(out string error)
    {
        if (string.IsNullOrWhiteSpace(_nameEdit.Text))
        {
            error = "Recipe name is required.";
            _nameEdit.Focus();
            return false;
        }

        if (ProductVariantId is null)
        {
            error = "Select a sellable product variant for this recipe.";
            _variantCombo.Focus();
            return false;
        }

        if (_yieldQtyEdit.Value <= 0)
        {
            error = "Yield quantity must be greater than zero.";
            _yieldQtyEdit.Focus();
            return false;
        }

        if (_ingredients.Count == 0)
        {
            error = "At least one ingredient is required in the bill of materials.";
            return false;
        }

        foreach (var ing in _ingredients)
        {
            if (ing.Quantity <= 0)
            {
                error = $"Ingredient '{ing.IngredientName}' must have a positive quantity.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(ing.UnitOfMeasureCode))
            {
                error = $"Select a valid Unit of Measure for '{ing.IngredientName}'.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    #region Public Properties

    /// <summary>The entered recipe name.</summary>
    public string RecipeName => _nameEdit.Text.Trim();

    /// <summary>The selected sellable menu item variant ID.</summary>
    public Guid? ProductVariantId => ComboBoxBinder.GetSelectedId(_variantCombo, _variantsByDisplay);

    /// <summary>The configured portion yield quantity.</summary>
    public decimal YieldQuantity => _yieldQtyEdit.Value;

    /// <summary>The configured yield unit.</summary>
    public string YieldUnit => string.IsNullOrWhiteSpace(_yieldUnitCombo.Text) ? "Portion" : _yieldUnitCombo.Text.Trim();

    /// <summary>Preparation / kitchen notes.</summary>
    public string? PreparationNotes => string.IsNullOrWhiteSpace(_notesEdit.Text) ? null : _notesEdit.Text.Trim();

    /// <summary>Whether this recipe is active.</summary>
    public bool IsRecipeActive => _isActiveCheck.Checked;

    /// <summary>Whether to archive the current version and create a new version snapshot.</summary>
    public bool CreateNewVersion => _newVersionCheck.Checked;

    /// <summary>The configured ingredient inputs.</summary>
    public IReadOnlyList<RecipeIngredientInput> IngredientInputs =>
        _ingredients.Select(i => new RecipeIngredientInput(
            i.IngredientVariantId,
            i.IngredientName,
            i.Quantity,
            i.UnitOfMeasureCode,
            i.WastagePercentage,
            i.CostPerUnitOverride)).ToList();

    /// <summary>The configured modifier inputs.</summary>
    public IReadOnlyList<RecipeModifierInput> ModifierInputs =>
        _modifiers.Select(m => new RecipeModifierInput(
            m.ModifierName,
            m.IngredientVariantId,
            m.IngredientName,
            m.Quantity,
            m.UnitOfMeasureCode,
            m.WastagePercentage,
            m.CostPerUnitOverride)).ToList();

    #endregion

    /// <summary>Editable ingredient model.</summary>
    internal sealed class IngredientEditItem
    {
        public Guid IngredientVariantId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public decimal Quantity { get; set; } = 1.0000m;
        public string UnitOfMeasureCode { get; set; } = "KG";
        public decimal WastagePercentage { get; set; } = 0m;
        public decimal? CostPerUnitOverride { get; set; }

        public decimal GrossQuantityRequired =>
            Math.Round(Quantity * (1m + (WastagePercentage / 100m)), 4, MidpointRounding.AwayFromZero);
    }

    /// <summary>Editable modifier model.</summary>
    internal sealed class ModifierEditItem
    {
        public string ModifierName { get; set; } = string.Empty;
        public Guid IngredientVariantId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public decimal Quantity { get; set; } = 1.0000m;
        public string UnitOfMeasureCode { get; set; } = "PCS";
        public decimal WastagePercentage { get; set; } = 0m;
        public decimal? CostPerUnitOverride { get; set; }
    }

    private sealed record VersionHistoryRow(
        int Version,
        string EffectiveDate,
        string Yield,
        int IngredientCount);
}
