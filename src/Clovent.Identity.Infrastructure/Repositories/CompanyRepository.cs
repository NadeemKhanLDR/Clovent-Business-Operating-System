using Clovent.Identity.Companies;
using Clovent.Identity.Infrastructure.Persistence;
using Clovent.Identity.Organizations;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Identity.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="ICompanyRepository"/>.</summary>
public sealed class CompanyRepository(IdentityDbContext dbContext) : ICompanyRepository
{
    /// <inheritdoc/>
    public Task<Company?> GetByIdAsync(CompanyId id, CancellationToken cancellationToken = default) =>
        dbContext.Companies.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Company>> GetByOrganizationIdAsync(OrganizationId organizationId, CancellationToken cancellationToken = default) =>
        await dbContext.Companies.Where(c => c.OrganizationId == organizationId).ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(Company company, CancellationToken cancellationToken = default) =>
        await dbContext.Companies.AddAsync(company, cancellationToken);
}
