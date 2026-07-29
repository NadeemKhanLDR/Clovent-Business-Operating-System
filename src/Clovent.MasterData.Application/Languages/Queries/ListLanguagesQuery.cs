using Clovent.MasterData.Application.Languages.Dtos;
using Clovent.MasterData.Languages;
using MediatR;

namespace Clovent.MasterData.Application.Languages.Queries;

/// <summary>Retrieves every language in the catalog.</summary>
public sealed record ListLanguagesQuery : IRequest<IReadOnlyCollection<LanguageDto>>;

/// <summary>Handles <see cref="ListLanguagesQuery"/>.</summary>
public sealed class ListLanguagesQueryHandler(ILanguageRepository languageRepository)
    : IRequestHandler<ListLanguagesQuery, IReadOnlyCollection<LanguageDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<LanguageDto>> Handle(ListLanguagesQuery request, CancellationToken cancellationToken)
    {
        var languages = await languageRepository.GetAllAsync(cancellationToken);
        return [.. languages.Select(LanguageDto.FromDomain)];
    }
}
