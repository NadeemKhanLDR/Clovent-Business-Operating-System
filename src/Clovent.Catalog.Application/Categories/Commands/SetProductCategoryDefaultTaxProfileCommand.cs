using Clovent.Catalog.Application.Categories.Dtos;
using Clovent.Catalog.Categories;
using Clovent.Catalog.TaxProfiles;
using Clovent.Domain;
using MediatR;

namespace Clovent.Catalog.Application.Categories.Commands;

/// <summary>Updates the default tax profile inherited by products in a category.</summary>
public sealed record SetProductCategoryDefaultTaxProfileCommand(Guid ProductCategoryId, Guid? DefaultTaxProfileId) : IRequest<ProductCategoryDto>;

/// <summary>Handles <see cref="SetProductCategoryDefaultTaxProfileCommand"/>.</summary>
public sealed class SetProductCategoryDefaultTaxProfileCommandHandler(IProductCategoryRepository repository)
    : IRequestHandler<SetProductCategoryDefaultTaxProfileCommand, ProductCategoryDto>
{
    /// <inheritdoc/>
    public async Task<ProductCategoryDto> Handle(SetProductCategoryDefaultTaxProfileCommand request, CancellationToken cancellationToken)
    {
        var categoryId = new ProductCategoryId(request.ProductCategoryId);
        var category = await repository.GetByIdAsync(categoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(ProductCategory), request.ProductCategoryId);

        var taxProfileId = request.DefaultTaxProfileId.HasValue
            ? new TaxProfileId(request.DefaultTaxProfileId.Value)
            : (TaxProfileId?)null;

        category.SetDefaultTaxProfile(taxProfileId);

        return ProductCategoryDto.FromDomain(category);
    }
}
