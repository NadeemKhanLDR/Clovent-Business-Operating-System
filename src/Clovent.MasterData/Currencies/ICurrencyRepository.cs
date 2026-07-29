namespace Clovent.MasterData.Currencies;

/// <summary>Persistence contract for <see cref="Currency"/> aggregates.</summary>
public interface ICurrencyRepository
{
    /// <summary>Retrieves a currency by identity, or <see langword="null"/> if none exists.</summary>
    Task<Currency?> GetByIdAsync(CurrencyId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a currency by its ISO 4217 code, or <see langword="null"/> if none exists.</summary>
    Task<Currency?> GetByCodeAsync(CurrencyCode code, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every currency in the catalog.</summary>
    Task<IReadOnlyCollection<Currency>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created currency.</summary>
    Task AddAsync(Currency currency, CancellationToken cancellationToken = default);
}
