using Clovent.Catalog.Application.Brands.Dtos;
using Clovent.Catalog.Brands;
using MediatR;

namespace Clovent.Catalog.Application.Brands.Queries;

/// <summary>Retrieves every brand.</summary>
public sealed record ListBrandsQuery : IRequest<IReadOnlyCollection<BrandDto>>;

/// <summary>Handles <see cref="ListBrandsQuery"/>.</summary>
public sealed class ListBrandsQueryHandler(IBrandRepository repository) : IRequestHandler<ListBrandsQuery, IReadOnlyCollection<BrandDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<BrandDto>> Handle(ListBrandsQuery request, CancellationToken cancellationToken)
    {
        var brands = await repository.GetAllAsync(cancellationToken);
        return [.. brands.Select(BrandDto.FromDomain)];
    }
}
