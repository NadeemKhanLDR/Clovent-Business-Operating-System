using System.Windows.Forms;
using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Inventory.Recipes;

partial class RecipeManagementView
{
    private System.ComponentModel.IContainer components = null;

    private MasterDataListView<RecipeRow> _listView;

    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;

        _listView = new MasterDataListView<RecipeRow>(
            [
                new MasterDataColumn("RecipeName", "Recipe Name", 190),
                new MasterDataColumn("ProductVariantName", "Sellable Menu Item", 190),
                new MasterDataColumn("YieldDisplay", "Yield", 100),
                new MasterDataColumn("Version", "Ver", 50),
                new MasterDataColumn("IngredientCount", "Ingredients", 80),
                new MasterDataColumn("CostPerPortion", "Theoretical Cost", 110),
                new MasterDataColumn("SellingPrice", "Selling Price", 100),
                new MasterDataColumn("FoodCostDisplay", "Food Cost %", 90),
                new MasterDataColumn("GrossMarginDisplay", "Gross Margin %", 100),
                new MasterDataColumn("Status", "Status", 75),
            ],
            [
                new MasterDataListAction<RecipeRow>("Costing Breakdown", ViewCostingAsync, _ => true, "costing")
            ])
        {
            Dock = DockStyle.Fill,
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = r => $"{r.RecipeName} {r.ProductVariantName}",
            StatusSelector = r => r.Status,
            CanUseFeatureAsync = op => CanUseFeatureAsync(op),
            OnNew = CreateRecipeAsync,
            OnEdit = EditRecipeAsync,
            OnActivate = ToggleActiveAsync,
            OnDeactivate = ToggleActiveAsync
        };

        Controls.Add(_listView);
    }
}
