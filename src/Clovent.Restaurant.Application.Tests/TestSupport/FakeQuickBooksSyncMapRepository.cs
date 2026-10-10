using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Restaurant.QuickBooks;

namespace Clovent.Restaurant.Application.Tests.TestSupport;

internal sealed class FakeQuickBooksSyncMapRepository : IQuickBooksSyncMapRepository
{
    private readonly Dictionary<Guid, QuickBooksSyncMap> _maps = [];

    public IReadOnlyCollection<QuickBooksSyncMap> AllMaps => _maps.Values;

    public Task AddAsync(QuickBooksSyncMap map, CancellationToken cancellationToken = default)
    {
        // Enforce unique (EntityType, LocalEntityId) constraint like SQL Server index
        var existing = _maps.Values.FirstOrDefault(m =>
            m.LocalEntityId == map.LocalEntityId &&
            string.Equals(m.EntityType, map.EntityType, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            throw new InvalidOperationException($"Duplicate QuickBooksSyncMap for entity {map.EntityType}:{map.LocalEntityId}. Unique constraint violation.");
        }

        _maps[map.Id] = map;
        return Task.CompletedTask;
    }

    public Task<QuickBooksSyncMap?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_maps.GetValueOrDefault(id));
    }

    public Task<QuickBooksSyncMap?> GetByLocalEntityAsync(Guid localEntityId, string entityType, CancellationToken cancellationToken = default)
    {
        var map = _maps.Values.FirstOrDefault(m =>
            m.LocalEntityId == localEntityId &&
            string.Equals(m.EntityType, entityType, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(map);
    }

    public Task<QuickBooksSyncMap?> GetByQuickBooksTxnIdAsync(string qbTxnId, CancellationToken cancellationToken = default)
    {
        var map = _maps.Values.FirstOrDefault(m =>
            string.Equals(m.QuickBooksTxnId, qbTxnId, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(map);
    }

    public Task<IReadOnlyList<QuickBooksSyncMap>> GetRecentAsync(
        int limit = 100,
        QuickBooksSyncStatus? statusFilter = null,
        string? entityTypeFilter = null,
        CancellationToken cancellationToken = default)
    {
        var query = _maps.Values.AsEnumerable();

        if (statusFilter.HasValue)
        {
            query = query.Where(m => m.Status == statusFilter.Value);
        }

        if (!string.IsNullOrWhiteSpace(entityTypeFilter))
        {
            query = query.Where(m => string.Equals(m.EntityType, entityTypeFilter, StringComparison.OrdinalIgnoreCase));
        }

        var results = query
            .OrderByDescending(m => m.CreatedAtUtc)
            .Take(limit)
            .ToList();

        return Task.FromResult<IReadOnlyList<QuickBooksSyncMap>>(results);
    }

    public Task UpdateAsync(QuickBooksSyncMap map, CancellationToken cancellationToken = default)
    {
        _maps[map.Id] = map;
        return Task.CompletedTask;
    }
}
