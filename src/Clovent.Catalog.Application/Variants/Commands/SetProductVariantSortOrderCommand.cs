using Clovent.Catalog.Application.Variants.Dtos;
using Clovent.Catalog.Variants;
using MediatR;

namespace Clovent.Catalog.Application.Variants.Commands;

/// <summary>Sets a product variant's manual display position for owner-driven drag-drop reordering (e.g. Menu Items/the POS tile wall).</summary>
public sealed record SetProductVariantSortOrderCommand(Guid ProductVariantId, int SortOrder) : IRequest<ProductVariantDto>;

/// <summary>Handles <see cref="SetProductVariantSortOrderCommand"/>.</summary>
public sealed class SetProductVariantSortOrderCommandHandler(IProductVariantRepository repository)
    : IRequestHandler<SetProductVariantSortOrderCommand, ProductVariantDto>
{
    /// <inheritdoc/>
    public async Task<ProductVariantDto> Handle(SetProductVariantSortOrderCommand request, CancellationToken cancellationToken)
    {
        var variant = await repository.GetByIdAsync(new ProductVariantId(request.ProductVariantId), cancellationToken)
            ?? throw new NotFoundException(nameof(ProductVariant), request.ProductVariantId);

        variant.SetSortOrder(request.SortOrder);
        return ProductVariantDto.FromDomain(variant);
    }
}
