using Clovent.Catalog.Application.Variants.Dtos;
using Clovent.Catalog.UnitsOfMeasure;
using Clovent.Catalog.Variants;
using MediatR;

namespace Clovent.Catalog.Application.Variants.Commands;

/// <summary>Changes a product variant's unit of measure.</summary>
public sealed record SetProductVariantUnitOfMeasureCommand(Guid ProductVariantId, Guid UnitOfMeasureId) : IRequest<ProductVariantDto>;

/// <summary>Handles <see cref="SetProductVariantUnitOfMeasureCommand"/>.</summary>
public sealed class SetProductVariantUnitOfMeasureCommandHandler(IProductVariantRepository repository)
    : IRequestHandler<SetProductVariantUnitOfMeasureCommand, ProductVariantDto>
{
    /// <inheritdoc/>
    public async Task<ProductVariantDto> Handle(SetProductVariantUnitOfMeasureCommand request, CancellationToken cancellationToken)
    {
        var variant = await repository.GetByIdAsync(new ProductVariantId(request.ProductVariantId), cancellationToken)
            ?? throw new NotFoundException(nameof(ProductVariant), request.ProductVariantId);

        variant.SetUnitOfMeasure(new UnitOfMeasureId(request.UnitOfMeasureId));
        return ProductVariantDto.FromDomain(variant);
    }
}
