using Clovent.Catalog.Application.Categories.Queries;
using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Application.Products.Queries;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Restaurant.Application.UniversalPosSearch.Dtos;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Tables;
using MediatR;

namespace Clovent.Restaurant.Application.UniversalPosSearch.Queries;

/// <summary>
/// The POS search box: one term matched simultaneously against sellable
/// catalog products (variant/product name), active customers (code, name,
/// phone numbers), restaurant orders (order number, most recent first), and
/// floor tables (name). Each category is capped at
/// <see cref="MaxResultsPerCategory"/> (default 5) hits; a term shorter than
/// two characters returns an empty result set.
/// </summary>
public sealed record UniversalPosSearchQuery(string Term, int MaxResultsPerCategory = 5)
    : IRequest<UniversalPosSearchResultsDto>;

/// <summary>Handles <see cref="UniversalPosSearchQuery"/>.</summary>
public sealed class UniversalPosSearchQueryHandler(
    ICustomerRepository customerRepository,
    IOrderRepository orderRepository,
    IOrderLineRepository orderLineRepository,
    ITableRepository tableRepository,
    IDiningAreaRepository diningAreaRepository,
    IMediator mediator) : IRequestHandler<UniversalPosSearchQuery, UniversalPosSearchResultsDto>
{
    private const int MinimumTermLength = 2;

    /// <inheritdoc/>
    public async Task<UniversalPosSearchResultsDto> Handle(UniversalPosSearchQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Term) || request.Term.Trim().Length < MinimumTermLength)
        {
            return new UniversalPosSearchResultsDto([], [], [], []);
        }

        var term = request.Term.Trim();
        var take = Math.Max(1, request.MaxResultsPerCategory);

        var products = await SearchProductsAsync(term, take, cancellationToken);
        var customers = await SearchCustomersAsync(term, take, cancellationToken);
        var orders = await SearchOrdersAsync(term, take, cancellationToken);
        var tables = await SearchTablesAsync(term, take, cancellationToken);

        return new UniversalPosSearchResultsDto(products, customers, orders, tables);
    }

    private async Task<IReadOnlyCollection<UniversalPosProductDto>> SearchProductsAsync(string term, int take, CancellationToken cancellationToken)
    {
        // The same catalog read model the POS product wall uses, filtered client-side by contains.
        var variants = await mediator.Send(new ListProductVariantsQuery(), cancellationToken);
        var products = await mediator.Send(new ListProductsQuery(), cancellationToken);
        var sellingPrices = await mediator.Send(new ListActiveProductPricesByTypeQuery(Clovent.Catalog.Prices.PriceType.Selling), cancellationToken);
        var categories = await mediator.Send(new ListProductCategoriesQuery(), cancellationToken);

        var productById = products.ToDictionary(p => p.ProductId);
        var categoryNameById = categories.ToDictionary(c => c.ProductCategoryId, c => c.Name);
        var unitPriceByVariantId = sellingPrices
            .GroupBy(p => p.ProductVariantId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.EffectiveFromUtc).First().Amount);

        var matches = variants
            .Where(v => v.Status == "Active" && (v.ProductStatus is null || v.ProductStatus == "Active"))
            .Where(v => Contains(v.Name, term)
                || (productById.TryGetValue(v.ProductId, out var product) && Contains(product.Name, term)))
            .OrderBy(v => v.SortOrder)
            .ThenBy(v => v.Name)
            .Select(v => new UniversalPosProductDto(
                v.ProductVariantId,
                productById.TryGetValue(v.ProductId, out var product) ? product.Name : "(unknown product)",
                v.Name,
                unitPriceByVariantId.GetValueOrDefault(v.ProductVariantId, 0m),
                productById.TryGetValue(v.ProductId, out var owner) && owner.CategoryId is not null && categoryNameById.TryGetValue(owner.CategoryId.Value, out var category)
                    ? category
                    : null))
            .Take(take)
            .ToList();

        return matches;
    }

    private async Task<IReadOnlyCollection<UniversalPosCustomerDto>> SearchCustomersAsync(string term, int take, CancellationToken cancellationToken)
    {
        var customers = await customerRepository.GetAllAsync(cancellationToken);

        return [.. customers
            .Where(c => c.IsActive)
            .Where(c => Contains(c.Code.Value, term)
                || Contains(c.Name, term)
                || Contains(c.MobileNumber, term)
                || (c.Mobile2 is not null && Contains(c.Mobile2, term))
                || (c.Phone is not null && Contains(c.Phone, term)))
            .OrderBy(c => c.Name)
            .Take(take)
            .Select(c => new UniversalPosCustomerDto(c.Id.Value, c.Code.Value, c.Name, c.MobileNumber))];
    }

    private async Task<IReadOnlyCollection<UniversalPosOrderDto>> SearchOrdersAsync(string term, int take, CancellationToken cancellationToken)
    {
        var orders = await orderRepository.GetAllAsync(cancellationToken);

        var matches = orders
            .Where(o => Contains(o.OrderNumber.Value, term))
            .OrderByDescending(o => o.CreatedAtUtc)
            .Take(take)
            .ToList();

        var results = new List<UniversalPosOrderDto>();
        foreach (var order in matches)
        {
            var lines = await orderLineRepository.GetByOrderIdAsync(order.Id, cancellationToken);
            var total = lines.Where(l => !l.IsVoided).Sum(l => l.LineTotal);

            results.Add(new UniversalPosOrderDto(
                order.Id.Value,
                order.OrderNumber.Value,
                order.OrderType.ToString(),
                total,
                order.Status.ToString()));
        }

        return results;
    }

    private async Task<IReadOnlyCollection<UniversalPosTableDto>> SearchTablesAsync(string term, int take, CancellationToken cancellationToken)
    {
        var tables = await tableRepository.GetAllAsync(cancellationToken);
        var areas = await diningAreaRepository.GetAllAsync(cancellationToken);
        var areaNameById = areas.ToDictionary(a => a.Id, a => a.Name.Value);

        var openOrderTableIds = (await orderRepository.GetOpenAsync(cancellationToken))
            .Concat(await orderRepository.GetHeldAsync(cancellationToken))
            .Where(o => o.TableId is not null)
            .Select(o => o.TableId!.Value)
            .ToHashSet();

        return [.. tables
            .Where(t => Contains(t.Name.Value, term) || Contains(t.Code.Value, term))
            .OrderBy(t => t.Name.Value)
            .Take(take)
            .Select(t => new UniversalPosTableDto(
                t.Id.Value,
                t.Name.Value,
                areaNameById.TryGetValue(t.DiningAreaId, out var area) ? area : null,
                openOrderTableIds.Contains(t.Id)))];
    }

    private static bool Contains(string value, string term) =>
        value.Contains(term, StringComparison.OrdinalIgnoreCase);
}
