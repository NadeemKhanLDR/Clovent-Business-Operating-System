namespace Clovent.Catalog.TaxProfiles;

/// <summary>Repository for persisting and querying <see cref="TaxProfile"/> aggregates.</summary>
public interface ITaxProfileRepository
{
    /// <summary>Gets a tax profile by its unique ID.</summary>
    Task<TaxProfile?> GetByIdAsync(TaxProfileId id, CancellationToken cancellationToken = default);

    /// <summary>Gets a tax profile by its statutory code.</summary>
    Task<TaxProfile?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>Gets all tax profiles.</summary>
    Task<IReadOnlyList<TaxProfile>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets all active tax profiles effective at a given UTC instant.</summary>
    Task<IReadOnlyList<TaxProfile>> GetEffectiveAsync(DateTimeOffset effectiveAtUtc, CancellationToken cancellationToken = default);

    /// <summary>Adds a new tax profile.</summary>
    Task AddAsync(TaxProfile taxProfile, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing tax profile.</summary>
    Task UpdateAsync(TaxProfile taxProfile, CancellationToken cancellationToken = default);
}
