using Clovent.Identity.Branches;
using Clovent.Identity.Companies;
using Clovent.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Identity.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IBranchRepository"/>.</summary>
public sealed class BranchRepository(IdentityDbContext dbContext) : IBranchRepository
{
    /// <inheritdoc/>
    public Task<Branch?> GetByIdAsync(BranchId id, CancellationToken cancellationToken = default) =>
        dbContext.Branches.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Branch>> GetByCompanyIdAsync(CompanyId companyId, CancellationToken cancellationToken = default) =>
        await dbContext.Branches.Where(b => b.CompanyId == companyId).ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(Branch branch, CancellationToken cancellationToken = default) =>
        await dbContext.Branches.AddAsync(branch, cancellationToken);
}
