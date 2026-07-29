using Clovent.Identity.Branches;
using Clovent.MasterData.Application.Terminals.Dtos;
using Clovent.MasterData.Terminals;
using MediatR;

namespace Clovent.MasterData.Application.Terminals.Queries;

/// <summary>Retrieves every terminal belonging to the given branch.</summary>
public sealed record ListTerminalsByBranchQuery(Guid BranchId) : IRequest<IReadOnlyCollection<TerminalDto>>;

/// <summary>Handles <see cref="ListTerminalsByBranchQuery"/>.</summary>
public sealed class ListTerminalsByBranchQueryHandler(ITerminalRepository terminalRepository)
    : IRequestHandler<ListTerminalsByBranchQuery, IReadOnlyCollection<TerminalDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<TerminalDto>> Handle(ListTerminalsByBranchQuery request, CancellationToken cancellationToken)
    {
        var terminals = await terminalRepository.GetByBranchIdAsync(new BranchId(request.BranchId), cancellationToken);
        return [.. terminals.Select(TerminalDto.FromDomain)];
    }
}
