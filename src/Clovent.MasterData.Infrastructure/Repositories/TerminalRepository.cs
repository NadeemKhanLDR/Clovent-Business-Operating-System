using Clovent.Identity.Branches;
using Clovent.MasterData.Infrastructure.Persistence;
using Clovent.MasterData.Terminals;
using Microsoft.EntityFrameworkCore;

namespace Clovent.MasterData.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="ITerminalRepository"/>.</summary>
public sealed class TerminalRepository(MasterDataDbContext dbContext) : ITerminalRepository
{
    /// <inheritdoc/>
    public async Task<Terminal?> GetByIdAsync(TerminalId id, CancellationToken cancellationToken = default) =>
        await dbContext.Terminals.FirstOrDefaultAsync(t => t.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Terminal>> GetByBranchIdAsync(BranchId branchId, CancellationToken cancellationToken = default) =>
        await dbContext.Terminals.Where(t => t.BranchId == branchId).ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task AddAsync(Terminal terminal, CancellationToken cancellationToken = default) =>
        await dbContext.Terminals.AddAsync(terminal, cancellationToken).ConfigureAwait(false);
}
