using Clovent.MasterData.Languages;

namespace Clovent.MasterData.Application.Tests.TestSupport;

internal sealed class FakeLanguageRepository : ILanguageRepository
{
    private readonly Dictionary<LanguageId, Language> _languages = [];

    public void Add(Language language) => _languages[language.Id] = language;

    public Task<Language?> GetByIdAsync(LanguageId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_languages.GetValueOrDefault(id));

    public Task<Language?> GetByCodeAsync(LanguageCode code, CancellationToken cancellationToken = default) =>
        Task.FromResult(_languages.Values.FirstOrDefault(l => l.Code == code));

    public Task<IReadOnlyCollection<Language>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Language>>([.. _languages.Values]);

    public Task AddAsync(Language language, CancellationToken cancellationToken = default)
    {
        _languages[language.Id] = language;
        return Task.CompletedTask;
    }
}
