using Clovent.Identity.Branches;
using Clovent.MasterData.Terminals;

namespace Clovent.MasterData.Application.Tests.TestSupport;

internal sealed class FakeTerminalRepository : ITerminalRepository
{
    private readonly Dictionary<TerminalId, Terminal> _terminals = [];

    public void Add(Terminal terminal) => _terminals[terminal.Id] = terminal;

    public Task<Terminal?> GetByIdAsync(TerminalId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_terminals.GetValueOrDefault(id));

    public Task<IReadOnlyCollection<Terminal>> GetByBranchIdAsync(BranchId branchId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Terminal>>([.. _terminals.Values.Where(t => t.BranchId == branchId)]);

    public Task AddAsync(Terminal terminal, CancellationToken cancellationToken = default)
    {
        _terminals[terminal.Id] = terminal;
        return Task.CompletedTask;
    }
}
