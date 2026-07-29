using Clovent.Catalog.Application.Brands.Dtos;
using Clovent.Catalog.Brands;
using Clovent.Catalog.Brands.ValueObjects;
using MediatR;

namespace Clovent.Catalog.Application.Brands.Commands;

/// <summary>Creates a new brand.</summary>
public sealed record CreateBrandCommand(string Name) : IRequest<BrandDto>;

/// <summary>Handles <see cref="CreateBrandCommand"/>.</summary>
public sealed class CreateBrandCommandHandler(IBrandRepository repository) : IRequestHandler<CreateBrandCommand, BrandDto>
{
    /// <inheritdoc/>
    public async Task<BrandDto> Handle(CreateBrandCommand request, CancellationToken cancellationToken)
    {
        var brand = Brand.Create(BrandName.Create(request.Name));
        await repository.AddAsync(brand, cancellationToken);
        return BrandDto.FromDomain(brand);
    }
}
