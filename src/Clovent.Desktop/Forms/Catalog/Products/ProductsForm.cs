using Clovent.Catalog.Application.Brands.Queries;
using Clovent.Catalog.Application.Categories.Queries;
using Clovent.Catalog.Application.Groups.Queries;
using Clovent.Catalog.Application.Products.Commands;
using Clovent.Catalog.Application.Products.Dtos;
using Clovent.Catalog.Application.Products.Queries;
using Clovent.Catalog.Application.UnitsOfMeasure.Queries;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.MasterData;
using Clovent.Desktop.Sessions;
using Clovent.Desktop.Shared;
using Clovent.Identity.Application.Authorization;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Forms.Catalog.Products;

/// <summary>
/// Product Management screen: search, filter, CRUD, activate/deactivate,
/// and CSV export/import over the product catalog - the flagship screen
/// every downstream module (Restaurant/Retail POS, Purchasing, Sales,
/// Manufacturing) reads from. Feature-gated per
/// <c>products.{create|edit|activate|deactivate}</c>. Replaces the old
/// <c>ProductManagementView</c>'s generic <see cref="MasterDataListView{TDto}"/>
/// chrome with a hand-authored grid+toolbar (see
/// <c>ProductsForm.Designer.cs</c>) - every <c>IMediator</c> call and
/// handler body below is unchanged from that predecessor.
/// </summary>
public sealed partial class ProductsForm : BaseForm
{
    private const string FeatureCode = "products";
    private static readonly string[] CsvHeaders = ["Sku", "Name", "BaseUnitOfMeasureCode", "TaxRatePercentage", "TaxIsInclusive", "Status"];

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;

    private IReadOnlyList<ProductDto> _allItems = [];

    /// <summary>Design-time-only constructor for the Visual Studio WinForms Designer - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public ProductsForm()
    {
        _scope = null!;
        _mediator = null!;
        _featurePolicy = null!;
        _currentSession = null!;

        InitializeComponent();
    }

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public ProductsForm(IServiceScopeFactory scopeFactory, ICurrentSession currentSession) : base()
    {
        InitializeComponent();

        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
        {
            _scope = null!;
            _mediator = null!;
            _featurePolicy = null!;
            _currentSession = null!;
            return;
        }

        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;
    }

    /// <inheritdoc/>
    public override string? PermissionKey => FeatureCode;

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _scope.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>Reloads every row, re-applies the current search filter, and re-evaluates feature permissions. Called once by <c>MainForm</c> when this document opens, and again on F5/the Refresh button.</summary>
    public override async Task RefreshAsync()
    {
        await RunBusyAsync(async () =>
        {
            _allItems = await LoadItemsAsync(CancellationToken.None);
            ApplyFilter();
            await UpdateFeaturePermissionsAsync();
            UpdateButtonStates();
        });
    }

    private void TxtSearch_EditValueChanged(object? sender, EventArgs e) => ApplyFilter();

    private void GridView_FocusedRowChanged(object? sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e) => UpdateButtonStates();

    private async void BtnNew_Click(object? sender, EventArgs e)
    {
        await CreateAsync();
        await RefreshAsync();
    }

    private async void BtnEdit_Click(object? sender, EventArgs e)
    {
        if (GetFocusedItem() is { } item)
        {
            await EditAsync(item);
            await RefreshAsync();
        }
    }

    private async void BtnActivate_Click(object? sender, EventArgs e)
    {
        if (GetFocusedItem() is { } item)
        {
            await _mediator.Send(new ActivateProductCommand(item.ProductId));
            await RefreshAsync();
        }
    }

    private async void BtnDeactivate_Click(object? sender, EventArgs e)
    {
        if (GetFocusedItem() is { } item)
        {
            await _mediator.Send(new DeactivateProductCommand(item.ProductId));
            await RefreshAsync();
        }
    }

    private async void BtnRefresh_Click(object? sender, EventArgs e) => await RefreshAsync();

    private async void BtnExportCsv_Click(object? sender, EventArgs e) =>
        await ExportCsvAsync(MasterDataFilter.Apply(_allItems, txtSearch.Text, dto => $"{dto.Sku} {dto.Name}"));

    private async void BtnImportCsv_Click(object? sender, EventArgs e)
    {
        await ImportCsvAsync();
        await RefreshAsync();
    }

