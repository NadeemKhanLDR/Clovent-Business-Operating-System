using Clovent.Catalog.Application.Brands.Queries;
using Clovent.Catalog.Application.Categories.Queries;
using Clovent.Catalog.Application.Groups.Queries;
using Clovent.Catalog.Application.Products.Commands;
using Clovent.Catalog.Application.Products.Dtos;
using Clovent.Catalog.Application.Products.Queries;
using Clovent.Catalog.Application.UnitsOfMeasure.Queries;
using Clovent.Desktop.MasterData;
using Clovent.Desktop.Sessions;
using Clovent.Desktop.Shared;
using Clovent.Identity.Application.Authorization;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Catalog.Products;

/// <summary>
/// Product Management screen: search, filter, CRUD, activate/deactivate,
/// and CSV export/import over the product catalog - the flagship Milestone
/// 14 screen every downstream module (Restaurant/Retail POS, Purchasing,
/// Sales, Manufacturing) reads from. Feature-gated per
/// <c>products.{create|edit|activate|deactivate}</c>.
/// </summary>
public sealed class ProductManagementView : XtraUserControl
{
    private const string FeatureCode = "products";
    private static readonly string[] CsvHeaders = ["Sku", "Name", "BaseUnitOfMeasureCode", "TaxRatePercentage", "TaxIsInclusive", "Status"];

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;
    private readonly MasterDataListView<ProductDto> _listView;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public ProductManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;

        Dock = DockStyle.Fill;

        _listView = new MasterDataListView<ProductDto>(
        [
            new MasterDataColumn(nameof(ProductDto.Sku), "SKU", 120),
            new MasterDataColumn(nameof(ProductDto.Name), "Name", 220),
            new MasterDataColumn(nameof(ProductDto.TaxRatePercentage), "Tax %", 70),
            new MasterDataColumn(nameof(ProductDto.Status), "Status", 90),
            new MasterDataColumn(nameof(ProductDto.CreatedAtUtc), "Created (UTC)", 160),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = dto => $"{dto.Sku} {dto.Name}",
            StatusSelector = dto => dto.Status,
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
            OnNew = CreateAsync,
            OnEdit = EditAsync,
            OnActivate = dto => _mediator.Send(new ActivateProductCommand(dto.ProductId)),
            OnDeactivate = dto => _mediator.Send(new DeactivateProductCommand(dto.ProductId)),
            OnExportCsv = ExportCsvAsync,
            OnImportCsv = ImportCsvAsync,
        };

        Controls.Add(_listView);
        Load += async (_, _) => await _listView.RefreshAsync();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _scope.Dispose();
        }

        base.Dispose(disposing);
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
}
