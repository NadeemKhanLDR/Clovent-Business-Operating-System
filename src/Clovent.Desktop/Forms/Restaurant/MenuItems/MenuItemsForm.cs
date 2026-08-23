using Clovent.Catalog.Application.Barcodes.Commands;
using Clovent.Catalog.Application.Barcodes.Queries;
using Clovent.Catalog.Application.Categories.Commands;
using Clovent.Catalog.Application.Categories.Queries;
using Clovent.Catalog.Application.Prices.Commands;
using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Application.Products.Commands;
using Clovent.Catalog.Application.UnitsOfMeasure.Queries;
using Clovent.Catalog.Application.Variants.Commands;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Catalog.Prices;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Desktop.MasterData;
using Clovent.Desktop.Restaurant.Shared;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.MasterData.Application.Currencies.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Drawing;

namespace Clovent.Desktop.Forms.Restaurant.MenuItems;

/// <summary>
/// Restaurant's own "Products" screen: Name, Category, Selling Price, Active
/// - nothing else. This is <b>presentation only</b> - there is no
/// <c>MenuItem</c> entity, table, repository, or handler anywhere in this
/// solution. Every row here is a Catalog <c>ProductVariant</c> (the same
/// aggregate <c>RestaurantPosView</c>'s product tiles and
/// <c>Clovent.Desktop.Forms.Catalog.Products.ProductsForm</c>'s grid already
/// read), and every action here calls the exact same
/// <c>Clovent.Catalog.Application</c> commands/queries those screens do -
/// <c>CreateProductWithPriceCommand</c> for "New Menu Item" (one Product +
/// one Variant + one Selling Price, in a single call - see that command's
/// own doc comment), <c>RenameProductCommand</c>/<c>RenameProductVariantCommand</c>/
/// <c>SetProductCategoryCommand</c>/<c>UpdateProductPriceAmountCommand</c>/
/// <c>Activate|DeactivateProductVariantCommand</c> for Edit/Activate/Deactivate,
/// and <c>CreateProductCategoryCommand</c> for the "New Category" quick-add
/// (so a Restaurant owner never has to open Catalog's own Categories screen
/// either). SKU, Unit of Measure, Currency, and Tax Configuration are Catalog
/// concepts every Product/Variant/Price still needs internally - this screen
/// resolves them automatically (SKU generated, unit/currency defaulted to
/// the first one configured) rather than asking a restaurant owner to think
/// about them. See <c>docs/architecture/RestaurantPOSArchitecture.md</c>
/// for the full rationale. Feature-gated per
/// <c>menuitems.{create|edit|activate|deactivate|createcategory}</c>.
/// </summary>
public sealed partial class MenuItemsForm : BaseForm
{
    private const string FeatureCode = "menuitems";

    private readonly IServiceScope _scope;
    private readonly ScreenOperationGate _gate = new();
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;
    private readonly IMenuItemsChangeNotifier _changeNotifier;
    private readonly ILogger<MenuItemsForm> _logger;

    private const string AllCategoriesDisplay = "All Categories";

    private IReadOnlyList<MenuItemRow> _allItems = [];
    private Dictionary<string, Guid?> _categoryFilterByDisplay = new() { [AllCategoriesDisplay] = null };
    private readonly Dictionary<Guid, Image> _imagesByProductId = [];

    /// <summary>Design-time-only constructor for the Visual Studio WinForms Designer - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public MenuItemsForm()
    {
        _scope = null!;
        _mediator = null!;
        _featurePolicy = null!;
        _currentSession = null!;
        _changeNotifier = null!;
        _logger = null!;

        InitializeComponent();
    }

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public MenuItemsForm(IServiceScopeFactory scopeFactory, ICurrentSession currentSession, IMenuItemsChangeNotifier changeNotifier) : base()
    {
        InitializeComponent();

        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
        {
            _scope = null!;
            _mediator = null!;
            _featurePolicy = null!;
            _currentSession = null!;
            _changeNotifier = null!;
            _logger = null!;
            return;
        }

        _scope = scopeFactory.CreateScope();
        _mediator = new SerializedMediator(_scope.ServiceProvider.GetRequiredService<IMediator>(), _gate);
        _featurePolicy = new SerializedFeatureAuthorizationPolicy(_scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>(), _gate);
        _logger = _scope.ServiceProvider.GetRequiredService<ILogger<MenuItemsForm>>();
        _currentSession = currentSession;
        _changeNotifier = changeNotifier;

        InitializeRuntime();
    }

