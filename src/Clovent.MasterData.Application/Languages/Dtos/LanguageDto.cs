using Clovent.MasterData.Languages;

namespace Clovent.MasterData.Application.Languages.Dtos;

/// <summary>Read-model shape for a <see cref="Language"/>, safe to cross a process boundary.</summary>
public sealed record LanguageDto(
    Guid LanguageId,
    string Code,
    string Name,
    string NativeName,
    string Status,
    DateTimeOffset CreatedAtUtc)
{
    /// <summary>Projects a domain <see cref="Language"/> into its DTO.</summary>
    public static LanguageDto FromDomain(Language language) => new(
        language.Id.Value,
        language.Code.Value,
        language.Name,
        language.NativeName,
        language.Status.ToString(),
        language.CreatedAtUtc);
}
