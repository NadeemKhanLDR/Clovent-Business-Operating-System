using Clovent.Catalog.Variants;

namespace Clovent.Restaurant.SmartRecommendations;

/// <summary>
/// Append-only persistence contract for <see cref="SuggestionEvent"/>
/// analytics facts. Events are never updated or deleted by the POS.
/// </summary>
public interface ISuggestionEventRepository
{
    /// <summary>Appends a new event.</summary>
    Task AddAsync(SuggestionEvent suggestionEvent, CancellationToken cancellationToken = default);

    /// <summary>Retrieves events recorded inside the (inclusive) UTC interval.</summary>
    Task<IReadOnlyCollection<SuggestionEvent>> GetRangeAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default);
}
