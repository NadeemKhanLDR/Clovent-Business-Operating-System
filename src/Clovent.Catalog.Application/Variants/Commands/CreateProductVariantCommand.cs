using Clovent.Catalog.Application.Variants.Dtos;
using Clovent.Catalog.Products;
using Clovent.Catalog.Shared.ValueObjects;
using Clovent.Catalog.UnitsOfMeasure;
using Clovent.Catalog.Variants;
using Clovent.Catalog.Variants.ValueObjects;
using MediatR;

namespace Clovent.Catalog.Application.Variants.Commands;

/// <summary>Creates a new variant under an existing product.</summary>
public sealed record CreateProductVariantCommand(Guid ProductId, string Name, string Sku, Guid UnitOfMeasureId) : IRequest<ProductVariantDto>;

/// <summary>Handles <see cref="CreateProductVariantCommand"/>.</summary>
public sealed class CreateProductVariantCommandHandler(IProductVariantRepository repository)
    : IRequestHandler<CreateProductVariantCommand, ProductVariantDto>
{
    /// <inheritdoc/>
    public async Task<ProductVariantDto> Handle(CreateProductVariantCommand request, CancellationToken cancellationToken)
    {
        var variant = ProductVariant.Create(
            new ProductId(request.ProductId),
            VariantName.Create(request.Name),
            Sku.Create(request.Sku),
            new UnitOfMeasureId(request.UnitOfMeasureId));

        await repository.AddAsync(variant, cancellationToken);

        return ProductVariantDto.FromDomain(variant);
    }
}
