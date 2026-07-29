using Clovent.Identity.Infrastructure.Persistence;
using Clovent.Identity.Roles;
using Clovent.Identity.Roles.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Identity.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IRoleRepository"/>.</summary>
public sealed class RoleRepository(IdentityDbContext dbContext) : IRoleRepository
{
    /// <inheritdoc/>
    public Task<Role?> GetByIdAsync(RoleId id, CancellationToken cancellationToken = default) =>
        dbContext.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    /// <inheritdoc/>
    public Task<Role?> GetByNameAsync(RoleName name, CancellationToken cancellationToken = default) =>
        dbContext.Roles.FirstOrDefaultAsync(r => r.Name == name, cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(Role role, CancellationToken cancellationToken = default) =>
        await dbContext.Roles.AddAsync(role, cancellationToken);
}
