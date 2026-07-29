using Clovent.Catalog.Application.Products.Dtos;
using Clovent.Catalog.Brands;
using Clovent.Catalog.Categories;
using Clovent.Catalog.Groups;
using Clovent.Catalog.Products;
using Clovent.Catalog.Products.ValueObjects;
using Clovent.Catalog.Shared.ValueObjects;
using Clovent.Catalog.UnitsOfMeasure;
using MediatR;

namespace Clovent.Catalog.Application.Products.Commands;

/// <summary>Creates a new product.</summary>
public sealed record CreateProductCommand(
    string Name,
    string Sku,
    Guid BaseUnitOfMeasureId,
    decimal TaxRatePercentage = 0,
    bool TaxIsInclusive = false,
    Guid? CategoryId = null,
    Guid? GroupId = null,
    Guid? BrandId = null) : IRequest<ProductDto>;

/// <summary>Handles <see cref="CreateProductCommand"/>.</summary>
public sealed class CreateProductCommandHandler(IProductRepository repository) : IRequestHandler<CreateProductCommand, ProductDto>
{
    /// <inheritdoc/>
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = Product.Create(
            ProductName.Create(request.Name),
            Sku.Create(request.Sku),
            new UnitOfMeasureId(request.BaseUnitOfMeasureId),
            TaxConfiguration.Create(request.TaxRatePercentage, request.TaxIsInclusive),
            request.CategoryId is { } categoryId ? new ProductCategoryId(categoryId) : null,
            request.GroupId is { } groupId ? new ProductGroupId(groupId) : null,
            request.BrandId is { } brandId ? new BrandId(brandId) : null);

        await repository.AddAsync(product, cancellationToken);

        return ProductDto.FromDomain(product);
    }
}
