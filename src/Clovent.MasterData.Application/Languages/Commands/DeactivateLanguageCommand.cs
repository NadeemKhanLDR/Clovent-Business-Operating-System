using Clovent.MasterData.Application.Languages.Dtos;
using Clovent.MasterData.Languages;
using MediatR;

namespace Clovent.MasterData.Application.Languages.Commands;

/// <summary>Deactivates a language.</summary>
public sealed record DeactivateLanguageCommand(Guid LanguageId) : IRequest<LanguageDto>;

/// <summary>Handles <see cref="DeactivateLanguageCommand"/>.</summary>
public sealed class DeactivateLanguageCommandHandler(ILanguageRepository languageRepository)
    : IRequestHandler<DeactivateLanguageCommand, LanguageDto>
{
    /// <inheritdoc/>
    public async Task<LanguageDto> Handle(DeactivateLanguageCommand request, CancellationToken cancellationToken)
    {
        var language = await languageRepository.GetByIdAsync(new LanguageId(request.LanguageId), cancellationToken)
            ?? throw new NotFoundException(nameof(Language), request.LanguageId);

        language.Deactivate();

        return LanguageDto.FromDomain(language);
    }
}
