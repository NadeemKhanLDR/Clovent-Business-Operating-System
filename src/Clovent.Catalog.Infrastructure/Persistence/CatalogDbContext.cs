using Clovent.Catalog.Barcodes;
using Clovent.Catalog.Brands;
using Clovent.Catalog.Categories;
using Clovent.Catalog.Groups;
using Clovent.Catalog.Prices;
using Clovent.Catalog.Products;
using Clovent.Catalog.UnitsOfMeasure;
using Clovent.Catalog.Variants;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Catalog.Infrastructure.Persistence;

/// <summary>
/// EF Core persistence context for the Catalog bounded context (Milestone
/// 14, "Product Catalog &amp; Inventory Foundation"). Tables live under the
/// <c>Catalog</c> schema, mirroring how <c>MasterDataDbContext</c> uses the
/// <c>MasterData</c> schema.
/// </summary>
public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    /// <summary>ProductCategory aggregates.</summary>
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();

    /// <summary>ProductGroup aggregates.</summary>
    public DbSet<ProductGroup> ProductGroups => Set<ProductGroup>();

    /// <summary>Brand aggregates.</summary>
    public DbSet<Brand> Brands => Set<Brand>();

    /// <summary>UnitOfMeasure aggregates.</summary>
    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();

    /// <summary>Product aggregates.</summary>
    public DbSet<Product> Products => Set<Product>();

    /// <summary>ProductVariant aggregates.</summary>
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

    /// <summary>Barcode aggregates.</summary>
    public DbSet<Barcode> Barcodes => Set<Barcode>();

    /// <summary>ProductPrice aggregates.</summary>
    public DbSet<ProductPrice> ProductPrices => Set<ProductPrice>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}
