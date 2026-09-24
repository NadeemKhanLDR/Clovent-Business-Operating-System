using Clovent.Catalog.Application.Categories.Dtos;
using Clovent.Catalog.Application.Categories.Queries;
using Clovent.Catalog.Application.Prices.Dtos;
using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Application.Products.Dtos;
using Clovent.Catalog.Application.Products.Queries;
using Clovent.Catalog.Application.Variants.Dtos;
using Clovent.Catalog.Application.Variants.Queries;

namespace Clovent.Restaurant.Application.Tests.TestSupport;

/// <summary>
/// Builds the catalog Application-layer DTOs the smart POS handlers consume,
/// plus a <see cref="FakeMediator"/> answering the four catalog queries from
/// an in-memory set of them - the cross-context seam's stand-in, the same
/// approach <c>AddOrderLineCommandHandler</c>'s tests use.
/// </summary>
internal static class CatalogFakes
{
    public static ProductVariantDto Variant(Guid variantId, Guid productId, string name, string status = "Active", string? productStatus = "Active", int sortOrder = 0) =>
        new(variantId, productId, name, $"SKU-{name}", Guid.NewGuid(), status, sortOrder, DateTimeOffset.UtcNow, null, productStatus);

    public static ProductDto Product(Guid productId, string name, Guid? categoryId = null) =>
        new(productId, name, $"SKU-{name}", categoryId, null, null, Guid.NewGuid(), 0m, true, "Active", DateTimeOffset.UtcNow);

    public static ProductPriceDto SellingPrice(Guid variantId, decimal amount, DateTimeOffset? effectiveFromUtc = null) =>
        new(Guid.NewGuid(), variantId, "Selling", amount, Guid.NewGuid(), effectiveFromUtc ?? DateTimeOffset.UtcNow, "Active", DateTimeOffset.UtcNow);

    public static ProductCategoryDto Category(Guid categoryId, string name) =>
        new(categoryId, name, null, "Active", null, 0, DateTimeOffset.UtcNow);

    public static FakeMediator Mediator(
        IReadOnlyCollection<ProductVariantDto>? variants = null,
        IReadOnlyCollection<ProductDto>? products = null,
        IReadOnlyCollection<ProductPriceDto>? prices = null,
        IReadOnlyCollection<ProductCategoryDto>? categories = null) =>
        new(request => Task.FromResult<object?>(
            request switch
            {
                ListProductVariantsQuery => variants ?? [],
                ListProductsQuery => products ?? [],
                ListActiveProductPricesByTypeQuery => prices ?? [],
                ListProductCategoriesQuery => categories ?? [],
                _ => throw new NotSupportedException($"Unexpected catalog query {request.GetType().Name}.")
            }));
}
