using Clovent.MasterData.Application.Languages.Dtos;
using Clovent.MasterData.Languages;
using MediatR;

namespace Clovent.MasterData.Application.Languages.Commands;

/// <summary>Creates a new language catalog entry.</summary>
public sealed record CreateLanguageCommand(string Code, string Name, string NativeName) : IRequest<LanguageDto>;

/// <summary>Handles <see cref="CreateLanguageCommand"/>.</summary>
public sealed class CreateLanguageCommandHandler(ILanguageRepository languageRepository)
    : IRequestHandler<CreateLanguageCommand, LanguageDto>
{
    /// <inheritdoc/>
    public async Task<LanguageDto> Handle(CreateLanguageCommand request, CancellationToken cancellationToken)
    {
        var language = Language.Create(LanguageCode.Create(request.Code), request.Name, request.NativeName);

        await languageRepository.AddAsync(language, cancellationToken);

        return LanguageDto.FromDomain(language);
    }
}
