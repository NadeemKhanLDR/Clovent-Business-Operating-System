using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.QuickBooks;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Restaurant.Infrastructure.Repositories;

/// <summary>
/// Entity Framework Core implementation of <see cref="IQuickBooksSyncMapRepository"/>.
/// </summary>
public sealed class QuickBooksSyncMapRepository : IQuickBooksSyncMapRepository
{
    private readonly RestaurantDbContext _dbContext;

    /// <summary>Creates a new instance of <see cref="QuickBooksSyncMapRepository"/>.</summary>
    public QuickBooksSyncMapRepository(RestaurantDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc/>
    public async Task<QuickBooksSyncMap?> GetByLocalEntityAsync(
        Guid localEntityId,
        string entityType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);

        return await _dbContext.QuickBooksSyncMaps
            .FirstOrDefaultAsync(m => m.LocalEntityId == localEntityId && m.EntityType == entityType, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<QuickBooksSyncMap?> GetByQuickBooksTxnIdAsync(
        string qbTxnId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(qbTxnId);

        return await _dbContext.QuickBooksSyncMaps
            .FirstOrDefaultAsync(m => m.QuickBooksTxnId == qbTxnId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<QuickBooksSyncMap?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.QuickBooksSyncMaps
            .FindAsync([id], cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<QuickBooksSyncMap>> GetRecentAsync(
        int limit = 100,
        QuickBooksSyncStatus? statusFilter = null,
        string? entityTypeFilter = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.QuickBooksSyncMaps.AsNoTracking();

        if (statusFilter.HasValue)
        {
            query = query.Where(m => m.Status == statusFilter.Value);
        }

        if (!string.IsNullOrWhiteSpace(entityTypeFilter))
        {
            query = query.Where(m => m.EntityType == entityTypeFilter);
        }

        // SQLite in test environments does not support DateTimeOffset ordering in SQL translation
        if (_dbContext.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            var sqliteItems = await query
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return sqliteItems
                .OrderByDescending(m => m.CreatedAtUtc)
                .Take(Math.Max(1, limit))
                .ToList();
        }

        return await query
            .OrderByDescending(m => m.CreatedAtUtc)
            .Take(Math.Max(1, limit))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task AddAsync(
        QuickBooksSyncMap map,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(map);

        await _dbContext.QuickBooksSyncMaps.AddAsync(map, cancellationToken).ConfigureAwait(false);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(
        QuickBooksSyncMap map,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(map);

        _dbContext.QuickBooksSyncMaps.Update(map);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