    private void InitializeRuntime()
    {
        gridView.OptionsSelection.MultiSelect = true;
        gridView.SelectionChanged += (s, e) => UpdateButtonStates();
        gridView.CustomColumnDisplayText += GridView_CustomColumnDisplayText;
        StatusBadgeStyler.Apply(gridView, colStatus, value => value == "Active");

        AppearanceManager.Changed += AppearanceManager_Changed;
    }

    /// <inheritdoc/>
    public override string? PermissionKey => FeatureCode;

    private void AppearanceManager_Changed(object? sender, EventArgs e) => AppearanceManager.Apply(this, "Restaurant", nameof(MenuItemsForm));

    private void GridView_CustomColumnDisplayText(object? sender, DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs e)
    {
        if (e.Column == colPrice && e.Value != null && e.Value != DBNull.Value)
        {
            try
            {
                var amount = Convert.ToDecimal(e.Value);
                e.DisplayText = CurrencyDisplay.FormatPlain(amount);
            }
            catch
            {
                // Fallback
            }
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            AppearanceManager.Changed -= AppearanceManager_Changed;
            _scope.Dispose();
            _gate.Dispose();
            ClearCachedImages();
        }

        base.Dispose(disposing);
    }

    private void ClearCachedImages()
    {
        foreach (var image in _imagesByProductId.Values)
        {
            image.Dispose();
        }

        _imagesByProductId.Clear();
    }

    /// <summary>
    /// Grid-only refresh for status actions (Activate/Deactivate/reorder):
    /// reloads the item rows and re-applies the current filter WITHOUT the
    /// full-reinit side of <see cref="RefreshAsync"/> (theme re-application,
    /// currency/category filter reloads, permission re-evaluation). Search
    /// text and category filter are preserved because they are only ever
    /// read here, never reset - and the focused row is restored by id, so
    /// the screen doesn't visibly "reset" after a one-row status change.
    /// </summary>
    private async Task RefreshGridAsync(Guid? focusProductVariantId = null, Guid? focusProductId = null)
    {
        var focusedRowHandle = gridView.FocusedRowHandle;
        var topRowIndex = gridView.TopRowIndex;

        Cursor = Cursors.WaitCursor;
        try
        {
            _allItems = await LoadItemsAsync();
            ApplyFilter();
            UpdateButtonStates();
        }
        finally
        {
            Cursor = Cursors.Default;
        }

        if (focusProductVariantId is { } id)
        {
            FocusItem(id);
        }
        else if (focusProductId is { } pId)
        {
            FocusItemByProductId(pId);
        }
        else if (focusedRowHandle >= 0 && focusedRowHandle < gridView.RowCount)
        {
            gridView.FocusedRowHandle = focusedRowHandle;
        }
        gridView.TopRowIndex = topRowIndex;
    }

    private void FocusItemByProductId(Guid productId)
    {
        for (var rowHandle = 0; rowHandle < gridView.DataRowCount; rowHandle++)
        {
            if (gridView.GetRow(rowHandle) is MenuItemRow row && row.ProductId == productId)
            {
                gridView.FocusedRowHandle = rowHandle;
                return;
            }
        }
    }

    /// <summary>Reloads every row, re-applies the current search/category filter, and re-evaluates feature permissions. Called once by <c>MainForm</c> when this document opens, and again on F5/the Refresh button.</summary>
    public override async Task RefreshAsync()
    {
        AppearanceManager.Apply(this, "Restaurant", nameof(MenuItemsForm));

        await RunBusyAsync(async () =>
        {
            await CurrencyDisplayLoader.ConfigureAsync(_mediator);

            _allItems = await LoadItemsAsync();
            await LoadCategoryFilterOptionsAsync();
            ApplyFilter();
            await UpdateFeaturePermissionsAsync();
            UpdateButtonStates();
        });
    }

