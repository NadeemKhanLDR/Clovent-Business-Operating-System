using Clovent.Identity.Infrastructure.Persistence;
using Clovent.Identity.Organizations;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Identity.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IOrganizationRepository"/>.</summary>
public sealed class OrganizationRepository(IdentityDbContext dbContext) : IOrganizationRepository
{
    /// <inheritdoc/>
    public Task<Organization?> GetByIdAsync(OrganizationId id, CancellationToken cancellationToken = default) =>
        dbContext.Organizations.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Organization>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Organizations.ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(Organization organization, CancellationToken cancellationToken = default) =>
        await dbContext.Organizations.AddAsync(organization, cancellationToken);
}
