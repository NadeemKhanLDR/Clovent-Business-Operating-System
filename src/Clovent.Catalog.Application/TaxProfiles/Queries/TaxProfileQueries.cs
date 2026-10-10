using Clovent.Catalog.Application.TaxProfiles.Dtos;
using Clovent.Catalog.TaxProfiles;
using MediatR;

namespace Clovent.Catalog.Application.TaxProfiles.Queries;

/// <summary>Query to retrieve all configured tax profiles.</summary>
public sealed record ListTaxProfilesQuery : IRequest<IReadOnlyList<TaxProfileDto>>;

/// <summary>Handles <see cref="ListTaxProfilesQuery"/>.</summary>
public sealed class ListTaxProfilesQueryHandler(ITaxProfileRepository repository)
    : IRequestHandler<ListTaxProfilesQuery, IReadOnlyList<TaxProfileDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<TaxProfileDto>> Handle(ListTaxProfilesQuery request, CancellationToken cancellationToken)
    {
        var profiles = await repository.GetAllAsync(cancellationToken);
        return profiles.Select(TaxProfileDto.FromDomain).ToList();
    }
}

/// <summary>Query to retrieve a single tax profile by its ID.</summary>
public sealed record GetTaxProfileByIdQuery(Guid TaxProfileId) : IRequest<TaxProfileDto?>;

/// <summary>Handles <see cref="GetTaxProfileByIdQuery"/>.</summary>
public sealed class GetTaxProfileByIdQueryHandler(ITaxProfileRepository repository)
    : IRequestHandler<GetTaxProfileByIdQuery, TaxProfileDto?>
{
    /// <inheritdoc/>
    public async Task<TaxProfileDto?> Handle(GetTaxProfileByIdQuery request, CancellationToken cancellationToken)
    {
        var profile = await repository.GetByIdAsync(new TaxProfileId(request.TaxProfileId), cancellationToken);
        return profile != null ? TaxProfileDto.FromDomain(profile) : null;
    }
}
