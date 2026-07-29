using Clovent.Catalog.Application.Variants.Dtos;
using Clovent.Catalog.Variants;
using MediatR;

namespace Clovent.Catalog.Application.Variants.Queries;

/// <summary>Retrieves every product variant in the catalog.</summary>
public sealed record ListProductVariantsQuery : IRequest<IReadOnlyCollection<ProductVariantDto>>;

/// <summary>Handles <see cref="ListProductVariantsQuery"/>.</summary>
public sealed class ListProductVariantsQueryHandler(IProductVariantRepository repository)
    : IRequestHandler<ListProductVariantsQuery, IReadOnlyCollection<ProductVariantDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<ProductVariantDto>> Handle(ListProductVariantsQuery request, CancellationToken cancellationToken)
    {
        var variants = await repository.GetAllAsync(cancellationToken);
        return [.. variants.Select(ProductVariantDto.FromDomain)];
    }
}
