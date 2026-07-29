using Clovent.Identity.Branches;
using Clovent.MasterData.Application.Terminals.Dtos;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Terminals;
using Clovent.MasterData.Terminals.ValueObjects;
using MediatR;

namespace Clovent.MasterData.Application.Terminals.Commands;

/// <summary>Creates a new terminal under an existing branch.</summary>
public sealed record CreateTerminalCommand(Guid BranchId, string Name, string Code) : IRequest<TerminalDto>;

/// <summary>Handles <see cref="CreateTerminalCommand"/>.</summary>
public sealed class CreateTerminalCommandHandler(ITerminalRepository terminalRepository)
    : IRequestHandler<CreateTerminalCommand, TerminalDto>
{
    /// <inheritdoc/>
    public async Task<TerminalDto> Handle(CreateTerminalCommand request, CancellationToken cancellationToken)
    {
        var terminal = Terminal.Create(new BranchId(request.BranchId), TerminalName.Create(request.Name), EntityCode.Create(request.Code));

        await terminalRepository.AddAsync(terminal, cancellationToken);

        return TerminalDto.FromDomain(terminal);
    }
}
