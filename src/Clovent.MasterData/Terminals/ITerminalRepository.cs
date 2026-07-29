using Clovent.Identity.Branches;

namespace Clovent.MasterData.Terminals;

/// <summary>Persistence contract for <see cref="Terminal"/> aggregates.</summary>
public interface ITerminalRepository
{
    /// <summary>Retrieves a terminal by identity, or <see langword="null"/> if none exists.</summary>
    Task<Terminal?> GetByIdAsync(TerminalId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every terminal belonging to a branch.</summary>
    Task<IReadOnlyCollection<Terminal>> GetByBranchIdAsync(BranchId branchId, CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created terminal.</summary>
    Task AddAsync(Terminal terminal, CancellationToken cancellationToken = default);
}