    private void TxtSearch_EditValueChanged(object? sender, EventArgs e) => ApplyFilter();

    private void CboCategory_EditValueChanged(object? sender, EventArgs e) => ApplyFilter();

    private void GridView_FocusedRowChanged(object? sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e) => UpdateButtonStates();

    private async void BtnNewMenuItem_Click(object? sender, EventArgs e) => await TryRunAsync(async () =>
    {
        var productId = await CreateAsync();
        await RefreshGridAsync(focusProductId: productId);
        _changeNotifier.NotifyChanged();
    }, "add this menu item");

    private async void BtnEdit_Click(object? sender, EventArgs e) => await TryRunAsync(async () =>
    {
        if (GetFocusedItem() is { } item)
        {
            await EditAsync(item);
            await RefreshGridAsync(item.ProductVariantId);
            _changeNotifier.NotifyChanged();
        }
    }, "save changes to this menu item");

    private async void BtnActivate_Click(object? sender, EventArgs e) => await TryRunAsync(async () =>
    {
        var selectedRows = gridView.GetSelectedRows();
        var items = selectedRows
            .Select(r => gridView.GetRow(r) as MenuItemRow)
            .Where(r => r is not null)
            .Cast<MenuItemRow>()
            .ToList();

        if (items.Count == 0) return;

        var confirmMsg = items.Count == 1
            ? "Activate the selected menu item?"
            : $"Activate the {items.Count} selected menu items?";

        if (XtraMessageBox.Show(this, confirmMsg, "Activate Menu Items", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            foreach (var item in items)
            {
                await _mediator.Send(new ActivateProductVariantCommand(item.ProductVariantId));
            }
            await RefreshGridAsync(items[0].ProductVariantId);
            _changeNotifier.NotifyChanged();
        }
    }, "activate menu items");

    private async void BtnDeactivate_Click(object? sender, EventArgs e) => await TryRunAsync(async () =>
    {
        var selectedRows = gridView.GetSelectedRows();
        var items = selectedRows
            .Select(r => gridView.GetRow(r) as MenuItemRow)
            .Where(r => r is not null)
            .Cast<MenuItemRow>()
            .ToList();

        if (items.Count == 0) return;

        var confirmMsg = items.Count == 1
            ? $"\"{items[0].Name}\" will no longer appear on the POS screen. Continue?"
            : $"{items.Count} selected menu items will no longer appear on the POS screen. Continue?";

        if (XtraMessageBox.Show(this, confirmMsg, "Deactivate Menu Items", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            foreach (var item in items)
            {
                await _mediator.Send(new DeactivateProductVariantCommand(item.ProductVariantId));
            }
            await RefreshGridAsync(items[0].ProductVariantId);
            _changeNotifier.NotifyChanged();
        }
    }, "deactivate menu items");

    private async void BtnNewCategory_Click(object? sender, EventArgs e) => await TryRunAsync(async () =>
    {
        using var form = new TextPromptForm("New Category", "Category Name:", required: true);
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        await _mediator.Send(new CreateProductCategoryCommand(form.Value!));
        await RefreshAsync();
        _changeNotifier.NotifyChanged();
    }, "add this category");

    /// <summary>Picks a category, then its display color - the POS category rail colors itself from this (<c>RestaurantPosView.BuildCategoryButtons</c>).</summary>
    private async void BtnCategoryColor_Click(object? sender, EventArgs e) => await TryRunAsync(async () =>
    {
        var categories = await _mediator.Send(new ListProductCategoriesQuery());
        if (categories.Count == 0)
        {
            XtraMessageBox.Show(this, "Add a category first (\"+ New Category\").", "No Categories Yet", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var pickForm = new SelectionPromptForm("Category Color", "Category:", [.. categories.Select(c => (c.ProductCategoryId, c.Name))]);
        if (pickForm.ShowDialog(this) != DialogResult.OK || pickForm.SelectedId is not { } categoryId)
        {
            return;
        }

        var category = categories.First(c => c.ProductCategoryId == categoryId);
        using var colorForm = new CategoryColorDialog(category.Name, category.ColorHex);
        if (colorForm.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        await _mediator.Send(new SetProductCategoryColorCommand(categoryId, colorForm.ColorHex));
        _changeNotifier.NotifyChanged();
    }, "save this category color");

    /// <summary>Swaps the focused item's <see cref="MenuItemRow.SortOrder"/> with the item immediately before/after it in the full (unfiltered) list - see <see cref="MoveAsync"/>.</summary>
    private async void BtnMoveUp_Click(object? sender, EventArgs e) => await TryRunAsync(() => MoveAsync(-1), "reorder this menu item");

    private async void BtnMoveDown_Click(object? sender, EventArgs e) => await TryRunAsync(() => MoveAsync(1), "reorder this menu item");

    /// <summary>
    /// Reorders menu items by swapping the focused row's persisted
    /// <c>SortOrder</c> with its neighbor's - the POS product tile wall and
    /// category rail both render in this same order (see
    /// <c>RestaurantPosView.ReloadMenuItemsAsync</c>). Operates against the
    /// full unfiltered list (not just what the current search/category
    /// filter shows), so results are clearest with "All Categories" selected
    /// and no search text - swapping past a filtered-out neighbor still
    /// updates the persisted order correctly, it just isn't visible until
    /// the filter is cleared.
    /// </summary>
    private async Task MoveAsync(int direction)
    {
        if (GetFocusedItem() is not { } focused)
        {
            return;
        }

        var ordered = _allItems.OrderBy(i => i.SortOrder).ThenBy(i => i.Name).ToList();
        var index = ordered.FindIndex(i => i.ProductVariantId == focused.ProductVariantId);
        var neighborIndex = index + direction;
        if (index < 0 || neighborIndex < 0 || neighborIndex >= ordered.Count)
        {
            return;
        }

        var current = ordered[index];
        var neighbor = ordered[neighborIndex];

        await _mediator.Send(new SetProductVariantSortOrderCommand(current.ProductVariantId, neighbor.SortOrder));
        await _mediator.Send(new SetProductVariantSortOrderCommand(neighbor.ProductVariantId, current.SortOrder));

        await RefreshGridAsync(current.ProductVariantId);
        _changeNotifier.NotifyChanged();
    }

    private async void BtnRefresh_Click(object? sender, EventArgs e) => await TryRunAsync(RefreshAsync, "refresh menu items");

    private Task TryRunAsync(Func<Task> action, string actionDescription) =>
        GuardedAction.RunAsync(this, _logger, action, actionDescription, RefreshAsync);

    private async Task<IReadOnlyList<MenuItemRow>> LoadItemsAsync()
    {
        var variants = await _mediator.Send(new ListProductVariantsQuery());
        var categories = await _mediator.Send(new ListProductCategoriesQuery());
        var categoryNamesById = categories.ToDictionary(c => c.ProductCategoryId, c => c.Name);

        // One flat query for every active Selling price, not one query per
        // variant - see RestaurantPosView.ReloadMenuItemsAsync's identical
        // fix for the same N+1 pattern, needed here for the same reason
        // (this grid needs to scale to 2000+ menu items too).
        var sellingPrices = await _mediator.Send(new ListActiveProductPricesByTypeQuery(PriceType.Selling));
        var newestSellingPriceByVariantId = sellingPrices
            .GroupBy(p => p.ProductVariantId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.EffectiveFromUtc).First());

        var rows = new List<MenuItemRow>();
        foreach (var variant in variants)
        {
            var sellingPrice = newestSellingPriceByVariantId.GetValueOrDefault(variant.ProductVariantId);

            if (!_imagesByProductId.ContainsKey(variant.ProductId) && MenuItemImageStore.Load(variant.ProductId) is { } photo)
            {
                _imagesByProductId[variant.ProductId] = photo;
            }

            rows.Add(new MenuItemRow(
                variant.ProductId,
                variant.ProductVariantId,
                sellingPrice?.ProductPriceId,
                variant.Name,
                variant.ProductCategoryId is { } categoryId ? categoryNamesById.GetValueOrDefault(categoryId, "(none)") : "(none)",
                variant.ProductCategoryId,
                sellingPrice?.Amount ?? 0m,
                variant.Status,
                _imagesByProductId.GetValueOrDefault(variant.ProductId),
                variant.SortOrder));
        }

        return [.. rows.OrderBy(r => r.SortOrder).ThenBy(r => r.Name)];
    }

    private async Task LoadCategoryFilterOptionsAsync()
    {
        var selectedId = ComboBoxBinder.GetSelectedId(cboCategory, _categoryFilterByDisplay);

        var categories = await LoadCategoryOptionsAsync();
        _categoryFilterByDisplay = new Dictionary<string, Guid?> { [AllCategoriesDisplay] = null };
        cboCategory.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        cboCategory.Properties.Items.Clear();
        cboCategory.Properties.Items.Add(AllCategoriesDisplay);
        foreach (var (id, display) in categories)
        {
            _categoryFilterByDisplay[display] = id;
            cboCategory.Properties.Items.Add(display);
        }

        ComboBoxBinder.SelectById(cboCategory, _categoryFilterByDisplay, selectedId);
        if (cboCategory.SelectedIndex < 0)
        {
            cboCategory.SelectedIndex = 0;
        }
    }

    private async Task<IReadOnlyList<(Guid Id, string Display)>> LoadCategoryOptionsAsync() =>
        [.. (await _mediator.Send(new ListProductCategoriesQuery())).Select(c => (c.ProductCategoryId, c.Name))];

    private async Task<IReadOnlyList<(Guid Id, string Display)>> LoadUnitOptionsAsync() =>
        [.. (await _mediator.Send(new ListUnitsOfMeasureQuery())).Select(u => (u.UnitOfMeasureId, $"{u.Code} - {u.Name}"))];

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession.UserId is { } userId
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}")
            : Task.FromResult(false);

    private async Task<Guid?> CreateAsync()
    {
        var units = await LoadUnitOptionsAsync();
        if (units.Count == 0)
        {
            XtraMessageBox.Show(this, "Ask an administrator to set up a unit of measure first.", "Not Ready Yet", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        var currencies = await _mediator.Send(new ListCurrenciesQuery());
        if (currencies.Count == 0)
        {
            XtraMessageBox.Show(this, "Ask an administrator to set up a currency first.", "Not Ready Yet", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        // "Save & New" (MasterDataEditFormBase.EnableSaveAndNew) reopens a
        // fresh dialog immediately for the next item instead of returning to
        // the grid - a restaurant owner keying in a whole menu section
        // (every Curry, every Drink) shouldn't have to reopen "New Menu
        // Item" from the command panel after every single item.
        bool saveAndNew;
        do
        {
            using var form = new MenuItemEditForm("Add Menu Item", await LoadCategoryOptionsAsync(), checkBarcodeExists: async (val) => await IsBarcodeInUseAsync(Guid.Empty, val));
            if (form.ShowDialog(this) != DialogResult.OK)
            {
                return null;
            }

            var product = await _mediator.Send(new CreateProductWithPriceCommand(
                form.NameValue,
                form.CategoryId,
                form.SellingPrice,
                currencies.First().CurrencyId,
                units[0].Id,
                form.ItemIsActive));

            var variants = await _mediator.Send(new ListProductVariantsByProductQuery(product.ProductId));
            if (variants.Count > 0)
            {
                var variantId = variants.First().ProductVariantId;
                await SaveMenuItemBarcodesAsync(variantId, form.Barcode1, form.Barcode2, form.Barcode3);
            }

            if (form.PendingImage is not null)
            {
                MenuItemImageStore.Save(product.ProductId, form.PendingImage);
                _imagesByProductId[product.ProductId] = MenuItemImageStore.Load(product.ProductId);
            }

            saveAndNew = form.IsSaveAndNew;
            if (!saveAndNew)
            {
                return product.ProductId;
            }
        }
        while (saveAndNew);

        return null;
    }

    private async Task EditAsync(MenuItemRow row)
    {
        var existingBarcodes = await _mediator.Send(new ListBarcodesByVariantQuery(row.ProductVariantId));
        var barcodeList = existingBarcodes.OrderBy(b => b.CreatedAtUtc).ToList();
        var bc1 = barcodeList.Count > 0 ? barcodeList[0].Value : null;
        var bc2 = barcodeList.Count > 1 ? barcodeList[1].Value : null;
        var bc3 = barcodeList.Count > 2 ? barcodeList[2].Value : null;

        using var existingImage = MenuItemImageStore.Load(row.ProductId);
        using var form = new MenuItemEditForm(
            "Edit Menu Item",
            await LoadCategoryOptionsAsync(),
            row.Name,
            row.CategoryId,
            row.Price,
            row.Status == "Active",
            existingImage,
            bc1,
            bc2,
            bc3,
            checkBarcodeExists: async (val) => await IsBarcodeInUseAsync(row.ProductVariantId, val));

        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        await _mediator.Send(new RenameProductCommand(row.ProductId, form.NameValue));
        await _mediator.Send(new RenameProductVariantCommand(row.ProductVariantId, form.NameValue));
        await _mediator.Send(new SetProductCategoryCommand(row.ProductId, form.CategoryId));
        await SaveMenuItemBarcodesAsync(row.ProductVariantId, form.Barcode1, form.Barcode2, form.Barcode3);

        if (row.ProductPriceId is { } priceId)
        {
            await _mediator.Send(new UpdateProductPriceAmountCommand(priceId, form.SellingPrice));
        }
        else
        {
            var currencies = await _mediator.Send(new ListCurrenciesQuery());
            if (currencies.Count > 0)
            {
                await _mediator.Send(new CreateProductPriceCommand(row.ProductVariantId, PriceType.Selling, form.SellingPrice, currencies.First().CurrencyId));
            }
        }

        var wasActive = row.Status == "Active";
        if (form.ItemIsActive && !wasActive)
        {
            await _mediator.Send(new ActivateProductVariantCommand(row.ProductVariantId));
        }
        else if (!form.ItemIsActive && wasActive)
        {
            await _mediator.Send(new DeactivateProductVariantCommand(row.ProductVariantId));
        }

        if (form.ImageCleared)
        {
            MenuItemImageStore.Delete(row.ProductId);
            if (_imagesByProductId.Remove(row.ProductId, out var oldImg))
            {
                oldImg.Dispose();
            }
        }
        else if (form.PendingImage is not null)
        {
            MenuItemImageStore.Save(row.ProductId, form.PendingImage);
            if (_imagesByProductId.Remove(row.ProductId, out var oldImg))
            {
                oldImg.Dispose();
            }
            _imagesByProductId[row.ProductId] = MenuItemImageStore.Load(row.ProductId);
        }
    }

    private void ApplyFilter()
    {
        var categoryId = ComboBoxBinder.GetSelectedId(cboCategory, _categoryFilterByDisplay);
        var scoped = categoryId is { } selectedCategoryId
            ? _allItems.Where(row => row.CategoryId == selectedCategoryId).ToList()
            : _allItems;

        var visible = MasterDataFilter.Apply(scoped, txtSearch.Text, row => row.Name);
        gridControl.DataSource = visible.ToList();
        StatusText = $"{visible.Count} of {_allItems.Count} record(s)";

        if (visible.Count == 0)
        {
            emptyStateLabel.Text = _allItems.Count == 0
                ? "No menu items yet.\nClick \"+ New Menu Item\" on the left to add your first dish."
                : "No menu items match your search.\nTry a different name or category.";
            emptyStateLabel.Visible = true;
            emptyStateLabel.BringToFront();
        }
        else
        {
            emptyStateLabel.Visible = false;
        }

        UpdateButtonStates();
    }

    private async Task UpdateFeaturePermissionsAsync()
    {
        btnNewMenuItem.Enabled = await CanUseFeatureAsync("create");
        btnNewCategory.Enabled = await CanUseFeatureAsync("createcategory");
        btnEdit.Tag = await CanUseFeatureAsync("edit");
        btnActivate.Tag = await CanUseFeatureAsync("activate");
        btnDeactivate.Tag = await CanUseFeatureAsync("deactivate");
    }

    private void UpdateButtonStates()
    {
        var selectedCount = gridView.GetSelectedRows().Length;
        var focused = GetFocusedItem();
        var hasFocusedRow = selectedCount > 0 && focused is not null;

        btnEdit.Enabled = (selectedCount == 1) && MasterDataFilter.CanEdit(hasFocusedRow, btnEdit.Tag as bool?, true);
        btnActivate.Enabled = (selectedCount > 0) && (btnActivate.Tag as bool? ?? true);
        btnDeactivate.Enabled = (selectedCount > 0) && (btnDeactivate.Tag as bool? ?? true);
    }

    private void GridView_DoubleClick(object? sender, EventArgs e)
    {
        if (gridView.GetSelectedRows().Length == 1 && GetFocusedItem() is not null)
        {
            BtnEdit_Click(gridView, EventArgs.Empty);
        }
    }

    private async Task SaveMenuItemBarcodesAsync(Guid variantId, string b1, string b2, string b3)
    {
        var existing = await _mediator.Send(new ListBarcodesByVariantQuery(variantId));
        var inputs = new[] { b1, b2, b3 }.Where(s => !string.IsNullOrEmpty(s)).ToList();

        // 1. Deactivate any existing barcode of this variant that is NOT in the inputs
        foreach (var bc in existing)
        {
            if (bc.Status == "Active" && !inputs.Contains(bc.Value))
            {
                await _mediator.Send(new DeactivateBarcodeCommand(bc.BarcodeId));
            }
        }

        // 2. Process inputs: activate inactive ones, mark primary, or create new ones
        for (int i = 0; i < inputs.Count; i++)
        {
            var val = inputs[i];
            var match = existing.FirstOrDefault(b => b.Value == val);
            var shouldBePrimary = (i == 0);

            if (match is not null)
            {
                if (match.Status != "Active")
                {
                    await _mediator.Send(new ActivateBarcodeCommand(match.BarcodeId));
                }

                if (shouldBePrimary && !match.IsPrimary)
                {
                    await _mediator.Send(new MarkBarcodeAsPrimaryCommand(match.BarcodeId));
                }
                else if (!shouldBePrimary && match.IsPrimary)
                {
                    await _mediator.Send(new UnmarkBarcodeAsPrimaryCommand(match.BarcodeId));
                }
            }
            else
            {
                await _mediator.Send(new CreateBarcodeCommand(variantId, val, IsPrimary: shouldBePrimary));
            }
        }
    }

    private async Task<bool> IsBarcodeInUseAsync(Guid currentVariantId, string barcodeValue)
    {
        try
        {
            var existing = await _mediator.Send(new GetBarcodeByValueQuery(barcodeValue));
            return existing.ProductVariantId != currentVariantId;
        }
        catch (Clovent.Catalog.Application.NotFoundException)
        {
            return false;
        }
    }

    private MenuItemRow? GetFocusedItem() => gridView.GetFocusedRow() as MenuItemRow;

    /// <summary>Re-focuses the just-edited row after a grid reload (the reload re-sorts and clears the selection) - a no-op if a filter now hides the item.</summary>
    private void FocusItem(Guid productVariantId)
    {
        for (var rowHandle = 0; rowHandle < gridView.DataRowCount; rowHandle++)
        {
            if (gridView.GetRow(rowHandle) is MenuItemRow row && row.ProductVariantId == productVariantId)
            {
                gridView.FocusedRowHandle = rowHandle;
                return;
            }
        }
    }

    private sealed record MenuItemRow(
        Guid ProductId,
        Guid ProductVariantId,
        Guid? ProductPriceId,
        string Name,
        string CategoryName,
        Guid? CategoryId,
        decimal Price,
        string Status,
        Image? Photo,
        int SortOrder);
}
