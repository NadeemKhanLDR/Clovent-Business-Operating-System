using Clovent.Catalog.Infrastructure.Persistence;
using Clovent.Catalog.TaxProfiles;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Catalog.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="ITaxProfileRepository"/>.</summary>
public sealed class TaxProfileRepository(CatalogDbContext dbContext) : ITaxProfileRepository
{
    /// <inheritdoc/>
    public async Task<TaxProfile?> GetByIdAsync(TaxProfileId id, CancellationToken cancellationToken = default) =>
        await dbContext.TaxProfiles.FirstOrDefaultAsync(p => p.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<TaxProfile?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        await dbContext.TaxProfiles.FirstOrDefaultAsync(p => p.Code == code, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TaxProfile>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.TaxProfiles.ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TaxProfile>> GetEffectiveAsync(DateTimeOffset effectiveAtUtc, CancellationToken cancellationToken = default) =>
        await dbContext.TaxProfiles
            .Where(p => p.IsActive && p.EffectiveFromUtc <= effectiveAtUtc && (p.EffectiveToUtc == null || p.EffectiveToUtc >= effectiveAtUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task AddAsync(TaxProfile taxProfile, CancellationToken cancellationToken = default) =>
        await dbContext.TaxProfiles.AddAsync(taxProfile, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public Task UpdateAsync(TaxProfile taxProfile, CancellationToken cancellationToken = default)
    {
        dbContext.TaxProfiles.Update(taxProfile);
        return Task.CompletedTask;
    }
}
