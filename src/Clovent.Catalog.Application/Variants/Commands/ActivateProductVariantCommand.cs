using Clovent.Catalog.Application.Variants.Dtos;
using Clovent.Catalog.Variants;
using MediatR;

namespace Clovent.Catalog.Application.Variants.Commands;

/// <summary>Activates a product variant.</summary>
public sealed record ActivateProductVariantCommand(Guid ProductVariantId) : IRequest<ProductVariantDto>;

/// <summary>Handles <see cref="ActivateProductVariantCommand"/>.</summary>
public sealed class ActivateProductVariantCommandHandler(IProductVariantRepository repository)
    : IRequestHandler<ActivateProductVariantCommand, ProductVariantDto>
{
    /// <inheritdoc/>
    public async Task<ProductVariantDto> Handle(ActivateProductVariantCommand request, CancellationToken cancellationToken)
    {
        var variant = await repository.GetByIdAsync(new ProductVariantId(request.ProductVariantId), cancellationToken)
            ?? throw new NotFoundException(nameof(ProductVariant), request.ProductVariantId);

        variant.Activate();
        return ProductVariantDto.FromDomain(variant);
    }
}
