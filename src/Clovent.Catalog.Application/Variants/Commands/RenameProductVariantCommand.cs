using Clovent.Catalog.Application.Variants.Dtos;
using Clovent.Catalog.Variants;
using Clovent.Catalog.Variants.ValueObjects;
using MediatR;

namespace Clovent.Catalog.Application.Variants.Commands;

/// <summary>Renames an existing product variant.</summary>
public sealed record RenameProductVariantCommand(Guid ProductVariantId, string Name) : IRequest<ProductVariantDto>;

/// <summary>Handles <see cref="RenameProductVariantCommand"/>.</summary>
public sealed class RenameProductVariantCommandHandler(IProductVariantRepository repository)
    : IRequestHandler<RenameProductVariantCommand, ProductVariantDto>
{
    /// <inheritdoc/>
    public async Task<ProductVariantDto> Handle(RenameProductVariantCommand request, CancellationToken cancellationToken)
    {
        var variant = await repository.GetByIdAsync(new ProductVariantId(request.ProductVariantId), cancellationToken)
            ?? throw new NotFoundException(nameof(ProductVariant), request.ProductVariantId);

        variant.Rename(VariantName.Create(request.Name));
        return ProductVariantDto.FromDomain(variant);
    }
}
