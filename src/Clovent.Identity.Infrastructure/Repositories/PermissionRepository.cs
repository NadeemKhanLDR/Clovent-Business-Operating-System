using Clovent.Identity.Infrastructure.Persistence;
using Clovent.Identity.Permissions;
using Clovent.Identity.Permissions.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Identity.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IPermissionRepository"/>.</summary>
public sealed class PermissionRepository(IdentityDbContext dbContext) : IPermissionRepository
{
    /// <inheritdoc/>
    public Task<Permission?> GetByIdAsync(PermissionId id, CancellationToken cancellationToken = default) =>
        dbContext.Permissions.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    /// <inheritdoc/>
    public Task<Permission?> GetByCodeAsync(PermissionCode code, CancellationToken cancellationToken = default) =>
        dbContext.Permissions.FirstOrDefaultAsync(p => p.Code == code, cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(Permission permission, CancellationToken cancellationToken = default) =>
        await dbContext.Permissions.AddAsync(permission, cancellationToken);
}
