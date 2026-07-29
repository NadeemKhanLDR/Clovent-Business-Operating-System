using Clovent.MasterData.Application.Terminals.Dtos;
using Clovent.MasterData.Terminals;
using Clovent.MasterData.Terminals.ValueObjects;
using MediatR;

namespace Clovent.MasterData.Application.Terminals.Commands;

/// <summary>Renames an existing terminal.</summary>
public sealed record RenameTerminalCommand(Guid TerminalId, string Name) : IRequest<TerminalDto>;

/// <summary>Handles <see cref="RenameTerminalCommand"/>.</summary>
public sealed class RenameTerminalCommandHandler(ITerminalRepository terminalRepository)
    : IRequestHandler<RenameTerminalCommand, TerminalDto>
{
    /// <inheritdoc/>
    public async Task<TerminalDto> Handle(RenameTerminalCommand request, CancellationToken cancellationToken)
    {
        var terminal = await terminalRepository.GetByIdAsync(new TerminalId(request.TerminalId), cancellationToken)
            ?? throw new NotFoundException(nameof(Terminal), request.TerminalId);

        terminal.Rename(TerminalName.Create(request.Name));

        return TerminalDto.FromDomain(terminal);
    }
}
