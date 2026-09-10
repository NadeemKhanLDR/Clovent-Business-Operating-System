using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
using Clovent.Catalog.Shared;
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
        if (options.Value.SeedDevelopmentCatalogData)
        {
            var existingProducts = await productRepository.GetAllAsync(cancellationToken);
            var organizations = await organizationRepository.GetAllAsync(cancellationToken);
            if (organizations.Count > 0 && organizations.First().CompanyIds.Count > 0)
            {
                var company = await companyRepository.GetByIdAsync(organizations.First().CompanyIds.First(), cancellationToken);
                if (company is not null && company.BranchIds.Count > 0)
                {
                    var warehouses = await warehouseRepository.GetByBranchIdAsync(company.BranchIds.First(), cancellationToken);
                    var currency = await currencyRepository.GetByCodeAsync(CurrencyCode.Create("USD"), cancellationToken);
                    if (warehouses.Count > 0 && currency is not null)
                    {
                        if (!existingProducts.Any(p => p.Sku.Value == "COLA-500"))
                        {
                            var category = ProductCategory.Create(ProductCategoryName.Create("Beverages"));
                            var each = UnitOfMeasure.Create(UnitOfMeasureCode.Create("EA"), "Each");
                            var group = ProductGroup.Create(ProductGroupName.Create("Soft Drinks"));
                            var brand = Brand.Create(BrandName.Create("Clovent Demo Brand"));

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
                }
            }
        }

        // Seed/Update target Pakistani dishes UNCONDITIONALLY on startup!
        await SeedPakistaniCuisineDishesAsync(cancellationToken);
    }

    private async Task SeedPakistaniCuisineDishesAsync(CancellationToken cancellationToken)
    {
        var existingProducts = await productRepository.GetAllAsync(cancellationToken);
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
        var currency = (await currencyRepository.GetAllAsync(cancellationToken)).FirstOrDefault()
            ?? await currencyRepository.GetByCodeAsync(CurrencyCode.Create("USD"), cancellationToken);
        if (warehouses.Count == 0 || currency is null)
        {
            return;
        }

        var each = (await unitOfMeasureRepository.GetAllAsync(cancellationToken)).FirstOrDefault(u => u.Code.Value == "EA")
            ?? UnitOfMeasure.Create(UnitOfMeasureCode.Create("EA"), "Each");
        var group = (await groupRepository.GetAllAsync(cancellationToken)).FirstOrDefault()
            ?? ProductGroup.Create(ProductGroupName.Create("Soft Drinks"));
        var brand = (await brandRepository.GetAllAsync(cancellationToken)).FirstOrDefault()
            ?? Brand.Create(BrandName.Create("Clovent Demo Brand"));

        if (each.Id.Value == Guid.Empty)
        {
            await unitOfMeasureRepository.AddAsync(each, cancellationToken);
        }
        if (group.Id.Value == Guid.Empty)
        {
            await groupRepository.AddAsync(group, cancellationToken);
        }
        if (brand.Id.Value == Guid.Empty)
        {
            await brandRepository.AddAsync(brand, cancellationToken);
        }
        await catalogDbContext.SaveChangesAsync(cancellationToken);

        // Create Main Course Category if not exists
        var mainCourseCategory = (await categoryRepository.GetAllAsync(cancellationToken))
            .FirstOrDefault(c => string.Equals(c.Name.Value, "Main Course", StringComparison.OrdinalIgnoreCase))
            ?? ProductCategory.Create(ProductCategoryName.Create("Main Course"));
        if (mainCourseCategory.Id.Value == Guid.Empty)
        {
            await categoryRepository.AddAsync(mainCourseCategory, cancellationToken);
            await catalogDbContext.SaveChangesAsync(cancellationToken);
        }

        // Seed/Update target menu items

        // 1. Chicken Karahi (Standard price: 1200)
        await SeedMultiVariantProductAsync(
            "Chicken Karahi",
            "CHICKEN-KARAHI",
            "Main Course",
            "Desi-Chicken-Karahi.jpg",
            [("Standard", 1200m, "STD", "888888880000")], // keep standard barcode 0001 or standard 0000
            each, mainCourseCategory, group, brand, currency, existingProducts, cancellationToken);

        // 2. White Daal Mash (Half: 220, Full: 340)
        await SeedMultiVariantProductAsync(
            "White Daal Mash",
            "WHITE-DAAL-MASH",
            "Main Course",
            "WHITE-DAAL-MASH.jpg",
            [
                ("Half Plate", 220m, "HALF", "888888880002"),
                ("Full Plate", 340m, "FULL", "888888880003")
            ],
            each, mainCourseCategory, group, brand, currency, existingProducts, cancellationToken);

        // 3. Aloo Chicken Qorma (Standard: 650)
        await SeedMultiVariantProductAsync(
            "Aloo Chicken Qorma",
            "ALOO-CHICKEN-QORMA",
            "Main Course",
            "ALOO-CHICKEN-QORMA.jpg",
            [("Standard", 650m, "STD", "888888880004")],
            each, mainCourseCategory, group, brand, currency, existingProducts, cancellationToken);

        // 4. Chicken Biryani (Standard: 450)
        await SeedMultiVariantProductAsync(
            "Chicken Biryani",
            "CHICKEN-BIRYANI",
            "Main Course",
            "CHICKEN-BIRYANI.jpg",
            [("Standard", 450m, "STD", "888888880005")],
            each, mainCourseCategory, group, brand, currency, existingProducts, cancellationToken);

        // 5. Chicken Haleem (Half: 260, Full: 420)
        await SeedMultiVariantProductAsync(
            "Chicken Haleem",
            "CHICKEN-HALEEM",
            "Main Course",
            "Chicken-Haleem.jpg",
            [
                ("Half Plate", 260m, "HALF", "888888880011"),
                ("Full Plate", 420m, "FULL", "888888880012")
            ],
            each, mainCourseCategory, group, brand, currency, existingProducts, cancellationToken);

        // 6. Murgh Chanay (Half: 260, Full: 400)
        await SeedMultiVariantProductAsync(
            "Murgh Chanay",
            "MURGH-CHANAY",
            "Main Course",
            "MURGH-CHANAY.jpg",
            [
                ("Half Plate", 260m, "HALF", "888888880013"),
                ("Full Plate", 400m, "FULL", "888888880014")
            ],
            each, mainCourseCategory, group, brand, currency, existingProducts, cancellationToken);

        // 7. Chicken Koyla Karahi (Half: 350, Full: 550)
        await SeedMultiVariantProductAsync(
            "Chicken Koyla Karahi",
            "CHICKEN-KOYLA-KARAHI",
            "Main Course",
            "Desi-Chicken-Karahi.jpg", // closest appropriate existing image
            [
                ("Half Plate", 350m, "HALF", "888888880015"),
                ("Full Plate", 550m, "FULL", "888888880016")
            ],
            each, mainCourseCategory, group, brand, currency, existingProducts, cancellationToken);

        // 8. Aloo Gobi (Half: 250, Full: 380)
        await SeedMultiVariantProductAsync(
            "Aloo Gobi",
            "ALOO-GOBI",
            "Main Course",
            "", // no genuinely suitable image
            [
                ("Half Plate", 250m, "HALF", "888888880017"),
                ("Full Plate", 380m, "FULL", "888888880018")
            ],
            each, mainCourseCategory, group, brand, currency, existingProducts, cancellationToken);

        // 9. Salad (Price: 30)
        await SeedMultiVariantProductAsync(
            "Salad",
            "SALAD",
            "Main Course",
            "SALAD-RAITA.jpg",
            [("Standard", 30m, "STD", "888888880019")],
            each, mainCourseCategory, group, brand, currency, existingProducts, cancellationToken);
    }

    private async Task SeedMultiVariantProductAsync(
        string productName,
        string sku,
        string categoryName,
        string imageName,
        (string Name, decimal Price, string SkuSuffix, string Barcode)[] portions,
        UnitOfMeasure each,
        ProductCategory defaultCategory,
        ProductGroup group,
        Brand brand,
        Currency currency,
        IReadOnlyCollection<Product> existingProducts,
        CancellationToken cancellationToken)
    {
        var product = existingProducts.FirstOrDefault(p => 
            p.Sku.Value == sku || 
            p.Sku.Value == "DAAL-MASH" && sku == "WHITE-DAAL-MASH" ||
            string.Equals(p.Name.Value, productName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(p.Name.Value, "Daal Mash", StringComparison.OrdinalIgnoreCase) && productName == "White Daal Mash");

        ProductCategory category = defaultCategory;
        if (categoryName != "Main Course")
        {
            var cats = await categoryRepository.GetAllAsync(cancellationToken);
            category = cats.FirstOrDefault(c => string.Equals(c.Name.Value, categoryName, StringComparison.OrdinalIgnoreCase))
                ?? ProductCategory.Create(ProductCategoryName.Create(categoryName));
            
            if (category.Id.Value == Guid.Empty || !cats.Contains(category))
            {
                await categoryRepository.AddAsync(category, cancellationToken);
                await catalogDbContext.SaveChangesAsync(cancellationToken);
            }
        }

        if (product is null)
        {
            product = Product.Create(
                ProductName.Create(productName),
                Sku.Create(sku),
                each.Id,
                TaxConfiguration.Create(0m, false),
                category.Id,
                group.Id,
                brand.Id);
            await productRepository.AddAsync(product, cancellationToken);
            await catalogDbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            product.Rename(ProductName.Create(productName));
            product.SetCategory(category.Id);
        }

        var dbVariants = await variantRepository.GetByProductIdAsync(product.Id, cancellationToken);

        foreach (var portion in portions)
        {
            var actualSku = product.Sku.Value;
            var portionSku = $"{actualSku}-{portion.SkuSuffix}";
            if (portionSku.Length > 40) portionSku = portionSku[..40];

            var variant = dbVariants.FirstOrDefault(v => 
                v.Sku.Value == portionSku || 
                string.Equals(v.Name.Value, portion.Name, StringComparison.OrdinalIgnoreCase));

            if (variant is null)
            {
                variant = ProductVariant.Create(product.Id, VariantName.Create(portion.Name), Sku.Create(portionSku), each.Id);
                await variantRepository.AddAsync(variant, cancellationToken);
                await catalogDbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                variant.Rename(VariantName.Create(portion.Name));
                if (variant.Status != CatalogStatus.Active)
                {
                    variant.Activate();
                }
            }

            // Prices
            var prices = await priceRepository.GetByProductVariantIdAsync(variant.Id, cancellationToken);
            var sellingPrice = prices.FirstOrDefault(p => p.PriceType == PriceType.Selling && p.Status == CatalogStatus.Active);
            if (sellingPrice is not null)
            {
                sellingPrice.UpdateAmount(portion.Price);
            }
            else
            {
                sellingPrice = ProductPrice.Create(variant.Id, PriceType.Selling, portion.Price, currency.Id);
                await priceRepository.AddAsync(sellingPrice, cancellationToken);
            }

            // Barcode
            var barcodes = await barcodeRepository.GetByProductVariantIdAsync(variant.Id, cancellationToken);
            var barcode = barcodes.FirstOrDefault();
            if (barcode is null)
            {
                barcode = Barcode.Create(variant.Id, BarcodeValue.Create(portion.Barcode), isPrimary: true);
                await barcodeRepository.AddAsync(barcode, cancellationToken);
            }
        }

        var activePortionNames = portions.Select(p => p.Name).ToList();
        foreach (var v in dbVariants.Where(v => v.Status == CatalogStatus.Active && !activePortionNames.Any(apn => string.Equals(apn, v.Name.Value, StringComparison.OrdinalIgnoreCase))))
        {
            v.Deactivate();
        }

        await catalogDbContext.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrEmpty(imageName))
        {
            SaveImageIfExists(product.Id.Value, imageName);
        }
    }

    private static string? FindImagesDirectory()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            var testPath = Path.Combine(current, "Images");
            if (Directory.Exists(testPath))
            {
                return testPath;
            }
            var parent = Directory.GetParent(current);
            if (parent == null || parent.FullName == current)
            {
                break;
            }
            current = parent.FullName;
        }
        return null;
    }

    private static void SaveImageIfExists(Guid productId, string fileName)
    {
        var imagesDir = FindImagesDirectory();
        if (imagesDir is null) return;

        var allFiles = Directory.GetFiles(imagesDir, "*", SearchOption.AllDirectories);

        var searchName = Path.GetFileNameWithoutExtension(fileName).Replace("-", "").Replace("_", "").ToLowerInvariant();

        var exactMatch = allFiles.FirstOrDefault(f => 
            string.Equals(Path.GetFileName(f), fileName, StringComparison.OrdinalIgnoreCase));
        if (exactMatch is not null)
        {
            if (TrySaveImage(productId, exactMatch)) return;
        }

        var fuzzyMatches = allFiles
            .Select(f => new { Path = f, Normalized = Path.GetFileNameWithoutExtension(f).Replace("-", "").Replace("_", "").ToLowerInvariant() })
            .Where(x => x.Normalized == searchName || x.Normalized.Contains(searchName) || searchName.Contains(x.Normalized))
            .OrderByDescending(x => x.Normalized == searchName)
            .ThenByDescending(x => x.Path.Contains("200x200"))
            .ThenByDescending(x => x.Path.Contains("600x600"))
            .ToList();

        foreach (var match in fuzzyMatches)
        {
            if (TrySaveImage(productId, match.Path)) return;
        }
    }

    private static bool TrySaveImage(Guid productId, string filePath)
    {
        if (File.Exists(filePath))
        {
            try
            {
                using var img = Image.FromFile(filePath);
                Clovent.Desktop.Forms.Restaurant.MenuItems.MenuItemImageStore.Save(productId, img);
                return true;
            }
            catch
            {
                // Ignore and try next
            }
        }
        return false;
    }
}