    private async Task<IReadOnlyList<ProductDto>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        var items = await _mediator.Send(new ListProductsQuery(), cancellationToken);
        return [.. items];
    }

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession.UserId is { } userId
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}")
            : Task.FromResult(false);

    private async Task<IReadOnlyList<(Guid Id, string Display)>> LoadCategoryOptionsAsync() =>
        [.. (await _mediator.Send(new ListProductCategoriesQuery())).Select(c => (c.ProductCategoryId, c.Name))];

    private async Task<IReadOnlyList<(Guid Id, string Display)>> LoadGroupOptionsAsync() =>
        [.. (await _mediator.Send(new ListProductGroupsQuery())).Select(g => (g.ProductGroupId, g.Name))];

    private async Task<IReadOnlyList<(Guid Id, string Display)>> LoadBrandOptionsAsync() =>
        [.. (await _mediator.Send(new ListBrandsQuery())).Select(b => (b.BrandId, b.Name))];

    private async Task<IReadOnlyList<(Guid Id, string Display)>> LoadUnitOptionsAsync() =>
        [.. (await _mediator.Send(new ListUnitsOfMeasureQuery())).Select(u => (u.UnitOfMeasureId, $"{u.Code} - {u.Name}"))];

    private async Task CreateAsync()
    {
        var units = await LoadUnitOptionsAsync();
        if (units.Count == 0)
        {
            XtraMessageBox.Show(this, "Create a unit of measure first.", "No Units Available", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var form = new ProductEditForm(
            "New Product",
            await LoadCategoryOptionsAsync(),
            await LoadGroupOptionsAsync(),
            await LoadBrandOptionsAsync(),
            units);

        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new CreateProductCommand(
                form.NameValue,
                form.SkuValue,
                form.BaseUnitOfMeasureId!.Value,
                form.TaxRatePercentage,
                form.TaxIsInclusive,
                form.CategoryId,
                form.GroupId,
                form.BrandId));
        }
    }

    private async Task EditAsync(ProductDto dto)
    {
        using var form = new ProductEditForm(
            "Edit Product",
            await LoadCategoryOptionsAsync(),
            await LoadGroupOptionsAsync(),
            await LoadBrandOptionsAsync(),
            await LoadUnitOptionsAsync(),
            dto.Name,
            dto.Sku,
            dto.CategoryId,
            dto.GroupId,
            dto.BrandId,
            dto.BaseUnitOfMeasureId,
            dto.TaxRatePercentage,
            dto.TaxIsInclusive,
            isNew: false);

        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new RenameProductCommand(dto.ProductId, form.NameValue));
            await _mediator.Send(new SetProductCategoryCommand(dto.ProductId, form.CategoryId));
            await _mediator.Send(new SetProductGroupCommand(dto.ProductId, form.GroupId));
            await _mediator.Send(new SetProductBrandCommand(dto.ProductId, form.BrandId));
            await _mediator.Send(new SetProductTaxConfigurationCommand(dto.ProductId, form.TaxRatePercentage, form.TaxIsInclusive));
        }
    }

    private async Task ExportCsvAsync(IReadOnlyList<ProductDto> rows)
    {
        using var dialog = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = "products.csv" };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var units = await LoadUnitOptionsAsync();
        var codesByUnitId = units.ToDictionary(u => u.Id, u => u.Display.Split(" - ")[0]);

        CsvFile.Write(
            dialog.FileName,
            CsvHeaders,
            rows.Select(dto => (IReadOnlyList<string>)
            [
                dto.Sku,
                dto.Name,
                codesByUnitId.GetValueOrDefault(dto.BaseUnitOfMeasureId, string.Empty),
                dto.TaxRatePercentage.ToString(System.Globalization.CultureInfo.InvariantCulture),
                dto.TaxIsInclusive.ToString(),
                dto.Status,
            ]));
    }

    private async Task ImportCsvAsync()
    {
        using var dialog = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv" };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var units = await LoadUnitOptionsAsync();
        var unitIdsByCode = units.ToDictionary(u => u.Display.Split(" - ")[0], u => u.Id, StringComparer.OrdinalIgnoreCase);

        var imported = 0;
        var skipped = 0;
        foreach (var row in CsvFile.ReadDataRows(dialog.FileName))
        {
            if (row.Count < 4 || !unitIdsByCode.TryGetValue(row[2], out var unitId))
            {
                skipped++;
                continue;
            }

            var taxRate = decimal.TryParse(row[3], System.Globalization.CultureInfo.InvariantCulture, out var rate) ? rate : 0m;
            var taxInclusive = row.Count > 4 && bool.TryParse(row[4], out var inclusive) && inclusive;

            await _mediator.Send(new CreateProductCommand(row[1], row[0], unitId, taxRate, taxInclusive));
            imported++;
        }

        XtraMessageBox.Show(this, $"Imported {imported} product(s), skipped {skipped}.", "Import Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ApplyFilter()
    {
        var visible = MasterDataFilter.Apply(_allItems, txtSearch.Text, dto => $"{dto.Sku} {dto.Name}");
        gridControl.DataSource = visible.ToList();
        StatusText = $"{visible.Count} of {_allItems.Count} record(s)";
        UpdateButtonStates();
    }

    private async Task UpdateFeaturePermissionsAsync()
    {
        btnNew.Enabled = await CanUseFeatureAsync("create");
        btnEdit.Tag = await CanUseFeatureAsync("edit");
        btnActivate.Tag = await CanUseFeatureAsync("activate");
        btnDeactivate.Tag = await CanUseFeatureAsync("deactivate");
    }

    private void UpdateButtonStates()
    {
        var focused = GetFocusedItem();
        var hasFocusedRow = focused is not null;
        var status = focused?.Status;

        btnEdit.Enabled = MasterDataFilter.CanEdit(hasFocusedRow, btnEdit.Tag as bool?, true);
        btnActivate.Enabled = MasterDataFilter.CanActivate(hasFocusedRow, btnActivate.Tag as bool?, status, true);
        btnDeactivate.Enabled = MasterDataFilter.CanDeactivate(hasFocusedRow, btnDeactivate.Tag as bool?, status, true);
    }

    private ProductDto? GetFocusedItem() => gridView.GetFocusedRow() as ProductDto;
}
