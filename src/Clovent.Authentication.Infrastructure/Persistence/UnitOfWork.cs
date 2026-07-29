using Clovent.Authentication.Application;

namespace Clovent.Authentication.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IUnitOfWork"/>, wrapping <see cref="AuthenticationDbContext"/>'s <c>SaveChangesAsync</c>.</summary>
public sealed class UnitOfWork(AuthenticationDbContext dbContext) : IUnitOfWork
{
    /// <inheritdoc/>
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}
