using System.Text.Json;
using Clovent.Catalog.Barcodes;
using Clovent.Catalog.Barcodes.ValueObjects;
using Clovent.Catalog.Brands;
using Clovent.Catalog.Brands.ValueObjects;
using Clovent.Catalog.Categories;
using Clovent.Catalog.Categories.ValueObjects;
using Clovent.Catalog.Groups;
using Clovent.Catalog.Groups.ValueObjects;
using Clovent.Catalog.Prices;
using Clovent.Catalog.Products;
using Clovent.Catalog.Products.ValueObjects;
using Clovent.Catalog.Shared.ValueObjects;
using Clovent.Catalog.UnitsOfMeasure;
using Clovent.Catalog.UnitsOfMeasure.ValueObjects;
using Clovent.Catalog.Variants;
using Clovent.Catalog.Variants.ValueObjects;
using Clovent.MasterData.Currencies;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Clovent.Catalog.Infrastructure.Persistence;

/// <summary>
/// EF Core <see cref="ValueConverter{TModel,TProvider}"/>s shared across
/// this project's entity type configurations - see
/// <c>Clovent.MasterData.Infrastructure.Persistence.ValueConverters</c> for
/// the identical pattern and reasoning.
/// </summary>
internal static class ValueConverters
{
    /// <summary><see cref="ProductCategoryId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<ProductCategoryId, Guid> ProductCategoryIdConverter =
        new(id => id.Value, value => new ProductCategoryId(value));

    /// <summary>Nullable <see cref="ProductCategoryId"/> &lt;-&gt; nullable <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<ProductCategoryId?, Guid?> NullableProductCategoryIdConverter =
        new(id => id == null ? null : id.Value.Value, value => value == null ? null : new ProductCategoryId(value.Value));

    /// <summary><see cref="ProductGroupId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<ProductGroupId, Guid> ProductGroupIdConverter =
        new(id => id.Value, value => new ProductGroupId(value));

    /// <summary>Nullable <see cref="ProductGroupId"/> &lt;-&gt; nullable <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<ProductGroupId?, Guid?> NullableProductGroupIdConverter =
        new(id => id == null ? null : id.Value.Value, value => value == null ? null : new ProductGroupId(value.Value));

    /// <summary><see cref="BrandId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<BrandId, Guid> BrandIdConverter =
        new(id => id.Value, value => new BrandId(value));

    /// <summary>Nullable <see cref="BrandId"/> &lt;-&gt; nullable <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<BrandId?, Guid?> NullableBrandIdConverter =
        new(id => id == null ? null : id.Value.Value, value => value == null ? null : new BrandId(value.Value));

    /// <summary><see cref="UnitOfMeasureId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<UnitOfMeasureId, Guid> UnitOfMeasureIdConverter =
        new(id => id.Value, value => new UnitOfMeasureId(value));

    /// <summary><see cref="ProductId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<ProductId, Guid> ProductIdConverter =
        new(id => id.Value, value => new ProductId(value));

    /// <summary><see cref="ProductVariantId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<ProductVariantId, Guid> ProductVariantIdConverter =
        new(id => id.Value, value => new ProductVariantId(value));

    /// <summary><see cref="BarcodeId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<BarcodeId, Guid> BarcodeIdConverter =
        new(id => id.Value, value => new BarcodeId(value));

    /// <summary><see cref="ProductPriceId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<ProductPriceId, Guid> ProductPriceIdConverter =
        new(id => id.Value, value => new ProductPriceId(value));

    /// <summary><see cref="CurrencyId"/> (from <c>Clovent.MasterData</c>) &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<CurrencyId, Guid> CurrencyIdConverter =
        new(id => id.Value, value => new CurrencyId(value));

    /// <summary><see cref="ProductCategoryName"/> &lt;-&gt; name text.</summary>
    public static readonly ValueConverter<ProductCategoryName, string> ProductCategoryNameConverter =
        new(v => v.Value, v => ProductCategoryName.Create(v));

    /// <summary><see cref="ProductGroupName"/> &lt;-&gt; name text.</summary>
    public static readonly ValueConverter<ProductGroupName, string> ProductGroupNameConverter =
        new(v => v.Value, v => ProductGroupName.Create(v));

    /// <summary><see cref="BrandName"/> &lt;-&gt; name text.</summary>
    public static readonly ValueConverter<BrandName, string> BrandNameConverter =
        new(v => v.Value, v => BrandName.Create(v));

    /// <summary><see cref="UnitOfMeasureCode"/> &lt;-&gt; code text.</summary>
    public static readonly ValueConverter<UnitOfMeasureCode, string> UnitOfMeasureCodeConverter =
        new(v => v.Value, v => UnitOfMeasureCode.Create(v));

    /// <summary><see cref="ProductName"/> &lt;-&gt; name text.</summary>
    public static readonly ValueConverter<ProductName, string> ProductNameConverter =
        new(v => v.Value, v => ProductName.Create(v));

    /// <summary><see cref="Sku"/> &lt;-&gt; code text.</summary>
    public static readonly ValueConverter<Sku, string> SkuConverter =
        new(v => v.Value, v => Sku.Create(v));

    /// <summary><see cref="VariantName"/> &lt;-&gt; name text.</summary>
    public static readonly ValueConverter<VariantName, string> VariantNameConverter =
        new(v => v.Value, v => VariantName.Create(v));

    /// <summary><see cref="BarcodeValue"/> &lt;-&gt; digit string.</summary>
    public static readonly ValueConverter<BarcodeValue, string> BarcodeValueConverter =
        new(v => v.Value, v => BarcodeValue.Create(v));

    /// <summary>
    /// <see cref="TaxConfiguration"/> &lt;-&gt; a single JSON column - the
    /// identical reasoning as <c>Clovent.Identity.Infrastructure.Persistence.ValueConverters.AddressConverter</c>:
    /// a multi-field value object mapped as a converter, not an EF Core
    /// owned type, so it stays constructor-bindable on <see cref="Products.Product"/>.
    /// </summary>
    public static readonly ValueConverter<TaxConfiguration, string> TaxConfigurationConverter = new(
        v => JsonSerializer.Serialize(new TaxConfigurationJson(v.RatePercentage, v.IsInclusive), (JsonSerializerOptions?)null),
        v => Deserialize(v));

    private static TaxConfiguration Deserialize(string json)
    {
        var dto = JsonSerializer.Deserialize<TaxConfigurationJson>(json, (JsonSerializerOptions?)null)!;
        return TaxConfiguration.Create(dto.RatePercentage, dto.IsInclusive);
    }

    private sealed record TaxConfigurationJson(decimal RatePercentage, bool IsInclusive);
}
