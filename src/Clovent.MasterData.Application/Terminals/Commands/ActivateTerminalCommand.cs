using Clovent.MasterData.Application.Terminals.Dtos;
using Clovent.MasterData.Terminals;
using MediatR;

namespace Clovent.MasterData.Application.Terminals.Commands;

/// <summary>Activates a terminal.</summary>
public sealed record ActivateTerminalCommand(Guid TerminalId) : IRequest<TerminalDto>;

/// <summary>Handles <see cref="ActivateTerminalCommand"/>.</summary>
public sealed class ActivateTerminalCommandHandler(ITerminalRepository terminalRepository)
    : IRequestHandler<ActivateTerminalCommand, TerminalDto>
{
    /// <inheritdoc/>
    public async Task<TerminalDto> Handle(ActivateTerminalCommand request, CancellationToken cancellationToken)
    {
        var terminal = await terminalRepository.GetByIdAsync(new TerminalId(request.TerminalId), cancellationToken)
            ?? throw new NotFoundException(nameof(Terminal), request.TerminalId);

        terminal.Activate();

        return TerminalDto.FromDomain(terminal);
    }
}
