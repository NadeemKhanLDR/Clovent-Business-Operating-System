using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Inventory.Application.Recipes.Dtos;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;

namespace Clovent.Desktop.Inventory.Recipes;

/// <summary>
/// Modal dialog displaying detailed theoretical recipe costing and ingredient contribution breakdown.
/// </summary>
public sealed partial class RecipeCostDialog : XtraForm
{
    /// <summary>Designer constructor.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public RecipeCostDialog()
    {
        InitializeComponent();
        Text = "Recipe Costing Breakdown";
    }

    /// <summary>Initializes a new instance of <see cref="RecipeCostDialog"/>.</summary>
    public RecipeCostDialog(RecipeCostDto cost, string recipeName, string menuItemName)
    {
        InitializeComponent();
        Text = $"Theoretical Costing — {recipeName}";

        if (DesignModeHelper.IsInDesignMode)
            return;

        _recipeNameLabel.Text = $"{recipeName} ({menuItemName})";
        _yieldLabel.Text = $"Yield: {cost.YieldQuantity:0.####} {cost.YieldUnit}";

        _batchCostValue.Text = cost.TotalBatchCost.ToString("C2");
        _portionCostValue.Text = cost.CostPerPortion.ToString("C2");
        _sellingPriceValue.Text = cost.SellingPrice.ToString("C2");
        _foodCostValue.Text = $"{cost.FoodCostPercentage:0.##}%";
        _grossMarginValue.Text = $"{cost.GrossMarginPercentage:0.##}%";

        if (cost.FoodCostPercentage > 35m)
        {
            _foodCostValue.ForeColor = Color.FromArgb(220, 38, 38); // Warning Red
            _alertBadge.Text = "HIGH FOOD COST WARNING (> 35%)";
            _alertBadge.Visible = true;
        }
        else
        {
            _foodCostValue.ForeColor = Color.FromArgb(22, 163, 74); // Healthy Green
            _alertBadge.Visible = false;
        }

        var rows = cost.Ingredients.Select(i => new IngredientCostRow(
            i.IngredientName,
            i.GrossQuantity,
            i.RecipeUnit,
            i.EffectiveUnitCostInRecipeUnit,
            i.TotalLineCost,
            cost.TotalBatchCost > 0m
                ? Math.Round((i.TotalLineCost / cost.TotalBatchCost) * 100m, 2, MidpointRounding.AwayFromZero)
                : 0m)).ToList();

        _gridControl.DataSource = rows;
    }

    private sealed record IngredientCostRow(
        string IngredientName,
        decimal GrossQuantity,
        string Unit,
        decimal UnitCost,
        decimal TotalCost,
        decimal CostPercentage);
}
