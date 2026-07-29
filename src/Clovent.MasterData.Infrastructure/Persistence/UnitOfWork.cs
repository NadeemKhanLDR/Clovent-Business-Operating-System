using Clovent.MasterData.Application;

namespace Clovent.MasterData.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IUnitOfWork"/>, wrapping <see cref="MasterDataDbContext"/>'s <c>SaveChangesAsync</c>. Mirrors <c>Clovent.Identity.Infrastructure.Persistence.UnitOfWork</c>.</summary>
public sealed class UnitOfWork(MasterDataDbContext dbContext) : IUnitOfWork
{
    /// <inheritdoc/>
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}
