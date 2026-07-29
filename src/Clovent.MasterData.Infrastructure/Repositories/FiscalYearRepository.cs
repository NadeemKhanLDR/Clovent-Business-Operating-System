using Clovent.Identity.Organizations;
using Clovent.MasterData.FiscalYears;
using Clovent.MasterData.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clovent.MasterData.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IFiscalYearRepository"/>.</summary>
public sealed class FiscalYearRepository(MasterDataDbContext dbContext) : IFiscalYearRepository
{
    /// <inheritdoc/>
    public Task<FiscalYear?> GetByIdAsync(FiscalYearId id, CancellationToken cancellationToken = default) =>
        dbContext.FiscalYears.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<FiscalYear>> GetByOrganizationIdAsync(OrganizationId organizationId, CancellationToken cancellationToken = default) =>
        await dbContext.FiscalYears.Where(f => f.OrganizationId == organizationId).ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(FiscalYear fiscalYear, CancellationToken cancellationToken = default) =>
        await dbContext.FiscalYears.AddAsync(fiscalYear, cancellationToken);
}
