using Clovent.Identity.Application;

namespace Clovent.Identity.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IUnitOfWork"/>, wrapping <see cref="IdentityDbContext"/>'s <c>SaveChangesAsync</c>. Mirrors <c>Clovent.Authentication.Infrastructure.Persistence.UnitOfWork</c>.</summary>
public sealed class UnitOfWork(IdentityDbContext dbContext) : IUnitOfWork
{
    /// <inheritdoc/>
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}
