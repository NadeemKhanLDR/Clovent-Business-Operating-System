using Clovent.MasterData.Application.Languages.Dtos;
using Clovent.MasterData.Languages;
using MediatR;

namespace Clovent.MasterData.Application.Languages.Commands;

/// <summary>Activates a language.</summary>
public sealed record ActivateLanguageCommand(Guid LanguageId) : IRequest<LanguageDto>;

/// <summary>Handles <see cref="ActivateLanguageCommand"/>.</summary>
public sealed class ActivateLanguageCommandHandler(ILanguageRepository languageRepository)
    : IRequestHandler<ActivateLanguageCommand, LanguageDto>
{
    /// <inheritdoc/>
    public async Task<LanguageDto> Handle(ActivateLanguageCommand request, CancellationToken cancellationToken)
    {
        var language = await languageRepository.GetByIdAsync(new LanguageId(request.LanguageId), cancellationToken)
            ?? throw new NotFoundException(nameof(Language), request.LanguageId);

        language.Activate();

        return LanguageDto.FromDomain(language);
    }
}
