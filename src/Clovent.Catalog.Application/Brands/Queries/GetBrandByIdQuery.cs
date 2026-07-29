using Clovent.Catalog.Application.Brands.Dtos;
using Clovent.Catalog.Brands;
using MediatR;

namespace Clovent.Catalog.Application.Brands.Queries;

/// <summary>Retrieves a single brand by identity.</summary>
public sealed record GetBrandByIdQuery(Guid BrandId) : IRequest<BrandDto>;

/// <summary>Handles <see cref="GetBrandByIdQuery"/>.</summary>
public sealed class GetBrandByIdQueryHandler(IBrandRepository repository) : IRequestHandler<GetBrandByIdQuery, BrandDto>
{
    /// <inheritdoc/>
    public async Task<BrandDto> Handle(GetBrandByIdQuery request, CancellationToken cancellationToken)
    {
        var brand = await repository.GetByIdAsync(new BrandId(request.BrandId), cancellationToken)
            ?? throw new NotFoundException(nameof(Brand), request.BrandId);

        return BrandDto.FromDomain(brand);
    }
}
