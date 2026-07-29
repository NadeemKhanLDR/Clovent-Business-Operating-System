using Clovent.Catalog.Application;
using Clovent.Catalog.Barcodes;
using Clovent.Catalog.Brands;
using Clovent.Catalog.Categories;
using Clovent.Catalog.Groups;
using Clovent.Catalog.Infrastructure.Persistence;
using Clovent.Catalog.Infrastructure.Repositories;
using Clovent.Catalog.Prices;
using Clovent.Catalog.Products;
using Clovent.Catalog.UnitsOfMeasure;
using Clovent.Catalog.Variants;
using Clovent.Platform.Bootstrap;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Clovent.Catalog.Infrastructure.DependencyInjection;

/// <summary>Catalog's own Persistence-layer registration, following the same AddApplication()/AddInfrastructure()/AddPersistence() convention as every other module.</summary>
public static class PersistenceServiceCollectionExtensions
{
    /// <summary>The <c>ConnectionStrings</c> configuration key this module reads its SQL Server connection string from.</summary>
    public const string ConnectionStringName = "Catalog";

    /// <summary>
    /// Registers <see cref="CatalogDbContext"/>, every repository
    /// implementation (Category/Group/Brand/UnitOfMeasure/Product/Variant/Barcode/Price),
    /// the <see cref="IUnitOfWork"/> seam, and an <see cref="IPersistenceInitializer"/>
    /// that applies migrations.
    /// </summary>
    /// <exception cref="InvalidOperationException">No <c>ConnectionStrings:Catalog</c> value is configured.</exception>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Missing required connection string 'ConnectionStrings:{ConnectionStringName}'.");

        services.AddDbContext<CatalogDbContext>(options => options.UseSqlServer(connectionString));

        services.TryAddScoped<IProductCategoryRepository, ProductCategoryRepository>();
        services.TryAddScoped<IProductGroupRepository, ProductGroupRepository>();
        services.TryAddScoped<IBrandRepository, BrandRepository>();
        services.TryAddScoped<IUnitOfMeasureRepository, UnitOfMeasureRepository>();
        services.TryAddScoped<IProductRepository, ProductRepository>();
        services.TryAddScoped<IProductVariantRepository, ProductVariantRepository>();
        services.TryAddScoped<IBarcodeRepository, BarcodeRepository>();
        services.TryAddScoped<IProductPriceRepository, ProductPriceRepository>();

        services.TryAddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IPersistenceInitializer, CatalogPersistenceInitializer>();

        return services;
    }
}
