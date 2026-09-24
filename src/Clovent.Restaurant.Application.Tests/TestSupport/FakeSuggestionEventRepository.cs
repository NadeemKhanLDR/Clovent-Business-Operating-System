using Clovent.Restaurant.SmartRecommendations;

namespace Clovent.Restaurant.Application.Tests.TestSupport;

internal sealed class FakeSuggestionEventRepository : ISuggestionEventRepository
{
    private readonly List<SuggestionEvent> _events = [];

    public IReadOnlyList<SuggestionEvent> Events => _events;

    public Task AddAsync(SuggestionEvent suggestionEvent, CancellationToken cancellationToken = default)
    {
        _events.Add(suggestionEvent);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<SuggestionEvent>> GetRangeAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<SuggestionEvent>>([.. _events.Where(e => e.OccurredAtUtc >= fromUtc && e.OccurredAtUtc <= toUtc)]);
}
