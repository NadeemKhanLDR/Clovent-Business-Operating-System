using Clovent.MasterData.Application.Terminals.Dtos;
using Clovent.MasterData.Terminals;
using MediatR;

namespace Clovent.MasterData.Application.Terminals.Queries;

/// <summary>Retrieves a single terminal by identity.</summary>
public sealed record GetTerminalByIdQuery(Guid TerminalId) : IRequest<TerminalDto>;

/// <summary>Handles <see cref="GetTerminalByIdQuery"/>.</summary>
public sealed class GetTerminalByIdQueryHandler(ITerminalRepository terminalRepository)
    : IRequestHandler<GetTerminalByIdQuery, TerminalDto>
{
    /// <inheritdoc/>
    public async Task<TerminalDto> Handle(GetTerminalByIdQuery request, CancellationToken cancellationToken)
    {
        var terminal = await terminalRepository.GetByIdAsync(new TerminalId(request.TerminalId), cancellationToken)
            ?? throw new NotFoundException(nameof(Terminal), request.TerminalId);

        return TerminalDto.FromDomain(terminal);
    }
}
