using Clovent.Catalog.Application.Variants.Dtos;
using Clovent.Catalog.Variants;
using MediatR;

namespace Clovent.Catalog.Application.Variants.Commands;

/// <summary>Deactivates a product variant.</summary>
public sealed record DeactivateProductVariantCommand(Guid ProductVariantId) : IRequest<ProductVariantDto>;

/// <summary>Handles <see cref="DeactivateProductVariantCommand"/>.</summary>
public sealed class DeactivateProductVariantCommandHandler(IProductVariantRepository repository)
    : IRequestHandler<DeactivateProductVariantCommand, ProductVariantDto>
{
    /// <inheritdoc/>
    public async Task<ProductVariantDto> Handle(DeactivateProductVariantCommand request, CancellationToken cancellationToken)
    {
        var variant = await repository.GetByIdAsync(new ProductVariantId(request.ProductVariantId), cancellationToken)
            ?? throw new NotFoundException(nameof(ProductVariant), request.ProductVariantId);

        variant.Deactivate();
        return ProductVariantDto.FromDomain(variant);
    }
}
