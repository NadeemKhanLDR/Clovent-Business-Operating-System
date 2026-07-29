using Clovent.Catalog.Application.Brands.Dtos;
using Clovent.Catalog.Brands;
using MediatR;

namespace Clovent.Catalog.Application.Brands.Commands;

/// <summary>Deactivates a brand.</summary>
public sealed record DeactivateBrandCommand(Guid BrandId) : IRequest<BrandDto>;

/// <summary>Handles <see cref="DeactivateBrandCommand"/>.</summary>
public sealed class DeactivateBrandCommandHandler(IBrandRepository repository) : IRequestHandler<DeactivateBrandCommand, BrandDto>
{
    /// <inheritdoc/>
    public async Task<BrandDto> Handle(DeactivateBrandCommand request, CancellationToken cancellationToken)
    {
        var brand = await repository.GetByIdAsync(new BrandId(request.BrandId), cancellationToken)
            ?? throw new NotFoundException(nameof(Brand), request.BrandId);

        brand.Deactivate();
        return BrandDto.FromDomain(brand);
    }
}
