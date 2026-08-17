using Clovent.Catalog.Application.Products.Dtos;
using Clovent.Catalog.Categories;
using Clovent.Catalog.Prices;
using Clovent.Catalog.Products;
using Clovent.Catalog.Products.ValueObjects;
using Clovent.Catalog.Shared.ValueObjects;
using Clovent.Catalog.UnitsOfMeasure;
using Clovent.Catalog.Variants;
using Clovent.Catalog.Variants.ValueObjects;
using Clovent.MasterData.Currencies;
using MediatR;

namespace Clovent.Catalog.Application.Products.Commands;

/// <summary>
/// Creates a Product, a single default Variant carrying the same name/SKU,
/// and one active Selling <see cref="ProductPrice"/> for that variant
/// - a one-shot alternative to the Products/Product Variants/Prices
/// three-screen flow, for callers (like the Restaurant "Add Menu Item"
/// dialog) that only need "name, category, one price" and don't want the
/// SKU/unit-of-measure split surfaced. Direct repository calls, not nested
/// <c>IMediator.Send</c> - all three aggregates are in this same
/// bounded context and the same <c>UnitOfWorkBehavior</c> transaction, the
/// same shape every other same-context handler in this solution already
/// uses (see <see cref="CreateProductCommandHandler"/>).
/// </summary>
public sealed record CreateProductWithPriceCommand(
    string Name,
    Guid? CategoryId,
    decimal SellingPrice,
    Guid CurrencyId,
    Guid BaseUnitOfMeasureId,
    bool IsActive = true) : IRequest<ProductDto>;

/// <summary>Handles <see cref="CreateProductWithPriceCommand"/>.</summary>
public sealed class CreateProductWithPriceCommandHandler(
    IProductRepository productRepository,
    IProductVariantRepository variantRepository,
    IProductPriceRepository priceRepository) : IRequestHandler<CreateProductWithPriceCommand, ProductDto>
{
    /// <inheritdoc/>
    public async Task<ProductDto> Handle(CreateProductWithPriceCommand request, CancellationToken cancellationToken)
    {
        var name = ProductName.Create(request.Name);
        var sku = await GenerateUniqueSkuAsync(name.Value, cancellationToken);
        var categoryId = request.CategoryId is { } rawCategoryId ? new ProductCategoryId(rawCategoryId) : (ProductCategoryId?)null;

        var product = Product.Create(name, sku, new UnitOfMeasureId(request.BaseUnitOfMeasureId), categoryId: categoryId);
        await productRepository.AddAsync(product, cancellationToken);

        var variant = ProductVariant.Create(product.Id, VariantName.Create(name.Value), sku, new UnitOfMeasureId(request.BaseUnitOfMeasureId));
        await variantRepository.AddAsync(variant, cancellationToken);

        var price = ProductPrice.Create(variant.Id, PriceType.Selling, request.SellingPrice, new CurrencyId(request.CurrencyId));
        await priceRepository.AddAsync(price, cancellationToken);

        if (!request.IsActive)
        {
            // RestaurantPosView filters product tiles on ProductVariant.Status,
            // not Product.Status - both must be deactivated or an "inactive"
            // menu item would still be sellable in POS.
            product.Deactivate();
            variant.Deactivate();
        }

        return ProductDto.FromDomain(product);
    }

    /// <summary>Slugs <paramref name="name"/> into a <see cref="Sku"/>-shaped code, appending a numeric suffix until it is unique among Products (Products and Variants each enforce uniqueness independently, but this command gives both the same code, so checking Products alone is sufficient here).</summary>
    private async Task<Sku> GenerateUniqueSkuAsync(string name, CancellationToken cancellationToken)
    {
        var baseCode = new string([.. name.ToUpperInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-')]).Trim('-');
        if (baseCode.Length < 2)
        {
            baseCode = baseCode.PadRight(2, 'X');
        }
        if (baseCode.Length > 36)
        {
            baseCode = baseCode[..36];
        }

        var candidate = baseCode;
        var suffix = 1;
        while (await productRepository.GetBySkuAsync(Sku.Create(candidate), cancellationToken) is not null)
        {
            suffix++;
            candidate = $"{baseCode}-{suffix}";
        }

        return Sku.Create(candidate);
    }
}
