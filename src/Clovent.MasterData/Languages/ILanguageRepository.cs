namespace Clovent.MasterData.Languages;

/// <summary>Persistence contract for <see cref="Language"/> aggregates.</summary>
public interface ILanguageRepository
{
    /// <summary>Retrieves a language by identity, or <see langword="null"/> if none exists.</summary>
    Task<Language?> GetByIdAsync(LanguageId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a language by its ISO 639-1 code, or <see langword="null"/> if none exists.</summary>
    Task<Language?> GetByCodeAsync(LanguageCode code, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every language in the catalog.</summary>
    Task<IReadOnlyCollection<Language>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created language.</summary>
    Task AddAsync(Language language, CancellationToken cancellationToken = default);
}
