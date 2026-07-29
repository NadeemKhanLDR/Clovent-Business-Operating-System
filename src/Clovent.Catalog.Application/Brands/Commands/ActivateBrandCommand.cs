using Clovent.Catalog.Application.Brands.Dtos;
using Clovent.Catalog.Brands;
using MediatR;

namespace Clovent.Catalog.Application.Brands.Commands;

/// <summary>Activates a brand.</summary>
public sealed record ActivateBrandCommand(Guid BrandId) : IRequest<BrandDto>;

/// <summary>Handles <see cref="ActivateBrandCommand"/>.</summary>
public sealed class ActivateBrandCommandHandler(IBrandRepository repository) : IRequestHandler<ActivateBrandCommand, BrandDto>
{
    /// <inheritdoc/>
    public async Task<BrandDto> Handle(ActivateBrandCommand request, CancellationToken cancellationToken)
    {
        var brand = await repository.GetByIdAsync(new BrandId(request.BrandId), cancellationToken)
            ?? throw new NotFoundException(nameof(Brand), request.BrandId);

        brand.Activate();
        return BrandDto.FromDomain(brand);
    }
}
