using Clovent.MasterData.Application.Terminals.Dtos;
using Clovent.MasterData.Terminals;
using MediatR;

namespace Clovent.MasterData.Application.Terminals.Commands;

/// <summary>Deactivates a terminal.</summary>
public sealed record DeactivateTerminalCommand(Guid TerminalId) : IRequest<TerminalDto>;

/// <summary>Handles <see cref="DeactivateTerminalCommand"/>.</summary>
public sealed class DeactivateTerminalCommandHandler(ITerminalRepository terminalRepository)
    : IRequestHandler<DeactivateTerminalCommand, TerminalDto>
{
    /// <inheritdoc/>
    public async Task<TerminalDto> Handle(DeactivateTerminalCommand request, CancellationToken cancellationToken)
    {
        var terminal = await terminalRepository.GetByIdAsync(new TerminalId(request.TerminalId), cancellationToken)
            ?? throw new NotFoundException(nameof(Terminal), request.TerminalId);

        terminal.Deactivate();

        return TerminalDto.FromDomain(terminal);
    }
}
