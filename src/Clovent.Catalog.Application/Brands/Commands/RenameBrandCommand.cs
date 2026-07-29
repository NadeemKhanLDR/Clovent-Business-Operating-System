using Clovent.Catalog.Application.Brands.Dtos;
using Clovent.Catalog.Brands;
using Clovent.Catalog.Brands.ValueObjects;
using MediatR;

namespace Clovent.Catalog.Application.Brands.Commands;

/// <summary>Renames an existing brand.</summary>
public sealed record RenameBrandCommand(Guid BrandId, string Name) : IRequest<BrandDto>;

/// <summary>Handles <see cref="RenameBrandCommand"/>.</summary>
public sealed class RenameBrandCommandHandler(IBrandRepository repository) : IRequestHandler<RenameBrandCommand, BrandDto>
{
    /// <inheritdoc/>
    public async Task<BrandDto> Handle(RenameBrandCommand request, CancellationToken cancellationToken)
    {
        var brand = await repository.GetByIdAsync(new BrandId(request.BrandId), cancellationToken)
            ?? throw new NotFoundException(nameof(Brand), request.BrandId);

        brand.Rename(BrandName.Create(request.Name));
        return BrandDto.FromDomain(brand);
    }
}
