using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Catalog.TaxProfiles;

namespace Clovent.Catalog.Application.Tests.TestSupport;

public sealed class FakeTaxProfileRepository : ITaxProfileRepository
{
    private readonly List<TaxProfile> _profiles = [];

    public Task<TaxProfile?> GetByIdAsync(TaxProfileId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_profiles.FirstOrDefault(p => p.Id == id));

    public Task<TaxProfile?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        Task.FromResult(_profiles.FirstOrDefault(p => string.Equals(p.Code, code, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<TaxProfile>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TaxProfile>>(_profiles.ToList());

    public Task<IReadOnlyList<TaxProfile>> GetEffectiveAsync(DateTimeOffset effectiveAtUtc, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TaxProfile>>(
            _profiles.Where(p => p.IsActive && p.EffectiveFromUtc <= effectiveAtUtc && (p.EffectiveToUtc == null || p.EffectiveToUtc >= effectiveAtUtc)).ToList());

    public Task AddAsync(TaxProfile taxProfile, CancellationToken cancellationToken = default)
    {
        _profiles.Add(taxProfile);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(TaxProfile taxProfile, CancellationToken cancellationToken = default)
    {
        var idx = _profiles.FindIndex(p => p.Id == taxProfile.Id);
        if (idx >= 0) _profiles[idx] = taxProfile;
        return Task.CompletedTask;
    }
}
