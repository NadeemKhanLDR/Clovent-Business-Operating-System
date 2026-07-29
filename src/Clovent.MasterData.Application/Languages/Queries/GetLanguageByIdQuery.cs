using Clovent.MasterData.Application.Languages.Dtos;
using Clovent.MasterData.Languages;
using MediatR;

namespace Clovent.MasterData.Application.Languages.Queries;

/// <summary>Retrieves a single language by identity.</summary>
public sealed record GetLanguageByIdQuery(Guid LanguageId) : IRequest<LanguageDto>;

/// <summary>Handles <see cref="GetLanguageByIdQuery"/>.</summary>
public sealed class GetLanguageByIdQueryHandler(ILanguageRepository languageRepository)
    : IRequestHandler<GetLanguageByIdQuery, LanguageDto>
{
    /// <inheritdoc/>
    public async Task<LanguageDto> Handle(GetLanguageByIdQuery request, CancellationToken cancellationToken)
    {
        var language = await languageRepository.GetByIdAsync(new LanguageId(request.LanguageId), cancellationToken)
            ?? throw new NotFoundException(nameof(Language), request.LanguageId);

        return LanguageDto.FromDomain(language);
    }
}
