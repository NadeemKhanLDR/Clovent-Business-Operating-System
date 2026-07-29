using Clovent.MasterData.Terminals;

namespace Clovent.MasterData.Application.Terminals.Dtos;

/// <summary>Read-model shape for a <see cref="Terminal"/>, safe to cross a process boundary.</summary>
public sealed record TerminalDto(
    Guid TerminalId,
    Guid BranchId,
    string Name,
    string Code,
    string Status,
    DateTimeOffset CreatedAtUtc)
{
    /// <summary>Projects a domain <see cref="Terminal"/> into its DTO.</summary>
    public static TerminalDto FromDomain(Terminal terminal) => new(
        terminal.Id.Value,
        terminal.BranchId.Value,
        terminal.Name.Value,
        terminal.Code.Value,
        terminal.Status.ToString(),
        terminal.CreatedAtUtc);
}
