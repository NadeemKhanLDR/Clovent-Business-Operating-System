using Clovent.Catalog.Barcodes;
using Clovent.Catalog.Barcodes.ValueObjects;
using Clovent.Catalog.Brands;
using Clovent.Catalog.Brands.ValueObjects;
using Clovent.Catalog.Categories;
using Clovent.Catalog.Categories.ValueObjects;
using Clovent.Catalog.Groups;
using Clovent.Catalog.Groups.ValueObjects;
using Clovent.Catalog.Infrastructure.Persistence;
using Clovent.Catalog.Prices;
using Clovent.Catalog.Products;
using Clovent.Catalog.Products.ValueObjects;
using Clovent.Catalog.Shared.ValueObjects;
using Clovent.Catalog.UnitsOfMeasure;
using Clovent.Catalog.UnitsOfMeasure.ValueObjects;
using Clovent.Catalog.Variants;
using Clovent.Catalog.Variants.ValueObjects;
using Clovent.Desktop.Theming;
using Clovent.Identity.Companies;
using Clovent.Identity.Organizations;
using Clovent.Inventory.Infrastructure.Persistence;
using Clovent.Inventory.Transactions;
using Clovent.Inventory.WarehouseStocks;
using Clovent.MasterData.Currencies;
using Clovent.MasterData.Warehouses;
using Clovent.Platform.Bootstrap;
using Microsoft.Extensions.Options;

namespace Clovent.Desktop.Seed;

/// <summary>
/// Development-only convenience: creates one demo Catalog/Inventory data set
/// (a category, group, brand, unit of measure, a product with one variant,
/// its barcode, cost/selling prices, and a receiving transaction into the
/// demo warehouse's stock) if no product exists yet - so the 11 Milestone
/// 14 management screens and dashboard widgets are demonstrable end-to-end
/// without a separate data-entry pass. Depends on
/// <see cref="DevelopmentMasterDataSeedStartupTask"/> having already created
/// the demo Organization/Company/Branch/Warehouse and USD currency (a no-op,
/// not an error, if that hasn't run - mirrors this project's other seed
/// tasks' "nothing to attach to yet" tolerance). Gated by
/// <see cref="DesktopOptions.SeedDevelopmentCatalogData"/>, the same
/// explicit-opt-in reasoning as every other development seed task here.
/// </summary>
public sealed class DevelopmentCatalogSeedStartupTask(
    IOrganizationRepository organizationRepository,
    ICompanyRepository companyRepository,
    IWarehouseRepository warehouseRepository,
    ICurrencyRepository currencyRepository,
    IProductCategoryRepository categoryRepository,
    IProductGroupRepository groupRepository,
    IBrandRepository brandRepository,
    IUnitOfMeasureRepository unitOfMeasureRepository,
    IProductRepository productRepository,
    IProductVariantRepository variantRepository,
    IBarcodeRepository barcodeRepository,
    IProductPriceRepository priceRepository,
    CatalogDbContext catalogDbContext,
    IWarehouseStockRepository warehouseStockRepository,
    IInventoryTransactionRepository transactionRepository,
    InventoryDbContext inventoryDbContext,
    IOptions<DesktopOptions> options) : IStartupTask
{
    /// <inheritdoc/>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!options.Value.SeedDevelopmentCatalogData)
        {
            return;
        }

        var existingProducts = await productRepository.GetAllAsync(cancellationToken);
        if (existingProducts.Count > 0)
        {
            return;
        }

        var organizations = await organizationRepository.GetAllAsync(cancellationToken);
        if (organizations.Count == 0 || organizations.First().CompanyIds.Count == 0)
        {
            return;
        }

        var company = await companyRepository.GetByIdAsync(organizations.First().CompanyIds.First(), cancellationToken);
        if (company is null || company.BranchIds.Count == 0)
        {
            return;
        }

        var warehouses = await warehouseRepository.GetByBranchIdAsync(company.BranchIds.First(), cancellationToken);
        var currency = await currencyRepository.GetByCodeAsync(CurrencyCode.Create("USD"), cancellationToken);
        if (warehouses.Count == 0 || currency is null)
        {
            return;
        }

        var category = ProductCategory.Create(ProductCategoryName.Create("Beverages"));
        var group = ProductGroup.Create(ProductGroupName.Create("Soft Drinks"));
        var brand = Brand.Create(BrandName.Create("Clovent Demo Brand"));
        var each = UnitOfMeasure.Create(UnitOfMeasureCode.Create("EA"), "Each");

        var product = Product.Create(
            ProductName.Create("Cola 500ml"),
            Sku.Create("COLA-500"),
            each.Id,
            TaxConfiguration.Create(15m, false),
            category.Id,
            group.Id,
            brand.Id);

        var variant = ProductVariant.Create(product.Id, VariantName.Create("Standard"), Sku.Create("COLA-500-STD"), each.Id);
        var barcode = Barcode.Create(variant.Id, BarcodeValue.Create("00012345678"), isPrimary: true);
        var costPrice = ProductPrice.Create(variant.Id, PriceType.Cost, 0.50m, currency.Id);
        var sellingPrice = ProductPrice.Create(variant.Id, PriceType.Selling, 1.25m, currency.Id);

        await categoryRepository.AddAsync(category, cancellationToken);
        await groupRepository.AddAsync(group, cancellationToken);
        await brandRepository.AddAsync(brand, cancellationToken);
        await unitOfMeasureRepository.AddAsync(each, cancellationToken);
        await productRepository.AddAsync(product, cancellationToken);
        await variantRepository.AddAsync(variant, cancellationToken);
        await barcodeRepository.AddAsync(barcode, cancellationToken);
        await priceRepository.AddAsync(costPrice, cancellationToken);
        await priceRepository.AddAsync(sellingPrice, cancellationToken);
        await catalogDbContext.SaveChangesAsync(cancellationToken);

        var warehouseId = warehouses.First().Id;
        var stock = WarehouseStock.Create(warehouseId, variant.Id, minimumStock: 10, maximumStock: 500);
        stock.Receive(100);
        var transaction = InventoryTransaction.Create(warehouseId, variant.Id, InventoryTransactionType.Receipt, 100, "Seed", notes: "Initial demo stock");

        await warehouseStockRepository.AddAsync(stock, cancellationToken);
        await transactionRepository.AddAsync(transaction, cancellationToken);
        await inventoryDbContext.SaveChangesAsync(cancellationToken);
    }
}
