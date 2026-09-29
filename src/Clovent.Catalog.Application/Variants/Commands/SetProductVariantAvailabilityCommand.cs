using Clovent.Catalog.Application.Variants.Dtos;
using Clovent.Catalog.Variants;
using MediatR;

namespace Clovent.Catalog.Application.Variants.Commands;

/// <summary>Updates the availability state of a single product variant (Available vs Sold Out).</summary>
public sealed record SetProductVariantAvailabilityCommand(
    Guid ProductVariantId,
    bool IsAvailable) : IRequest<ProductVariantDto>;

/// <summary>Handles <see cref="SetProductVariantAvailabilityCommand"/>.</summary>
public sealed class SetProductVariantAvailabilityCommandHandler(IProductVariantRepository repository)
    : IRequestHandler<SetProductVariantAvailabilityCommand, ProductVariantDto>
{
    /// <inheritdoc/>
    public async Task<ProductVariantDto> Handle(SetProductVariantAvailabilityCommand request, CancellationToken cancellationToken)
    {
        var variant = await repository.GetByIdAsync(new ProductVariantId(request.ProductVariantId), cancellationToken)
            ?? throw new NotFoundException(nameof(ProductVariant), request.ProductVariantId);

        variant.SetAvailability(request.IsAvailable);
        return ProductVariantDto.FromDomain(variant);
    }
}
